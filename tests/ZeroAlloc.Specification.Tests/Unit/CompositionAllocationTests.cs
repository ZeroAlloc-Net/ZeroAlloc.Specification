using System.Linq.Expressions;
using Xunit;
using ZeroAlloc.Specification;
using ZeroAlloc.TestHelpers;

namespace ZeroAlloc.Specification.Tests.Unit;

/// <summary>
/// The composed specifications are structs over structs, so building one and evaluating
/// <c>IsSatisfiedBy</c> must not allocate. <c>ToExpression</c> builds a tree and is not covered.
/// </summary>
public class CompositionAllocationTests
{
    private const int Iterations = 1000;

    private readonly struct Positive : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x > 0;
        public Expression<Func<int, bool>> ToExpression() => x => x > 0;
    }

    private readonly struct Even : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x % 2 == 0;
        public Expression<Func<int, bool>> ToExpression() => x => x % 2 == 0;
    }

    private readonly struct Small : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x < 100;
        public Expression<Func<int, bool>> ToExpression() => x => x < 100;
    }

    private static bool _sink;

    [Fact]
    public void And_IsSatisfiedBy_IsZeroAlloc() =>
        AllocationGate.AssertBudget(0, Iterations, () =>
        {
            var spec = Spec.And<Positive, Even, int>(default, default);
            _sink = spec.IsSatisfiedBy(4) && !spec.IsSatisfiedBy(3);
        }, "And.IsSatisfiedBy");

    [Fact]
    public void Or_IsSatisfiedBy_IsZeroAlloc() =>
        AllocationGate.AssertBudget(0, Iterations, () =>
        {
            var spec = Spec.Or<Positive, Even, int>(default, default);
            _sink = spec.IsSatisfiedBy(-2) && !spec.IsSatisfiedBy(-1);
        }, "Or.IsSatisfiedBy");

    [Fact]
    public void Not_IsSatisfiedBy_IsZeroAlloc() =>
        AllocationGate.AssertBudget(0, Iterations, () =>
        {
            var spec = Spec.Not<Positive, int>(default);
            _sink = spec.IsSatisfiedBy(-5) && !spec.IsSatisfiedBy(5);
        }, "Not.IsSatisfiedBy");

    [Fact]
    public void NestedComposition_IsSatisfiedBy_IsZeroAlloc() =>
        AllocationGate.AssertBudget(0, Iterations, () =>
        {
            // (Positive AND Even) OR NOT Small
            var spec = Spec.Or<AndSpecification<Positive, Even, int>, NotSpecification<Small, int>, int>(
                Spec.And<Positive, Even, int>(default, default),
                Spec.Not<Small, int>(default));
            _sink = spec.IsSatisfiedBy(4) && spec.IsSatisfiedBy(101) && !spec.IsSatisfiedBy(3);
        }, "(And) Or (Not).IsSatisfiedBy");
    // The README's fluent sample goes through the generated And/Or/Not instance methods, not
    // Spec.And/Or/Not, so the generated path is measured on its own.

    [Fact]
    public void FluentAnd_ComposeAndEvaluate_IsZeroAlloc() =>
        AllocationGate.AssertBudget(0, Iterations, () =>
        {
            var spec = new EvenSpec().And(new GT0Spec());
            _sink = spec.IsSatisfiedBy(4) && !spec.IsSatisfiedBy(-2);
        }, "fluent And.IsSatisfiedBy");

    [Fact]
    public void FluentOr_ComposeAndEvaluate_IsZeroAlloc() =>
        AllocationGate.AssertBudget(0, Iterations, () =>
        {
            var spec = new EvenSpec().Or(new GT0Spec());
            _sink = spec.IsSatisfiedBy(-2) && !spec.IsSatisfiedBy(-1);
        }, "fluent Or.IsSatisfiedBy");

    [Fact]
    public void FluentNot_ComposeAndEvaluate_IsZeroAlloc() =>
        AllocationGate.AssertBudget(0, Iterations, () =>
        {
            var spec = new EvenSpec().Not();
            _sink = spec.IsSatisfiedBy(3) && !spec.IsSatisfiedBy(4);
        }, "fluent Not.IsSatisfiedBy");
}
