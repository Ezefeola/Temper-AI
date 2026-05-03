---
name: repository-usage
description: >
  Contracts and usage rules for existing repositories and UnitOfWork.
  Load when using repositories in use cases, adding query methods to existing
  repositories, or writing any code that interacts with IUnitOfWork.
  DO NOT load when creating repositories from scratch — load REPOSITORY_PATTERN.md instead.
requires: [dotnet-csharp]
produces: [correct-repository-usage, unit-of-work-calls]
---

# Repository Usage — TemperAI

## When to load this file

Load this file when the task involves **using** repositories that already exist:
- Writing use cases that call repository methods through `_unitOfWork`
- Adding a new query method to an existing repository interface and implementation
- Any code that calls `_unitOfWork.CompleteAsync()`

For creating repositories or UnitOfWork from scratch, load `REPOSITORY_PATTERN.md` instead.

---

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **Tracking is determined by intent, not by method name** — if you will modify or delete the entity after loading it, the repository method you call must NOT use `AsNoTracking()`. If you only read or project to a DTO, the method MUST use `AsNoTracking()`. See the tracking rules section below.
2. **NEVER call `.Update()`** — EF change tracker detects modifications automatically on tracked entities
3. **ALWAYS call `CompleteAsync()`** after any write operation — never call `SaveChangesAsync()` directly
4. **ALWAYS check `SaveResult.IsSuccess`** after `CompleteAsync()` — never assume success
5. **When implementing a new repository method**, you are responsible for deciding tracking — the use case cannot override it from the outside

---

## Tracking rules — the principle is EF intent, not the method name

EF Core's change tracker is what allows `CompleteAsync()` to detect and persist modifications automatically. The tracking decision must be made at the **repository method implementation level**, and respected at the **use case call level**.

### Rule: match the method to your intent

| Intent | Tracking | How to implement the repository method |
|---|---|---|
| Load → modify → save | ON | Do NOT add `AsNoTracking()` |
| Load → delete → save | ON | Do NOT add `AsNoTracking()` — `Remove()` requires a tracked entity |
| Load → read / project to DTO | OFF | Add `AsNoTracking()` |
| Load collection → read / project | OFF | Add `AsNoTracking()` — collections are almost always read-only |
| Check existence only | OFF | Use `AnyAsync()` — don't load the entity at all |

### Why this matters

If you load an entity with `AsNoTracking()` and then modify it, EF will not detect the change — `CompleteAsync()` will succeed but **nothing will be persisted**. There will be no exception; the data will silently not update.

If you load an entity without `AsNoTracking()` when you only need to read it, you pay the overhead of the change tracker for no reason.

---

## Available contracts

### IGenericRepository\<TEntity\>

```csharp
// Load entity WITH tracking — use when you will modify or delete after loading
Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

// Load entity WITHOUT tracking — use when you will only read or project to DTO
Task<TEntity?> GetByIdAsNoTrackingAsync(Guid id, CancellationToken ct = default);

// Stage new entity for insertion — does NOT save, call CompleteAsync after
Task AddAsync(TEntity entity, CancellationToken ct = default);

// Stage entity for deletion — does NOT save, call CompleteAsync after
// Entity MUST be tracked — load with a non-AsNoTracking method first
void Delete(TEntity entity);
```

> **Note:** `GetByIdAsync` and `GetByIdAsNoTrackingAsync` are the generic base methods.
> Specific repositories may expose additional methods (e.g. `GetByNameAsync`, `GetWithOrdersAsync`).
> The same tracking rules apply to all of them — check the method's implementation
> to know whether it uses `AsNoTracking()` before deciding which one to call.

### IUnitOfWork

```csharp
// Access repositories — one property per aggregate root
IProductRepository ProductRepository { get; }

// Persist all pending changes — ALWAYS check SaveResult.IsSuccess
Task<SaveResult> CompleteAsync(CancellationToken ct = default);

// Explicit transactions — only needed when writing to multiple aggregate roots atomically
Task BeginTransactionAsync(CancellationToken ct = default);
Task CommitTransactionAsync(CancellationToken ct = default);
Task RollbackTransactionAsync(CancellationToken ct = default);
```

### SaveResult

```csharp
bool IsSuccess      // ALWAYS check this — never assume true
int RowsAffected    // informational
string ErrorMessage // populated only when IsSuccess is false
```

---

## Usage patterns

### Load → modify → save

```csharp
// ✅ CORRECT
// GetByIdAsync returns a tracked entity — EF detects the modification automatically.
// No .Update() call needed. CompleteAsync() sends the change to the DB.
Product? product = await _unitOfWork.ProductRepository
    .GetByIdAsync(request.Id, cancellationToken);

if (product is null)
    return Result<...>.Failure(HttpStatusCode.NotFound).WithErrors(["Product not found"]);

(List<string> errors, bool updated) = product.UpdateName(request.Name);
if (!updated)
    return Result<...>.Failure(HttpStatusCode.BadRequest).WithErrors(errors);

SaveResult saveResult = await _unitOfWork.CompleteAsync(cancellationToken);
if (!saveResult.IsSuccess)
    return Result<...>.Failure(HttpStatusCode.InternalServerError)
        .WithErrors([saveResult.ErrorMessage]);

return Result<...>.Success(HttpStatusCode.OK);
```

