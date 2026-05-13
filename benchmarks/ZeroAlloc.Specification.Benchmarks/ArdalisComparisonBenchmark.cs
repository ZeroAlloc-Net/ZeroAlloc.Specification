using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using Ardalis.Specification;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Benchmarks;

// Ardalis.Specification is the de-facto specification library in .NET for
// EF Core query composition. Its design is class-based: each specification
// inherits from `Specification<T>` and accumulates a list of expressions in
// constructor calls. ZA.Specification's design is struct-based: composition
// is a value-type tree resolved at compile time.
//
// This benchmark compares the apples-to-apples cases both libraries support:
//   1. Construct (instantiate the spec object/struct)
//   2. In-memory evaluation (IsSatisfiedBy / Evaluate against an IEnumerable)
//   3. Compose (And/Or) two specs
//
// IQueryable / EF Core translation is out of scope here — both libraries
// produce expression trees of equivalent shape, and the EF Core provider
// dominates that benchmark.

public sealed class ArdalisPositiveAndEvenSpec : Specification<int>
{
    public ArdalisPositiveAndEvenSpec()
    {
        Query.Where(x => x > 0 && x % 2 == 0);
    }
}

public sealed class ArdalisPositiveSpec : Specification<int>
{
    public ArdalisPositiveSpec() { Query.Where(x => x > 0); }
}

public sealed class ArdalisEvenSpec : Specification<int>
{
    public ArdalisEvenSpec() { Query.Where(x => x % 2 == 0); }
}

[MemoryDiagnoser]
[SimpleJob]
public class ArdalisComparisonBenchmark
{
    [Params(100)]
    public int Iterations;

    // Reused for the eval rows.
    private readonly int[] _data = Enumerable.Range(-50, 100).ToArray();

    // --- Construct ---

    [Benchmark(Baseline = true, Description = "Ardalis: construct composed spec")]
    [BenchmarkCategory("Construct")]
    public ArdalisPositiveAndEvenSpec Ardalis_Construct() => new();

    [Benchmark(Description = "ZeroAlloc.Specification: construct composed spec")]
    [BenchmarkCategory("Construct")]
    public object Za_Construct() => default(IsPositiveSpec).And(default(IsEvenSpec));

    // --- Compose And on the fly ---

    [Benchmark(Description = "Ardalis: compose two specs into a new one")]
    [BenchmarkCategory("Compose")]
    public Expression<System.Func<int, bool>> Ardalis_Compose()
    {
        // Ardalis doesn't ship an in-API And operator; you compose by writing
        // a new spec whose Query.Where takes both predicates. The equivalent
        // cost is building both spec instances and pulling the WhereExpressions.
        var p = new ArdalisPositiveSpec();
        var e = new ArdalisEvenSpec();
        // Combine the WhereExpressions manually — closest mirror of ZA's And().
        Expression<System.Func<int, bool>> pExpr = p.WhereExpressions.First().Filter;
        Expression<System.Func<int, bool>> eExpr = e.WhereExpressions.First().Filter;
        var param = Expression.Parameter(typeof(int), "x");
        var body = Expression.AndAlso(
            Expression.Invoke(pExpr, param),
            Expression.Invoke(eExpr, param));
        return Expression.Lambda<System.Func<int, bool>>(body, param);
    }

    [Benchmark(Description = "ZeroAlloc.Specification: compose two specs")]
    [BenchmarkCategory("Compose")]
    public object Za_Compose() => default(IsPositiveSpec).And(default(IsEvenSpec));

    // --- In-memory evaluation over 100-item array ---

    private readonly ArdalisPositiveAndEvenSpec _ardalisSpec = new();

    [Benchmark(Description = "Ardalis: Evaluate over IEnumerable<int>")]
    [BenchmarkCategory("EvalInMemory")]
    public int Ardalis_Eval()
    {
        // Ardalis exposes a SpecificationEvaluator.Default.GetQuery for IQueryable;
        // for IEnumerable the fair comparison is to compile WhereExpression
        // (which is what any caller does in practice via .AsQueryable).
        var compiled = _ardalisSpec.WhereExpressions.First().Filter.Compile();
        var count = 0;
        for (var i = 0; i < _data.Length; i++)
            if (compiled(_data[i])) count++;
        return count;
    }

    [Benchmark(Description = "ZeroAlloc.Specification: IsSatisfiedBy loop")]
    [BenchmarkCategory("EvalInMemory")]
    public int Za_Eval()
    {
        var spec = default(IsPositiveSpec).And(default(IsEvenSpec));
        var count = 0;
        for (var i = 0; i < _data.Length; i++)
            if (spec.IsSatisfiedBy(_data[i])) count++;
        return count;
    }
}
