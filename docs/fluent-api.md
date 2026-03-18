---
id: fluent-api
title: Fluent API
sidebar_label: Fluent API
sidebar_position: 4
---

# Fluent API

The source generator adds three methods to every `[Specification]`-attributed struct:

## Generated Methods

```csharp
// For a spec: public readonly partial struct MySpec : ISpecification<T>

public AndSpecification<MySpec, TOther, T> And<TOther>(TOther other)
    where TOther : struct, ISpecification<T>;

public OrSpecification<MySpec, TOther, T> Or<TOther>(TOther other)
    where TOther : struct, ISpecification<T>;

public NotSpecification<MySpec, T> Not();
```

## Usage

```csharp
// AND — both must be satisfied
var activeAndPremium = new ActiveUserSpec().And(new PremiumUserSpec(1000m));

// OR — either must be satisfied
var activeOrPremium = new ActiveUserSpec().Or(new PremiumUserSpec(500m));

// NOT — must not be satisfied
var notActive = new ActiveUserSpec().Not();

// Chaining
var spec = new ActiveUserSpec()
    .And(new PremiumUserSpec(1000m))
    .Or(new AdminUserSpec());
```

## Return Types

Every method returns a concrete struct — no boxing, no heap allocation:

| Method | Return type |
|--------|------------|
| `.And(other)` | `AndSpecification<MySpec, TOther, T>` |
| `.Or(other)` | `OrSpecification<MySpec, TOther, T>` |
| `.Not()` | `NotSpecification<MySpec, T>` |

Because return types are concrete structs, chained compositions also type-check at compile time. Chaining two `And` calls produces `AndSpecification<AndSpecification<A, B, T>, C, T>` — the type grows but remains a struct at every level.
