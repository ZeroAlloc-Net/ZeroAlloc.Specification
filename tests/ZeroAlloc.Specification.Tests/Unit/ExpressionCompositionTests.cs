using System.Linq.Expressions;
using FluentAssertions;
using Xunit;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Tests.Unit;

public class ExpressionCompositionTests
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
    public void And_ProducesAndAlsoNode()
    {
        var spec = new AndSpecification<GT0Spec, LT10Spec, int>(new(), new());
        var expr = spec.ToExpression();
        expr.Body.NodeType.Should().Be(ExpressionType.AndAlso);
    }

    [Fact]
    public void Or_ProducesOrElseNode()
    {
        var spec = new OrSpecification<GT0Spec, LT10Spec, int>(new(), new());
        var expr = spec.ToExpression();
        expr.Body.NodeType.Should().Be(ExpressionType.OrElse);
    }

    [Fact]
    public void Not_ProducesNotNode()
    {
        var spec = new NotSpecification<GT0Spec, int>(new());
        var expr = spec.ToExpression();
        expr.Body.NodeType.Should().Be(ExpressionType.Not);
    }

    [Fact]
    public void And_UsesSingleSharedParameter()
    {
        var spec = new AndSpecification<GT0Spec, LT10Spec, int>(new(), new());
        var expr = spec.ToExpression();

        expr.Parameters.Should().HaveCount(1);

        // Both sides of AndAlso must reference the same ParameterExpression object
        var andAlso = (BinaryExpression)expr.Body;
        var leftParam = ((BinaryExpression)andAlso.Left).Left as ParameterExpression;
        var rightParam = ((BinaryExpression)andAlso.Right).Left as ParameterExpression;
        ReferenceEquals(leftParam, rightParam).Should().BeTrue(
            "parameter rebinding must produce a single shared ParameterExpression for EF Core translation");
    }
}
