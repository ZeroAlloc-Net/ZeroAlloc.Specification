using System.Linq.Expressions;
using ZeroAlloc.Specification;

// And and Not exist only if the compiler loaded the generator.
var spec = new IsPositiveSpec().And(new IsEvenSpec());
var odd = new IsEvenSpec().Not();
if (!spec.IsSatisfiedBy(4) || spec.IsSatisfiedBy(3) || spec.IsSatisfiedBy(-2) || !odd.IsSatisfiedBy(3))
{
    Console.Error.WriteLine("FAIL: composed specification gave a wrong answer");
    return 1;
}

Console.WriteLine("SDK 10 consumer: generator loaded, specifications composed.");
return 0;

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
