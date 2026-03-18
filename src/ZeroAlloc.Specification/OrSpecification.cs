using System.Linq.Expressions;
using ZeroAlloc.Specification.Internal;

namespace ZeroAlloc.Specification;

/// <summary>
/// A zero-allocation specification that is satisfied when either <typeparamref name="TLeft"/>
/// or <typeparamref name="TRight"/> is satisfied. Composed expressions use
/// <see cref="System.Linq.Expressions.Expression.OrElse"/> and are compatible with EF Core query translation.
/// </summary>
/// <typeparam name="TLeft">The left-hand specification type. Must be a struct.</typeparam>
/// <typeparam name="TRight">The right-hand specification type. Must be a struct.</typeparam>
/// <typeparam name="T">The candidate type being evaluated.</typeparam>
public readonly struct OrSpecification<TLeft, TRight, T> : ISpecification<T>
    where TLeft : struct, ISpecification<T>
    where TRight : struct, ISpecification<T>
{
    private readonly TLeft _left;
    private readonly TRight _right;

    public OrSpecification(TLeft left, TRight right)
    {
        _left = left;
        _right = right;
    }

    public bool IsSatisfiedBy(T candidate) =>
        _left.IsSatisfiedBy(candidate) || _right.IsSatisfiedBy(candidate);

    /// <summary>
    /// Composes the left and right expressions into a single <c>OrElse</c> expression tree.
    /// A new expression tree is built on each call; for stateless specifications consider
    /// caching the result at the call site.
    /// </summary>
    public Expression<Func<T, bool>> ToExpression()
    {
        var left = _left.ToExpression();
        var right = _right.ToExpression();
        var param = left.Parameters[0];
        var rightBody = ParameterRebinder.ReplaceParameter(right.Body, right.Parameters[0], param);
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Body, rightBody), param);
    }

    public static implicit operator Expression<Func<T, bool>>(OrSpecification<TLeft, TRight, T> spec)
        => spec.ToExpression();
}