### Add new entity → save

```csharp
// ✅ CORRECT
// AddAsync stages the entity — EF starts tracking it as Added.
// CompleteAsync() issues the INSERT.
(List<string> errors, Product? product) = Product.Create(request.Name, request.Price);
if (product is null)
    return Result<...>.Failure(HttpStatusCode.BadRequest).WithErrors(errors);

await _unitOfWork.ProductRepository.AddAsync(product, cancellationToken);

SaveResult saveResult = await _unitOfWork.CompleteAsync(cancellationToken);
if (!saveResult.IsSuccess)
    return Result<...>.Failure(HttpStatusCode.InternalServerError)
        .WithErrors([saveResult.ErrorMessage]);

return Result<...>.Success(HttpStatusCode.Created);
```

### Load for read → project to DTO → return

```csharp
// ✅ CORRECT
// GetByIdAsNoTrackingAsync — no tracking overhead since we only read.
// Never use a tracked method here; it wastes resources for no benefit.
Product? product = await _unitOfWork.ProductRepository
    .GetByIdAsNoTrackingAsync(request.Id, cancellationToken);

if (product is null)
    return Result<...>.Failure(HttpStatusCode.NotFound).WithErrors(["Product not found"]);

return Result<...>.Success(HttpStatusCode.OK)
    .WithPayload(product.ToProductResponseDto());
```

### Load → delete → save

```csharp
// ✅ CORRECT
// Delete() calls EF's Remove() internally, which requires the entity to be tracked.
// GetByIdAsync (tracked) is mandatory here — AsNoTracking would cause Remove() to fail
// or silently do nothing depending on the EF version.
Product? product = await _unitOfWork.ProductRepository
    .GetByIdAsync(request.Id, cancellationToken);

if (product is null)
    return Result<...>.Failure(HttpStatusCode.NotFound).WithErrors(["Product not found"]);

_unitOfWork.ProductRepository.Delete(product);

SaveResult saveResult = await _unitOfWork.CompleteAsync(cancellationToken);
if (!saveResult.IsSuccess)
    return Result<...>.Failure(HttpStatusCode.InternalServerError)
        .WithErrors([saveResult.ErrorMessage]);

return Result<...>.Success(HttpStatusCode.NoContent);
```

### Adding a new method to an existing repository

When adding a query method to a specific repository interface and implementation,
apply the tracking decision at the implementation level:

```csharp
// In the interface — name reflects intent, not tracking detail
Task<Product?> GetByNameAsync(string name, CancellationToken ct = default);
Task<Product?> GetByNameForUpdateAsync(string name, CancellationToken ct = default);
Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);

// In the implementation
public async Task<Product?> GetByNameAsync(string name, CancellationToken ct = default)
{
    // Read-only — always AsNoTracking
    return await AppDbContext.Products
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Name == name, ct);
}

public async Task<Product?> GetByNameForUpdateAsync(string name, CancellationToken ct = default)
{
    // Will be modified — no AsNoTracking
    return await AppDbContext.Products
        .FirstOrDefaultAsync(p => p.Name == name, ct);
}

public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
{
    // Collections are almost always read-only — always AsNoTracking
    return await AppDbContext.Products
        .AsNoTracking()
        .ToListAsync(ct);
}
```

> Return type for collections is always `IReadOnlyList<T>` — never `List<T>` or `IEnumerable<T>`.

---

## What you must NOT do

```csharp
// ❌ NEVER call .Update() — change tracker handles modifications on tracked entities automatically
_appDbContext.Products.Update(product);

// ❌ NEVER call SaveChangesAsync directly — always go through UnitOfWork.CompleteAsync()
await _appDbContext.SaveChangesAsync(cancellationToken);

// ❌ NEVER assume CompleteAsync succeeded — always check IsSuccess
SaveResult result = await _unitOfWork.CompleteAsync(cancellationToken);
return Result<...>.Success(HttpStatusCode.OK); // WRONG — IsSuccess was never checked

// ❌ NEVER load with AsNoTracking if you intend to modify — changes will be silently lost
Product? product = await _unitOfWork.ProductRepository
    .GetByIdAsNoTrackingAsync(id, cancellationToken);
product.UpdateName("New Name"); // EF does not know about this change — it will NOT be saved

// ❌ NEVER load with tracking if you only intend to read — unnecessary overhead
Product? product = await _unitOfWork.ProductRepository
    .GetByIdAsync(id, cancellationToken);         // tracking on
return Result<...>.Success(HttpStatusCode.OK)
    .WithPayload(product.ToProductResponseDto()); // but you only needed the DTO

// ❌ NEVER return List<T> or IEnumerable<T> from collection repository methods
Task<List<Product>> GetAllAsync(...);       // wrong
Task<IEnumerable<Product>> GetAllAsync(...); // wrong
Task<IReadOnlyList<Product>> GetAllAsync(...); // correct
```