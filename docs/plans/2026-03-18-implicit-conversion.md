# Implicit Expression Conversion Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add `implicit operator Expression<Func<T, bool>>` to all spec types so call sites can write `.Where(spec)` instead of `.Where(spec.ToExpression())`.

**Architecture:** Add the operator to the three hand-written combinator structs, then extend the source generator to emit the same operator in every `[Specification]`-attributed partial struct. Update integration tests to use the implicit conversion. No interface changes — fully backwards compatible.

**Tech Stack:** C#, Roslyn incremental source generator, xUnit, FluentAssertions, EF Core InMemory

---

### Task 1: Add implicit operator to AndSpecification

**Files:**
- Modify: `src/ZeroAlloc.Specification/AndSpecification.cs`

**Step 1: Write the failing test**

Add to `tests/ZeroAlloc.Specification.Tests/Unit/AndSpecificationTests.cs` (at the bottom of the class):

```csharp
[Fact]
public void ImplicitConversion_ReturnsToExpression()
{
    var spec = new AndSpecification<TrueSpec, TrueSpec, int>(new TrueSpec(), new TrueSpec());
    Expression<Func<int, bool>> expr = spec; // implicit conversion
    expr.Should().NotBeNull();
    var compiled = expr.Compile();
    compiled(42).Should().BeTrue();
}
```

**Step 2: Run the test to verify it fails**

```bash
cd c:/Projects/Prive/ZeroAlloc.Specification
dotnet test --filter "AndSpecificationTests" --configuration Release 2>&1 | tail -10
```

Expected: FAIL — no implicit conversion defined.

**Step 3: Add the implicit operator to AndSpecification**

In `src/ZeroAlloc.Specification/AndSpecification.cs`, add after `ToExpression()`:

```csharp
public static implicit operator Expression<Func<T, bool>>(AndSpecification<TLeft, TRight, T> spec)
    => spec.ToExpression();
```

**Step 4: Run the test to verify it passes**

```bash
dotnet test --filter "AndSpecificationTests" --configuration Release 2>&1 | tail -10
```

Expected: PASS.

**Step 5: Commit**

```bash
git add src/ZeroAlloc.Specification/AndSpecification.cs tests/ZeroAlloc.Specification.Tests/Unit/AndSpecificationTests.cs
git commit -m "feat: add implicit Expression conversion to AndSpecification"
```

---

### Task 2: Add implicit operator to OrSpecification and NotSpecification

**Files:**
- Modify: `src/ZeroAlloc.Specification/OrSpecification.cs`
- Modify: `src/ZeroAlloc.Specification/NotSpecification.cs`

**Step 1: Write the failing tests**

Add to `tests/ZeroAlloc.Specification.Tests/Unit/OrSpecificationTests.cs`:

```csharp
[Fact]
public void ImplicitConversion_ReturnsToExpression()
{
    var spec = new OrSpecification<TrueSpec, FalseSpec, int>(new TrueSpec(), new FalseSpec());
    Expression<Func<int, bool>> expr = spec;
    expr.Should().NotBeNull();
    expr.Compile()(42).Should().BeTrue();
}
```

Add to `tests/ZeroAlloc.Specification.Tests/Unit/NotSpecificationTests.cs`:

```csharp
[Fact]
public void ImplicitConversion_ReturnsToExpression()
{
    var spec = new NotSpecification<TrueSpec, int>(new TrueSpec());
    Expression<Func<int, bool>> expr = spec;
    expr.Should().NotBeNull();
    expr.Compile()(42).Should().BeFalse();
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test --filter "OrSpecificationTests|NotSpecificationTests" --configuration Release 2>&1 | tail -10
```

Expected: 2 failures.

**Step 3: Add implicit operator to OrSpecification**

In `src/ZeroAlloc.Specification/OrSpecification.cs`, read the file first, then add after `ToExpression()`:

```csharp
public static implicit operator Expression<Func<T, bool>>(OrSpecification<TLeft, TRight, T> spec)
    => spec.ToExpression();
```

**Step 4: Add implicit operator to NotSpecification**

In `src/ZeroAlloc.Specification/NotSpecification.cs`, add after `ToExpression()`:

```csharp
public static implicit operator Expression<Func<T, bool>>(NotSpecification<TInner, T> spec)
    => spec.ToExpression();
```

**Step 5: Run tests to verify they pass**

```bash
dotnet test --filter "OrSpecificationTests|NotSpecificationTests" --configuration Release 2>&1 | tail -10
```

Expected: PASS.

**Step 6: Commit**

```bash
git add src/ZeroAlloc.Specification/OrSpecification.cs src/ZeroAlloc.Specification/NotSpecification.cs tests/ZeroAlloc.Specification.Tests/Unit/OrSpecificationTests.cs tests/ZeroAlloc.Specification.Tests/Unit/NotSpecificationTests.cs
git commit -m "feat: add implicit Expression conversion to OrSpecification and NotSpecification"
```

---

### Task 3: Extend the source generator to emit the implicit operator

**Files:**
- Modify: `src/ZeroAlloc.Specification.Generator/SpecificationGenerator.cs` — `GenerateSource` method

**Step 1: Write a failing generator snapshot test**

In `tests/ZeroAlloc.Specification.Tests/Generator/GeneratorSnapshotTests.cs`, add a new test that verifies the generated code contains the implicit operator. Look at the existing snapshot tests for the pattern — they compile a source string with the generator and check the output.

Add:

