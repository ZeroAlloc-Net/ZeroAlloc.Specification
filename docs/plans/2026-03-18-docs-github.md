# Docs, README, and GitHub Infrastructure Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add Docusaurus-ready markdown documentation, a polished README matching ZeroAlloc.Mediator style, and a complete `.github/` CI/release/issue-template infrastructure.

**Architecture:** Plain markdown files with Docusaurus frontmatter (portable, no build tooling in this repo). GitHub Actions for CI (commitlint + build/test) and release (release-please + NuGet publish). All files committed individually per task.

**Tech Stack:** Markdown, Docusaurus frontmatter, GitHub Actions, release-please-action@v4, wagoid/commitlint-github-action, NuGet

---

### Task 1: Directory.Build.props — centralize build properties

**Files:**
- Create: `Directory.Build.props`
- Modify: `src/ZeroAlloc.Specification/ZeroAlloc.Specification.csproj` (remove duplicated props)
- Modify: `src/ZeroAlloc.Specification.Generator/ZeroAlloc.Specification.Generator.csproj` (remove duplicated props)

**Step 1: Create `Directory.Build.props` at the solution root**

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <LangVersion>12</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

> Note: `TreatWarningsAsErrors` is fine for the core lib and tests. The generator project has `EnforceExtendedAnalyzerRules>true` which must stay in its own csproj.
> The generator targets `netstandard2.0` which doesn't support `ImplicitUsings` — keep `ImplicitUsings` out of the generator's props by NOT setting it globally, or set it only for net8.0 TFMs. Safest: only centralize `Nullable`, `LangVersion`, and `TreatWarningsAsErrors` — leave `ImplicitUsings` in the individual csproj files that need it.

Revised `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <LangVersion>12</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

**Step 2: Remove `Nullable` and `LangVersion` from `src/ZeroAlloc.Specification/ZeroAlloc.Specification.csproj`**

Before:
```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <Nullable>enable</Nullable>
  <LangVersion>12</LangVersion>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

After:
```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

**Step 3: Remove `Nullable` and `LangVersion` from `src/ZeroAlloc.Specification.Generator/ZeroAlloc.Specification.Generator.csproj`**

Before:
```xml
<PropertyGroup>
  <TargetFramework>netstandard2.0</TargetFramework>
  <Nullable>enable</Nullable>
  <LangVersion>12</LangVersion>
  <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
</PropertyGroup>
```

After:
```xml
<PropertyGroup>
  <TargetFramework>netstandard2.0</TargetFramework>
  <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
</PropertyGroup>
```

**Step 4: Build to verify nothing broke**

```bash
cd c:/Projects/Prive/ZeroAlloc.Specification
dotnet build --configuration Release
```

Expected: `Build succeeded. 0 Warning(s). 0 Error(s).`

**Step 5: Run tests**

```bash
dotnet test --configuration Release --no-build
```

Expected: all tests pass.

**Step 6: Commit**

```bash
git add Directory.Build.props src/ZeroAlloc.Specification/ZeroAlloc.Specification.csproj src/ZeroAlloc.Specification.Generator/ZeroAlloc.Specification.Generator.csproj
git commit -m "build: centralize Nullable/LangVersion in Directory.Build.props"
```

---

### Task 2: `docs/intro.md`

**Files:**
- Create: `docs/intro.md`

**Step 1: Create `docs/intro.md`**

```markdown
---
id: intro
title: Introduction
sidebar_label: Introduction
sidebar_position: 1
---

# ZeroAlloc.Specification

