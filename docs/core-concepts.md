---
id: core-concepts
title: Core Concepts
sidebar_label: Core Concepts
sidebar_position: 3
---

# Core Concepts

## ISpecification&lt;T&gt;

Every spec implements this interface:

```csharp
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T candidate);
    Expression<Func<T, bool>> ToExpression();
}
```

`IsSatisfiedBy` is for in-memory evaluation. `ToExpression` returns an expression tree for ORM translation.

## Why Structs?

Class-based specifications allocate on every composition:

```csharp
// Classic pattern — heap allocations
ISpecification<User> spec = new ActiveSpec().And(new PremiumSpec()); // new AndSpec() allocated
```

Struct-based specifications compose without allocation:

```csharp
// ZeroAlloc — no heap allocation
var spec = new ActiveUserSpec().And(new PremiumUserSpec(1000m));
// spec is AndSpecification<ActiveUserSpec, PremiumUserSpec, User> — a stack value
```

## The [Specification] Attribute

Marks a `partial struct` for source generation:

```csharp
[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public sealed class SpecificationAttribute : Attribute { }
```

The generator reads this attribute and emits `And<TOther>()`, `Or<TOther>()`, and `Not()` methods onto your struct.

## Required Modifiers

Your spec struct must be:

| Modifier | Required | Why |
|----------|----------|-----|
| `partial` | Yes | Generator adds a second partial declaration |
| `struct` | Yes | Only structs can be zero-allocation |
| `readonly` | Recommended | Prevents defensive copies (ZA004 warning if missing) |

## Stateful vs Stateless Specs

**Stateless** specs hold no instance fields — their expression is always the same:

```csharp
[Specification]
public readonly partial struct ActiveUserSpec : ISpecification<User>
{
    // No fields — expression never changes
    private static readonly Expression<Func<User, bool>> _cachedExpression =
        new ActiveUserSpec().ToExpression();

    public bool IsSatisfiedBy(User user) => user.IsActive;
    public Expression<Func<User, bool>> ToExpression() => _cachedExpression;
}
```

**Stateful** specs hold instance fields — their expression depends on captured values:

```csharp
[Specification]
public readonly partial struct PremiumUserSpec : ISpecification<User>
{
    private readonly decimal _minSpend; // captured value

    public PremiumUserSpec(decimal minSpend) => _minSpend = minSpend;

    public bool IsSatisfiedBy(User user) => user.TotalSpend >= _minSpend;
    public Expression<Func<User, bool>> ToExpression()
    {
        var min = _minSpend; // copy to local — struct lambda restriction
        return u => u.TotalSpend >= min;
    }
}
```

> **Struct lambda restriction:** Lambdas in structs cannot capture `this`. Always copy fields to locals before using them in expressions.

## Generic Combinator Structs

Composition is handled by three generic structs in the core library:

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

These are hand-written and reused for all compositions. The generator does not emit new combinator types — it only adds fluent methods that construct instances of these combinators.
