using System.Linq.Expressions;

namespace ZeroAlloc.Specification.Internal;

internal sealed class ParameterRebinder : ExpressionVisitor
{
    private readonly ParameterExpression _from;
    private readonly ParameterExpression _to;

    private ParameterRebinder(ParameterExpression from, ParameterExpression to)
    {
        _from = from;
        _to = to;
    }

    public static Expression ReplaceParameter(
        Expression body,
        ParameterExpression from,
        ParameterExpression to) =>
        new ParameterRebinder(from, to).Visit(body)!;

    protected override Expression VisitParameter(ParameterExpression node) =>
        node == _from ? _to : base.VisitParameter(node);
}
