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
