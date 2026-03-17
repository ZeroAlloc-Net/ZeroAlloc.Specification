# ZeroAlloc.Specification Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a source-generated Specification pattern library for .NET 8+ that composes predicates at compile time as concrete structs with zero closure allocations.

**Architecture:** Three projects — a core library (interfaces, combinator structs, static builder), an incremental Roslyn source generator (emits `And`/`Or`/`Not` instance methods on attributed partial structs), and a test project. Composed specifications are generic structs inlined at compile time; expressions are built per-instance for stateful specs and statically cached for stateless ones.

**Tech Stack:** .NET 8, C# 12, Roslyn incremental source generators (`Microsoft.CodeAnalysis.CSharp`), xUnit, FluentAssertions, Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing (snapshot tests), EF Core 8 + Sqlite (integration tests).

---

## Task 1: Solution & Project Scaffolding

**Files:**
- Create: `ZeroAlloc.Specification.sln`
- Create: `src/ZeroAlloc.Specification/ZeroAlloc.Specification.csproj`
- Create: `src/ZeroAlloc.Specification.Generator/ZeroAlloc.Specification.Generator.csproj`
- Create: `tests/ZeroAlloc.Specification.Tests/ZeroAlloc.Specification.Tests.csproj`

**Step 1: Create solution and projects**

```bash
cd c:/Projects/Prive/ZeroAlloc.Specification
dotnet new sln -n ZeroAlloc.Specification
dotnet new classlib -n ZeroAlloc.Specification -o src/ZeroAlloc.Specification --framework net8.0
dotnet new classlib -n ZeroAlloc.Specification.Generator -o src/ZeroAlloc.Specification.Generator --framework netstandard2.0
dotnet new xunit -n ZeroAlloc.Specification.Tests -o tests/ZeroAlloc.Specification.Tests --framework net8.0
dotnet sln add src/ZeroAlloc.Specification/ZeroAlloc.Specification.csproj
dotnet sln add src/ZeroAlloc.Specification.Generator/ZeroAlloc.Specification.Generator.csproj
dotnet sln add tests/ZeroAlloc.Specification.Tests/ZeroAlloc.Specification.Tests.csproj
```

**Step 2: Configure core library csproj**

Replace `src/ZeroAlloc.Specification/ZeroAlloc.Specification.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>12</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

**Step 3: Configure generator csproj**

Replace `src/ZeroAlloc.Specification.Generator/ZeroAlloc.Specification.Generator.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>12</LangVersion>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.9.2" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

**Step 4: Configure test csproj**

Replace `tests/ZeroAlloc.Specification.Tests/ZeroAlloc.Specification.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>12</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageReference Include="xunit" Version="2.7.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.XUnit" Version="1.1.2" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\ZeroAlloc.Specification\ZeroAlloc.Specification.csproj" />
    <ProjectReference Include="..\..\src\ZeroAlloc.Specification.Generator\ZeroAlloc.Specification.Generator.csproj"
                      OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

**Step 5: Wire generator into core library**

Add to `src/ZeroAlloc.Specification/ZeroAlloc.Specification.csproj`:

```xml
  <ItemGroup>
    <ProjectReference Include="..\ZeroAlloc.Specification.Generator\ZeroAlloc.Specification.Generator.csproj"
                      OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>
```

**Step 6: Delete generated placeholder files**

```bash
rm src/ZeroAlloc.Specification/Class1.cs
rm src/ZeroAlloc.Specification.Generator/Class1.cs
rm tests/ZeroAlloc.Specification.Tests/UnitTest1.cs
```

**Step 7: Verify solution builds**

```bash
dotnet build ZeroAlloc.Specification.sln
```

Expected: Build succeeded, 0 errors.

**Step 8: Commit**

```bash
git init
git add .
git commit -m "chore: scaffold solution with core, generator, and test projects"
```

---

## Task 2: Core Interface & Attribute

**Files:**
- Create: `src/ZeroAlloc.Specification/ISpecification.cs`
- Create: `src/ZeroAlloc.Specification/SpecificationAttribute.cs`

**Step 1: Write failing test for interface contract**

Create `tests/ZeroAlloc.Specification.Tests/Unit/ISpecificationTests.cs`:

```csharp
using System.Linq.Expressions;
using FluentAssertions;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Tests.Unit;

public class ISpecificationTests
{
    [Fact]
    public void IsSatisfiedBy_ReturnsTrueWhenPredicateMatches()
    {
        var spec = new AlwaysTrueSpec();
        spec.IsSatisfiedBy(42).Should().BeTrue();
    }

    [Fact]
    public void ToExpression_ReturnsCompilableExpression()
    {
        var spec = new AlwaysTrueSpec();
        var compiled = spec.ToExpression().Compile();
        compiled(42).Should().BeTrue();
    }

    // Test double — inline struct implementing the interface
    private readonly struct AlwaysTrueSpec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int candidate) => true;
        public Expression<Func<int, bool>> ToExpression() => _ => true;
    }
}
```

**Step 2: Run test to verify it fails**

```bash
dotnet test tests/ZeroAlloc.Specification.Tests --filter "ISpecificationTests" -v minimal
```

Expected: FAIL — `ISpecification<T>` not found.

**Step 3: Create the interface**

Create `src/ZeroAlloc.Specification/ISpecification.cs`:

```csharp
using System.Linq.Expressions;

