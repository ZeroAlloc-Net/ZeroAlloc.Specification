# ZeroAlloc.Specification

[![NuGet](https://img.shields.io/nuget/v/ZeroAlloc.Specification.svg)](https://www.nuget.org/packages/ZeroAlloc.Specification)
[![Build](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/actions/workflows/ci.yml/badge.svg)](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![AOT](https://img.shields.io/badge/AOT--Compatible-passing-brightgreen)](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
[![GitHub Sponsors](https://img.shields.io/github/sponsors/MarcelRoozekrans?style=flat&logo=githubsponsors&color=ea4aaa&label=Sponsor)](https://github.com/sponsors/MarcelRoozekrans)

Source-generated, zero-allocation specification pattern for .NET 8+.

Multiple packages in this family — see [Documentation](docs/) or NuGet for the full list.

## Install

The source generator is bundled into the main package — a single `PackageReference` is all you need:

```bash
dotnet add package ZeroAlloc.Specification
```

> The standalone `ZeroAlloc.Specification.Generator` package is still published for backwards compatibility with existing direct PackageReferences, but new consumers should reference only `ZeroAlloc.Specification`.

## Example

```csharp
[Specification]
public readonly partial struct ActiveUserSpec : ISpecification<User>
{
    public bool IsSatisfiedBy(User user) => user.IsActive;
    public Expression<Func<User, bool>> ToExpression() => u => u.IsActive;
}

[Specification]
public readonly partial struct PremiumUserSpec : ISpecification<User>
{
    private readonly decimal _minSpend;
    public PremiumUserSpec(decimal minSpend) => _minSpend = minSpend;

    public bool IsSatisfiedBy(User user) => user.TotalSpend >= _minSpend;
    public Expression<Func<User, bool>> ToExpression()
    {
        var min = _minSpend;
        return u => u.TotalSpend >= min;
    }
}

// Fluent composition — zero allocation
var spec = new ActiveUserSpec().And(new PremiumUserSpec(1000m));

// In-memory
bool result = spec.IsSatisfiedBy(user);

// EF Core — translates to SQL
var users = await dbContext.Users.Where(spec.ToExpression()).ToListAsync();
```

## Performance

Head-to-head vs **Ardalis.Specification** 8.0 (the de-facto specification library in .NET). .NET 10.0.7, i9-12900HK, BenchmarkDotNet v0.15.4.

| Operation | Ardalis.Specification | ZA.Specification | Speedup |
|---|---:|---:|---:|
| Construct composed spec | 1,719 ns / 1,248 B | **40 ns / 24 B** | **43× faster, 52× less alloc** |
| Compose two specs | 3,959 ns / 2,648 B | **22 ns / 24 B** | **180× faster, 110× less alloc** |
| Evaluate over 100 items (in-memory) | 170,862 ns / 4,688 B | **150 ns / 0 B** | **1,136× faster, 0 B alloc** |

The in-memory-evaluation gap is dramatic because Ardalis pays `Expression.Compile()` per call. For EF Core / database queries the SQL provider dominates and the overhead is invisible; for in-memory filtering it dominates.

Full methodology + analysis: [docs/performance.md](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/blob/main/docs/performance.md).

## Features

- **Zero allocations** — composed specs are `readonly struct` values, not heap objects
- **EF Core compatible** — every spec exposes `ToExpression()` returning `Expression<Func<T, bool>>`
- **Source-generated fluent API** — `And<TOther>()`, `Or<TOther>()`, `Not()` added by Roslyn generator
- **Static builder** — `Spec.And()`, `Spec.Or()`, `Spec.Not()` for explicit type arguments
- **Compile-time safety** — ZA001–ZA004 diagnostics enforce correct `partial struct` usage
- **.NET 8+, C# 12**

## Documentation

| Page | Description |
|------|-------------|
| [Introduction](docs/intro.md) | What it is and why it exists |
| [Getting Started](docs/getting-started.md) | Install and first specification in 5 minutes |
| [Core Concepts](docs/core-concepts.md) | ISpecification&lt;T&gt;, structs, stateful vs stateless |
| [Fluent API](docs/fluent-api.md) | Generated And/Or/Not methods |
| [Static Builder](docs/static-builder.md) | Spec.And/Or/Not |
| [Expression Composition](docs/expression-composition.md) | ToExpression() and EF Core translation |
| [Diagnostics](docs/diagnostics.md) | ZA001–ZA004 compile-time errors and fixes |
| [Generator Internals](docs/generator-internals.md) | How the Roslyn generator works |
| [Performance](docs/performance.md) | Allocation comparison and benchmarks |
| [Cookbook: EF Core Repository](docs/cookbook/ef-core-repository.md) | Repository pattern integration |
| [Cookbook: Combining Specs](docs/cookbook/combining-specs.md) | Complex compositions |
| [Cookbook: Stateless Caching](docs/cookbook/stateless-caching.md) | Cache expression trees for stateless specs |

## License

MIT — see [LICENSE](LICENSE).
