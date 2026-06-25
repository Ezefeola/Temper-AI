---
name: repository-usage
description: >
  Canonical rules for using existing repositories and UnitOfWork from application code.
  Load when calling repositories or CompleteAsync from use cases.
requires: [dotnet-csharp]
produces: [repository-usage, unit-of-work-calls]
---

# Repository Usage — TemperAI

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **Tracking is determined by intent** — tracked for modify/delete, no-tracking for read-only
2. **NEVER call `.Update()`**
3. **ALWAYS call `CompleteAsync()` after successful writes**
4. **ALWAYS check `SaveResult.IsSuccess`** after `CompleteAsync()`
5. **NEVER assume the use case can override repository tracking externally**

## Tracking rules

- Load to modify or delete: repository method must keep tracking enabled
- Load to read or project: repository method must use `AsNoTracking()`
- Existence checks: prefer `AnyAsync()`

## Available contracts

```csharp
Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
Task<TEntity?> GetByIdAsNoTrackingAsync(Guid id, CancellationToken ct = default);
Task AddAsync(TEntity entity, CancellationToken ct = default);
void Delete(TEntity entity);

IProductRepository ProductRepository { get; }
Task<SaveResult> CompleteAsync(CancellationToken ct = default);
Task BeginTransactionAsync(CancellationToken ct = default);
Task CommitTransactionAsync(CancellationToken ct = default);
Task RollbackTransactionAsync(CancellationToken ct = default);
```

## Usage patterns

### Load -> modify -> save

```csharp
Product? product = await _unitOfWork.ProductRepository
    .GetByIdAsync(request.Id, cancellationToken);

if (product is null)
    return Result<UpdateProductResponseDto>.Failure(HttpStatusCode.NotFound)
        .WithErrors(["Product not found"]);

(List<string> errors, bool updated) = product.UpdateName(request.Name);
if (!updated)
    return Result<UpdateProductResponseDto>.Failure(HttpStatusCode.BadRequest)
        .WithErrors(errors);

SaveResult saveResult = await _unitOfWork.CompleteAsync(cancellationToken);
if (!saveResult.IsSuccess)
    return Result<UpdateProductResponseDto>.Failure(HttpStatusCode.InternalServerError)
        .WithErrors([saveResult.ErrorMessage]);

return Result<UpdateProductResponseDto>.Success(HttpStatusCode.OK);
```

### Add -> save

```csharp
(List<string> errors, Product? product) = Product.Create(request.Name, request.Price);
if (product is null)
    return Result<CreateProductResponseDto>.Failure(HttpStatusCode.BadRequest)
        .WithErrors(errors);

await _unitOfWork.ProductRepository.AddAsync(product, cancellationToken);

SaveResult saveResult = await _unitOfWork.CompleteAsync(cancellationToken);
if (!saveResult.IsSuccess)
    return Result<CreateProductResponseDto>.Failure(HttpStatusCode.InternalServerError)
        .WithErrors([saveResult.ErrorMessage]);

return Result<CreateProductResponseDto>.Success(HttpStatusCode.Created);
```

### Read-only load -> project -> return

```csharp
Product? product = await _unitOfWork.ProductRepository
    .GetByIdAsNoTrackingAsync(request.Id, cancellationToken);

if (product is null)
    return Result<ProductResponseDto>.Failure(HttpStatusCode.NotFound)
        .WithErrors(["Product not found"]);

return Result<ProductResponseDto>.Success(HttpStatusCode.OK)
    .WithPayload(product.ToProductResponseDto());
```

### Load -> delete -> save

```csharp
Product? product = await _unitOfWork.ProductRepository
    .GetByIdAsync(request.Id, cancellationToken);

if (product is null)
    return Result<DeleteProductResponseDto>.Failure(HttpStatusCode.NotFound)
        .WithErrors(["Product not found"]);

_unitOfWork.ProductRepository.Delete(product);

SaveResult saveResult = await _unitOfWork.CompleteAsync(cancellationToken);
if (!saveResult.IsSuccess)
    return Result<DeleteProductResponseDto>.Failure(HttpStatusCode.InternalServerError)
        .WithErrors([saveResult.ErrorMessage]);

return Result<DeleteProductResponseDto>.Success(HttpStatusCode.NoContent);
```

## Adding a new method to an existing repository

```csharp
public interface IProductRepository
{
    Task<Product?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Product?> GetByNameForUpdateAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
}

public async Task<Product?> GetByNameAsync(string name, CancellationToken ct = default)
{
    return await AppDbContext.Products
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Name == name, ct);
}

public async Task<Product?> GetByNameForUpdateAsync(string name, CancellationToken ct = default)
{
    return await AppDbContext.Products
        .FirstOrDefaultAsync(p => p.Name == name, ct);
}
```

## What you must not do

```csharp
// ❌ NEVER call .Update()
_appDbContext.Products.Update(product);

// ❌ NEVER call SaveChangesAsync directly
await _appDbContext.SaveChangesAsync(cancellationToken);

// ❌ NEVER ignore SaveResult.IsSuccess
SaveResult result = await _unitOfWork.CompleteAsync(cancellationToken);

// ❌ NEVER load with AsNoTracking when you intend to modify
Product? product = await _unitOfWork.ProductRepository
    .GetByIdAsNoTrackingAsync(id, cancellationToken);
```
