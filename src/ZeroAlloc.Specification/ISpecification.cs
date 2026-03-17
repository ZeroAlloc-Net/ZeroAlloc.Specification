using System.Linq.Expressions;

namespace ZeroAlloc.Specification;

/// <summary>
/// Defines a zero-allocation specification over a candidate of type <typeparamref name="T"/>.
/// </summary>
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T candidate);
    Expression<Func<T, bool>> ToExpression();
}
