---
id: combining-specs
title: Combining Specifications
sidebar_label: Combining Specs
sidebar_position: 2
---

# Combining Specifications

## Basic Composition

```csharp
// AND: user must be active AND spend >= 1000
var andSpec = new ActiveUserSpec().And(new PremiumUserSpec(1000m));

// OR: user is active OR is an admin
var orSpec = new ActiveUserSpec().Or(new AdminUserSpec());

// NOT: user is not active
var notSpec = new ActiveUserSpec().Not();
```

## Chaining

```csharp
// Active AND premium AND verified
var spec = new ActiveUserSpec()
    .And(new PremiumUserSpec(1000m))
    .And(new VerifiedUserSpec());
```

## Complex Combinations

```csharp
// (Active AND Premium) OR Admin
var premiumActive = new ActiveUserSpec().And(new PremiumUserSpec(1000m));
var spec = premiumActive.Or(new AdminUserSpec());
```

## Static Builder for Explicit Types

When type inference struggles with complex chains, use the static builder:

```csharp
var and = Spec.And<ActiveUserSpec, PremiumUserSpec, User>(new(), new(1000m));
var or = Spec.Or<AndSpecification<ActiveUserSpec, PremiumUserSpec, User>, AdminUserSpec, User>(and, new());
```

## In-Memory vs ORM

Every composed spec works both in-memory and with EF Core:

```csharp
var spec = new ActiveUserSpec().And(new PremiumUserSpec(1000m));

// In-memory
bool result = spec.IsSatisfiedBy(user);

// EF Core
var users = await dbContext.Users.Where(spec.ToExpression()).ToListAsync();
```
