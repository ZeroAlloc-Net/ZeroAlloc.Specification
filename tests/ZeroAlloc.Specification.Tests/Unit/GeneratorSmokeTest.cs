using System.Linq.Expressions;
using FluentAssertions;
using Xunit;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Tests.Unit;

// Spec structs must be at namespace level (not nested in a class) so the source
// generator's partial struct output merges with the same type declaration.

[Specification]
public readonly partial struct EvenSpec : ISpecification<int>
{
    public bool IsSatisfiedBy(int x) => x % 2 == 0;
    public Expression<Func<int, bool>> ToExpression() => x => x % 2 == 0;
}

[Specification]
public readonly partial struct GT0Spec : ISpecification<int>
{
    public bool IsSatisfiedBy(int x) => x > 0;
    public Expression<Func<int, bool>> ToExpression() => x => x > 0;
}

/// <summary>
/// Verifies the source generator emits And/Or/Not fluent methods on attributed partial structs.
/// </summary>
public class GeneratorSmokeTest
{
    [Fact]
    public void GeneratedAnd_WorksCorrectly()
    {
        var spec = new EvenSpec().And(new GT0Spec());
        spec.IsSatisfiedBy(4).Should().BeTrue();   // even AND > 0
        spec.IsSatisfiedBy(-2).Should().BeFalse(); // even but NOT > 0
        spec.IsSatisfiedBy(3).Should().BeFalse();  // > 0 but NOT even
    }

    [Fact]
    public void GeneratedOr_WorksCorrectly()
    {
        var spec = new EvenSpec().Or(new GT0Spec());
        spec.IsSatisfiedBy(4).Should().BeTrue();   // both
        spec.IsSatisfiedBy(-2).Should().BeTrue();  // even only
        spec.IsSatisfiedBy(3).Should().BeTrue();   // > 0 only
        spec.IsSatisfiedBy(-3).Should().BeFalse(); // neither
    }

    [Fact]
    public void GeneratedNot_WorksCorrectly()
    {
        var spec = new EvenSpec().Not();
        spec.IsSatisfiedBy(3).Should().BeTrue();
        spec.IsSatisfiedBy(4).Should().BeFalse();
    }
}