namespace ZeroAlloc.Specification;

/// <summary>
/// Defines a zero-allocation specification over a candidate of type <typeparamref name="T"/>.
/// </summary>
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T candidate);
    Expression<Func<T, bool>> ToExpression();
}
```

**Step 4: Create the attribute**

Create `src/ZeroAlloc.Specification/SpecificationAttribute.cs`:

```csharp
namespace ZeroAlloc.Specification;

/// <summary>
/// Marks a partial struct as a source-generated specification.
/// The generator emits And, Or, and Not composition methods.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public sealed class SpecificationAttribute : Attribute { }
```

**Step 5: Run tests to verify they pass**

```bash
dotnet test tests/ZeroAlloc.Specification.Tests --filter "ISpecificationTests" -v minimal
```

Expected: PASS.

**Step 6: Commit**

```bash
git add src/ZeroAlloc.Specification/ISpecification.cs src/ZeroAlloc.Specification/SpecificationAttribute.cs tests/ZeroAlloc.Specification.Tests/Unit/ISpecificationTests.cs
git commit -m "feat: add ISpecification<T> interface and SpecificationAttribute"
```

---

## Task 3: Expression Parameter Rebinder

**Files:**
- Create: `src/ZeroAlloc.Specification/Internal/ParameterRebinder.cs`

This utility is needed by all combinator structs to unify lambda parameters when composing expressions.

**Step 1: Write failing test**

Create `tests/ZeroAlloc.Specification.Tests/Unit/ParameterRebinderTests.cs`:

```csharp
using System.Linq.Expressions;
using FluentAssertions;
using ZeroAlloc.Specification.Internal;

namespace ZeroAlloc.Specification.Tests.Unit;

public class ParameterRebinderTests
{
    [Fact]
    public void Rebind_ReplacesParameterInBody()
    {
        Expression<Func<int, bool>> expr = x => x > 0;
        var newParam = Expression.Parameter(typeof(int), "y");

        var rebound = ParameterRebinder.ReplaceParameter(expr.Body, expr.Parameters[0], newParam);

        // Compile and verify the rebound expression works with the new parameter
        var lambda = Expression.Lambda<Func<int, bool>>(rebound, newParam).Compile();
        lambda(1).Should().BeTrue();
        lambda(-1).Should().BeFalse();
    }
}
```

**Step 2: Run test to verify it fails**

```bash
dotnet test tests/ZeroAlloc.Specification.Tests --filter "ParameterRebinderTests" -v minimal
```

Expected: FAIL — `ParameterRebinder` not found.

**Step 3: Implement ParameterRebinder**

Create `src/ZeroAlloc.Specification/Internal/ParameterRebinder.cs`:

```csharp
using System.Linq.Expressions;

namespace ZeroAlloc.Specification.Internal;

internal sealed class ParameterRebinder : ExpressionVisitor
{
    private readonly ParameterExpression _from;
    private readonly ParameterExpression _to;

    private ParameterRebinder(ParameterExpression from, ParameterExpression to)
    {
        _from = from;
        _to = to;
    }

    public static Expression ReplaceParameter(
        Expression body,
        ParameterExpression from,
        ParameterExpression to) =>
        new ParameterRebinder(from, to).Visit(body)!;

    protected override Expression VisitParameter(ParameterExpression node) =>
        node == _from ? _to : base.VisitParameter(node);
}
```

**Step 4: Run test to verify it passes**

```bash
dotnet test tests/ZeroAlloc.Specification.Tests --filter "ParameterRebinderTests" -v minimal
```

Expected: PASS.

**Step 5: Commit**

```bash
git add src/ZeroAlloc.Specification/Internal/ParameterRebinder.cs tests/ZeroAlloc.Specification.Tests/Unit/ParameterRebinderTests.cs
git commit -m "feat: add ParameterRebinder for expression composition"
```

---

## Task 4: AndSpecification Combinator Struct

**Files:**
- Create: `src/ZeroAlloc.Specification/AndSpecification.cs`
- Create: `tests/ZeroAlloc.Specification.Tests/Unit/AndSpecificationTests.cs`

**Step 1: Write failing tests**

Create `tests/ZeroAlloc.Specification.Tests/Unit/AndSpecificationTests.cs`:

```csharp
using System.Linq.Expressions;
using FluentAssertions;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Tests.Unit;

