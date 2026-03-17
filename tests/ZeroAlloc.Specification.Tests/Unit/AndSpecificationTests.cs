using System.Linq.Expressions;
using FluentAssertions;
using Xunit;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Tests.Unit;

public class AndSpecificationTests
{
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
    public void IsSatisfiedBy_ReturnsTrueWhenBothSatisfied()
    {
        var spec = new AndSpecification<GT0Spec, LT10Spec, int>(new GT0Spec(), new LT10Spec());
        spec.IsSatisfiedBy(5).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalseWhenLeftFails()
    {
        var spec = new AndSpecification<GT0Spec, LT10Spec, int>(new GT0Spec(), new LT10Spec());
        spec.IsSatisfiedBy(-1).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalseWhenRightFails()
    {
        var spec = new AndSpecification<GT0Spec, LT10Spec, int>(new GT0Spec(), new LT10Spec());
        spec.IsSatisfiedBy(15).Should().BeFalse();
    }

    [Fact]
    public void ToExpression_ComposesCorrectly()
    {
        var spec = new AndSpecification<GT0Spec, LT10Spec, int>(new GT0Spec(), new LT10Spec());
        var compiled = spec.ToExpression().Compile();
        compiled(5).Should().BeTrue();
        compiled(-1).Should().BeFalse();
        compiled(15).Should().BeFalse();
    }
}
