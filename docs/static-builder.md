---
id: static-builder
title: Static Builder
sidebar_label: Static Builder
sidebar_position: 5
---

# Static Builder

The `Spec` static class provides an alternative to the fluent API when you prefer explicit type arguments or are composing specs from two separate variables.

## API

```csharp
public static class Spec
{
    public static AndSpecification<TLeft, TRight, T> And<TLeft, TRight, T>(TLeft left, TRight right)
        where TLeft : struct, ISpecification<T>
        where TRight : struct, ISpecification<T>;

    public static OrSpecification<TLeft, TRight, T> Or<TLeft, TRight, T>(TLeft left, TRight right)
        where TLeft : struct, ISpecification<T>
        where TRight : struct, ISpecification<T>;

    public static NotSpecification<TInner, T> Not<TInner, T>(TInner inner)
        where TInner : struct, ISpecification<T>;
}
```

## Usage

```csharp
var spec = Spec.And<ActiveUserSpec, PremiumUserSpec, User>(new(), new(1000m));
var orSpec = Spec.Or<ActiveUserSpec, PremiumUserSpec, User>(new(), new(500m));
var notSpec = Spec.Not<ActiveUserSpec, User>(new());
```

## Fluent vs Static Builder

Both APIs produce the same struct types:

```csharp
// Fluent
var a = new ActiveUserSpec().And(new PremiumUserSpec(1000m));
// type: AndSpecification<ActiveUserSpec, PremiumUserSpec, User>

// Static builder
var b = Spec.And<ActiveUserSpec, PremiumUserSpec, User>(new(), new(1000m));
// type: AndSpecification<ActiveUserSpec, PremiumUserSpec, User>
```

Use the static builder when:
- You need explicit type arguments for disambiguation
- You are composing two existing spec variables
- You prefer a functional style over method chaining
