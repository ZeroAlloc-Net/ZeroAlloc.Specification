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
        // Pipeline: find structs with [Specification] attribute
        var specs = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "ZeroAlloc.Specification.SpecificationAttribute",
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, _) => GetSpecificationInfo(ctx))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!);

        context.RegisterSourceOutput(specs, static (ctx, info) => Execute(ctx, info));
    }

    private static SpecificationInfo? GetSpecificationInfo(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol structSymbol)
            return null;

        // Find ISpecification<T> implementation
        var specInterface = structSymbol.AllInterfaces
            .FirstOrDefault(i =>
                i.Name == "ISpecification" &&
                i.TypeArguments.Length == 1 &&
                i.ContainingNamespace.ToDisplayString() == "ZeroAlloc.Specification");

        if (specInterface is null)
            return null;

        var candidateType = specInterface.TypeArguments[0];
        var isStateless = !structSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Any(f => !f.IsStatic);

        return new SpecificationInfo(
            structSymbol.Name,
            structSymbol.ContainingNamespace.ToDisplayString(),
            candidateType.ToDisplayString(),
            isStateless,
            structSymbol.IsReadOnly,
            structSymbol.IsPartialDefinition());
    }

    private static void Execute(SourceProductionContext ctx, SpecificationInfo info)
    {
        if (!info.IsPartial)
            return; // Diagnostics added in Task 8

        var source = GenerateSource(info);
        ctx.AddSource($"{info.TypeName}.g.cs", SourceText.From(source, Encoding.UTF8));
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

    public SpecificationInfo(
        string typeName,
        string @namespace,
        string candidateType,
        bool isStateless,
        bool isReadOnly,
        bool isPartial)
    {
        TypeName = typeName;
        Namespace = @namespace;
        CandidateType = candidateType;
        IsStateless = isStateless;
        IsReadOnly = isReadOnly;
        IsPartial = isPartial;
    }

    public override bool Equals(object? obj) =>
        obj is SpecificationInfo other &&
        TypeName == other.TypeName &&
        Namespace == other.Namespace &&
        CandidateType == other.CandidateType &&
        IsStateless == other.IsStateless &&
        IsReadOnly == other.IsReadOnly &&
        IsPartial == other.IsPartial;

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
            return hash;
        }
    }
}
