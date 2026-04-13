---
name: dbcontext-setup
description: >
  AppDbContext structure and OnModelCreating configuration.
  Load when creating or modifying DbContext.
---

# DbContext Setup — TemperAI

## AppDbContext structure

```csharp
// Infrastructure/Persistence/AppDbContext.cs
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> dbContextOptions)
        : base(dbContextOptions)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

## Rules

| Rule | Explanation |
|---|---|
| `sealed class` | DbContext should be sealed |
| `DbSet properties` | Use `=> Set<TEntity>()` expression body |
| `ApplyConfigurationsFromAssembly` | Auto-discovers all `IEntityTypeConfiguration<T>` |
| Never `OnConfiguring` | Connection string configured in DI |

## DI Registration

```csharp
// Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        
        return services;
    }
}
```

## Migration commands

```bash
# Create migration
dotnet ef migrations add InitialCreate --project Infrastructure --startup-project Api

# Update database
dotnet ef database update --project Infrastructure --startup-project Api

# Remove last migration
dotnet ef migrations remove --project Infrastructure --startup-project Api
```

## Anti-patterns — NEVER DO THIS

```csharp
// ❌ NEVER override OnConfiguring for connection string
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseSqlServer("..."); // WRONG
}

// ✅ CORRECT: Connection string from DI
public AppDbContext(DbContextOptions<AppDbContext> dbContextOptions)
    : base(dbContextOptions)
{
}

// ❌ NEVER add entities manually in OnModelCreating
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>(entity => { ... }); // WRONG
}

// ✅ CORRECT: Use ApplyConfigurationsFromAssembly
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    base.OnModelCreating(modelBuilder);
}
```