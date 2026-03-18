# Implicit Conversion to Expression — Design Document

**Date:** 2026-03-18

## Problem

Call sites that use specs with EF Core or any `IQueryable` provider must write `.Where(spec.ToExpression())`. The `.ToExpression()` call is boilerplate — the intent is always the same.

## Goal

Allow `.Where(spec)` directly by adding an implicit conversion operator from each spec type to `Expression<Func<T, bool>>`.

## Approach

Option A (chosen): add `public static implicit operator Expression<Func<T, bool>>` to each struct.

### Hand-written combinator structs

Add one implicit operator to each of the three built-in combinator structs:

```csharp
// AndSpecification<TLeft, TRight, T>
public static implicit operator Expression<Func<T, bool>>(AndSpecification<TLeft, TRight, T> spec)
    => spec.ToExpression();

// OrSpecification<TLeft, TRight, T>
public static implicit operator Expression<Func<T, bool>>(OrSpecification<TLeft, TRight, T> spec)
    => spec.ToExpression();

// NotSpecification<TInner, T>
public static implicit operator Expression<Func<T, bool>>(NotSpecification<TInner, T> spec)
    => spec.ToExpression();
```

### Source generator

Emit the same operator in the generated partial for every `[Specification]`-attributed struct:

```csharp
public static implicit operator global::System.Linq.Expressions.Expression<global::System.Func<T, bool>>(MySpec spec)
    => spec.ToExpression();
```

## Result

```csharp
// Before
var users = dbContext.Users.Where(spec.ToExpression()).ToListAsync();

// After
var users = dbContext.Users.Where(spec).ToListAsync();
```

Both composed and user-defined specs support the conversion. No interface changes. Fully backwards compatible.

## Testing

- Unit: verify implicit conversion returns same instance as `ToExpression()` for stateless specs
- Integration: `EfCoreQueryTests` updated to use `.Where(spec)` directly (proves EF Core translates correctly)
