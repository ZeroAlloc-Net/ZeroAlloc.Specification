using System;
using System.Linq;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using ZeroAlloc.Specification.Generator;

namespace ZeroAlloc.Specification.Tests.Generator;

/// <summary>
/// A generated file is named after its specification's namespace and containing types, so two
/// specifications with the same simple name never produce the same hint name. A duplicate hint
/// name made the generator throw CS8785, and then no specification in the project was generated.
/// </summary>
public class HintNameTests
{
    private const string Usings = """
        using System;
        using System.Linq.Expressions;
        using ZeroAlloc.Specification;

        """;

    private static string Spec(string name) => $$"""
        [Specification]
        public readonly partial struct {{name}} : ISpecification<int>
        {
            public bool IsSatisfiedBy(int x) => x > 0;
            public Expression<Func<int, bool>> ToExpression() => x => x > 0;
        }
        """;

    private static (string[] HintNames, Diagnostic[] Diagnostics) Run(string source)
    {
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ISpecification<>).Assembly.Location),
            MetadataReference.CreateFromFile(
                System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!,
                    "System.Runtime.dll")),
        };
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(Usings + source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        CSharpGeneratorDriver.Create(new SpecificationGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);

        var hintNames = output.SyntaxTrees
            .Skip(1)
            .Select(t => System.IO.Path.GetFileName(t.FilePath))
            .OrderBy(h => h, StringComparer.Ordinal)
            .ToArray();
        var diagnostics = generatorDiagnostics
            .Concat(output.GetDiagnostics())
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .ToArray();
        return (hintNames, diagnostics);
    }

    [Fact]
    public void SameNamedSpecifications_InDifferentNamespaces_AreBothGenerated()
    {
        var (hintNames, diagnostics) = Run($$"""
            namespace App.Orders { {{Spec("IsActive")}} }
            namespace App.Customers { {{Spec("IsActive")}} }
            """);

        diagnostics.Should().BeEmpty();
        hintNames.Should().Equal("App.Customers.IsActive.g.cs", "App.Orders.IsActive.g.cs");
    }

    // The generator emits a nested or generic specification at namespace level without its
    // containing types or type parameters, so that output does not compile yet (#79). These
    // tests pin only the hint names: each specification gets its own file and none is dropped.
    [Fact]
    public void SameNamedSpecifications_InDifferentContainingTypes_GetUniqueHintNames()
    {
        var (hintNames, diagnostics) = Run($$"""
            namespace App
            {
                public static partial class Orders { {{Spec("IsActive")}} }
                public static partial class Customers { {{Spec("IsActive")}} }
            }
            """);

        diagnostics.Should().NotContain(d => d.Id == "CS8785");
        hintNames.Should().Equal("App.Customers+IsActive.g.cs", "App.Orders+IsActive.g.cs");
    }

    [Fact]
    public void SameNamedSpecifications_OfDifferentArity_GetUniqueHintNames()
    {
        var (hintNames, diagnostics) = Run($$"""
            namespace App
            {
                {{Spec("IsActive")}}
                {{Spec("IsActive<T>")}}
                public partial class Outer { {{Spec("IsActive")}} }
                public partial class Outer<T> { {{Spec("IsActive")}} }
            }
            """);

        diagnostics.Should().NotContain(d => d.Id == "CS8785");
        hintNames.Should().Equal(
            "App.IsActive.g.cs",
            "App.IsActive`1.g.cs",
            "App.Outer+IsActive.g.cs",
            "App.Outer`1+IsActive.g.cs");
    }

    [Theory]
    [InlineData("M", "M")]
    [InlineData("App.M", "App.M")]
    [InlineData("App.Outer+M`1", "App.Outer+M`1")]
    [InlineData("Café.Ωmega_1", "Café.Ωmega_1")]
    [InlineData("a/b|c:d*e?f<g>h", "a-u002Fb-u007Cc-u003Ad-u002Ae-u003Ff-u003Cg-u003Eh")]
    [InlineData("a-b", "a-u002Db")]
    public void Sanitize_KeepsIdentifierCharactersAndEscapesTheRest(string name, string expected)
    {
        HintNames.Sanitize(name).Should().Be(expected);
    }
}
