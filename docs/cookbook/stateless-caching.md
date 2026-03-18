---
id: stateless-caching
title: Stateless Expression Caching
sidebar_label: Stateless Caching
sidebar_position: 3
---

# Stateless Expression Caching

For stateless specifications (no instance fields), the expression is always the same. Caching it as a static field avoids rebuilding the expression tree on every call.

## Pattern

```csharp
[Specification]
public readonly partial struct ActiveUserSpec : ISpecification<User>
{
    // Cache the expression tree — built once, reused forever
    private static readonly Expression<Func<User, bool>> _cachedExpression =
        new ActiveUserSpec().ToExpression();

    public bool IsSatisfiedBy(User user) => user.IsActive;

    public Expression<Func<User, bool>> ToExpression() => _cachedExpression;
}
```

## Why This Works

`ActiveUserSpec` has no instance fields, so `ToExpression()` always returns the same logical expression. The static field ensures it is built exactly once per AppDomain.

Note: the static field is a cached delegate/expression, not a captured instance value — so this does not make the spec "stateful" in the sense that matters for composition.

## Verifying the Cache

The expression returned by `ToExpression()` should be reference-equal across calls:

```csharp
var spec1 = new ActiveUserSpec();
var spec2 = new ActiveUserSpec();

Assert.Same(spec1.ToExpression(), spec2.ToExpression()); // ✅
```

## When NOT to Cache

Stateful specs must NOT use this pattern — each instance has different captured values:

```csharp
[Specification]
public readonly partial struct PremiumUserSpec : ISpecification<User>
{
    private readonly decimal _minSpend; // different per instance!

    // ❌ Do not cache — each instance has a different _minSpend
    public Expression<Func<User, bool>> ToExpression()
    {
        var min = _minSpend;
        return u => u.TotalSpend >= min; // captures the specific min value
    }
}
```
