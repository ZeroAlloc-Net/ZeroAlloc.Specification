# ZeroAlloc.Specification — Design Document

**Date:** 2026-03-17

## Problem

The classic DDD Specification pattern allocates heap objects on every composition (`And`, `Or`, `Not` return wrapper class instances). Delegate-based predicates create closures. This makes specifications expensive to use in hot paths.

## Goal

Source-generated, compile-time predicate composition with no closure allocations. Specifications remain ergonomic (familiar interface, fluent + static builder APIs) while composed types are concrete structs emitted by a Roslyn incremental source generator.

## Target Framework

.NET 8+ only. C# 11+ features (static abstract interface members, readonly record struct) are in scope.

---

## Solution Structure

```
ZeroAlloc.Specification/           ← core library
ZeroAlloc.Specification.Generator/ ← Roslyn incremental source generator
ZeroAlloc.Specification.Tests/     ← unit, snapshot, and integration tests
```

---

## Core Library

### Interface

```csharp
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T candidate);
    Expression<Func<T, bool>> ToExpression();
}
```

### Attribute

```csharp
[AttributeUsage(AttributeTargets.Struct)]
public sealed class SpecificationAttribute : Attribute { }
```

Declared in the core library; embedded as a source in the generator so no separate package is needed.

### Built-in Combinator Structs

Hand-written generic structs reused for all compositions:

```csharp
public readonly struct AndSpecification<TLeft, TRight, T> : ISpecification<T>
    where TLeft : struct, ISpecification<T>
    where TRight : struct, ISpecification<T>;

public readonly struct OrSpecification<TLeft, TRight, T> : ISpecification<T>
    where TLeft : struct, ISpecification<T>
    where TRight : struct, ISpecification<T>;

public readonly struct NotSpecification<TInner, T> : ISpecification<T>
    where TInner : struct, ISpecification<T>;
```

`ToExpression()` composes expression trees using `ExpressionVisitor` parameter rebinding to unify lambda parameters before combining bodies.

### Static Builder

```csharp
public static class Spec
{
    public static AndSpecification<TLeft, TRight, T> And<TLeft, TRight, T>(TLeft left, TRight right)
        where TLeft : struct, ISpecification<T>
        where TRight : struct, ISpecification<T> => new(left, right);

    public static OrSpecification<TLeft, TRight, T> Or<TLeft, TRight, T>(TLeft left, TRight right)
        where TLeft : struct, ISpecification<T>
        where TRight : struct, ISpecification<T> => new(left, right);

    public static NotSpecification<TInner, T> Not<TInner, T>(TInner inner)
        where TInner : struct, ISpecification<T> => new(inner);
}
```

---

## Source Generator

### Approach

Incremental Roslyn generator (`IIncrementalGenerator`) for performance — only re-runs on changed syntax nodes.

### Pipeline

```
SyntaxProvider
  → filter: struct declarations with [Specification] attribute
  → transform: extract type name, type parameter T, declared instance fields
  → emit: partial struct file with And/Or/Not methods + optional CachedExpression
```

### Generated Output (per spec)

For each `[Specification]`-attributed `partial struct`, the generator emits:

```csharp
public partial struct ActiveUserSpec
{
    public AndSpecification<ActiveUserSpec, TOther, T> And<TOther>(TOther other)
        where TOther : struct, ISpecification<T> => new(this, other);

    public OrSpecification<ActiveUserSpec, TOther, T> Or<TOther>(TOther other)
        where TOther : struct, ISpecification<T> => new(this, other);

    public NotSpecification<ActiveUserSpec, T> Not() => new(this);
}
```

### Stateless Expression Caching

The generator detects statelessness by inspecting declared instance fields. If none are present, it emits a static cached expression:

```csharp
public partial struct ActiveUserSpec
{
    private static readonly Expression<Func<T, bool>> _cachedExpression =
        new ActiveUserSpec().ToExpression();

    // ToExpression() override returns _cachedExpression directly
}
```

Stateful specs (with fields) build the expression per instance — no cache, since captured values differ per instance.

---

## Diagnostics

All contracts enforced at compile time:

| Code  | Condition                                        | Severity |
|-------|--------------------------------------------------|----------|
| ZA001 | `[Specification]` applied to a class, not struct | Error    |
| ZA002 | Struct does not implement `ISpecification<T>`    | Error    |
| ZA003 | Struct is not declared `partial`                 | Error    |
| ZA004 | Struct is not `readonly`                         | Warning  |

---

## Testing

```
ZeroAlloc.Specification.Tests/
  Unit/
    AndSpecificationTests.cs       ← IsSatisfiedBy correctness
    OrSpecificationTests.cs
    NotSpecificationTests.cs
    ExpressionCompositionTests.cs  ← expression tree structure
    CachingTests.cs                ← stateless specs return same expression instance
  Generator/
    GeneratorSnapshotTests.cs      ← Roslyn source generator snapshot tests
  Integration/
    EfCoreQueryTests.cs            ← specs translate to SQL via EF Core Sqlite
```

---

## Usage Example

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
    public Expression<Func<User, bool>> ToExpression() => u => u.TotalSpend >= _minSpend;
}

// Fluent:
var spec = new ActiveUserSpec().And(new PremiumUserSpec(1000m));

// Static builder:
var spec = Spec.And<ActiveUserSpec, PremiumUserSpec, User>(new(), new(1000m));

// Both produce: AndSpecification<ActiveUserSpec, PremiumUserSpec, User> — a struct, zero heap allocation

// ORM:
var users = dbContext.Users.Where(spec.ToExpression()).ToList();
```
