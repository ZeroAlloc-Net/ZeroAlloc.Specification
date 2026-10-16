# ZeroAlloc.Specification

[![NuGet](https://img.shields.io/nuget/v/ZeroAlloc.Specification.svg)](https://www.nuget.org/packages/ZeroAlloc.Specification)
[![Build](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/actions/workflows/ci.yml/badge.svg)](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/blob/main/LICENSE)
[![AOT](https://img.shields.io/badge/AOT--Compatible-passing-brightgreen)](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
[![GitHub Sponsors](https://img.shields.io/github/sponsors/MarcelRoozekrans?style=flat&logo=githubsponsors&color=ea4aaa&label=Sponsor)](https://github.com/sponsors/MarcelRoozekrans)

**Source-generated specification pattern for .NET, an Ardalis.Specification alternative: specs are structs, so composing and evaluating them allocates nothing, and the same spec still translates to SQL through EF Core.**

## Why ZeroAlloc.Specification

- **Composing and evaluating allocates nothing.** `And`, `Or` and `Not` build `readonly struct` values. Composing and calling `IsSatisfiedBy` allocates 0 B, asserted by tests. Ardalis.Specification allocates 1,752 B to compose two specs.
- **About 320× faster for in-memory filtering than Ardalis.Specification**, measured in the same run: 100 items in 49 ns instead of 15,599 ns, because nothing is compiled per call.
- **One spec for memory and the database.** Every spec also exposes `ToExpression()`, so EF Core turns the composed spec into a `WHERE` clause.
- **Works under NativeAOT and trimming**, with a NativeAOT smoke app published and run in CI. Mistakes such as `[Specification]` on a class or a missing `partial` are compile errors ZA001 to ZA003.

## Install

```bash
dotnet add package ZeroAlloc.Specification
```

The source generator ships inside this package. Targets .NET 8 and later.

## Quick start

```csharp
using System.Linq.Expressions;
using ZeroAlloc.Specification;

User[] users = [new("Ann", true, 1500m), new("Bob", true, 200m), new("Cy", false, 3000m)];

var spec = new ActiveUserSpec().And(new BigSpenderSpec(1000m));        // a struct, 0 B

Console.WriteLine(users.Count(u => spec.IsSatisfiedBy(u)));             // 1, in memory
Console.WriteLine(spec.ToExpression());   // one combined expression for dbContext.Users.Where(...)

public sealed record User(string Name, bool IsActive, decimal TotalSpend);

[Specification]
public readonly partial struct ActiveUserSpec : ISpecification<User>
{
    public bool IsSatisfiedBy(User user) => user.IsActive;
    public Expression<Func<User, bool>> ToExpression() => u => u.IsActive;
}

[Specification]
public readonly partial struct BigSpenderSpec(decimal minSpend) : ISpecification<User>
{
    public bool IsSatisfiedBy(User user) => user.TotalSpend >= minSpend;
    public Expression<Func<User, bool>> ToExpression() { var min = minSpend; return u => u.TotalSpend >= min; }
}
```

The generator adds `And`, `Or` and `Not` to every `[Specification]` struct. Evaluation is a direct call on a struct that the JIT can inline, with no virtual dispatch.

## What you get

- **Fluent composition**: `And`, `Or` and `Not`, nested to any depth, without allocating.
- **Static builder**: `Spec.And`, `Spec.Or` and `Spec.Not` when you want explicit type arguments.
- **In-memory evaluation** through `IsSatisfiedBy`, for hot loops and domain rules.
- **EF Core translation** through `ToExpression()`, which combines the specs' expression trees into one.
- **Compile-time checks** that each spec is a `readonly partial struct` implementing `ISpecification<T>`.
- **Recipes** for an [EF Core repository](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/blob/main/docs/cookbook/ef-core-repository.md), [complex compositions](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/blob/main/docs/cookbook/combining-specs.md) and [caching the expression of a stateless spec](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/blob/main/docs/cookbook/stateless-caching.md).

Building an expression tree with `ToExpression()` does allocate, as every expression tree does. Cache it for stateless specs.

## Performance

Head-to-head vs **Ardalis.Specification** 9.3.1, .NET 10.0.13, BenchmarkDotNet v0.15.8.

| Operation | Ardalis.Specification | ZA.Specification | Speedup |
|---|---:|---:|---:|
| Construct composed spec | 305 ns / 792 B | **< 1 ns / 0 B** | **> 300× faster, 0 B alloc** |
| Compose two specs | 821 ns / 1,752 B | **< 1 ns / 0 B** | **> 800× faster, 0 B alloc** |
| Evaluate over 100 items (in-memory) | 15,599 ns / 4,743 B | **49 ns / 0 B** | **~320× faster, 0 B alloc** |

Rows under 1 ns are below what BenchmarkDotNet can tell apart from an empty method, so those speedups are lower bounds. The machine was shared while measuring, so timings are approximate; the allocations are exact. For database queries the SQL provider dominates and the gap is invisible; for in-memory filtering it dominates. Full methodology: [docs/performance.md](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/blob/main/docs/performance.md).

## Learn more

- Documentation: [specification.zeroalloc.net](https://specification.zeroalloc.net). Start with [getting started](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/blob/main/docs/getting-started.md), [core concepts](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/blob/main/docs/core-concepts.md) and the [diagnostics reference](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/blob/main/docs/diagnostics.md).
- Release history: [CHANGELOG.md](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/blob/main/CHANGELOG.md).
- More ZeroAlloc libraries, such as [ZeroAlloc.Validation](https://github.com/ZeroAlloc-Net/ZeroAlloc.Validation) and [ZeroAlloc.Mapping](https://github.com/ZeroAlloc-Net/ZeroAlloc.Mapping): [zeroalloc.net](https://zeroalloc.net).
- Issues and pull requests are welcome on [GitHub](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/issues).
- MIT licensed. See [LICENSE](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/blob/main/LICENSE).
