using System;
using System.Linq;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace ZeroAlloc.Specification.Tests.Generator;

/// <summary>
/// A specification is generated into the real type wherever it is declared: in the global
/// namespace, nested in other types, or generic. The generator used to write every spec as a
/// non-generic struct at the top of its namespace, so these got output that did not compile, or
/// members on a different type (#79). Each case here compiles code that calls the generated
/// members on the real type.
/// </summary>
public class NestedSpecificationTests
{
    // Calls every generated member on a value of the given spec type.
    private static string Use(string type) => $$"""
        public static class Use
        {
            public static void Run({{type}} spec)
            {
                var and = spec.And(spec);
                var or = spec.Or(spec);
                var not = spec.Not();
                Expression<Func<int, bool>> expression = spec;
            }
        }
        """;

    [Fact]
    public void SpecInGlobalNamespace_IsGenerated()
    {
        var result = GeneratorRunner.Run(GeneratorRunner.Spec("IsActive") + Use("IsActive"));

        result.Diagnostics.Should().BeEmpty();
        result.HintNames.Should().Equal("IsActive.g.cs");
        result.Sources[0].Should().NotContain("namespace");
    }

    [Fact]
    public void NestedSpecs_WithTheSameName_AreGeneratedIntoTheirOwnContainingTypes()
    {
        var result = GeneratorRunner.Run($$"""
            namespace App
            {
                public partial class Orders { {{GeneratorRunner.Spec("IsActive")}} }
                public partial class Customers { {{GeneratorRunner.Spec("IsActive")}} }
                {{Use("Orders.IsActive")}}
                public static class UseToo { public static object Run(Customers.IsActive s) => s.Not(); }
            }
            """);

        result.Diagnostics.Should().BeEmpty();
        result.HintNames.Should().Equal("App.Customers+IsActive.g.cs", "App.Orders+IsActive.g.cs");
    }

    [Fact]
    public void GenericSpecs_OfEveryArity_AreGeneratedWithTheirTypeParameters()
    {
        var result = GeneratorRunner.Run($$"""
            namespace App
            {
                {{GeneratorRunner.Spec("IsActive")}}
                {{GeneratorRunner.Spec("IsActive<T>")}}
                {{GeneratorRunner.Spec("IsActive<T1, T2>")}}
                public static class Use
                {
                    public static void Run(IsActive a, IsActive<string> b, IsActive<string, int> c)
                    {
                        var x = a.And(b);
                        var y = b.Or(c);
                        var z = c.Not();
                        Expression<Func<int, bool>> e = c;
                    }
                }
            }
            """);

        result.Diagnostics.Should().BeEmpty();
        result.HintNames.Should().Equal("App.IsActive.g.cs", "App.IsActive`1.g.cs", "App.IsActive`2.g.cs");
    }

