---
id: expression-composition
title: Expression Composition
sidebar_label: Expression Composition
sidebar_position: 6
---

# Expression Composition

`ToExpression()` returns an `Expression<Func<T, bool>>` that can be passed directly to EF Core, LINQ-to-SQL, or any other IQueryable provider.

## How It Works

When composing two expressions, both sides must share a single `ParameterExpression` object — EF Core requires this for correct SQL translation.

ZeroAlloc.Specification uses an internal `ParameterRebinder` (an `ExpressionVisitor`) to replace the right-hand expression's parameter with the left-hand expression's parameter before combining bodies:

```csharp
// AndSpecification<TLeft, TRight, T>.ToExpression()
var left = _left.ToExpression();
var right = _right.ToExpression();
var param = left.Parameters[0];
var rightBody = ParameterRebinder.ReplaceParameter(right.Body, right.Parameters[0], param);
return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, rightBody), param);
```

The resulting expression tree uses `Expression.AndAlso` / `Expression.OrElse` / `Expression.Not` — all translatable to SQL by EF Core.

## EF Core Usage

```csharp
var spec = new ActiveUserSpec().And(new PremiumUserSpec(1000m));

// Works with any IQueryable provider
var users = await dbContext.Users
    .Where(spec.ToExpression())
    .ToListAsync();
```

## Expression Tree Structure

For `ActiveUserSpec.And(PremiumUserSpec)`:

```
Lambda (u =>
  AndAlso(
    u.IsActive,
    u.TotalSpend >= 1000
  )
)
```

Both `u.IsActive` and `u.TotalSpend` reference the same `ParameterExpression` `u`.

## Stateless Expression Caching

For stateless specs (no instance fields), you can cache the expression as a static field:

```csharp
public readonly partial struct ActiveUserSpec : ISpecification<User>
{
    private static readonly Expression<Func<User, bool>> _cachedExpression =
        new ActiveUserSpec().ToExpression();

    public Expression<Func<User, bool>> ToExpression() => _cachedExpression;
}
```

This avoids rebuilding the expression tree on every call. See [Stateless Caching](cookbook/stateless-caching) for the full pattern.

## Stateful Expressions

For stateful specs, do not cache — the expression captures a specific value:

```csharp
public readonly partial struct PremiumUserSpec : ISpecification<User>
{
    private readonly decimal _minSpend;

    public Expression<Func<User, bool>> ToExpression()
    {
        var min = _minSpend; // copy field to local — required for struct lambdas
        return u => u.TotalSpend >= min;
    }
}
```
