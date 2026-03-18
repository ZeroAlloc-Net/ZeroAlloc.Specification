---
id: diagnostics
title: Diagnostics
sidebar_label: Diagnostics
sidebar_position: 7
---

# Diagnostics

The source generator enforces correct usage at compile time via four diagnostic codes.

## Diagnostic Table

| Code | Severity | Condition | Remediation |
|------|----------|-----------|-------------|
| ZA001 | Error | `[Specification]` applied to a class | Change `class` to `struct` |
| ZA002 | Error | Struct does not implement `ISpecification<T>` | Add `: ISpecification<YourType>` |
| ZA003 | Error | Struct is not declared `partial` | Add `partial` modifier |
| ZA004 | Warning | Struct is not declared `readonly` | Add `readonly` modifier |

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
