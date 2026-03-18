using System.Linq.Expressions;
using FluentAssertions;
using Xunit;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Tests.Unit;

public class OrSpecificationTests
{
    private readonly struct NegativeSpec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x < 0;
        public Expression<Func<int, bool>> ToExpression() => x => x < 0;
    }

    private readonly struct GT100Spec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x > 100;
        public Expression<Func<int, bool>> ToExpression() => x => x > 100;
    }

    private readonly struct GT0Spec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x > 0;
        public Expression<Func<int, bool>> ToExpression() => x => x > 0;
    }

    private readonly struct LT10Spec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x < 10;
        public Expression<Func<int, bool>> ToExpression() => x => x < 10;
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrueWhenLeftSatisfied()
    {
        var spec = new OrSpecification<NegativeSpec, GT100Spec, int>(new(), new());
        spec.IsSatisfiedBy(-5).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrueWhenRightSatisfied()
    {
        var spec = new OrSpecification<NegativeSpec, GT100Spec, int>(new(), new());
        spec.IsSatisfiedBy(200).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalseWhenNeitherSatisfied()
    {
        var spec = new OrSpecification<NegativeSpec, GT100Spec, int>(new(), new());
        spec.IsSatisfiedBy(50).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrueWhenBothSatisfied()
    {
        var spec = new OrSpecification<GT0Spec, LT10Spec, int>(new(), new());
        spec.IsSatisfiedBy(5).Should().BeTrue(); // 5 > 0 AND 5 < 10 — both true
    }

    [Fact]
    public void ToExpression_ComposesCorrectly()
    {
        var spec = new OrSpecification<NegativeSpec, GT100Spec, int>(new(), new());
        var compiled = spec.ToExpression().Compile();
        compiled(-5).Should().BeTrue();
        compiled(200).Should().BeTrue();
        compiled(50).Should().BeFalse();
    }

    [Fact]
    public void ImplicitConversion_ReturnsExpression()
    {
        var spec = new OrSpecification<GT0Spec, NegativeSpec, int>(new GT0Spec(), new NegativeSpec());
        Expression<Func<int, bool>> expr = spec; // implicit conversion
        expr.Should().NotBeNull();
        expr.Compile()(42).Should().BeTrue();
    }
}
