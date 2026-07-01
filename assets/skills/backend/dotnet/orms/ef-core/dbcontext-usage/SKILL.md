---
name: dbcontext-usage
description: >
  Canonical rules for using AppDbContext DIRECTLY from application code
  (use cases / handlers) when the project's data-access pattern is Direct
  DbContext — no repositories, no UnitOfWork. Load when the chosen pattern is
  Direct DbContext and a use case reads or writes data. This is the counterpart
  to repository-usage; never load both for the same task.
requires: [backend-dotnet-csharp]
produces: [dbcontext-usage, savechanges-calls]
---

# DbContext Usage (Direct) — TemperAI

Use this skill when the architect chose **Direct DbContext** as the data-access pattern. The use
case injects `AppDbContext` and talks to EF Core directly — there is no repository abstraction and
no UnitOfWork. Query composition rules (tracking, includes, projection, pagination) come from
`query-best-practices`; this skill defines how a **use case** owns the load → change → save flow.

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **Tracking is determined by intent** — tracked for modify/delete, `AsNoTracking()` for read-only (see `query-best-practices`)
2. **NEVER call `.Update()`** on a tracked entity — mutate it through its own methods and let `SaveChangesAsync` detect the change
3. **ALWAYS call `SaveChangesAsync(cancellationToken)` exactly once** at the end of a write, after all changes are staged
4. **ALWAYS handle the save outcome** and map failure to a `Result<T>.Failure(...)` — never let a `DbUpdateException` escape unhandled
5. **NEVER inject or reference a repository or `IUnitOfWork`** in a Direct DbContext project — that is the other pattern
6. **NEVER expose `AppDbContext` beyond the application layer** (controllers, DTOs, or the domain must not see it)

## When NOT to apply this skill

- The project's data-access pattern is **Repository + UnitOfWork** → use `repository-usage` instead
- You are creating the `AppDbContext` itself or its DI wiring → `dbcontext-setup`
- You are composing the query internals (tracking/includes/projection) → `query-best-practices`

## Injecting AppDbContext into a use case

```csharp
public sealed class CreateProduct : ICreateProduct
{
    private readonly AppDbContext _appDbContext;

    public CreateProduct(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    // ExecuteAsync below
}
```

`AppDbContext` is registered `Scoped` (see `dbcontext-setup`), so one instance is shared for the
whole request — every use case in a request sees the same change tracker.

## Usage patterns

### Add → save

```csharp
public async Task<Result<CreateProductResponseDto>> ExecuteAsync(
    CreateProductRequestDto request,
    CancellationToken cancellationToken)
{
    bool nameTaken = await _appDbContext.Products
        .AsNoTracking()
        .AnyAsync(product => product.Name == request.Name, cancellationToken);

    if (nameTaken)
        return Result<CreateProductResponseDto>.Failure(HttpStatusCode.Conflict)
            .WithErrors(["A product with this name already exists"]);

    (List<string> errors, Product? product) = Product.Create(request.Name, request.Price);
    if (product is null)
        return Result<CreateProductResponseDto>.Failure(HttpStatusCode.BadRequest)
            .WithErrors(errors);

    await _appDbContext.Products.AddAsync(product, cancellationToken);

    Result<CreateProductResponseDto>? saveFailure =
        await SaveOrFailureAsync<CreateProductResponseDto>(cancellationToken);
    if (saveFailure is not null)
        return saveFailure;

    return Result<CreateProductResponseDto>.Success(HttpStatusCode.Created)
        .WithPayload(product.ToCreateProductResponseDto());
}
```

### Load → modify → save (tracked)

```csharp
Product? product = await _appDbContext.Products
    .FirstOrDefaultAsync(product => product.Id == request.Id, cancellationToken);

if (product is null)
    return Result<UpdateProductResponseDto>.Failure(HttpStatusCode.NotFound)
        .WithErrors(["Product not found"]);

(List<string> errors, bool updated) = product.UpdateName(request.Name);
if (!updated)
    return Result<UpdateProductResponseDto>.Failure(HttpStatusCode.BadRequest)
        .WithErrors(errors);

// No .Update() call — the entity is tracked, SaveChangesAsync detects the change
Result<UpdateProductResponseDto>? saveFailure =
    await SaveOrFailureAsync<UpdateProductResponseDto>(cancellationToken);
if (saveFailure is not null)
    return saveFailure;

return Result<UpdateProductResponseDto>.Success(HttpStatusCode.OK);
```

