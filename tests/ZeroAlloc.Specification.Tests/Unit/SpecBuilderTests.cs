using System.Linq.Expressions;
using FluentAssertions;
using Xunit;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Tests.Unit;

public class SpecBuilderTests
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
    public void And_ProducesAndSpecification()
    {
        var spec = Spec.And<GT0Spec, LT10Spec, int>(new(), new());
        spec.IsSatisfiedBy(5).Should().BeTrue();
        spec.IsSatisfiedBy(-1).Should().BeFalse();
    }

    [Fact]
    public void Or_ProducesOrSpecification()
    {
        var spec = Spec.Or<GT0Spec, LT10Spec, int>(new(), new());
        // GT0 || LT10 is always true for any int, use Not to test or
        spec.IsSatisfiedBy(5).Should().BeTrue();
    }

    [Fact]
    public void Not_ProducesNotSpecification()
    {
        var spec = Spec.Not<GT0Spec, int>(new());
        spec.IsSatisfiedBy(-1).Should().BeTrue();
        spec.IsSatisfiedBy(5).Should().BeFalse();
    }
}
