namespace ZeroAlloc.Specification;

/// <summary>
/// Provides static factory methods for composing specifications without using instance fluent methods.
/// All returned types are structs — no heap allocation.
/// </summary>
public static class Spec
{
    /// <summary>Creates a specification that is satisfied when both <paramref name="left"/> and <paramref name="right"/> are satisfied.</summary>
    public static AndSpecification<TLeft, TRight, T> And<TLeft, TRight, T>(TLeft left, TRight right)
        where TLeft : struct, ISpecification<T>
        where TRight : struct, ISpecification<T>
        => new(left, right);

    /// <summary>Creates a specification that is satisfied when either <paramref name="left"/> or <paramref name="right"/> is satisfied.</summary>
    public static OrSpecification<TLeft, TRight, T> Or<TLeft, TRight, T>(TLeft left, TRight right)
        where TLeft : struct, ISpecification<T>
        where TRight : struct, ISpecification<T>
        => new(left, right);

    /// <summary>Creates a specification that is satisfied when <paramref name="inner"/> is not satisfied.</summary>
    public static NotSpecification<TInner, T> Not<TInner, T>(TInner inner)
        where TInner : struct, ISpecification<T>
        => new(inner);
}