### Load → delete → save

```csharp
Product? product = await _appDbContext.Products
    .FirstOrDefaultAsync(product => product.Id == request.Id, cancellationToken);

if (product is null)
    return Result<DeleteProductResponseDto>.Failure(HttpStatusCode.NotFound)
        .WithErrors(["Product not found"]);

_appDbContext.Products.Remove(product);

Result<DeleteProductResponseDto>? saveFailure =
    await SaveOrFailureAsync<DeleteProductResponseDto>(cancellationToken);
if (saveFailure is not null)
    return saveFailure;

return Result<DeleteProductResponseDto>.Success(HttpStatusCode.NoContent);
```

### Read-only → project → return

```csharp
ProductResponseDto? dto = await _appDbContext.Products
    .AsNoTracking()
    .Where(product => product.Id == request.Id)
    .Select(product => product.ToProductResponseDto())
    .FirstOrDefaultAsync(cancellationToken);

if (dto is null)
    return Result<ProductResponseDto>.Failure(HttpStatusCode.NotFound)
        .WithErrors(["Product not found"]);

return Result<ProductResponseDto>.Success(HttpStatusCode.OK).WithPayload(dto);
```

## Handling the save outcome

Because there is no `UnitOfWork.CompleteAsync` to normalize the result, guard `SaveChangesAsync`
against `DbUpdateException` (concurrency conflicts, constraint violations) and map it to a failure.
Keep it in one small private helper so every write path handles it identically:

```csharp
private async Task<Result<TResponse>?> SaveOrFailureAsync<TResponse>(CancellationToken cancellationToken)
{
    try
    {
        await _appDbContext.SaveChangesAsync(cancellationToken);
        return null; // null == saved successfully
    }
    catch (DbUpdateException dbUpdateException)
    {
        return Result<TResponse>.Failure(HttpStatusCode.InternalServerError)
            .WithErrors([dbUpdateException.InnerException?.Message ?? dbUpdateException.Message]);
    }
}
```

## Multi-step writes that must be atomic

A single `SaveChangesAsync` is already one transaction. Only open an explicit transaction when one
logical operation spans **multiple** `SaveChanges` calls (or mixes a set-based
`ExecuteUpdate/DeleteAsync` with tracked changes) and all must commit or roll back together.

```csharp
await using IDbContextTransaction transaction =
    await _appDbContext.Database.BeginTransactionAsync(cancellationToken);
try
{
    // ... multiple SaveChangesAsync / ExecuteUpdateAsync calls ...
    await _appDbContext.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
}
catch
{
    await transaction.RollbackAsync(cancellationToken);
    throw;
}
```

For the ordinary "load one aggregate, change it, save" path you do **not** need an explicit
transaction — a single `SaveChangesAsync` is atomic on its own.

## What you must not do

```csharp
// ❌ NEVER call .Update() — the tracked entity already reports its own changes
_appDbContext.Products.Update(product);

// ❌ NEVER call SaveChangesAsync multiple times for one logical write
await _appDbContext.SaveChangesAsync(cancellationToken);
await _appDbContext.SaveChangesAsync(cancellationToken);

// ❌ NEVER ignore the save result
await _appDbContext.SaveChangesAsync(cancellationToken); // unguarded — a DbUpdateException escapes as a 500 with no Result

// ❌ NEVER load AsNoTracking when you intend to modify + save
Product? product = await _appDbContext.Products
    .AsNoTracking()
    .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

// ❌ NEVER inject a repository or IUnitOfWork here — that is the other pattern
```

## Related skills

- Query internals (tracking, includes, projection, pagination, tricks) → `query-best-practices`
- Creating / registering the `AppDbContext` → `dbcontext-setup`
- Use-case naming, `Result<T>` flow, DI → `use-case-patterns`
- Repository + UnitOfWork alternative (never combined with this skill) → `repository-usage`
