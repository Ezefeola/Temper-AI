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
- Writing use cases that call `_unitOfWork.ProductRepository.GetByIdAsync(...)`
- Adding a new query method to an existing repository interface and implementation
- Any code that calls `_unitOfWork.CompleteAsync()`

For creating repositories or UnitOfWork from scratch, load `REPOSITORY_PATTERN.md` instead.

---

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **ALWAYS use `GetByIdAsync`** when you will modify the entity after loading
2. **ALWAYS use `GetByIdAsNoTrackingAsync`** for read-only operations
3. **NEVER call `.Update()`** — EF change tracker handles this automatically
4. **ALWAYS call `CompleteAsync()`** after any modification — never call `SaveChangesAsync` directly
5. **ALWAYS check `SaveResult.IsSuccess`** after `CompleteAsync` — never assume success

---

## Available contracts

### IGenericRepository\<TEntity\>

```csharp
// Load entity for MODIFICATION — EF tracks changes
Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

// Load entity for READ ONLY — no tracking overhead
Task<TEntity?> GetByIdAsNoTrackingAsync(Guid id, CancellationToken ct = default);

// Add new entity — does NOT save, call CompleteAsync after
Task AddAsync(TEntity entity, CancellationToken ct = default);

// Mark entity for deletion — does NOT save, call CompleteAsync after
void Delete(TEntity entity);
```

### IUnitOfWork

```csharp
// Access repositories
IProductRepository ProductRepository { get; }

// Persist all pending changes — always check SaveResult.IsSuccess
Task<SaveResult> CompleteAsync(CancellationToken ct = default);

// Transaction management — only when atomicity across multiple aggregates is needed
Task BeginTransactionAsync(CancellationToken ct = default);
Task CommitTransactionAsync(CancellationToken ct = default);
Task RollbackTransactionAsync(CancellationToken ct = default);
```

### SaveResult

```csharp
bool IsSuccess      // always check this
int RowsAffected    // informational
string ErrorMessage // populated when IsSuccess is false
```

---

## Usage patterns

### Load → modify → save

```csharp
// ✅ CORRECT — load with tracking, modify, save
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
// ✅ CORRECT — create entity, add, save
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

### Load for read → return

```csharp
// ✅ CORRECT — no tracking for reads
Product? product = await _unitOfWork.ProductRepository
    .GetByIdAsNoTrackingAsync(request.Id, cancellationToken);

if (product is null)
    return Result<...>.Failure(HttpStatusCode.NotFound).WithErrors(["Product not found"]);

return Result<...>.Success(HttpStatusCode.OK)
    .WithPayload(product.ToProductResponseDto());
```

### Delete entity → save

```csharp
// ✅ CORRECT — load with tracking (required for Delete), then delete
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

---

## Tracking decision — quick reference

| You need to... | Method |
|---|---|
| Load and modify | `GetByIdAsync` — tracking on |
| Load and read/display | `GetByIdAsNoTrackingAsync` — tracking off |
| Load and delete | `GetByIdAsync` — tracking required for `Delete()` |
| Check if exists | `ExistsByNameAsync` or `AnyAsync` — no entity loaded |

---

## What you must NOT do

```csharp
// ❌ NEVER call .Update() — change tracker handles modifications automatically
_appDbContext.Products.Update(product);

// ❌ NEVER call SaveChangesAsync directly — always go through CompleteAsync
await _appDbContext.SaveChangesAsync(cancellationToken);

// ❌ NEVER assume CompleteAsync succeeded — always check IsSuccess
SaveResult result = await _unitOfWork.CompleteAsync(cancellationToken);
return Result<...>.Success(HttpStatusCode.OK);  // WRONG — result was not checked

// ❌ NEVER load with GetByIdAsNoTrackingAsync if you plan to modify
Product? product = await _unitOfWork.ProductRepository
    .GetByIdAsNoTrackingAsync(id, cancellationToken);
product.UpdateName("New");  // changes will NOT be saved — entity is not tracked
```