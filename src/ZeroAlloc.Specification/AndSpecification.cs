using System.Linq.Expressions;
using ZeroAlloc.Specification.Internal;

namespace ZeroAlloc.Specification;

/// <summary>
/// A zero-allocation specification that is satisfied when both <typeparamref name="TLeft"/>
/// and <typeparamref name="TRight"/> are satisfied. Composed expressions use
/// <see cref="System.Linq.Expressions.Expression.AndAlso"/> and are compatible with EF Core query translation.
/// </summary>
/// <typeparam name="TLeft">The left-hand specification type. Must be a struct.</typeparam>
/// <typeparam name="TRight">The right-hand specification type. Must be a struct.</typeparam>
/// <typeparam name="T">The candidate type being evaluated.</typeparam>
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

    /// <summary>
    /// Composes the left and right expressions into a single <c>AndAlso</c> expression tree.
    /// A new expression tree is built on each call; for stateless specifications consider
    /// caching the result at the call site.
    /// </summary>
    public Expression<Func<T, bool>> ToExpression()
    {
        var left = _left.ToExpression();
        var right = _right.ToExpression();
        var param = left.Parameters[0];
        var rightBody = ParameterRebinder.ReplaceParameter(right.Body, right.Parameters[0], param);
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, rightBody), param);
    }
}
