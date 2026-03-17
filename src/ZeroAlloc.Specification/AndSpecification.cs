using System.Linq.Expressions;
using ZeroAlloc.Specification.Internal;

namespace ZeroAlloc.Specification;

public readonly struct AndSpecification<TLeft, TRight, T> : ISpecification<T>
    where TLeft : struct, ISpecification<T>
    where TRight : struct, ISpecification<T>
{
    private readonly TLeft _left;
    private readonly TRight _right;

    public AndSpecification(TLeft left, TRight right)
    {
        _left = left;
        _right = right;
    }

    public bool IsSatisfiedBy(T candidate) =>
        _left.IsSatisfiedBy(candidate) && _right.IsSatisfiedBy(candidate);

    public Expression<Func<T, bool>> ToExpression()
    {
        var left = _left.ToExpression();
        var right = _right.ToExpression();
        var param = left.Parameters[0];
        var rightBody = ParameterRebinder.ReplaceParameter(right.Body, right.Parameters[0], param);
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, rightBody), param);
    }
}