public class AndSpecificationTests
{
    private readonly struct GT0Spec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x > 0;
        public Expression<Func<int, bool>> ToExpression() => x => x > 0;
    }

    private readonly struct LT10Spec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x < 10;
        public Expression<Func<int, bool>> ToExpression() => x => x < 10;
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrueWhenBothSatisfied()
    {
        var spec = new AndSpecification<GT0Spec, LT10Spec, int>(new GT0Spec(), new LT10Spec());
        spec.IsSatisfiedBy(5).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalseWhenLeftFails()
    {
        var spec = new AndSpecification<GT0Spec, LT10Spec, int>(new GT0Spec(), new LT10Spec());
        spec.IsSatisfiedBy(-1).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalseWhenRightFails()
    {
        var spec = new AndSpecification<GT0Spec, LT10Spec, int>(new GT0Spec(), new LT10Spec());
        spec.IsSatisfiedBy(15).Should().BeFalse();
    }

    [Fact]
    public void ToExpression_ComposesCorrectly()
    {
        var spec = new AndSpecification<GT0Spec, LT10Spec, int>(new GT0Spec(), new LT10Spec());
        var compiled = spec.ToExpression().Compile();
        compiled(5).Should().BeTrue();
        compiled(-1).Should().BeFalse();
        compiled(15).Should().BeFalse();
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test tests/ZeroAlloc.Specification.Tests --filter "AndSpecificationTests" -v minimal
```

Expected: FAIL — `AndSpecification` not found.

**Step 3: Implement AndSpecification**

Create `src/ZeroAlloc.Specification/AndSpecification.cs`:

```csharp
using System.Linq.Expressions;
using ZeroAlloc.Specification.Internal;

namespace ZeroAlloc.Specification;

public readonly struct AndSpecification<TLeft, TRight, T> : ISpecification<T>
    where TLeft : struct, ISpecification<T>
    where TRight : struct, ISpecification<T>
{
    private readonly TLeft _left;
    private readonly TRight _right;

    public AndSpecification(TLeft left, TRight right)
    {
        _left = left;
        _right = right;
    }

    public bool IsSatisfiedBy(T candidate) =>
        _left.IsSatisfiedBy(candidate) && _right.IsSatisfiedBy(candidate);

    public Expression<Func<T, bool>> ToExpression()
    {
        var left = _left.ToExpression();
        var right = _right.ToExpression();
        var param = left.Parameters[0];
        var rightBody = ParameterRebinder.ReplaceParameter(right.Body, right.Parameters[0], param);
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, rightBody), param);
    }
}
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test tests/ZeroAlloc.Specification.Tests --filter "AndSpecificationTests" -v minimal
```

Expected: PASS.

**Step 5: Commit**

```bash
git add src/ZeroAlloc.Specification/AndSpecification.cs tests/ZeroAlloc.Specification.Tests/Unit/AndSpecificationTests.cs
git commit -m "feat: add AndSpecification<TLeft, TRight, T> combinator struct"
```

---

## Task 5: OrSpecification & NotSpecification Combinator Structs

**Files:**
- Create: `src/ZeroAlloc.Specification/OrSpecification.cs`
- Create: `src/ZeroAlloc.Specification/NotSpecification.cs`
- Create: `tests/ZeroAlloc.Specification.Tests/Unit/OrSpecificationTests.cs`
- Create: `tests/ZeroAlloc.Specification.Tests/Unit/NotSpecificationTests.cs`

**Step 1: Write failing tests for Or**

Create `tests/ZeroAlloc.Specification.Tests/Unit/OrSpecificationTests.cs`:

```csharp
using System.Linq.Expressions;
using FluentAssertions;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Tests.Unit;

public class OrSpecificationTests
{
    private readonly struct NegativeSpec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x < 0;
        public Expression<Func<int, bool>> ToExpression() => x => x < 0;
    }

    private readonly struct GT100Spec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x > 100;
        public Expression<Func<int, bool>> ToExpression() => x => x > 100;
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrueWhenLeftSatisfied()
    {
        var spec = new OrSpecification<NegativeSpec, GT100Spec, int>(new(), new());
        spec.IsSatisfiedBy(-5).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrueWhenRightSatisfied()
    {
        var spec = new OrSpecification<NegativeSpec, GT100Spec, int>(new(), new());
        spec.IsSatisfiedBy(200).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalseWhenNeitherSatisfied()
    {
        var spec = new OrSpecification<NegativeSpec, GT100Spec, int>(new(), new());
        spec.IsSatisfiedBy(50).Should().BeFalse();
    }

    [Fact]
    public void ToExpression_ComposesCorrectly()
    {
        var spec = new OrSpecification<NegativeSpec, GT100Spec, int>(new(), new());
        var compiled = spec.ToExpression().Compile();
        compiled(-5).Should().BeTrue();
        compiled(200).Should().BeTrue();
        compiled(50).Should().BeFalse();
    }
}
```

**Step 2: Write failing tests for Not**

Create `tests/ZeroAlloc.Specification.Tests/Unit/NotSpecificationTests.cs`:

```csharp
using System.Linq.Expressions;
using FluentAssertions;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Tests.Unit;

public class NotSpecificationTests
{
    private readonly struct EvenSpec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x % 2 == 0;
        public Expression<Func<int, bool>> ToExpression() => x => x % 2 == 0;
    }

    [Fact]
    public void IsSatisfiedBy_NegatesCorrectly()
    {
        var spec = new NotSpecification<EvenSpec, int>(new());
        spec.IsSatisfiedBy(3).Should().BeTrue();
        spec.IsSatisfiedBy(4).Should().BeFalse();
    }

    [Fact]
    public void ToExpression_NegatesCorrectly()
    {
        var spec = new NotSpecification<EvenSpec, int>(new());
        var compiled = spec.ToExpression().Compile();
        compiled(3).Should().BeTrue();
        compiled(4).Should().BeFalse();
    }
}
```

**Step 3: Run tests to verify they fail**

```bash
dotnet test tests/ZeroAlloc.Specification.Tests --filter "OrSpecificationTests|NotSpecificationTests" -v minimal
```

Expected: FAIL — types not found.

**Step 4: Implement OrSpecification**

Create `src/ZeroAlloc.Specification/OrSpecification.cs`:

```csharp
using System.Linq.Expressions;
using ZeroAlloc.Specification.Internal;

namespace ZeroAlloc.Specification;

public readonly struct OrSpecification<TLeft, TRight, T> : ISpecification<T>
    where TLeft : struct, ISpecification<T>
    where TRight : struct, ISpecification<T>
{
    private readonly TLeft _left;
    private readonly TRight _right;

    public OrSpecification(TLeft left, TRight right)
    {
        _left = left;
        _right = right;
    }

    public bool IsSatisfiedBy(T candidate) =>
        _left.IsSatisfiedBy(candidate) || _right.IsSatisfiedBy(candidate);

    public Expression<Func<T, bool>> ToExpression()
    {
        var left = _left.ToExpression();
        var right = _right.ToExpression();
        var param = left.Parameters[0];
        var rightBody = ParameterRebinder.ReplaceParameter(right.Body, right.Parameters[0], param);
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Body, rightBody), param);
    }
}
```

**Step 5: Implement NotSpecification**

Create `src/ZeroAlloc.Specification/NotSpecification.cs`:

```csharp
using System.Linq.Expressions;

