using System.Linq.Expressions;
using FluentAssertions;
using Xunit;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Tests.Unit;

public class NotSpecificationTests
{
    private readonly struct EvenSpec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x % 2 == 0;
        public Expression<Func<int, bool>> ToExpression() => x => x % 2 == 0;
    }

    [Fact]
    public void IsSatisfiedBy_NegatesCorrectly()
    {
        var spec = new NotSpecification<EvenSpec, int>(new());
        spec.IsSatisfiedBy(3).Should().BeTrue();
        spec.IsSatisfiedBy(4).Should().BeFalse();
    }

    [Fact]
    public void ToExpression_NegatesCorrectly()
    {
        var spec = new NotSpecification<EvenSpec, int>(new());
        var compiled = spec.ToExpression().Compile();
        compiled(3).Should().BeTrue();
        compiled(4).Should().BeFalse();
    }

    [Fact]
    public void ImplicitConversion_ReturnsExpression()
    {
        var spec = new NotSpecification<EvenSpec, int>(new EvenSpec());
        Expression<Func<int, bool>> expr = spec; // implicit conversion
        expr.Should().NotBeNull();
        expr.Compile()(42).Should().BeFalse();
    }
}
