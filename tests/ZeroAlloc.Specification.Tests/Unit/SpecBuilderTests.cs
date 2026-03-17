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
        var spec = Spec.Or<NegativeSpec, GT100Spec, int>(new(), new());
        spec.IsSatisfiedBy(-5).Should().BeTrue();   // covered by left
        spec.IsSatisfiedBy(200).Should().BeTrue();  // covered by right
        spec.IsSatisfiedBy(50).Should().BeFalse(); // covered by neither
    }

    [Fact]
    public void Not_ProducesNotSpecification()
    {
        var spec = Spec.Not<GT0Spec, int>(new());
        spec.IsSatisfiedBy(-1).Should().BeTrue();
        spec.IsSatisfiedBy(5).Should().BeFalse();
    }
}