ZeroAlloc.Specification is a source-generated, zero-allocation implementation of the [Specification pattern](https://en.wikipedia.org/wiki/Specification_pattern) for .NET 8+.

## Why ZeroAlloc.Specification?

The classic DDD Specification pattern allocates heap objects on every composition — `And`, `Or`, and `Not` return new wrapper class instances, and delegate-based predicates create closures. In hot paths this adds GC pressure.

ZeroAlloc.Specification uses a Roslyn incremental source generator to emit `And<TOther>()`, `Or<TOther>()`, and `Not()` methods directly onto your spec struct. Composed types are concrete `readonly struct` types — no heap allocation, no closures.

## Key Properties

- **Zero allocations** — composed specs are structs, not classes
- **EF Core compatible** — every spec exposes `ToExpression()` returning `Expression<Func<T, bool>>`
- **Compile-time safety** — ZA001–ZA004 diagnostics enforce correct usage
- **Familiar API** — fluent (`.And()`, `.Or()`, `.Not()`) and static builder (`Spec.And()`, `Spec.Or()`, `Spec.Not()`)
- **.NET 8+** — uses C# 12 features throughout

## Packages

| Package | Purpose |
|---------|---------|
| `ZeroAlloc.Specification` | Core interfaces and combinator structs |
| `ZeroAlloc.Specification.Generator` | Roslyn source generator (auto-referenced) |
```

**Step 2: Commit**

```bash
git add docs/intro.md
git commit -m "docs: add intro page"
```

---

### Task 3: `docs/getting-started.md`

**Files:**
- Create: `docs/getting-started.md`

**Step 1: Create `docs/getting-started.md`**

```markdown
---
id: getting-started
title: Getting Started
sidebar_label: Getting Started
sidebar_position: 2
---

# Getting Started

## Installation

```bash
dotnet add package ZeroAlloc.Specification
dotnet add package ZeroAlloc.Specification.Generator
```

The generator package is a Roslyn analyzer/source generator. It is automatically applied at compile time — no runtime dependency.

## Your First Specification

### 1. Define a spec struct

```csharp
using ZeroAlloc.Specification;
using System.Linq.Expressions;

[Specification]
public readonly partial struct ActiveUserSpec : ISpecification<User>
{
    public bool IsSatisfiedBy(User user) => user.IsActive;
    public Expression<Func<User, bool>> ToExpression() => u => u.IsActive;
}
```

The `[Specification]` attribute and `partial` keyword are both required. The generator adds fluent composition methods to your struct.

### 2. Use it in memory

```csharp
var spec = new ActiveUserSpec();
bool result = spec.IsSatisfiedBy(user); // true/false
```

### 3. Compose specifications

```csharp
[Specification]
public readonly partial struct PremiumUserSpec : ISpecification<User>
{
    private readonly decimal _minSpend;
    public PremiumUserSpec(decimal minSpend) => _minSpend = minSpend;

    public bool IsSatisfiedBy(User user) => user.TotalSpend >= _minSpend;
    public Expression<Func<User, bool>> ToExpression()
    {
        var min = _minSpend; // copy field to local for lambda capture
        return u => u.TotalSpend >= min;
    }
}

// Fluent:
var spec = new ActiveUserSpec().And(new PremiumUserSpec(1000m));

// Static builder:
var spec = Spec.And<ActiveUserSpec, PremiumUserSpec, User>(new(), new(1000m));
```

Both produce `AndSpecification<ActiveUserSpec, PremiumUserSpec, User>` — a struct, zero heap allocation.

### 4. Query with EF Core

```csharp
var users = await dbContext.Users
    .Where(spec.ToExpression())
    .ToListAsync();
```

EF Core translates the composed expression tree to SQL.

## Next Steps

- [Core Concepts](core-concepts) — understand struct constraints and zero-allocation guarantees
- [Fluent API](fluent-api) — all generated methods
- [Diagnostics](diagnostics) — fix ZA001–ZA004 compile errors
```

**Step 2: Commit**

```bash
git add docs/getting-started.md
git commit -m "docs: add getting-started page"
```

---

### Task 4: `docs/core-concepts.md`

**Files:**
- Create: `docs/core-concepts.md`

**Step 1: Create `docs/core-concepts.md`**

```markdown
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

public readonly struct OrSpecification<TLeft, TRight, T> : ISpecification<T>;
public readonly struct NotSpecification<TInner, T> : ISpecification<T>;
```

These are hand-written and reused for all compositions. The generator does not emit new combinator types — it only adds fluent methods that construct instances of these combinators.
```

**Step 2: Commit**

```bash
git add docs/core-concepts.md
git commit -m "docs: add core-concepts page"
```

---

### Task 5: `docs/fluent-api.md`, `docs/static-builder.md`, `docs/expression-composition.md`

**Files:**
- Create: `docs/fluent-api.md`
- Create: `docs/static-builder.md`
- Create: `docs/expression-composition.md`

**Step 1: Create `docs/fluent-api.md`**

```markdown
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

Because return types are concrete structs, chained compositions also type-check at compile time.
```

**Step 2: Create `docs/static-builder.md`**

```markdown
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
- You're composing two existing spec variables
- You prefer a functional style over method chaining
```

**Step 3: Create `docs/expression-composition.md`**

```markdown
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
```

**Step 4: Commit**

```bash
git add docs/fluent-api.md docs/static-builder.md docs/expression-composition.md
git commit -m "docs: add fluent-api, static-builder, and expression-composition pages"
```

---

### Task 6: `docs/diagnostics.md`, `docs/generator-internals.md`, `docs/performance.md`

**Files:**
- Create: `docs/diagnostics.md`
- Create: `docs/generator-internals.md`
- Create: `docs/performance.md`

**Step 1: Create `docs/diagnostics.md`**

```markdown
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
```

**Step 2: Create `docs/generator-internals.md`**

```markdown
---
id: generator-internals
title: Generator Internals
sidebar_label: Generator Internals
sidebar_position: 8
---

# Generator Internals

This page describes how the Roslyn incremental source generator works. You don't need to understand this to use the library — it's here for contributors and the curious.

## Architecture

The generator uses the Roslyn `IIncrementalGenerator` API, which only re-runs on changed syntax nodes (incremental caching).

### Pipeline

```
SyntaxProvider
  → ForAttributeWithMetadataName("ZeroAlloc.Specification.SpecificationAttribute")
  → filter: node is StructDeclarationSyntax
  → transform: extract SpecificationInfo (name, namespace, type parameter, accessibility)
  → emit: partial struct file with And/Or/Not methods
```

A second pipeline handles ZA001 (non-struct types):

```
SyntaxProvider
  → ForAttributeWithMetadataName("ZeroAlloc.Specification.SpecificationAttribute")
  → filter: node is NOT StructDeclarationSyntax
  → emit: ZA001 diagnostic
```

### SpecificationInfo

The `SpecificationInfo` class captures what the generator needs per spec:

```csharp
internal sealed class SpecificationInfo
{
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string TypeParameterName { get; set; }
    public string Accessibility { get; set; }
    public Location Location { get; set; } // excluded from Equals/GetHashCode
    public bool ImplementsInterface { get; set; }
    public bool IsPartial { get; set; }
    public bool IsReadonly { get; set; }
}
```

`Location` is excluded from `Equals`/`GetHashCode` so that editing a spec's location (e.g., adding a blank line) doesn't trigger unnecessary generator re-runs.

## Generated Output

For `public readonly partial struct ActiveUserSpec : ISpecification<User>`, the generator emits:

```csharp
// <auto-generated/>
#nullable enable

namespace MyApp
{
    public readonly partial struct ActiveUserSpec
    {
        public global::ZeroAlloc.Specification.AndSpecification<ActiveUserSpec, TOther, User> And<TOther>(TOther other)
            where TOther : struct, global::ZeroAlloc.Specification.ISpecification<User>
            => new(this, other);

        public global::ZeroAlloc.Specification.OrSpecification<ActiveUserSpec, TOther, User> Or<TOther>(TOther other)
            where TOther : struct, global::ZeroAlloc.Specification.ISpecification<User>
            => new(this, other);

        public global::ZeroAlloc.Specification.NotSpecification<ActiveUserSpec, User> Not()
            => new(this);
    }
}
```

The accessibility modifier (`public`/`internal`) matches the user's declaration.

## Diagnostics Enforcement

All four diagnostics are enforced in the transform step before code emission. If a diagnostic is reported, the generator does not emit any code for that type.
```

**Step 3: Create `docs/performance.md`**

```markdown
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
```

**Step 4: Commit**

```bash
git add docs/diagnostics.md docs/generator-internals.md docs/performance.md
git commit -m "docs: add diagnostics, generator-internals, and performance pages"
```

---

### Task 7: Cookbook recipes

**Files:**
- Create: `docs/cookbook/ef-core-repository.md`
- Create: `docs/cookbook/combining-specs.md`
- Create: `docs/cookbook/stateless-caching.md`

**Step 1: Create `docs/cookbook/ef-core-repository.md`**

```markdown
---
id: ef-core-repository
title: EF Core Repository Pattern
sidebar_label: EF Core Repository
sidebar_position: 1
---

# EF Core Repository Pattern

This recipe shows how to use ZeroAlloc.Specification inside an EF Core repository.

## Setup

```csharp
// Specification
[Specification]
public readonly partial struct ActiveUserSpec : ISpecification<User>
{
    public bool IsSatisfiedBy(User user) => user.IsActive;
    public Expression<Func<User, bool>> ToExpression() => u => u.IsActive;
}

[Specification]
public readonly partial struct PremiumUserSpec : ISpecification<User>
{
    private readonly decimal _minSpend;
    public PremiumUserSpec(decimal minSpend) => _minSpend = minSpend;

    public bool IsSatisfiedBy(User user) => user.TotalSpend >= _minSpend;
    public Expression<Func<User, bool>> ToExpression()
    {
        var min = _minSpend;
        return u => u.TotalSpend >= min;
    }
}
```

## Generic Repository

```csharp
public class Repository<T> where T : class
{
    private readonly DbContext _db;

    public Repository(DbContext db) => _db = db;

    public Task<List<T>> FindAsync<TSpec>(TSpec spec, CancellationToken ct = default)
        where TSpec : struct, ISpecification<T>
        => _db.Set<T>().Where(spec.ToExpression()).ToListAsync(ct);

    public Task<int> CountAsync<TSpec>(TSpec spec, CancellationToken ct = default)
        where TSpec : struct, ISpecification<T>
        => _db.Set<T>().CountAsync(spec.ToExpression(), ct);
}
```

## Usage

```csharp
var repo = new Repository<User>(dbContext);

// Single spec
var activeUsers = await repo.FindAsync(new ActiveUserSpec());

// Composed spec
var spec = new ActiveUserSpec().And(new PremiumUserSpec(1000m));
var premiumActive = await repo.FindAsync(spec);
```

The generic constraint `where TSpec : struct, ISpecification<T>` ensures the spec is a struct (zero allocation) and implements the interface.
```

**Step 2: Create `docs/cookbook/combining-specs.md`**

```markdown
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
var spec = new ActiveUserSpec().And(new PremiumUserSpec(1000m));

// OR: user is active OR is an admin
var spec = new ActiveUserSpec().Or(new AdminUserSpec());

// NOT: user is not active
var spec = new ActiveUserSpec().Not();
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
```

**Step 3: Create `docs/cookbook/stateless-caching.md`**

```markdown
---
id: stateless-caching
title: Stateless Expression Caching
sidebar_label: Stateless Caching
sidebar_position: 3
---

# Stateless Expression Caching

For stateless specifications (no instance fields), the expression is always the same. Caching it avoids rebuilding the expression tree on every call.

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

`ActiveUserSpec` has no instance fields, so `ToExpression()` always returns the same logical expression. The static field ensures it's built exactly once per AppDomain.

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
```

**Step 4: Commit**

```bash
git add docs/cookbook/
git commit -m "docs: add cookbook recipes for EF Core, combining specs, and stateless caching"
```

---

### Task 8: `README.md`

**Files:**
- Create: `README.md`

**Step 1: Create `README.md`**

Check the ZeroAlloc.Mediator README structure for badge and section conventions before writing. The README should have:
1. Title + one-liner
2. Badges: NuGet (core), NuGet (generator), build status, license
3. Install section (CLI commands)
4. Quick example (full working spec + composition + EF Core)
5. Features list
6. Documentation table
7. License

```markdown
# ZeroAlloc.Specification

Source-generated, zero-allocation specification pattern for .NET 8+.

[![NuGet](https://img.shields.io/nuget/v/ZeroAlloc.Specification.svg)](https://www.nuget.org/packages/ZeroAlloc.Specification)
[![NuGet](https://img.shields.io/nuget/v/ZeroAlloc.Specification.Generator.svg?label=ZeroAlloc.Specification.Generator)](https://www.nuget.org/packages/ZeroAlloc.Specification.Generator)
[![Build](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/actions/workflows/ci.yml/badge.svg)](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## Install

```bash
dotnet add package ZeroAlloc.Specification
dotnet add package ZeroAlloc.Specification.Generator
```

## Example

```csharp
[Specification]
public readonly partial struct ActiveUserSpec : ISpecification<User>
{
    public bool IsSatisfiedBy(User user) => user.IsActive;
    public Expression<Func<User, bool>> ToExpression() => u => u.IsActive;
}

[Specification]
public readonly partial struct PremiumUserSpec : ISpecification<User>
{
    private readonly decimal _minSpend;
    public PremiumUserSpec(decimal minSpend) => _minSpend = minSpend;

    public bool IsSatisfiedBy(User user) => user.TotalSpend >= _minSpend;
    public Expression<Func<User, bool>> ToExpression()
    {
        var min = _minSpend;
        return u => u.TotalSpend >= min;
    }
}

// Fluent composition — zero allocation
var spec = new ActiveUserSpec().And(new PremiumUserSpec(1000m));

// In-memory
bool result = spec.IsSatisfiedBy(user);

// EF Core — translates to SQL
var users = await dbContext.Users.Where(spec.ToExpression()).ToListAsync();
```

## Features

- **Zero allocations** — composed specs are `readonly struct` values, not heap objects
- **EF Core compatible** — every spec exposes `ToExpression()` returning `Expression<Func<T, bool>>`
- **Source-generated fluent API** — `And<TOther>()`, `Or<TOther>()`, `Not()` added by Roslyn generator
- **Static builder** — `Spec.And()`, `Spec.Or()`, `Spec.Not()` for explicit type arguments
- **Compile-time safety** — ZA001–ZA004 diagnostics enforce correct `partial struct` usage
- **.NET 8+, C# 12**

## Documentation

| Page | Description |
|------|-------------|
| [Introduction](docs/intro.md) | What it is and why it exists |
| [Getting Started](docs/getting-started.md) | Install and first specification in 5 minutes |
| [Core Concepts](docs/core-concepts.md) | ISpecification&lt;T&gt;, structs, stateful vs stateless |
| [Fluent API](docs/fluent-api.md) | Generated And/Or/Not methods |
| [Static Builder](docs/static-builder.md) | Spec.And/Or/Not |
| [Expression Composition](docs/expression-composition.md) | ToExpression() and EF Core translation |
| [Diagnostics](docs/diagnostics.md) | ZA001–ZA004 compile-time errors and fixes |
| [Generator Internals](docs/generator-internals.md) | How the Roslyn generator works |
| [Performance](docs/performance.md) | Allocation comparison and benchmarks |
| [Cookbook: EF Core Repository](docs/cookbook/ef-core-repository.md) | Repository pattern integration |
| [Cookbook: Combining Specs](docs/cookbook/combining-specs.md) | Complex compositions |
| [Cookbook: Stateless Caching](docs/cookbook/stateless-caching.md) | Cache expression trees for stateless specs |

## License

MIT — see [LICENSE](LICENSE).
```

**Step 2: Create `LICENSE`** (MIT, year 2026, organization ZeroAlloc-Net)

```
MIT License

Copyright (c) 2026 ZeroAlloc-Net

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

**Step 3: Commit**

```bash
git add README.md LICENSE
git commit -m "docs: add README and LICENSE"
```

---

### Task 9: `.github/ISSUE_TEMPLATE/` and `PULL_REQUEST_TEMPLATE.md`

**Files:**
- Create: `.github/ISSUE_TEMPLATE/bug_report.yml`
- Create: `.github/ISSUE_TEMPLATE/feature_request.yml`
- Create: `.github/PULL_REQUEST_TEMPLATE.md`

**Step 1: Create `.github/ISSUE_TEMPLATE/bug_report.yml`**

```yaml
name: Bug Report
description: Report a bug in ZeroAlloc.Specification
labels: ["bug"]
body:
  - type: markdown
    attributes:
      value: |
        Thanks for taking the time to report a bug. Please fill out the sections below.

  - type: input
    id: version
    attributes:
      label: Package version
      placeholder: e.g. 0.1.0
    validations:
      required: true

  - type: input
    id: dotnet-version
    attributes:
      label: .NET version
      placeholder: e.g. .NET 8.0.100
    validations:
      required: true

  - type: textarea
    id: description
    attributes:
      label: Description
      description: What happened? What did you expect?
    validations:
      required: true

  - type: textarea
    id: repro
    attributes:
      label: Minimal reproduction
      description: Minimal code to reproduce the issue
      render: csharp
    validations:
      required: true

  - type: textarea
    id: additional
    attributes:
      label: Additional context
      description: Logs, screenshots, or anything else useful
```

**Step 2: Create `.github/ISSUE_TEMPLATE/feature_request.yml`**

```yaml
name: Feature Request
description: Suggest a new feature or improvement
labels: ["enhancement"]
body:
  - type: markdown
    attributes:
      value: |
        Thanks for the suggestion! Please describe what you'd like and why.

  - type: textarea
    id: problem
    attributes:
      label: Problem
      description: What problem does this solve? What can't you do today?
    validations:
      required: true

  - type: textarea
    id: solution
    attributes:
      label: Proposed solution
      description: What would you like to see?
    validations:
      required: true

  - type: textarea
    id: alternatives
    attributes:
      label: Alternatives considered
      description: Other approaches you've tried or considered

  - type: textarea
    id: additional
    attributes:
      label: Additional context
```

**Step 3: Create `.github/PULL_REQUEST_TEMPLATE.md`**

```markdown
## Summary

<!-- What does this PR do? Why? -->

## Changes

<!-- List the key changes -->

## Checklist

- [ ] Tests added or updated
- [ ] Docs updated (if API or behavior changed)
- [ ] Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/)
- [ ] `dotnet build` passes with no warnings
- [ ] `dotnet test` passes
```

**Step 4: Commit**

```bash
git add .github/ISSUE_TEMPLATE/ .github/PULL_REQUEST_TEMPLATE.md
git commit -m "ci: add issue templates and PR template"
```

---

### Task 10: `.github/workflows/ci.yml`

**Files:**
- Create: `.github/workflows/ci.yml`

**Step 1: Create `.github/workflows/ci.yml`**

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:

jobs:
  commitlint:
    name: Commitlint
    runs-on: ubuntu-latest
    if: github.event_name == 'pull_request'
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      - uses: wagoid/commitlint-github-action@v6
        with:
          configFile: .commitlintrc.yml

  build:
    name: Build & Test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Test
        run: dotnet test --configuration Release --no-build --verbosity normal
```

