using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Benchmarks;

// Compares the struct-based Specification composition against a Func<T, bool>
// pipeline composed with lambdas. The claim: AndSpecification/OrSpecification
// are structs — the composition path allocates 0 B/op, while Func lambdas
// always capture a closure = 1 heap allocation per composed predicate.
[MemoryDiagnoser]
[SimpleJob]
public class SpecVsFuncBenchmark
{
    [Params(100)]
    public int Iterations;

    [Benchmark(Baseline = true, Description = "Func<int, bool> composed")]
    public int FuncComposed()
    {
        // Each composition allocates a closure.
        Func<int, bool> positive = x => x > 0;
        Func<int, bool> even = x => x % 2 == 0;
        Func<int, bool> combined = x => positive(x) && even(x);
        var accepted = 0;
        for (var i = 0; i < Iterations; i++)
        {
            if (combined(i)) accepted++;
        }
        return accepted;
    }

    [Benchmark(Description = "Specification struct composed")]
    public int SpecComposed()
    {
        // AndSpecification<IsPositiveSpec, IsEvenSpec, int> is a struct value.
        var combined = default(IsPositiveSpec).And(default(IsEvenSpec));
        var accepted = 0;
        for (var i = 0; i < Iterations; i++)
        {
            if (combined.IsSatisfiedBy(i)) accepted++;
        }
        return accepted;
    }
}

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
