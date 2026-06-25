---
name: repository-pattern
description: >
  Canonical repository and UnitOfWork creation rules for EF Core.
  Load when creating repositories or UnitOfWork from scratch.
requires: [dotnet-csharp]
produces: [repositories, unit-of-work, repository-interfaces]
---

# Repository Pattern — TemperAI

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **NEVER call `.Update()`** on tracked entities
2. **ALWAYS separate interfaces from implementations** according to the chosen architecture
3. **ALWAYS expose repository methods by intent** rather than generic query dumping
4. **ALWAYS persist writes through `CompleteAsync()` on UnitOfWork**
5. **NEVER let application code call `SaveChangesAsync()` directly**

## Load when

- Creating a repository interface and implementation
- Creating or modifying UnitOfWork contracts
- Establishing repository wiring in infrastructure

## Generic repository base

```csharp
public abstract class GenericRepository<TEntity> where TEntity : class
{
    protected readonly AppDbContext AppDbContext;

    protected GenericRepository(AppDbContext appDbContext)
    {
        AppDbContext = appDbContext;
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await AppDbContext.Set<TEntity>()
            .FirstOrDefaultAsync(BuildIdPredicate(id), ct);
    }

    public async Task<TEntity?> GetByIdAsNoTrackingAsync(Guid id, CancellationToken ct = default)
    {
        return await AppDbContext.Set<TEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(BuildIdPredicate(id), ct);
    }

    public async Task AddAsync(TEntity entity, CancellationToken ct = default)
    {
        await AppDbContext.Set<TEntity>().AddAsync(entity, ct);
    }

    public void Delete(TEntity entity)
    {
        AppDbContext.Set<TEntity>().Remove(entity);
    }

    private Expression<Func<TEntity, bool>> BuildIdPredicate(Guid id)
    {
        Microsoft.EntityFrameworkCore.Metadata.IEntityType? entityType = AppDbContext.Model.FindEntityType(typeof(TEntity));
        Microsoft.EntityFrameworkCore.Metadata.IProperty? pkProperty = entityType?.FindPrimaryKey()?.Properties.FirstOrDefault();

        if (pkProperty is null)
            throw new InvalidOperationException($"Entity {typeof(TEntity).Name} has no primary key defined.");

        ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "entity");
        MemberExpression property = Expression.Property(parameter, pkProperty.Name);
        ConstantExpression value = Expression.Constant(id, typeof(Guid));
        BinaryExpression equality = Expression.Equal(property, value);

        return Expression.Lambda<Func<TEntity, bool>>(equality, parameter);
    }
}

public interface IGenericRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsNoTrackingAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Delete(TEntity entity);
}
```

## Specific repository pattern

```csharp
public interface IProductRepository : IGenericRepository<Product>
{
    Task<bool> ExistsByNameAsync(string productName, CancellationToken cancellationToken = default);
    Task<Product?> GetByNameAsync(string productName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
}

public sealed class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext appDbContext) : base(appDbContext)
    {
    }

    public async Task<bool> ExistsByNameAsync(string productName, CancellationToken cancellationToken = default)
    {
        return await AppDbContext.Products
            .AsNoTracking()
            .AnyAsync(product => product.Name == productName, cancellationToken);
    }

    public async Task<Product?> GetByNameAsync(string productName, CancellationToken cancellationToken = default)
    {
        return await AppDbContext.Products
            .FirstOrDefaultAsync(product => product.Name == productName, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await AppDbContext.Products
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
```

## UnitOfWork and SaveResult

```csharp
public interface IUnitOfWork : IDisposable
{
    IProductRepository ProductRepository { get; }
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task<SaveResult> CompleteAsync(CancellationToken cancellationToken = default);
}

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _appDbContext;

    public IProductRepository ProductRepository { get; }

    public UnitOfWork(AppDbContext appDbContext, IProductRepository productRepository)
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
            return new SaveResult { IsSuccess = true, RowsAffected = rowsAffected, ErrorMessage = string.Empty };
        }
        catch (DbUpdateException dbUpdateException)
        {
            return new SaveResult
            {
                IsSuccess = false,
                ErrorMessage = dbUpdateException.InnerException?.Message ?? dbUpdateException.Message
            };
        }
    }

    public void Dispose()
    {
        _appDbContext.Dispose();
    }
}

public sealed class SaveResult
{
    public bool IsSuccess { get; init; }
    public int RowsAffected { get; init; }
    public required string ErrorMessage { get; init; }
}
```

## Anti-patterns — never do this

```csharp
// ❌ NEVER call .Update()
_appDbContext.Products.Update(product);

// ✅ CORRECT — modify tracked entity, then CompleteAsync
product.UpdateName("New Name");
await _unitOfWork.CompleteAsync(cancellationToken);

// ❌ NEVER rely on lazy loading
public class Product
{
    public virtual ICollection<Order> Orders { get; set; } = [];
}
```
