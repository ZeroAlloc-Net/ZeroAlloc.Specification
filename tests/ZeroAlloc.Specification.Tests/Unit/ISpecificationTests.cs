using System.Linq.Expressions;
using FluentAssertions;
using Xunit;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Tests.Unit;

public class ISpecificationTests
{
    [Fact]
    public void IsSatisfiedBy_ReturnsTrueWhenPredicateMatches()
    {
        var spec = new AlwaysTrueSpec();
        spec.IsSatisfiedBy(42).Should().BeTrue();
    }

    [Fact]
    public void ToExpression_ReturnsCompilableExpression()
    {
        var spec = new AlwaysTrueSpec();
        var compiled = spec.ToExpression().Compile();
        compiled(42).Should().BeTrue();
    }

    // Test double — inline struct implementing the interface
    private readonly struct AlwaysTrueSpec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int candidate) => true;
        public Expression<Func<int, bool>> ToExpression() => _ => true;
    }
}
