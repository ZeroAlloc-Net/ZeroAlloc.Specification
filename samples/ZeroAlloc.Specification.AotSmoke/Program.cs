using System;
using System.Linq.Expressions;
using ZeroAlloc.Specification;
using ZeroAlloc.TestHelpers;

namespace ZeroAlloc.Specification.AotSmoke;

[Specification]
public readonly partial struct IsPositiveSpec : ISpecification<int>
{
    public bool IsSatisfiedBy(int x) => x > 0;
    public Expression<Func<int, bool>> ToExpression() => x => x > 0;
}

[Specification]
public readonly partial struct IsEvenSpec : ISpecification<int>
{
    public bool IsSatisfiedBy(int x) => x % 2 == 0;
    public Expression<Func<int, bool>> ToExpression() => x => x % 2 == 0;
}

public static class Program
{
    public static int Main()
    {
        var positive = default(IsPositiveSpec);
        var even = default(IsEvenSpec);

        // Core predicate
        if (!positive.IsSatisfiedBy(3)) return Fail("IsPositive(3) should be true");
        if (positive.IsSatisfiedBy(-1)) return Fail("IsPositive(-1) should be false");

        // Generator-emitted And / Or / Not combinators
        var posAndEven = positive.And(even);
        if (!posAndEven.IsSatisfiedBy(4)) return Fail("Positive AND Even should accept 4");
        if (posAndEven.IsSatisfiedBy(3)) return Fail("Positive AND Even should reject 3 (odd)");
        if (posAndEven.IsSatisfiedBy(-2)) return Fail("Positive AND Even should reject -2 (not positive)");

        var posOrEven = positive.Or(even);
        if (!posOrEven.IsSatisfiedBy(-2)) return Fail("Positive OR Even should accept -2 (even)");
        if (!posOrEven.IsSatisfiedBy(3)) return Fail("Positive OR Even should accept 3 (positive)");
        if (posOrEven.IsSatisfiedBy(-1)) return Fail("Positive OR Even should reject -1 (neither)");

        var notPositive = positive.Not();
        if (notPositive.IsSatisfiedBy(5)) return Fail("NOT Positive should reject 5");
        if (!notPositive.IsSatisfiedBy(-5)) return Fail("NOT Positive should accept -5");

        // Expression composition under NativeAOT: ToExpression on composed specs runs
        // ParameterRebinder and AndAlso / OrElse / Not, and Compile falls back to the
        // expression interpreter because there is no dynamic code. Each compiled delegate
        // must agree with IsSatisfiedBy.
        var nested = Spec.Or<AndSpecification<IsPositiveSpec, IsEvenSpec, int>, NotSpecification<IsEvenSpec, int>, int>(
            posAndEven, even.Not());
        int[] candidates = [-3, -2, -1, 0, 1, 2, 3, 4];
        if (!AgreesWithExpression(posAndEven, posAndEven.ToExpression(), candidates, out var failure)) return Fail("AND " + failure);
        if (!AgreesWithExpression(posOrEven, posOrEven.ToExpression(), candidates, out failure)) return Fail("OR " + failure);
        if (!AgreesWithExpression(notPositive, notPositive.ToExpression(), candidates, out failure)) return Fail("NOT " + failure);
        if (!AgreesWithExpression(nested, nested.ToExpression(), candidates, out failure)) return Fail("(AND) OR (NOT) " + failure);

        // The composed expression must be one lambda over one parameter: the rebinder replaced
        // the right-hand parameter instead of leaving a dangling one.
        var nestedExpr = nested.ToExpression();
        if (nestedExpr.Parameters.Count != 1) return Fail("composed expression should have one parameter");
        if (nestedExpr.Body.NodeType != ExpressionType.OrElse) return Fail("(AND) OR (NOT) body should be OrElse");

        Console.WriteLine("AOT expression composition: PASS");

        // Allocation gate: composing and evaluating structs over structs is 0 B under AOT too.
        var sink = false;
        AllocationGate.AssertBudget(0, 1000, () =>
        {
            var spec = Spec.Or<AndSpecification<IsPositiveSpec, IsEvenSpec, int>, NotSpecification<IsEvenSpec, int>, int>(
                default(IsPositiveSpec).And(default(IsEvenSpec)), default(IsEvenSpec).Not());
            sink ^= spec.IsSatisfiedBy(4) ^ spec.IsSatisfiedBy(3);
        }, "(And) Or (Not).IsSatisfiedBy");
        GC.KeepAlive(sink);

        Console.WriteLine("AOT allocation gate: PASS");
        Console.WriteLine("AOT smoke: PASS");
        return 0;
    }

    private static bool AgreesWithExpression<TSpec>(
        TSpec spec, Expression<Func<int, bool>> expression, int[] candidates, out string failure)
        where TSpec : ISpecification<int>
    {
        var compiled = expression.Compile();
        foreach (var candidate in candidates)
        {
            if (compiled(candidate) != spec.IsSatisfiedBy(candidate))
            {
                failure = $"compiled ToExpression disagrees with IsSatisfiedBy for {candidate}";
                return false;
            }
        }

        failure = "";
        return true;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine($"AOT smoke: FAIL — {message}");
        return 1;
    }
}