namespace ZeroAlloc.Specification;

public readonly struct NotSpecification<TInner, T> : ISpecification<T>
    where TInner : struct, ISpecification<T>
{
    private readonly TInner _inner;

    public NotSpecification(TInner inner) => _inner = inner;

    public bool IsSatisfiedBy(T candidate) => !_inner.IsSatisfiedBy(candidate);

    public Expression<Func<T, bool>> ToExpression()
    {
        var inner = _inner.ToExpression();
        var param = inner.Parameters[0];
        return Expression.Lambda<Func<T, bool>>(Expression.Not(inner.Body), param);
    }
}
```

**Step 6: Run tests to verify they pass**

```bash
dotnet test tests/ZeroAlloc.Specification.Tests --filter "OrSpecificationTests|NotSpecificationTests" -v minimal
```

Expected: PASS.

**Step 7: Commit**

```bash
git add src/ZeroAlloc.Specification/OrSpecification.cs src/ZeroAlloc.Specification/NotSpecification.cs tests/ZeroAlloc.Specification.Tests/Unit/OrSpecificationTests.cs tests/ZeroAlloc.Specification.Tests/Unit/NotSpecificationTests.cs
git commit -m "feat: add OrSpecification and NotSpecification combinator structs"
```

---

## Task 6: Static Spec Builder

**Files:**
- Create: `src/ZeroAlloc.Specification/Spec.cs`
- Create: `tests/ZeroAlloc.Specification.Tests/Unit/SpecBuilderTests.cs`

**Step 1: Write failing tests**

Create `tests/ZeroAlloc.Specification.Tests/Unit/SpecBuilderTests.cs`:

```csharp
using System.Linq.Expressions;
using FluentAssertions;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Tests.Unit;