    [Fact]
    public void GenericSpec_OverItsOwnTypeParameter_IsGenerated()
    {
        var result = GeneratorRunner.Run("""
            namespace App
            {
                [Specification]
                public readonly partial struct IsPositive<T> : ISpecification<T> where T : IComparable<T>
                {
                    private readonly T _zero;
                    public IsPositive(T zero) => _zero = zero;
                    public bool IsSatisfiedBy(T x) => x.CompareTo(_zero) > 0;
                    public Expression<Func<T, bool>> ToExpression() { var z = _zero; return x => x.CompareTo(z) > 0; }
                }

                public static class Use
                {
                    public static Expression<Func<int, bool>> Run(IsPositive<int> spec) => spec.And(spec.Not());
                }
            }
            """);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void SpecsInContainingTypesOfEveryKind_AreGeneratedIntoTheRealType()
    {
        var result = GeneratorRunner.Run($$"""
            namespace App
            {
                public partial struct Holder<T> where T : class { {{GeneratorRunner.Spec("InStruct")}} }
                public partial interface IHolder { {{GeneratorRunner.Spec("InInterface")}} }
                public partial record struct RecordStruct { {{GeneratorRunner.Spec("InRecordStruct")}} }
                public partial record Record(int X) { {{GeneratorRunner.Spec("InRecord")}} }
                public readonly ref partial struct RefHolder { {{GeneratorRunner.Spec("InRefStruct")}} }
                public static partial class Outer<T>
                {
                    public partial class Middle<U> { {{GeneratorRunner.Spec("Deep<V>")}} }
                }
                public static class Use
                {
                    public static void Run(
                        Holder<string>.InStruct a, IHolder.InInterface b, RecordStruct.InRecordStruct c,
                        Record.InRecord d, RefHolder.InRefStruct e, Outer<int>.Middle<string>.Deep<long> f)
                    {
                        _ = (a.And(b), c.Or(d), e.Not(), f.And(a));
                    }
                }
            }
            """);

        result.Diagnostics.Should().BeEmpty();
        result.HintNames.Should().Contain("App.Outer`1+Middle`1+Deep`1.g.cs");
    }

    [Fact]
    public void SpecsWithKeywordNames_AreGeneratedWithVerbatimIdentifiers()
    {
        var result = GeneratorRunner.Run($$"""
            namespace App
            {
                public partial class @class<@int> { {{GeneratorRunner.Spec("@static<@void>")}} }
                public static class Use
                {
                    public static object Run(@class<string>.@static<int> spec) => spec.Not();
                }
            }
            """);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void SpecNestedInATypeDeclaredInSeveralParts_IsGenerated()
    {
        var result = GeneratorRunner.Run($$"""
            namespace App
            {
                public partial class Outer { public int A; }
                public partial class Outer { {{GeneratorRunner.Spec("IsActive")}} }
                {{Use("Outer.IsActive")}}
            }
            """);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void SpecInANonPartialContainingType_ReportsZa005_AndGeneratesNothing()
    {
        var result = GeneratorRunner.Run($$"""
            namespace App
            {
                public class Outer { {{GeneratorRunner.Spec("IsActive")}} }
            }
            """);

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("ZA005");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.GetMessage().Should().Be(
            "Specification 'App.Outer.IsActive' is not generated because its containing type 'App.Outer' is not partial");
        diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan)
            .Should().StartWith("[Specification]");
        result.HintNames.Should().BeEmpty();
    }

    [Fact]
    public void SpecUnderANonPartialOuterType_ReportsZa005_NamingTheOutermostOne()
    {
        var result = GeneratorRunner.Run($$"""
            namespace App
            {
                public class Outer
                {
                    public partial class Middle { {{GeneratorRunner.Spec("IsActive")}} }
                }
            }
            """);

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("ZA005");
        diagnostic.GetMessage().Should().Be(
            "Specification 'App.Outer.Middle.IsActive' is not generated because its containing type 'App.Outer' is not partial");
        result.HintNames.Should().BeEmpty();
    }

    // The output for a spec at the top of a namespace, the only kind that compiled before, is
    // unchanged apart from line endings: the file is now always written with LF.
    [Fact]
    public void SpecAtTheTopOfANamespace_IsGeneratedAsBefore()
    {
        const string expected = """
            // <auto-generated/>
            #nullable enable

            namespace MyApp
            {
                internal partial struct ActiveSpec
                {
                    public global::ZeroAlloc.Specification.AndSpecification<ActiveSpec, TOther, int> And<TOther>(TOther other)
                        where TOther : struct, global::ZeroAlloc.Specification.ISpecification<int> => new(this, other);

                    public global::ZeroAlloc.Specification.OrSpecification<ActiveSpec, TOther, int> Or<TOther>(TOther other)
                        where TOther : struct, global::ZeroAlloc.Specification.ISpecification<int> => new(this, other);

                    public global::ZeroAlloc.Specification.NotSpecification<ActiveSpec, int> Not() => new(this);

                    public static implicit operator global::System.Linq.Expressions.Expression<global::System.Func<int, bool>>(ActiveSpec spec)
                        => spec.ToExpression();
                }
            }
            """;

        var result = GeneratorRunner.Run($$"""
            namespace MyApp
            {
                {{GeneratorRunner.Spec("ActiveSpec").Replace("public readonly", "internal readonly", StringComparison.Ordinal)}}
            }
            """);

        result.Diagnostics.Should().BeEmpty();
        result.Sources.Single().Should().Be(expected.Replace("\r\n", "\n", StringComparison.Ordinal));
    }
}
