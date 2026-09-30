using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZeroAlloc.Specification.Generator;

/// <summary>
/// The partial declarations a specification's generated members are emitted into: those of its
/// containing types, outermost first, then the specification's own. The same approach as
/// ZeroAlloc.Mapping's <c>HostDeclarations</c>.
/// </summary>
internal static class TypeDeclarations
{
    /// <summary>
    /// The outermost containing type of <paramref name="type"/> that is not declared
    /// <c>partial</c>, or null when all of them are. The generated code has to reopen every
    /// containing type, which only a partial type allows.
    /// </summary>
    public static INamedTypeSymbol? FirstNonPartialContainingType(INamedTypeSymbol type)
    {
        INamedTypeSymbol? outermost = null;
        for (var t = type.ContainingType; t is not null; t = t.ContainingType)
        {
            if (!IsPartial(t)) outermost = t;
        }
        return outermost;
    }

    /// <summary>
    /// The partial declaration of each containing type, outermost first, as in
    /// <c>partial class Outer&lt;T&gt;</c>. Empty for a type at the top of a namespace.
    /// </summary>
    public static EquatableArray<string> ContainingTypeHeaders(INamedTypeSymbol type)
    {
        var chain = new List<INamedTypeSymbol>();
        for (var t = type.ContainingType; t is not null; t = t.ContainingType) chain.Add(t);
        chain.Reverse();

        var headers = ImmutableArray.CreateBuilder<string>(chain.Count);
        foreach (var t in chain)
        {
            var sb = new StringBuilder();
            if (t.IsRefLikeType) sb.Append("ref ");
            sb.Append("partial ").Append(Keyword(t)).Append(' ').Append(Reference(t));
            headers.Add(sb.ToString());
        }
        return new EquatableArray<string>(headers.MoveToImmutable());
    }

    /// <summary>
    /// How <paramref name="type"/> is written inside its own declaration: its name, escaped when
    /// it is a keyword, and its type parameter names, as in <c>IsActive&lt;T&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Type parameter names only. A partial part may leave out constraints, and variance never
    /// appears here: an interface with a variant type parameter cannot contain types.
    /// </remarks>
    public static string Reference(INamedTypeSymbol type)
    {
        var sb = new StringBuilder(Identifier(type.Name));
        if (type.TypeParameters.Length == 0) return sb.ToString();
        sb.Append('<');
        for (var i = 0; i < type.TypeParameters.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(Identifier(type.TypeParameters[i].Name));
        }
        return sb.Append('>').ToString();
    }

    private static bool IsPartial(INamedTypeSymbol type) =>
        type.DeclaringSyntaxReferences.Any(static r =>
            r.GetSyntax() is TypeDeclarationSyntax declaration &&
            declaration.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword)));

    private static string Keyword(INamedTypeSymbol type) => type switch
    {
        { IsRecord: true, TypeKind: TypeKind.Struct } => "record struct",
        { IsRecord: true } => "record",
        { TypeKind: TypeKind.Struct } => "struct",
        { TypeKind: TypeKind.Interface } => "interface",
        _ => "class",
    };

    private static string Identifier(string name) =>
        SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
}
