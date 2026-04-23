using System;
using System.Linq.Expressions;
using ZeroAlloc.Specification;

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

        Console.WriteLine("AOT smoke: PASS");
        return 0;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine($"AOT smoke: FAIL — {message}");
        return 1;
    }
}
