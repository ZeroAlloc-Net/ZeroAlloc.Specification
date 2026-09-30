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

            if (info.NonPartialContainingType is not null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ContainingTypeNotPartial, info.Location, info.FullName, info.NonPartialContainingType));
                return;
            }

            if (!info.IsReadOnly)
                ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.NotReadonly, info.Location, info.TypeName));

            var source = GenerateSource(info);
            ctx.AddSource(info.HintName, SourceText.From(source, Encoding.UTF8));
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

        var accessibility = structSymbol.DeclaredAccessibility switch
        {
            Accessibility.Internal => "internal",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => "public"
        };

        return new SpecificationInfo(
            structSymbol.Name,
            HintNames.ForSpecification(structSymbol),
            structSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : structSymbol.ContainingNamespace.ToDisplayString(),
            candidateType,
            structSymbol.IsReadOnly,
            structSymbol.IsPartialDefinition(),
            hasInterface,
            location,
            accessibility,
            structSymbol.ToDisplayString(),
            TypeDeclarations.Reference(structSymbol),
            TypeDeclarations.ContainingTypeHeaders(structSymbol),
            TypeDeclarations.FirstNonPartialContainingType(structSymbol)?.ToDisplayString());
    }

    // The spec is emitted inside its namespace, when it has one, and inside a partial declaration
    // of every containing type, outermost first, so the members land on the real type (#79).
    // Output for a spec at the top of a namespace is the same as before these were supported,
    // except that lines now always end in LF; the raw string used to take the source file's.
    private static string GenerateSource(SpecificationInfo info)
    {
        var type = info.TypeReference;
        var t = info.CandidateType;

        var sb = new StringBuilder();
        sb.Append("// <auto-generated/>\n");
        sb.Append("#nullable enable\n");
        sb.Append('\n');

        var indent = "";
        var closers = 0;
        if (info.Namespace is not null)
        {
            sb.Append("namespace ").Append(info.Namespace).Append('\n');
            sb.Append("{\n");
            indent = "    ";
            closers++;
        }

        foreach (var header in info.ContainingTypeHeaders)
        {
            sb.Append(indent).Append(header).Append('\n');
            sb.Append(indent).Append("{\n");
            indent += "    ";
            closers++;
        }

        var body = $$"""
            {{info.Accessibility}} partial struct {{type}}
            {
                public global::ZeroAlloc.Specification.AndSpecification<{{type}}, TOther, {{t}}> And<TOther>(TOther other)
                    where TOther : struct, global::ZeroAlloc.Specification.ISpecification<{{t}}> => new(this, other);

                public global::ZeroAlloc.Specification.OrSpecification<{{type}}, TOther, {{t}}> Or<TOther>(TOther other)
                    where TOther : struct, global::ZeroAlloc.Specification.ISpecification<{{t}}> => new(this, other);

                public global::ZeroAlloc.Specification.NotSpecification<{{type}}, {{t}}> Not() => new(this);

                public static implicit operator global::System.Linq.Expressions.Expression<global::System.Func<{{t}}, bool>>({{type}} spec)
                    => spec.ToExpression();
            }
            """;
        foreach (var line in body.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.Length > 0) sb.Append(indent).Append(line);
            sb.Append('\n');
        }

        for (var i = closers; i > 0; i--)
        {
            indent = indent.Substring(0, indent.Length - 4);
            sb.Append(indent).Append("}\n");
        }

        // The file has always ended at the last brace, without a newline.
        sb.Length--;
        return sb.ToString();
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
    // The generated file's name, unique within the compilation; see HintNames.ForSpecification.
    public string HintName { get; }
    public string? Namespace { get; }
    public string CandidateType { get; }
    public bool IsReadOnly { get; }
    public bool IsPartial { get; }
    public bool HasInterface { get; }
    public Location Location { get; }
    public string Accessibility { get; }
    // The struct's display name, for diagnostics.
    public string FullName { get; }
    // How the struct is written in generated code: its escaped name and type parameters.
    public string TypeReference { get; }
    // Partial declarations of the containing types, outermost first; empty at namespace level.
    public EquatableArray<string> ContainingTypeHeaders { get; }
    // The outermost containing type that is not partial, or null when there is none (ZA005).
    public string? NonPartialContainingType { get; }

    public SpecificationInfo(
        string typeName,
        string hintName,
        string? @namespace,
        string candidateType,
        bool isReadOnly,
        bool isPartial,
        bool hasInterface,
        Location location,
        string accessibility,
        string fullName,
        string typeReference,
        EquatableArray<string> containingTypeHeaders,
        string? nonPartialContainingType)
    {
        TypeName = typeName;
        HintName = hintName;
        Namespace = @namespace;
        CandidateType = candidateType;
        IsReadOnly = isReadOnly;
        IsPartial = isPartial;
        HasInterface = hasInterface;
        Location = location;
        Accessibility = accessibility;
        FullName = fullName;
        TypeReference = typeReference;
        ContainingTypeHeaders = containingTypeHeaders;
        NonPartialContainingType = nonPartialContainingType;
    }

    // NOTE: Location is intentionally excluded from equality — it does not implement value equality
    // and including it would break incremental generator caching (causing re-runs on every keystroke).
    // Location is available on the instance for diagnostic reporting only.
    public override bool Equals(object? obj) =>
        obj is SpecificationInfo other &&
        TypeName == other.TypeName &&
        HintName == other.HintName &&
        Namespace == other.Namespace &&
        CandidateType == other.CandidateType &&
        IsReadOnly == other.IsReadOnly &&
        IsPartial == other.IsPartial &&
        HasInterface == other.HasInterface &&
        Accessibility == other.Accessibility &&
        FullName == other.FullName &&
        TypeReference == other.TypeReference &&
        ContainingTypeHeaders.Equals(other.ContainingTypeHeaders) &&
        NonPartialContainingType == other.NonPartialContainingType;

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = TypeName?.GetHashCode() ?? 0;
            hash = (hash * 397) ^ (HintName?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (Namespace?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (CandidateType?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ IsReadOnly.GetHashCode();
            hash = (hash * 397) ^ IsPartial.GetHashCode();
            hash = (hash * 397) ^ HasInterface.GetHashCode();
            hash = (hash * 397) ^ (Accessibility?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (FullName?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (TypeReference?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ ContainingTypeHeaders.GetHashCode();
            hash = (hash * 397) ^ (NonPartialContainingType?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
