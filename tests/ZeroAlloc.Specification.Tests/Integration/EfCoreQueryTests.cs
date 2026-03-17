using System;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
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

// Specifications under test — attributed partial structs, generator emits And/Or/Not
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
    public Expression<Func<Product, bool>> ToExpression()
    {
        var min = _min; // copy to local — lambda in struct cannot capture 'this'
        return p => p.Price > min;
    }
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