**Step 2: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add CI workflow (commitlint + build + test)"
```

---

### Task 11: `.github/workflows/release.yml` and release-please config

**Files:**
- Create: `.github/workflows/release.yml`
- Create: `release-please-config.json`
- Create: `.release-please-manifest.json`
- Create: `.commitlintrc.yml`

**Step 1: Create `.commitlintrc.yml`**

```yaml
extends:
  - '@commitlint/config-conventional'
```

**Step 2: Create `release-please-config.json`**

```json
{
  "$schema": "https://raw.githubusercontent.com/googleapis/release-please/main/schemas/config.json",
  "release-type": "simple",
  "packages": {
    "src/ZeroAlloc.Specification": {
      "release-type": "simple",
      "package-name": "ZeroAlloc.Specification",
      "changelog-path": "CHANGELOG.md"
    },
    "src/ZeroAlloc.Specification.Generator": {
      "release-type": "simple",
      "package-name": "ZeroAlloc.Specification.Generator",
      "changelog-path": "CHANGELOG.md"
    }
  }
}
```

**Step 3: Create `.release-please-manifest.json`**

```json
{
  "src/ZeroAlloc.Specification": "0.1.0",
  "src/ZeroAlloc.Specification.Generator": "0.1.0"
}
```

**Step 4: Create `.github/workflows/release.yml`**

The release workflow:
- Runs `release-please-action@v4` on every push to `main`
- When a release is created, builds, packs, and publishes both NuGet packages

```yaml
name: Release

