---
name: repository-pattern
description: >
  Generic and specific repository patterns with UnitOfWork.
  Load when creating or modifying repositories.
---

# Repository Pattern — TemperAI

## GenericRepository<TEntity>

```csharp
// Infrastructure/Persistence/Repositories/GenericRepository.cs
public abstract class GenericRepository<TEntity> where TEntity : class
{
    protected readonly AppDbContext AppDbContext;

    protected GenericRepository(AppDbContext appDbContext)
    {
        AppDbContext = appDbContext;
    }

    public async Task<TEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await AppDbContext.Set<TEntity>()
            .FirstOrDefaultAsync(entity => EF.Property<Guid>(entity, "Id") == id, cancellationToken);
    }

    public async Task<TEntity?> GetByIdAsNoTrackingAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await AppDbContext.Set<TEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => EF.Property<Guid>(entity, "Id") == id, cancellationToken);
    }

    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        await AppDbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public void Delete(TEntity entity)
    {
        AppDbContext.Set<TEntity>().Remove(entity);
    }
}
```

## IGenericRepository<TEntity>

```csharp
// Domain/Products/Repositories/IGenericRepository.cs
public interface IGenericRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsNoTrackingAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Delete(TEntity entity);
}
```

## Specific repository interface

```csharp
// Domain/Products/Repositories/IProductRepository.cs
public interface IProductRepository : IGenericRepository<Product>
{
    Task<bool> ExistsByNameAsync(
        string productName,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByNameAsync(
        string productName,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
```

## Specific repository implementation

```csharp
// Infrastructure/Persistence/Repositories/ProductRepository.cs
public sealed class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext appDbContext) : base(appDbContext)
    {
    }

    public async Task<bool> ExistsByNameAsync(
        string productName,
        CancellationToken cancellationToken = default)
    {
        return await AppDbContext.Products
            .AsNoTracking()
            .AnyAsync(
                product => product.Name == productName,
                cancellationToken);
    }

    public async Task<Product?> GetByNameAsync(
        string productName,
        CancellationToken cancellationToken = default)
    {
        return await AppDbContext.Products
            .FirstOrDefaultAsync(
                product => product.Name == productName,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await AppDbContext.Products
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
```

## UnitOfWork

```csharp
// Domain/Common/UnitOfWork/IUnitOfWork.cs
public interface IUnitOfWork : IDisposable
{
    IProductRepository ProductRepository { get; }
    // Add more repositories as needed

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task<SaveResult> CompleteAsync(CancellationToken cancellationToken = default);
}

// Infrastructure/Persistence/UnitOfWork/UnitOfWork.cs
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

// Domain/Common/UnitOfWork/SaveResult.cs
public sealed class SaveResult
{
    public bool IsSuccess { get; init; }
    public int RowsAffected { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
}
```

## Rules

| Rule | Explanation |
|---|---|
| No `.Update()` | EF change tracker detects changes automatically |
| `GetByIdAsync` with tracking | Use for modifications — EF tracks changes |
| `GetByIdAsNoTrackingAsync` without tracking | Use for reads — more performant |
| Always `AsNoTracking()` | On read-only queries |
| Handle `DbUpdateException` | In `CompleteAsync` — never let it bubble up |
| No lazy loading | Use explicit `.Include()` always |

## Anti-patterns — NEVER DO THIS

```csharp
// ❌ NEVER call .Update() — change tracker handles this
public async Task UpdateAsync(Product product, CancellationToken ct)
{
    _appDbContext.Products.Update(product); // WRONG
    await _appDbContext.SaveChangesAsync(ct);
}

// ✅ CORRECT: EF detects changes automatically
public async Task UpdateAsync(Product product, CancellationToken ct)
{
    // Just modify the tracked entity and call CompleteAsync
    await _unitOfWork.CompleteAsync(ct);
}

// ❌ NEVER use lazy loading
public class Product
{
    public virtual ICollection<Order> Orders { get; set; } // WRONG
}

// ✅ CORRECT: Explicit includes
var product = await _appDbContext.Products
    .Include(p => p.Orders)
    .FirstOrDefaultAsync(p => p.Id == id, ct);
```