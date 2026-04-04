---
name: dotnet-ef-core
description: >
  Entity Framework Core standards for .NET 10 projects. Covers entity
  configuration, repository patterns, UnitOfWork, DbContext, migrations,
  and query patterns. Use when creating or modifying any EF Core component.
---

# EF Core — TemperAI Standards

## Entity configuration — Fluent API only

- Never `DataAnnotations` on entities or Value Objects.
- One `IEntityTypeConfiguration<T>` per entity in `Infrastructure/Persistence/Configurations/`.
- Never `nvarchar(max)` or `varchar(max)` — always length from `Entity.Rules`.
- `varchar` for ASCII, `nvarchar` for Unicode.
- Value Objects configured with `OwnsOne` in the entity configuration.

```csharp
// ProductConfiguration.cs
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .HasMaxLength(Product.Rules.NAME_MAX_LENGTH)
            .HasColumnType("varchar");

        builder.Property(product => product.Description)
            .HasMaxLength(Product.Rules.DESCRIPTION_MAX_LENGTH)
            .HasColumnType("nvarchar");

        builder.Property(product => product.Price)
            .HasColumnType("decimal(18,2)");

        builder.Property(product => product.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnType("varchar");

        builder.Property(product => product.CreatedAt)
            .HasColumnType("datetime2");

        builder.Property(product => product.UpdatedAt)
            .HasColumnType("datetime2");

        builder.HasIndex(product => product.Name)
            .IsUnique();
    }
}
```

### Value Object configuration with OwnsOne

```csharp
// OrderConfiguration.cs
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(order => order.Id);

        builder.OwnsOne(order => order.Total, moneyBuilder =>
        {
            moneyBuilder.Property(money => money.Amount)
                .HasColumnName("TotalAmount")
                .HasColumnType("decimal(18,2)");

            moneyBuilder.Property(money => money.Currency)
                .HasColumnName("TotalCurrency")
                .HasMaxLength(3)
                .HasColumnType("varchar");
        });
    }
}
```

## DbContext

```csharp
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> dbContextOptions)
        : base(dbContextOptions)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

## Repositories — no .Update(), explicit tracking naming

```csharp
public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _appDbContext;

    public ProductRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    // With tracking — EF detects changes, use for updates
    public async Task<Product?> GetByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Products
            .FirstOrDefaultAsync(
                product => product.Id == productId,
                cancellationToken);
    }

    // Without tracking — more performant, read-only
    public async Task<Product?> GetByIdAsNoTrackingAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                product => product.Id == productId,
                cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string productName,
        CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Products
            .AsNoTracking()
            .AnyAsync(
                product => product.Name == productName,
                cancellationToken);
    }

    public async Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        await _appDbContext.Products.AddAsync(product, cancellationToken);
    }

    // No .Update() — EF detects changes via change tracker automatically
    // Just call CompleteAsync after modifying the tracked entity
}
```

## UnitOfWork — single entry point to repositories

```csharp
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _appDbContext;

    public IProductRepository ProductRepository { get; }

    public UnitOfWork(
        AppDbContext appDbContext,
        IProductRepository productRepository)
    {
        _appDbContext = appDbContext;
        ProductRepository = productRepository;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _appDbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _appDbContext.Database.CommitTransactionAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _appDbContext.Database.RollbackTransactionAsync(cancellationToken);
    }

    public async Task<SaveResult> CompleteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            int rowsAffected = await _appDbContext.SaveChangesAsync(cancellationToken);

            return new SaveResult
            {
                IsSuccess = true,
                RowsAffected = rowsAffected
            };
        }
        catch (DbUpdateException dbUpdateException)
        {
            return new SaveResult
            {
                IsSuccess = false,
                ErrorMessage = dbUpdateException.InnerException?.Message
                    ?? dbUpdateException.Message
            };
        }
    }

    public void Dispose()
    {
        _appDbContext.Dispose();
    }
}

// SaveResult.cs
public sealed class SaveResult
{
    public bool IsSuccess { get; init; }
    public int RowsAffected { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
}
```

## Rules

- Never `DataAnnotations` on entities or Value Objects.
- Never `nvarchar(max)` or `varchar(max)` — always specify lengths from `Entity.Rules`.
- Never `.Update()` from EF Core — change tracker detects changes automatically.
- Always `GetByIdAsync` with tracking (for modifications) and `GetByIdAsNoTrackingAsync` without tracking (for reads).
- Always `CancellationToken` on repository methods.
- Always `AsNoTracking()` on read-only queries.
- Always one `IEntityTypeConfiguration<T>` per entity.
- Always `ApplyConfigurationsFromAssembly` in `OnModelCreating`.
- Always handle `DbUpdateException` in `CompleteAsync` — never let it bubble up.
- Never lazy loading — explicit includes always.
- Value Objects configured with `OwnsOne` — no `[ComplexType]` or DataAnnotations.
- **Never use `using static`** — always use explicit `using` directives with the namespace, then reference types by their name. Static usings hide the type origin and make code harder to read and navigate.
- **Never call `builder.ToTable()`** — the table name must be the plural of the entity name, which EF Core infers automatically from the `DbSet<T>` property name. Explicit `ToTable` calls are redundant and error-prone.
- **Never call `HasDefaultValueSql()` or `ValueGeneratedOnAdd()` for primary keys** — EF Core handles this automatically for `int` and `Guid` keys.
