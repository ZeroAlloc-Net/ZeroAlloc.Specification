# ZeroAlloc.Specification

Source-generated, zero-allocation specification pattern for .NET 8+.

[![NuGet](https://img.shields.io/nuget/v/ZeroAlloc.Specification.svg)](https://www.nuget.org/packages/ZeroAlloc.Specification)
[![NuGet](https://img.shields.io/nuget/v/ZeroAlloc.Specification.Generator.svg?label=ZeroAlloc.Specification.Generator)](https://www.nuget.org/packages/ZeroAlloc.Specification.Generator)
[![Build](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/actions/workflows/ci.yml/badge.svg)](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## Install

```bash
dotnet add package ZeroAlloc.Specification
dotnet add package ZeroAlloc.Specification.Generator
```

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
