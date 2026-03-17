using System.Linq.Expressions;
using FluentAssertions;
using Xunit;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Tests.Unit;

/// <summary>
/// Demonstrates and verifies the zero-allocation expression caching pattern for stateless specs.
/// Stateless specs (no instance fields) should cache their expression in a static field.
/// The generator detects stateless specs and emits And/Or/Not; expression caching is by convention.
/// </summary>
public class CachingTests
{
    [Fact]
    public void StatelessSpec_WithStaticExpressionField_ToExpression_ReturnsSameInstance()
    {
        var spec1 = new CachedStatelessSpec();
        var spec2 = new CachedStatelessSpec();
        // Both return the same static field — reference equal
        ReferenceEquals(spec1.ToExpression(), spec2.ToExpression()).Should().BeTrue();
    }

    [Fact]
    public void StatefulSpec_ToExpression_ReturnsNewInstancePerCall()
    {
        var spec1 = new MinValueSpec(1);
        var spec2 = new MinValueSpec(2);
        // Each instance captures a different closure — not reference equal
        ReferenceEquals(spec1.ToExpression(), spec2.ToExpression()).Should().BeFalse();
    }

    [Fact]
    public void StatelessSpec_GeneratedAnd_WorksWithCachedExpression()
    {
        var spec = new CachedStatelessSpec().And(new MinValueSpec(0));
        spec.IsSatisfiedBy(5).Should().BeTrue();
        spec.IsSatisfiedBy(-1).Should().BeFalse();
        // Expression composition works regardless of caching strategy
        spec.ToExpression().Compile()(5).Should().BeTrue();
    }
}

/// <summary>
/// Stateless spec: no instance fields. Caches expression in a static field by convention.
/// The generator detects this as stateless and emits And/Or/Not methods.
/// </summary>
[Specification]
public readonly partial struct CachedStatelessSpec : ISpecification<int>
{
    private static readonly Expression<Func<int, bool>> s_expression = x => x > 0;

    public bool IsSatisfiedBy(int x) => x > 0;

    // Stateless convention: return the static expression field for zero-alloc ToExpression
    public Expression<Func<int, bool>> ToExpression() => s_expression;
}

/// <summary>
/// Stateful spec: has an instance field. Expression cannot be statically cached.
/// </summary>
[Specification]
public readonly partial struct MinValueSpec : ISpecification<int>
{
    private readonly int _min;
    public MinValueSpec(int min) => _min = min;
    public bool IsSatisfiedBy(int x) => x > _min;
    public Expression<Func<int, bool>> ToExpression()
    {
        var min = _min;
        return x => x > min;
    }
}