public class SpecBuilderTests
{
    private readonly struct GT0Spec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x > 0;
        public Expression<Func<int, bool>> ToExpression() => x => x > 0;
    }

    private readonly struct LT10Spec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x < 10;
        public Expression<Func<int, bool>> ToExpression() => x => x < 10;
    }

    [Fact]
    public void And_ProducesAndSpecification()
    {
        var spec = Spec.And<GT0Spec, LT10Spec, int>(new(), new());
        spec.IsSatisfiedBy(5).Should().BeTrue();
        spec.IsSatisfiedBy(-1).Should().BeFalse();
    }

    [Fact]
    public void Or_ProducesOrSpecification()
    {
        var spec = Spec.Or<GT0Spec, LT10Spec, int>(new(), new());
        // GT0 || LT10 is always true for any int, use Not to test or
        spec.IsSatisfiedBy(5).Should().BeTrue();
    }

    [Fact]
    public void Not_ProducesNotSpecification()
    {
        var spec = Spec.Not<GT0Spec, int>(new());
        spec.IsSatisfiedBy(-1).Should().BeTrue();
        spec.IsSatisfiedBy(5).Should().BeFalse();
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test tests/ZeroAlloc.Specification.Tests --filter "SpecBuilderTests" -v minimal
```

Expected: FAIL — `Spec` not found.

**Step 3: Implement Spec static builder**

Create `src/ZeroAlloc.Specification/Spec.cs`:

```csharp
namespace ZeroAlloc.Specification;

public static class Spec
{
    public static AndSpecification<TLeft, TRight, T> And<TLeft, TRight, T>(TLeft left, TRight right)
        where TLeft : struct, ISpecification<T>
        where TRight : struct, ISpecification<T>
        => new(left, right);

    public static OrSpecification<TLeft, TRight, T> Or<TLeft, TRight, T>(TLeft left, TRight right)
        where TLeft : struct, ISpecification<T>
        where TRight : struct, ISpecification<T>
        => new(left, right);

    public static NotSpecification<TInner, T> Not<TInner, T>(TInner inner)
        where TInner : struct, ISpecification<T>
        => new(inner);
}
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test tests/ZeroAlloc.Specification.Tests --filter "SpecBuilderTests" -v minimal
```

Expected: PASS.

**Step 5: Commit**

```bash
git add src/ZeroAlloc.Specification/Spec.cs tests/ZeroAlloc.Specification.Tests/Unit/SpecBuilderTests.cs
git commit -m "feat: add static Spec builder for And/Or/Not"
```

---

## Task 7: Source Generator — Skeleton & Attribute Embedding

**Files:**
- Create: `src/ZeroAlloc.Specification.Generator/SpecificationGenerator.cs`

The generator must embed the `[Specification]` attribute source so the core library and generator are self-contained. The attribute source is injected as a `PostInitializationOutput` so it is available during compilation.

**Step 1: Implement generator skeleton**

Create `src/ZeroAlloc.Specification.Generator/SpecificationGenerator.cs`:

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace ZeroAlloc.Specification.Generator;

[Generator]
public sealed class SpecificationGenerator : IIncrementalGenerator
{
    private const string AttributeSource = """
        using System;

        namespace ZeroAlloc.Specification
        {
            [AttributeUsage(AttributeTargets.Struct, Inherited = false)]
            public sealed class SpecificationAttribute : Attribute { }
        }
        """;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Embed the attribute so it's available in compilation
        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource("SpecificationAttribute.g.cs",
                SourceText.From(AttributeSource, Encoding.UTF8)));

        // Pipeline: find attributed structs
        var specs = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "ZeroAlloc.Specification.SpecificationAttribute",
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, _) => GetSpecificationInfo(ctx))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!);

        context.RegisterSourceOutput(specs, static (ctx, info) => Execute(ctx, info));
    }

    private static SpecificationInfo? GetSpecificationInfo(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol structSymbol)
            return null;

        // Find ISpecification<T> implementation
        var specInterface = structSymbol.AllInterfaces
            .FirstOrDefault(i => i.Name == "ISpecification" && i.TypeArguments.Length == 1);

        if (specInterface is null)
            return null;

        var candidateType = specInterface.TypeArguments[0];
        var isStateless = !structSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Any(f => !f.IsStatic);

        return new SpecificationInfo(
            structSymbol.Name,
            structSymbol.ContainingNamespace.ToDisplayString(),
            candidateType.ToDisplayString(),
            isStateless,
            structSymbol.IsReadOnly,
            structSymbol.IsPartialDefinition());
    }

    private static void Execute(SourceProductionContext ctx, SpecificationInfo info)
    {
        // Diagnostics: emit errors for invalid usage
        if (!info.IsPartial)
        {
            // ZA003 - not partial (no location available without syntax ref; skipped for now)
            return;
        }

        var source = GenerateSource(info);
        ctx.AddSource($"{info.TypeName}.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static string GenerateSource(SpecificationInfo info)
    {
        var ns = info.Namespace;
        var type = info.TypeName;
        var t = info.CandidateType;

        return $$"""
            using ZeroAlloc.Specification;

            namespace {{ns}}
            {
                public partial struct {{type}}
                {
                    public AndSpecification<{{type}}, TOther, {{t}}> And<TOther>(TOther other)
                        where TOther : struct, ISpecification<{{t}}> => new(this, other);

                    public OrSpecification<{{type}}, TOther, {{t}}> Or<TOther>(TOther other)
                        where TOther : struct, ISpecification<{{t}}> => new(this, other);

                    public NotSpecification<{{type}}, {{t}}> Not() => new(this);
                }
            }
            """;
    }
}

internal sealed record SpecificationInfo(
    string TypeName,
    string Namespace,
    string CandidateType,
    bool IsStateless,
    bool IsReadOnly,
    bool IsPartial);
```

**Step 2: Add IsPartialDefinition extension method**

Add to the same file above the `SpecificationInfo` record:

```csharp
internal static class SymbolExtensions
{
    public static bool IsPartialDefinition(this INamedTypeSymbol symbol) =>
        symbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.StructDeclarationSyntax>()
            .Any(s => s.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)));
}
```

**Step 3: Build the generator project**

```bash
dotnet build src/ZeroAlloc.Specification.Generator/ZeroAlloc.Specification.Generator.csproj
```

Expected: Build succeeded.

**Step 4: Commit**

```bash
git add src/ZeroAlloc.Specification.Generator/SpecificationGenerator.cs
git commit -m "feat: add incremental source generator skeleton with attribute embedding"
```

---

## Task 8: Source Generator — Diagnostics

**Files:**
- Create: `src/ZeroAlloc.Specification.Generator/Diagnostics.cs`
- Modify: `src/ZeroAlloc.Specification.Generator/SpecificationGenerator.cs`

**Step 1: Create diagnostics definitions**

Create `src/ZeroAlloc.Specification.Generator/Diagnostics.cs`:

```csharp
using Microsoft.CodeAnalysis;

namespace ZeroAlloc.Specification.Generator;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor NotAStruct = new(
        id: "ZA001",
        title: "[Specification] must be applied to a struct",
        messageFormat: "'{0}' must be a struct to use [Specification]",
        category: "ZeroAlloc.Specification",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingInterface = new(
        id: "ZA002",
        title: "Specification struct must implement ISpecification<T>",
        messageFormat: "'{0}' must implement ISpecification<T>",
        category: "ZeroAlloc.Specification",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotPartial = new(
        id: "ZA003",
        title: "Specification struct must be partial",
        messageFormat: "'{0}' must be declared partial",
        category: "ZeroAlloc.Specification",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotReadonly = new(
        id: "ZA004",
        title: "Specification struct should be readonly",
        messageFormat: "'{0}' should be declared readonly for correctness",
        category: "ZeroAlloc.Specification",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
```

**Step 2: Wire diagnostics into the generator**

In `GetSpecificationInfo`, update the method to return a richer result that includes location and diagnostic info. Replace the existing `GetSpecificationInfo` and `Execute` methods in `SpecificationGenerator.cs`:

```csharp
private static SpecificationInfo? GetSpecificationInfo(GeneratorAttributeSyntaxContext ctx)
{
    if (ctx.TargetSymbol is not INamedTypeSymbol structSymbol)
        return null;

    var location = ctx.TargetNode.GetLocation();

    var specInterface = structSymbol.AllInterfaces
        .FirstOrDefault(i => i.Name == "ISpecification" && i.TypeArguments.Length == 1);

    var isPartial = structSymbol.IsPartialDefinition();
    var hasInterface = specInterface is not null;

    var candidateType = hasInterface ? specInterface!.TypeArguments[0].ToDisplayString() : "object";
    var isStateless = !structSymbol.GetMembers()
        .OfType<IFieldSymbol>()
        .Any(f => !f.IsStatic);

    return new SpecificationInfo(
        structSymbol.Name,
        structSymbol.ContainingNamespace.ToDisplayString(),
        candidateType,
        isStateless,
        structSymbol.IsReadOnly,
        isPartial,
        hasInterface,
        location);
}

private static void Execute(SourceProductionContext ctx, SpecificationInfo info)
{
    if (!info.HasInterface)
    {
        ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingInterface, info.Location, info.TypeName));
        return;
    }

    if (!info.IsPartial)
    {
        ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.NotPartial, info.Location, info.TypeName));
        return;
    }

    if (!info.IsReadOnly)
        ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.NotReadonly, info.Location, info.TypeName));

    var source = GenerateSource(info);
    ctx.AddSource($"{info.TypeName}.g.cs", SourceText.From(source, Encoding.UTF8));
}
```

Update `SpecificationInfo` record to include the new fields:

```csharp
internal sealed record SpecificationInfo(
    string TypeName,
    string Namespace,
    string CandidateType,
    bool IsStateless,
    bool IsReadOnly,
    bool IsPartial,
    bool HasInterface,
    Location Location);
```

**Step 3: Build**

```bash
dotnet build src/ZeroAlloc.Specification.Generator/ZeroAlloc.Specification.Generator.csproj
```

Expected: Build succeeded.

**Step 4: Commit**

```bash
git add src/ZeroAlloc.Specification.Generator/Diagnostics.cs src/ZeroAlloc.Specification.Generator/SpecificationGenerator.cs
git commit -m "feat: add ZA001-ZA004 compile-time diagnostics to generator"
```

---

## Task 9: Source Generator — Stateless Expression Caching

**Files:**
- Modify: `src/ZeroAlloc.Specification.Generator/SpecificationGenerator.cs`

Update `GenerateSource` to emit a static cached expression for stateless specs:

**Step 1: Update GenerateSource**

Replace the `GenerateSource` method in `SpecificationGenerator.cs`:

```csharp
private static string GenerateSource(SpecificationInfo info)
{
    var ns = info.Namespace;
    var type = info.TypeName;
    var t = info.CandidateType;

    var cacheBlock = info.IsStateless ? $$"""

                    private static readonly global::System.Linq.Expressions.Expression<global::System.Func<{{t}}, bool>> _cachedExpression
                        = new {{type}}().ToExpression();

                    public global::System.Linq.Expressions.Expression<global::System.Func<{{t}}, bool>> ToExpression()
                        => _cachedExpression;
        """ : string.Empty;

    return $$"""
        using ZeroAlloc.Specification;

        namespace {{ns}}
        {
            public partial struct {{type}}
            {
                public AndSpecification<{{type}}, TOther, {{t}}> And<TOther>(TOther other)
                    where TOther : struct, ISpecification<{{t}}> => new(this, other);

                public OrSpecification<{{type}}, TOther, {{t}}> Or<TOther>(TOther other)
                    where TOther : struct, ISpecification<{{t}}> => new(this, other);

                public NotSpecification<{{type}}, {{t}}> Not() => new(this);
        {{cacheBlock}}
            }
        }
        """;
}
```

**Step 2: Write caching test**

Create `tests/ZeroAlloc.Specification.Tests/Unit/CachingTests.cs`:

```csharp
using System.Linq.Expressions;
using FluentAssertions;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Tests.Unit;

public class CachingTests
{
    [Specification]
    public readonly partial struct StatelessSpec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x > 0;
        public Expression<Func<int, bool>> ToExpression() => x => x > 0;
    }

    [Fact]
    public void StatelessSpec_ToExpression_ReturnsSameInstance()
    {
        var spec1 = new StatelessSpec();
        var spec2 = new StatelessSpec();
        ReferenceEquals(spec1.ToExpression(), spec2.ToExpression()).Should().BeTrue();
    }
}
```

**Step 3: Run caching test**

```bash
dotnet test tests/ZeroAlloc.Specification.Tests --filter "CachingTests" -v minimal
```

Expected: PASS (generator overrides `ToExpression()` to return cached instance).

**Step 4: Commit**

```bash
git add src/ZeroAlloc.Specification.Generator/SpecificationGenerator.cs tests/ZeroAlloc.Specification.Tests/Unit/CachingTests.cs
git commit -m "feat: emit static expression cache for stateless specifications"
```

---

## Task 10: Generator Snapshot Tests

**Files:**
- Create: `tests/ZeroAlloc.Specification.Tests/Generator/GeneratorSnapshotTests.cs`
- Create: `tests/ZeroAlloc.Specification.Tests/Generator/Snapshots/` (directory for verified outputs)

Snapshot tests verify the exact generated source, catching regressions.

**Step 1: Write snapshot tests**

Create `tests/ZeroAlloc.Specification.Tests/Generator/GeneratorSnapshotTests.cs`:

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using FluentAssertions;
using ZeroAlloc.Specification.Generator;

namespace ZeroAlloc.Specification.Tests.Generator;

public class GeneratorSnapshotTests
{
    private static Compilation CreateCompilation(string source)
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        return CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [Fact]
    public void Generator_EmitsAndOrNotMethods_ForStatelessSpec()
    {
        var source = """
            using System;
            using System.Linq.Expressions;
            using ZeroAlloc.Specification;

            namespace MyApp
            {
                [Specification]
                public readonly partial struct ActiveSpec : ISpecification<int>
                {
                    public bool IsSatisfiedBy(int x) => x > 0;
                    public Expression<Func<int, bool>> ToExpression() => x => x > 0;
                }
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new SpecificationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);
        var result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
        result.GeneratedTrees.Should().HaveCount(2); // attribute + spec

        var specSource = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ActiveSpec"))
            .GetText().ToString();

        specSource.Should().Contain("public AndSpecification<ActiveSpec");
        specSource.Should().Contain("public OrSpecification<ActiveSpec");
        specSource.Should().Contain("public NotSpecification<ActiveSpec");
        specSource.Should().Contain("_cachedExpression"); // stateless caching
    }

    [Fact]
    public void Generator_EmitsError_WhenInterfaceMissing()
    {
        var source = """
            using ZeroAlloc.Specification;

            namespace MyApp
            {
                [Specification]
                public readonly partial struct BadSpec { }
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new SpecificationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);
        var result = driver.GetRunResult();

        result.Diagnostics.Should().Contain(d =>
            d.Id == "ZA002" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Generator_EmitsWarning_WhenNotReadonly()
    {
        var source = """
            using System;
            using System.Linq.Expressions;
            using ZeroAlloc.Specification;

            namespace MyApp
            {
                [Specification]
                public partial struct MutableSpec : ISpecification<int>
                {
                    public bool IsSatisfiedBy(int x) => x > 0;
                    public Expression<Func<int, bool>> ToExpression() => x => x > 0;
                }
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new SpecificationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);
        var result = driver.GetRunResult();

        result.Diagnostics.Should().Contain(d =>
            d.Id == "ZA004" && d.Severity == DiagnosticSeverity.Warning);
    }
}
```

**Step 2: Run snapshot tests**

```bash
dotnet test tests/ZeroAlloc.Specification.Tests --filter "GeneratorSnapshotTests" -v minimal
```

Expected: PASS.

**Step 3: Commit**

```bash
git add tests/ZeroAlloc.Specification.Tests/Generator/GeneratorSnapshotTests.cs
git commit -m "test: add generator snapshot tests for And/Or/Not emission and diagnostics"
```

---

## Task 11: EF Core Integration Tests

**Files:**
- Create: `tests/ZeroAlloc.Specification.Tests/Integration/EfCoreQueryTests.cs`

**Step 1: Write integration tests**

Create `tests/ZeroAlloc.Specification.Tests/Integration/EfCoreQueryTests.cs`:

```csharp
using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Tests.Integration;

// Domain model
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

// DbContext
public class TestDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
}

// Specifications under test
[Specification]
public readonly partial struct ActiveProductSpec : ISpecification<Product>
{
    public bool IsSatisfiedBy(Product p) => p.IsActive;
    public Expression<Func<Product, bool>> ToExpression() => p => p.IsActive;
}

[Specification]
public readonly partial struct PriceAboveSpec : ISpecification<Product>
{
    private readonly decimal _min;
    public PriceAboveSpec(decimal min) => _min = min;
    public bool IsSatisfiedBy(Product p) => p.Price > _min;
    public Expression<Func<Product, bool>> ToExpression() => p => p.Price > _min;
}

public class EfCoreQueryTests : IDisposable
{
    private readonly TestDbContext _db;

    public EfCoreQueryTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new TestDbContext(options);
        _db.Products.AddRange(
            new Product { Id = 1, Name = "A", Price = 50m,  IsActive = true  },
            new Product { Id = 2, Name = "B", Price = 150m, IsActive = true  },
            new Product { Id = 3, Name = "C", Price = 50m,  IsActive = false },
            new Product { Id = 4, Name = "D", Price = 150m, IsActive = false }
        );
        _db.SaveChanges();
    }

    [Fact]
    public void ActiveProductSpec_TranslatesToQuery()
    {
        var spec = new ActiveProductSpec();
        var results = _db.Products.Where(spec.ToExpression()).ToList();
        results.Should().HaveCount(2);
        results.Should().OnlyContain(p => p.IsActive);
    }

    [Fact]
    public void PriceAboveSpec_TranslatesToQuery()
    {
        var spec = new PriceAboveSpec(100m);
        var results = _db.Products.Where(spec.ToExpression()).ToList();
        results.Should().HaveCount(2);
        results.Should().OnlyContain(p => p.Price > 100m);
    }

    [Fact]
    public void AndComposition_TranslatesToQuery()
    {
        var spec = new ActiveProductSpec().And(new PriceAboveSpec(100m));
        var results = _db.Products.Where(spec.ToExpression()).ToList();
        results.Should().HaveCount(1);
        results[0].Id.Should().Be(2);
    }

    [Fact]
    public void OrComposition_TranslatesToQuery()
    {
        var spec = new ActiveProductSpec().Or(new PriceAboveSpec(100m));
        var results = _db.Products.Where(spec.ToExpression()).ToList();
        results.Should().HaveCount(3);
    }

    [Fact]
    public void NotComposition_TranslatesToQuery()
    {
        var spec = new ActiveProductSpec().Not();
        var results = _db.Products.Where(spec.ToExpression()).ToList();
        results.Should().HaveCount(2);
        results.Should().OnlyContain(p => !p.IsActive);
    }

    public void Dispose() => _db.Dispose();
}
```

**Step 2: Run integration tests**

```bash
dotnet test tests/ZeroAlloc.Specification.Tests --filter "EfCoreQueryTests" -v minimal
```

Expected: PASS — all 5 query scenarios translate correctly.

**Step 3: Run full test suite**

```bash
dotnet test ZeroAlloc.Specification.sln -v minimal
```

Expected: All tests PASS.

**Step 4: Commit**

```bash
git add tests/ZeroAlloc.Specification.Tests/Integration/EfCoreQueryTests.cs
git commit -m "test: add EF Core integration tests for spec query translation"
```

---

## Task 12: Expression Composition Tests

**Files:**
- Create: `tests/ZeroAlloc.Specification.Tests/Unit/ExpressionCompositionTests.cs`

Verify expression tree structure (not just compiled behavior) — ensures the trees are valid for ORM translation.

**Step 1: Write tests**

Create `tests/ZeroAlloc.Specification.Tests/Unit/ExpressionCompositionTests.cs`:

```csharp
using System.Linq.Expressions;
using FluentAssertions;
using ZeroAlloc.Specification;

namespace ZeroAlloc.Specification.Tests.Unit;

public class ExpressionCompositionTests
{
    private readonly struct GT0Spec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x > 0;
        public Expression<Func<int, bool>> ToExpression() => x => x > 0;
    }

    private readonly struct LT10Spec : ISpecification<int>
    {
        public bool IsSatisfiedBy(int x) => x < 10;
        public Expression<Func<int, bool>> ToExpression() => x => x < 10;
    }

    [Fact]
    public void And_ProducesAndAlsoNode()
    {
        var spec = new AndSpecification<GT0Spec, LT10Spec, int>(new(), new());
        var expr = spec.ToExpression();
        expr.Body.NodeType.Should().Be(ExpressionType.AndAlso);
    }

    [Fact]
    public void Or_ProducesOrElseNode()
    {
        var spec = new OrSpecification<GT0Spec, LT10Spec, int>(new(), new());
        var expr = spec.ToExpression();
        expr.Body.NodeType.Should().Be(ExpressionType.OrElse);
    }

    [Fact]
    public void Not_ProducesNotNode()
    {
        var spec = new NotSpecification<GT0Spec, int>(new());
        var expr = spec.ToExpression();
        expr.Body.NodeType.Should().Be(ExpressionType.Not);
    }

    [Fact]
    public void And_UsesSingleParameter()
    {
        var spec = new AndSpecification<GT0Spec, LT10Spec, int>(new(), new());
        var expr = spec.ToExpression();
        expr.Parameters.Should().HaveCount(1);

        // Both sides of AndAlso must reference the same parameter
        var andAlso = (BinaryExpression)expr.Body;
        var leftParam = ((BinaryExpression)andAlso.Left).Left as ParameterExpression;
        var rightParam = ((BinaryExpression)andAlso.Right).Left as ParameterExpression;
        ReferenceEquals(leftParam, rightParam).Should().BeTrue();
    }
}
```

**Step 2: Run tests**

```bash
dotnet test tests/ZeroAlloc.Specification.Tests --filter "ExpressionCompositionTests" -v minimal
```

Expected: PASS.

**Step 3: Final full suite run**

```bash
dotnet test ZeroAlloc.Specification.sln -v minimal
```

Expected: All tests PASS.

**Step 4: Commit**

```bash
git add tests/ZeroAlloc.Specification.Tests/Unit/ExpressionCompositionTests.cs
git commit -m "test: add expression tree structure tests for And/Or/Not composition"
```