on:
  push:
    branches: [main]

permissions:
  contents: write
  pull-requests: write

jobs:
  release-please:
    name: Release Please
    runs-on: ubuntu-latest
    outputs:
      releases_created: ${{ steps.release.outputs.releases_created }}
      tag_name: ${{ steps.release.outputs.tag_name }}
    steps:
      - uses: googleapis/release-please-action@v4
        id: release
        with:
          token: ${{ secrets.GITHUB_TOKEN }}
          config-file: release-please-config.json
          manifest-file: .release-please-manifest.json

  publish:
    name: Publish NuGet
    needs: release-please
    if: ${{ needs.release-please.outputs.releases_created }}
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Pack ZeroAlloc.Specification
        run: dotnet pack src/ZeroAlloc.Specification/ZeroAlloc.Specification.csproj --configuration Release --no-build --output ./artifacts

      - name: Pack ZeroAlloc.Specification.Generator
        run: dotnet pack src/ZeroAlloc.Specification.Generator/ZeroAlloc.Specification.Generator.csproj --configuration Release --no-build --output ./artifacts

      - name: Push to NuGet
        run: dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
```

**Step 5: Commit**

```bash
git add .commitlintrc.yml release-please-config.json .release-please-manifest.json .github/workflows/release.yml
git commit -m "ci: add release-please workflow and commitlint config"
```

---

### Task 12: NuGet metadata in csproj files

The csproj files need NuGet metadata for `dotnet pack` to produce correct packages.

**Files:**
- Modify: `src/ZeroAlloc.Specification/ZeroAlloc.Specification.csproj`
- Modify: `src/ZeroAlloc.Specification.Generator/ZeroAlloc.Specification.Generator.csproj`

**Step 1: Add NuGet metadata to `Directory.Build.props`** (shared properties)

Add to `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <LangVersion>12</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>

    <!-- NuGet metadata -->
    <Authors>ZeroAlloc-Net</Authors>
    <Company>ZeroAlloc-Net</Company>
    <Copyright>Copyright © 2026 ZeroAlloc-Net</Copyright>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification</PackageProjectUrl>
    <RepositoryUrl>https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageTags>specification;ddd;source-generator;zero-allocation;efcore</PackageTags>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
  </PropertyGroup>
