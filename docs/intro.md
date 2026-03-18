---
id: intro
title: Introduction
sidebar_label: Introduction
sidebar_position: 1
slug: /
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
