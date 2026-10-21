---
id: performance
title: Performance
sidebar_label: Performance
sidebar_position: 9
---

# Performance

## Allocation Comparison

| Approach | Allocation per composition |
|----------|--------------------------|
| Classic class-based specification | 1 heap object per `And`/`Or`/`Not` call |
| Delegate/lambda-based | 1 closure object per predicate |
| ZeroAlloc.Specification | **0 — struct values only** |

## How Zero Allocation Works

Composed specifications are `readonly struct` values. Structs live on the stack (or inline in the containing object) — they are never allocated on the heap.

```csharp
// This allocates nothing:
var spec = new ActiveUserSpec().And(new PremiumUserSpec(1000m));
// typeof(spec) == AndSpecification<ActiveUserSpec, PremiumUserSpec, User>
// This is a struct — stack allocated
```

## Expression Tree Allocation

`ToExpression()` does allocate — expression trees are class objects. This is unavoidable because `Expression<Func<T, bool>>` is a reference type.

For stateless specs, this can be mitigated by caching:

```csharp
private static readonly Expression<Func<User, bool>> _cachedExpression =
    new ActiveUserSpec().ToExpression();

public Expression<Func<User, bool>> ToExpression() => _cachedExpression;
```

See [Stateless Caching](cookbook/stateless-caching).

## Recommendation

- Use `IsSatisfiedBy` in hot loops (pure struct evaluation, zero allocation)
- Call `ToExpression()` once and cache for ORM queries (expression tree is allocated but reused)

## Head-to-head vs Ardalis.Specification

<!-- BENCH:START -->
_Last refreshed: 2026-10-21 — Ardalis.Specification 9.3.1, .NET 10.0.13, BenchmarkDotNet v0.15.8, Windows 11 on an otherwise idle i9-12900HK; out of process, three launches, pinned to two performance cores. Allocations are exact. Speed is shown as same-run ratios; absolute timings are not published._

Ardalis.Specification is the most-used specification library in .NET — class-based, designed for EF Core query composition. ZA.Specification's struct-based design pays off across every operation that exercises both libraries' core surface.

| Operation | Ardalis.Specification time ÷ ZA's | Ardalis alloc | ZA alloc |
|---|---:|---:|---:|
| Construct composed spec | ZA below resolution ¹ | 792 B | **0 B** |
| Compose two specs | ZA below resolution ¹ | 1,752 B | **0 B** |
| Evaluate over 100 items (in-memory) | **630×** (632–633) | 4,743 B | **0 B** |

¹ ZA.Specification's construct and compose rows are below what BenchmarkDotNet can tell apart from an empty method: the JIT reduces building two empty structs to nothing, while Ardalis' rows are measurable, so no finite ratio can be given. Ardalis' compose takes 2.74 times as long as its construct. Earlier versions of this table charged ZA 24 B per composed spec; that was the benchmark boxing the struct into `object`, not the library.

The in-memory-evaluation gap is dramatic because Ardalis is designed for **IQueryable composition** — `WhereExpression.Filter.Compile()` runs on every call, almost all of the Ardalis row. For EF Core / database queries where the SQL provider dominates the cost, this overhead is invisible. For in-memory filtering (which many users do), it dominates.

ZA.Specification's `IsSatisfiedBy` is a direct, non-virtual call on a struct value — the JIT inlines it, the composition tree resolves to a sequence of `&&` operators in straight-line code. Zero allocation per evaluation regardless of composition depth.
<!-- BENCH:END -->

## Vs `Func<T, bool>` (no library)

The [benchmarks/ZeroAlloc.Specification.Benchmarks](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/tree/main/benchmarks/ZeroAlloc.Specification.Benchmarks) project also contains `SpecVsFuncBenchmark` — a head-to-head comparing `AndSpecification` struct composition against a `Func<T, bool>` pipeline of lambdas.

Why this pairing: `Func<T, bool>` is the idiomatic C# alternative to struct specifications. Every lambda capturing outer state is a heap allocation, so composing `positive && even` via `Func` allocates roughly one closure per captured predicate plus one for the composed result. The struct path allocates none — the `AndSpecification<TLeft, TRight, T>` value lives on the stack.

```bash
dotnet run --project benchmarks/ZeroAlloc.Specification.Benchmarks -c Release -- --filter "*" --launchCount 3
```

What to watch:

- **Allocated column**: the `Specification struct composed` row must read `0 B/op`. The `Func<int, bool> composed` row allocates 32 B per call for its lambda captures; measured 2026-10-21 alongside the table above
- **Ratio column**: struct inlining makes the struct path faster than delegate invocation for simple predicates: the `Func` row takes 1.93 times as long (1.91–1.96) at this composition depth