</Project>
```

**Step 2: Add package-specific metadata to `src/ZeroAlloc.Specification/ZeroAlloc.Specification.csproj`**

Add inside `<PropertyGroup>`:

```xml
<PackageId>ZeroAlloc.Specification</PackageId>
<Version>0.1.0</Version>
<Description>Source-generated, zero-allocation specification pattern for .NET 8+. Compose predicates as structs — no heap allocations, EF Core compatible.</Description>
<PackageReadmeFile>README.md</PackageReadmeFile>
```

Also add `<ItemGroup>` for README:

```xml
<ItemGroup>
  <None Include="..\..\README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

**Step 3: Add package-specific metadata to `src/ZeroAlloc.Specification.Generator/ZeroAlloc.Specification.Generator.csproj`**

Add inside `<PropertyGroup>`:

```xml
<PackageId>ZeroAlloc.Specification.Generator</PackageId>
<Version>0.1.0</Version>
<Description>Roslyn source generator for ZeroAlloc.Specification. Emits And/Or/Not fluent methods on [Specification]-attributed structs.</Description>
```

Also ensure the generator is packaged correctly as an analyzer:

```xml
<ItemGroup>
  <None Include="$(OutputPath)\$(AssemblyName).dll" Pack="true"
        PackagePath="analyzers/dotnet/cs" Visible="false" />
</ItemGroup>
```

