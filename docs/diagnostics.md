---
id: diagnostics
title: Diagnostics
sidebar_label: Diagnostics
sidebar_position: 7
---

# Diagnostics

The source generator enforces correct usage at compile time via five diagnostic codes.

## Diagnostic Table

| Code | Severity | Condition | Remediation |
|------|----------|-----------|-------------|
| ZA001 | Error | `[Specification]` applied to a class | Change `class` to `struct` |
| ZA002 | Error | Struct does not implement `ISpecification<T>` | Add `: ISpecification<YourType>` |
| ZA003 | Error | Struct is not declared `partial` | Add `partial` modifier |
| ZA004 | Warning | Struct is not declared `readonly` | Add `readonly` modifier |
| ZA005 | Warning | Nested struct inside a containing type that is not `partial` | Make every containing type `partial` |

## ZA001 — Not a struct

```csharp
// ❌ Error ZA001
[Specification]
public class ActiveUserSpec : ISpecification<User> { ... }

// ✅ Fix
[Specification]
public readonly partial struct ActiveUserSpec : ISpecification<User> { ... }
```

## ZA002 — Missing ISpecification&lt;T&gt;

```csharp
// ❌ Error ZA002
[Specification]
public readonly partial struct ActiveUserSpec { ... }

// ✅ Fix
[Specification]
public readonly partial struct ActiveUserSpec : ISpecification<User> { ... }
```

## ZA003 — Not partial

```csharp
// ❌ Error ZA003
[Specification]
public readonly struct ActiveUserSpec : ISpecification<User> { ... }

// ✅ Fix
[Specification]
public readonly partial struct ActiveUserSpec : ISpecification<User> { ... }
```

## ZA004 — Not readonly (Warning)

```csharp
// ⚠️ Warning ZA004
[Specification]
public partial struct ActiveUserSpec : ISpecification<User> { ... }

// ✅ Fix
[Specification]
public readonly partial struct ActiveUserSpec : ISpecification<User> { ... }
```

`readonly` prevents the compiler from emitting defensive copies when passing the struct by value, which is important for performance.

## ZA005 — Containing type not partial (Warning)

**Message**: `Specification 'App.Rules.ActiveUserSpec' is not generated because its containing type 'App.Rules' is not partial`.

A specification nested in another type is generated inside a partial declaration of every containing type, so each of them must be `partial`. When one is not, the generator reports ZA005 on the specification and generates nothing for it, so `And`, `Or`, `Not` and the implicit `Expression` conversion do not exist. The warning names the outermost containing type that is not `partial`.

Earlier versions generated such a specification into a new struct at the top of the namespace, which did not compile.

```csharp
// ⚠️ Warning ZA005
public static class Rules
{
    [Specification]
    public readonly partial struct ActiveUserSpec : ISpecification<User> { ... }
}

// ✅ Fix
public static partial class Rules
{
    [Specification]
    public readonly partial struct ActiveUserSpec : ISpecification<User> { ... }
}
```

Moving the specification to the top of a namespace also fixes it.
