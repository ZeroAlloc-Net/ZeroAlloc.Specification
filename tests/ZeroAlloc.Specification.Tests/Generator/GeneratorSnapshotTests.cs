using System;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using ZeroAlloc.Specification.Generator;

namespace ZeroAlloc.Specification.Tests.Generator;

public class GeneratorSnapshotTests
{
    private static Compilation CreateCompilation(string source)
    {
        // Reference core library types explicitly — more reliable than AppDomain.GetAssemblies()
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ZeroAlloc.Specification.ISpecification<>).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [Fact]
    public void Generator_EmitsAndOrNotMethods_ForValidPartialStruct()
    {
        var source = """
            using System;
            using System.Linq.Expressions;
            using ZeroAlloc.Specification;

            namespace MyApp
            {
                [Specification]
                public readonly partial struct ActiveSpec : ISpecification<int>
                {
                    public bool IsSatisfiedBy(int x) => x > 0;
                    public Expression<Func<int, bool>> ToExpression() => x => x > 0;
                }
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new SpecificationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);
        var result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

        result.GeneratedTrees.Should().HaveCount(1);

        var specSource = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ActiveSpec"))
            .GetText().ToString();

        specSource.Should().Contain("AndSpecification<ActiveSpec");
        specSource.Should().Contain("OrSpecification<ActiveSpec");
        specSource.Should().Contain("NotSpecification<ActiveSpec");
    }

    [Fact]
    public void Generator_EmitsError_WhenInterfaceMissing()
    {
        var source = """
            using ZeroAlloc.Specification;

            namespace MyApp
            {
                [Specification]
                public readonly partial struct BadSpec { }
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new SpecificationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);
        var result = driver.GetRunResult();

        result.Diagnostics.Should().Contain(d =>
            d.Id == "ZA002" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Generator_EmitsError_WhenNotPartial()
    {
        var source = """
            using System;
            using System.Linq.Expressions;
            using ZeroAlloc.Specification;

            namespace MyApp
            {
                [Specification]
                public readonly struct NonPartialSpec : ISpecification<int>
                {
                    public bool IsSatisfiedBy(int x) => x > 0;
                    public Expression<Func<int, bool>> ToExpression() => x => x > 0;
                }
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new SpecificationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);
        var result = driver.GetRunResult();

        result.Diagnostics.Should().Contain(d =>
            d.Id == "ZA003" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Generator_EmitsWarning_WhenNotReadonly()
    {
        var source = """
            using System;
            using System.Linq.Expressions;
            using ZeroAlloc.Specification;

            namespace MyApp
            {
                [Specification]
                public partial struct MutableSpec : ISpecification<int>
                {
                    public bool IsSatisfiedBy(int x) => x > 0;
                    public Expression<Func<int, bool>> ToExpression() => x => x > 0;
                }
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new SpecificationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);
        var result = driver.GetRunResult();

        result.Diagnostics.Should().Contain(d =>
            d.Id == "ZA004" && d.Severity == DiagnosticSeverity.Warning);
        // Source should still be generated despite the warning
        result.GeneratedTrees
            .Should().Contain(t => t.FilePath.Contains("MutableSpec"));
    }

    [Fact]
    public void Generator_EmitsError_WhenAppliedToClass()
    {
        var source = """
            using ZeroAlloc.Specification;

            namespace MyApp
            {
                [Specification]
                public class NotAStruct { }
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new SpecificationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);
        var result = driver.GetRunResult();

        result.Diagnostics.Should().Contain(d =>
            d.Id == "ZA001" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Generator_EmitsImplicitConversionOperator()
    {
        var source = """
            using ZeroAlloc.Specification;
            using System.Linq.Expressions;
            using System;

            namespace MyApp
            {
                [Specification]
                public readonly partial struct MySpec : ISpecification<int>
                {
                    public bool IsSatisfiedBy(int x) => x > 0;
                    public Expression<Func<int, bool>> ToExpression() => x => x > 0;
                }
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new SpecificationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);
        var result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

        result.GeneratedTrees.Should().HaveCount(1);

        var specSource = result.GeneratedTrees
            .First(t => t.FilePath.Contains("MySpec"))
            .GetText().ToString();

        specSource.Should().Contain("implicit operator");
        specSource.Should().Contain("Expression<");
    }
}