**Step 4: Build and pack to verify**

```bash
dotnet build --configuration Release
dotnet pack src/ZeroAlloc.Specification/ZeroAlloc.Specification.csproj --configuration Release --no-build --output ./artifacts
dotnet pack src/ZeroAlloc.Specification.Generator/ZeroAlloc.Specification.Generator.csproj --configuration Release --no-build --output ./artifacts
ls ./artifacts/
```

Expected: two `.nupkg` files appear in `./artifacts/`.

**Step 5: Commit**

```bash
git add Directory.Build.props src/ZeroAlloc.Specification/ZeroAlloc.Specification.csproj src/ZeroAlloc.Specification.Generator/ZeroAlloc.Specification.Generator.csproj
git commit -m "build: add NuGet metadata for both packages"
```

---

### Task 13: Final verification

**Step 1: Full build**

```bash
cd c:/Projects/Prive/ZeroAlloc.Specification
dotnet build --configuration Release
```

Expected: `Build succeeded. 0 Warning(s). 0 Error(s).`

**Step 2: Full test run**

```bash
dotnet test --configuration Release --no-build --verbosity normal
```

Expected: all tests pass.

**Step 3: Verify pack**

```bash
dotnet pack --configuration Release --no-build --output ./artifacts
ls ./artifacts/
```

Expected: `ZeroAlloc.Specification.0.1.0.nupkg` and `ZeroAlloc.Specification.Generator.0.1.0.nupkg`.

**Step 4: Verify git log**

```bash
git log --oneline -15
```

Expected: all commits present with conventional commit messages.
