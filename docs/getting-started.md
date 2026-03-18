---
id: getting-started
title: Getting Started
sidebar_label: Getting Started
sidebar_position: 2
---

# Getting Started

## Installation

```bash
dotnet add package ZeroAlloc.Specification
dotnet add package ZeroAlloc.Specification.Generator
```

The generator package is a Roslyn analyzer/source generator. It is automatically applied at compile time — no runtime dependency.

## Your First Specification

### 1. Define a spec struct

```csharp
using ZeroAlloc.Specification;
using System.Linq.Expressions;

[Specification]
public readonly partial struct ActiveUserSpec : ISpecification<User>
{
    public bool IsSatisfiedBy(User user) => user.IsActive;
    public Expression<Func<User, bool>> ToExpression() => u => u.IsActive;
}
```

The `[Specification]` attribute and `partial` keyword are both required. The generator adds fluent composition methods to your struct.

### 2. Use it in memory

```csharp
var spec = new ActiveUserSpec();
bool result = spec.IsSatisfiedBy(user); // true/false
```

### 3. Compose specifications

```csharp
[Specification]
public readonly partial struct PremiumUserSpec : ISpecification<User>
{
    private readonly decimal _minSpend;
    public PremiumUserSpec(decimal minSpend) => _minSpend = minSpend;

    public bool IsSatisfiedBy(User user) => user.TotalSpend >= _minSpend;
    public Expression<Func<User, bool>> ToExpression()
    {
        var min = _minSpend; // copy field to local for lambda capture
        return u => u.TotalSpend >= min;
    }
}

// Fluent:
var spec = new ActiveUserSpec().And(new PremiumUserSpec(1000m));

// Static builder:
var spec = Spec.And<ActiveUserSpec, PremiumUserSpec, User>(new(), new(1000m));
```

Both produce `AndSpecification<ActiveUserSpec, PremiumUserSpec, User>` — a struct, zero heap allocation.

### 4. Query with EF Core

```csharp
var users = await dbContext.Users
    .Where(spec.ToExpression())
    .ToListAsync();
```

EF Core translates the composed expression tree to SQL.

## Next Steps

- [Core Concepts](core-concepts) — understand struct constraints and zero-allocation guarantees
- [Fluent API](fluent-api) — all generated methods
- [Diagnostics](diagnostics) — fix ZA001–ZA004 compile errors
