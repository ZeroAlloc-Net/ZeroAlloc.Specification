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
