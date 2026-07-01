---
name: dbcontext-setup
description: >
  Canonical DbContext setup and registration rules for EF Core.
  Load when creating or modifying DbContext classes or their DI wiring.
requires: [backend-dotnet-csharp]
produces: [dbcontext, dbsets, configuration-registration]
---

# DbContext Setup — TemperAI

> **On the DbContext name:** the examples in this and every EF Core skill use `AppDbContext` as an
> illustrative name for readability. The name is NOT mandated — a project may define one or several
> DbContexts (e.g. per bounded context or module). Use whatever name(s) the project defines, and
> apply these rules to whichever DbContext the task targets. Whatever name is chosen, keep every
> example internally consistent with it.

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **ALWAYS keep `DbSet<T>` names aligned with EF conventions and the chosen architecture**
2. **ALWAYS register entity configurations explicitly or via assembly scanning consistently**
3. **NEVER embed business logic in `DbContext`**
4. **ALWAYS register `AppDbContext` as `Scoped`** (the default of `AddDbContext`) — one instance per request
5. **NEVER inject `AppDbContext` directly into application code when the data-access pattern is `Repository + UnitOfWork`** — go through the repository abstraction. Direct injection into use cases is allowed ONLY when the pattern is `Direct DbContext`.

## Load when

- Creating a new application DbContext
- Adding `DbSet<T>` properties
- Adjusting registration in infrastructure DI

## AppDbContext structure

```csharp
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
| `DbSet` properties | Prefer `Set<TEntity>()`-backed properties with names aligned to EF conventions |
| `ApplyConfigurationsFromAssembly` | Auto-discovers `IEntityTypeConfiguration<T>` classes |
| Never `OnConfiguring` | Provider and connection string belong in DI |

## DI registration

Registering `AppDbContext` is identical for both data-access patterns. What differs is whether you
**also** register repositories + UnitOfWork on top of it.

### Direct DbContext pattern — register only the context

Use cases inject `AppDbContext` directly (see `dbcontext-usage`). Nothing else to register.

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
}
```

### Repository + UnitOfWork pattern — register the context plus the abstractions

Use cases inject repositories / `IUnitOfWork`, never the context (see `repository-usage`).

```csharp
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
dotnet ef migrations add InitialCreate --project Infrastructure --startup-project Api
dotnet ef database update --project Infrastructure --startup-project Api
dotnet ef migrations remove --project Infrastructure --startup-project Api
```

## Anti-patterns — never do this

```csharp
// ❌ NEVER configure provider or connection string in OnConfiguring
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseSqlServer("...");
}

// ❌ NEVER inline entity mappings in AppDbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>(entity => { });
}
```
