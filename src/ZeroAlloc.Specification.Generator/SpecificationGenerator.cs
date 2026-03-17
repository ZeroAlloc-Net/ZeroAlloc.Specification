using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;

namespace ZeroAlloc.Specification.Generator;

[Generator]
public sealed class SpecificationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ZA001: Catch non-struct types decorated with [Specification]
        var nonStructs = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "ZeroAlloc.Specification.SpecificationAttribute",
                predicate: static (node, _) => node is not StructDeclarationSyntax,
                transform: static (ctx, _) => (
                    Name: (ctx.TargetSymbol as INamedTypeSymbol)?.Name ?? "unknown",
                    Location: ctx.TargetNode.GetLocation()))
            .Where(static x => x.Name != null);

        context.RegisterSourceOutput(nonStructs, static (ctx, data) =>
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.NotAStruct, data.Location, data.Name)));

        var specs = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "ZeroAlloc.Specification.SpecificationAttribute",
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, _) => GetSpecificationData(ctx))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!);

        context.RegisterSourceOutput(specs, static (ctx, info) =>
        {
            if (!info.HasInterface)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingInterface, info.Location, info.TypeName));
                return;
            }

            if (!info.IsPartial)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.NotPartial, info.Location, info.TypeName));
                return;
            }

            if (!info.IsReadOnly)
                ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.NotReadonly, info.Location, info.TypeName));

            var source = GenerateSource(info);
            ctx.AddSource($"{info.TypeName}.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static SpecificationInfo? GetSpecificationData(GeneratorAttributeSyntaxContext ctx)
    {
        var location = ctx.TargetNode.GetLocation();

        if (ctx.TargetSymbol is not INamedTypeSymbol structSymbol)
            return null;

        var specInterface = structSymbol.AllInterfaces
            .FirstOrDefault(i =>
                i.Name == "ISpecification" &&
                i.TypeArguments.Length == 1 &&
                i.ContainingNamespace.ToDisplayString() == "ZeroAlloc.Specification");

        var hasInterface = specInterface is not null;
        var candidateType = hasInterface
            ? specInterface!.TypeArguments[0].ToDisplayString()
            : "object";

        var isStateless = !structSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Any(f => !f.IsStatic);

        return new SpecificationInfo(
            structSymbol.Name,
            structSymbol.ContainingNamespace.ToDisplayString(),
            candidateType,
            isStateless,
            structSymbol.IsReadOnly,
            structSymbol.IsPartialDefinition(),
            hasInterface,
            location);
    }

    private static string GenerateSource(SpecificationInfo info)
    {
        var ns = info.Namespace;
        var type = info.TypeName;
        var t = info.CandidateType;

        return $$"""
            using ZeroAlloc.Specification;

            namespace {{ns}}
            {
                public partial struct {{type}}
                {
                    public AndSpecification<{{type}}, TOther, {{t}}> And<TOther>(TOther other)
                        where TOther : struct, ISpecification<{{t}}> => new(this, other);

                    public OrSpecification<{{type}}, TOther, {{t}}> Or<TOther>(TOther other)
                        where TOther : struct, ISpecification<{{t}}> => new(this, other);

                    public NotSpecification<{{type}}, {{t}}> Not() => new(this);
                }
            }
            """;
    }
}

internal static class SymbolExtensions
{
    public static bool IsPartialDefinition(this INamedTypeSymbol symbol) =>
        symbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<StructDeclarationSyntax>()
            .Any(s => s.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)));
}

internal sealed class SpecificationInfo
{
    public string TypeName { get; }
    public string Namespace { get; }
    public string CandidateType { get; }
    public bool IsStateless { get; }
    public bool IsReadOnly { get; }
    public bool IsPartial { get; }
    public bool HasInterface { get; }
    public Location Location { get; }

    public SpecificationInfo(
        string typeName,
        string @namespace,
        string candidateType,
        bool isStateless,
        bool isReadOnly,
        bool isPartial,
        bool hasInterface,
        Location location)
    {
        TypeName = typeName;
        Namespace = @namespace;
        CandidateType = candidateType;
        IsStateless = isStateless;
        IsReadOnly = isReadOnly;
        IsPartial = isPartial;
        HasInterface = hasInterface;
        Location = location;
    }

    public override bool Equals(object? obj) =>
        obj is SpecificationInfo other &&
        TypeName == other.TypeName &&
        Namespace == other.Namespace &&
        CandidateType == other.CandidateType &&
        IsStateless == other.IsStateless &&
        IsReadOnly == other.IsReadOnly &&
        IsPartial == other.IsPartial &&
        HasInterface == other.HasInterface;

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = TypeName?.GetHashCode() ?? 0;
            hash = (hash * 397) ^ (Namespace?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (CandidateType?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ IsStateless.GetHashCode();
            hash = (hash * 397) ^ IsReadOnly.GetHashCode();
            hash = (hash * 397) ^ IsPartial.GetHashCode();
            hash = (hash * 397) ^ HasInterface.GetHashCode();
            return hash;
        }
    }
}
