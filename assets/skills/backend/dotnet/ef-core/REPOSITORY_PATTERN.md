---
name: repository-pattern
description: >
  Full implementation of GenericRepository, specific repositories, and UnitOfWork.
  Load when CREATING repositories or UnitOfWork from scratch.
  DO NOT load when only adding query methods to existing repositories or using
  repositories in use cases — load REPOSITORY_USAGE.md instead.
requires: [dotnet-ef-core]
produces: [generic-repository, specific-repositories, unit-of-work, save-result]
---

# Repository Pattern — Implementation — TemperAI

## When to load this file

Load this file when the task requires **creating** any of these from scratch:
- `GenericRepository<TEntity>` base class
- A new specific repository interface and implementation
- `IUnitOfWork` interface or `UnitOfWork` implementation
- `SaveResult`

For adding methods to an existing repository or using repositories in use cases,
load `REPOSITORY_USAGE.md` instead — it is significantly lighter.

---

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **NEVER call `.Update()`** — EF change tracker handles this automatically
2. **NEVER use lazy loading** — always explicit `.Include()`
3. **ALWAYS use `AsNoTracking()`** on read-only queries
4. **ALWAYS handle `DbUpdateException`** in `CompleteAsync` — never let it bubble up
5. **ALWAYS return `IReadOnlyList<T>`** from collection methods — never `List<T>`

---

## GenericRepository\<TEntity\>

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
            .FirstOrDefaultAsync(
                entity => EF.Property<Guid>(entity, "Id") == id,
                cancellationToken);
    }

    public async Task<TEntity?> GetByIdAsNoTrackingAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await AppDbContext.Set<TEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entity => EF.Property<Guid>(entity, "Id") == id,
                cancellationToken);
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

## IGenericRepository\<TEntity\>

```csharp
// Domain/Common/Repositories/IGenericRepository.cs
public interface IGenericRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsNoTrackingAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Delete(TEntity entity);
}
```

---

## Specific repository — interface and implementation

```csharp
// Domain/Products/Repositories/IProductRepository.cs
public interface IProductRepository : IGenericRepository<Product>
{
    Task<bool> ExistsByNameAsync(string productName, CancellationToken cancellationToken = default);
    Task<Product?> GetByNameAsync(string productName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
}

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
            .AnyAsync(product => product.Name == productName, cancellationToken);
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

---

## UnitOfWork

```csharp
// Domain/Common/UnitOfWork/IUnitOfWork.cs
public interface IUnitOfWork : IDisposable
{
    IProductRepository ProductRepository { get; }
    // Add repositories as the project grows

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
            return new SaveResult { IsSuccess = true, RowsAffected = rowsAffected };
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

---

## Anti-patterns — NEVER DO THIS

```csharp
// ❌ NEVER call .Update()
_appDbContext.Products.Update(product);

// ✅ CORRECT — modify tracked entity, call CompleteAsync
product.UpdateName("New Name");
await _unitOfWork.CompleteAsync(cancellationToken);

// ❌ NEVER lazy loading navigation properties
public class Product
{
    public virtual ICollection<Order> Orders { get; set; }
}

// ✅ CORRECT — explicit includes in repository methods
return await AppDbContext.Products
    .Include(p => p.Orders)
    .FirstOrDefaultAsync(p => p.Id == id, ct);
```