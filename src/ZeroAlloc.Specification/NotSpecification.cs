using System.Linq.Expressions;

namespace ZeroAlloc.Specification;

/// <summary>
/// A zero-allocation specification that is satisfied when <typeparamref name="TInner"/> is not satisfied.
/// Composed expressions use <see cref="System.Linq.Expressions.Expression.Not"/> and are compatible
/// with EF Core query translation.
/// </summary>
/// <typeparam name="TInner">The inner specification type to negate. Must be a struct.</typeparam>
/// <typeparam name="T">The candidate type being evaluated.</typeparam>
public readonly struct NotSpecification<TInner, T> : ISpecification<T>
    where TInner : struct, ISpecification<T>
{
    private readonly TInner _inner;

    public NotSpecification(TInner inner) => _inner = inner;

    public bool IsSatisfiedBy(T candidate) => !_inner.IsSatisfiedBy(candidate);

    /// <summary>
    /// Wraps the inner expression in a <c>Not</c> node.
    /// A new expression tree is built on each call; for stateless specifications consider
    /// caching the result at the call site.
    /// </summary>
    public Expression<Func<T, bool>> ToExpression()
    {
        var inner = _inner.ToExpression();
        var param = inner.Parameters[0];
        return Expression.Lambda<Func<T, bool>>(Expression.Not(inner.Body), param);
    }

    public static implicit operator Expression<Func<T, bool>>(NotSpecification<TInner, T> spec)
        => spec.ToExpression();
}