```csharp
[Fact]
public void Generator_EmitsImplicitConversionOperator()
{
    var source = """
        using ZeroAlloc.Specification;
        using System.Linq.Expressions;

        [Specification]
        public readonly partial struct MySpec : ISpecification<int>
        {
            public bool IsSatisfiedBy(int x) => x > 0;
            public Expression<Func<int, bool>> ToExpression() => x => x > 0;
        }
        """;

    var (_, output) = RunGenerator(source);

    output.Should().Contain("implicit operator");
    output.Should().Contain("Expression<");
}
```

Where `RunGenerator` returns `(Compilation compilation, string output)` — match the pattern used in the existing snapshot tests in that file.

**Step 2: Run the test to verify it fails**

```bash
dotnet test --filter "Generator_EmitsImplicitConversionOperator" --configuration Release 2>&1 | tail -10
```

Expected: FAIL — generated output does not contain `implicit operator`.

**Step 3: Update `GenerateSource` in the generator**

In `src/ZeroAlloc.Specification.Generator/SpecificationGenerator.cs`, find the `GenerateSource` method. The current template ends with the `Not()` method. Add the implicit operator after it:

Current generated body:
```csharp
return $$"""
    using ZeroAlloc.Specification;

    namespace {{ns}}
    {
        {{accessibility}} partial struct {{type}}
        {
            public AndSpecification<{{type}}, TOther, {{t}}> And<TOther>(TOther other)
                where TOther : struct, ISpecification<{{t}}> => new(this, other);

            public OrSpecification<{{type}}, TOther, {{t}}> Or<TOther>(TOther other)
                where TOther : struct, ISpecification<{{t}}> => new(this, other);

            public NotSpecification<{{type}}, {{t}}> Not() => new(this);
        }
    }
    """;
```

New generated body (add implicit operator after `Not()`):

```csharp
return $$"""
    using ZeroAlloc.Specification;

    namespace {{ns}}
    {
        {{accessibility}} partial struct {{type}}
        {
            public global::ZeroAlloc.Specification.AndSpecification<{{type}}, TOther, {{t}}> And<TOther>(TOther other)
                where TOther : struct, global::ZeroAlloc.Specification.ISpecification<{{t}}> => new(this, other);

            public global::ZeroAlloc.Specification.OrSpecification<{{type}}, TOther, {{t}}> Or<TOther>(TOther other)
                where TOther : struct, global::ZeroAlloc.Specification.ISpecification<{{t}}> => new(this, other);

            public global::ZeroAlloc.Specification.NotSpecification<{{type}}, {{t}}> Not() => new(this);

            public static implicit operator global::System.Linq.Expressions.Expression<global::System.Func<{{t}}, bool>>({{type}} spec)
                => spec.ToExpression();
        }
    }
    """;
```

Note: use fully-qualified `global::` names throughout to avoid `using` directive conflicts in the user's code. Remove the `using ZeroAlloc.Specification;` at the top since all types are now fully qualified.

**Step 4: Run the generator test to verify it passes**

```bash
dotnet test --filter "Generator_EmitsImplicitConversionOperator" --configuration Release 2>&1 | tail -10
```

Expected: PASS.

**Step 5: Run the full test suite**

```bash
dotnet test --configuration Release 2>&1 | tail -10
```

Expected: all tests pass. If the generator smoke test (`GeneratorSmokeTest`) fails, it may be because the generated code now uses `global::` types but the smoke test's spec calls `.And()`/`.Or()` etc. — those should still work. Investigate any failures before proceeding.

**Step 6: Commit**

```bash
git add src/ZeroAlloc.Specification.Generator/SpecificationGenerator.cs tests/ZeroAlloc.Specification.Tests/Generator/GeneratorSnapshotTests.cs
git commit -m "feat: generator emits implicit Expression conversion operator"
```

---

### Task 4: Update integration tests and add implicit conversion smoke test

**Files:**
- Modify: `tests/ZeroAlloc.Specification.Tests/Integration/EfCoreQueryTests.cs`
- Modify: `tests/ZeroAlloc.Specification.Tests/Unit/GeneratorSmokeTest.cs` (or create a new file)

**Step 1: Update EfCoreQueryTests to use implicit conversion**

In `tests/ZeroAlloc.Specification.Tests/Integration/EfCoreQueryTests.cs`, change all `.Where(spec.ToExpression())` calls to `.Where(spec)`:

Before:
```csharp
var results = _db.Products.Where(spec.ToExpression()).ToList();
```

After:
```csharp
var results = _db.Products.Where(spec).ToList();
```

Do this for all 5 test methods. The test logic and assertions stay the same — only the `.ToExpression()` call is removed.

**Step 2: Run integration tests to verify they still pass**

```bash
dotnet test --filter "EfCoreQueryTests" --configuration Release 2>&1 | tail -10
```

Expected: all 5 pass. This proves EF Core translates the implicit conversion correctly to SQL.

**Step 3: Run the full test suite**

```bash
dotnet test --configuration Release 2>&1 | tail -5
```

Expected: all tests pass.

**Step 4: Commit**

```bash
git add tests/ZeroAlloc.Specification.Tests/Integration/EfCoreQueryTests.cs
git commit -m "test: use implicit Expression conversion in EF Core integration tests"
```

---

### Task 5: Final verification

**Step 1: Full build**

```bash
cd c:/Projects/Prive/ZeroAlloc.Specification
dotnet build --configuration Release
```

Expected: `Build succeeded. 0 Error(s).`

**Step 2: Full test run**

```bash
dotnet test --configuration Release --verbosity normal
```

Expected: all tests pass (count should be 37 + the new implicit conversion tests = 40+).

**Step 3: Push to remote**

```bash
git push
```
