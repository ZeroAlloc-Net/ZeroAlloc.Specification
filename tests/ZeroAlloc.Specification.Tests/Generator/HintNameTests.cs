using System;
using System.Linq;
using AwesomeAssertions;
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
    [Fact]
    public void SameNamedSpecifications_InDifferentNamespaces_AreBothGenerated()
    {
        var (hintNames, _, diagnostics) = GeneratorRunner.Run($$"""
            namespace App.Orders { {{GeneratorRunner.Spec("IsActive")}} }
            namespace App.Customers { {{GeneratorRunner.Spec("IsActive")}} }
            """);

        diagnostics.Should().BeEmpty();
        hintNames.Should().Equal("App.Customers.IsActive.g.cs", "App.Orders.IsActive.g.cs");
    }

    [Fact]
    public void SameNamedSpecifications_InDifferentContainingTypes_AreBothGenerated()
    {
        var (hintNames, _, diagnostics) = GeneratorRunner.Run($$"""
            namespace App
            {
                public static partial class Orders { {{GeneratorRunner.Spec("IsActive")}} }
                public static partial class Customers { {{GeneratorRunner.Spec("IsActive")}} }
            }
            """);

        diagnostics.Should().BeEmpty();
        hintNames.Should().Equal("App.Customers+IsActive.g.cs", "App.Orders+IsActive.g.cs");
    }

    [Fact]
    public void SameNamedSpecifications_OfDifferentArity_AreAllGenerated()
    {
        var (hintNames, _, diagnostics) = GeneratorRunner.Run($$"""
            namespace App
            {
                {{GeneratorRunner.Spec("IsActive")}}
                {{GeneratorRunner.Spec("IsActive<T>")}}
                public partial class Outer { {{GeneratorRunner.Spec("IsActive")}} }
                public partial class Outer<T> { {{GeneratorRunner.Spec("IsActive")}} }
            }
            """);

        diagnostics.Should().BeEmpty();
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
