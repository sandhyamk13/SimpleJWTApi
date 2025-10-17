using Microsoft.EntityFrameworkCore;
using SimpleJWTApi.Models;

namespace SimpleJWTApi.Data;

/// <summary>
/// Database context for the application using Entity Framework Core
/// Using InMemory database for simplicity in this demo
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });

        // Seed some initial data
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Laptop", Description = "High-performance laptop", Price = 999.99m, CategoryId = 1 },
            new Product { Id = 2, Name = "Mouse", Description = "Wireless mouse", Price = 29.99m, CategoryId = 2 },
            new Product { Id = 3, Name = "Keyboard", Description = "Mechanical keyboard", Price = 89.99m, CategoryId = 2 }
        );
    }
}