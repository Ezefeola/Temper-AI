---
name: dotnet-linq
description: >
  LINQ query standards for .NET 10 projects using EF Core. Covers query
  patterns, projections, includes, pagination, and performance best practices.
  Use when writing or reviewing LINQ queries in repositories or use cases.
---

# LINQ — TemperAI Standards

## Query patterns

### Tracking vs NoTracking

- Use tracking queries (`GetByIdAsync`) when the entity will be modified.
- Use no-tracking queries (`GetByIdAsNoTrackingAsync`) for read-only operations.
- Always express tracking behavior in the method name.

```csharp
// With tracking — for modifications
public async Task<Product?> GetByIdAsync(
    Guid productId,
    CancellationToken cancellationToken = default)
{
    return await _appDbContext.Products
        .FirstOrDefaultAsync(
            product => product.Id == productId,
            cancellationToken);
}

// Without tracking — for reads
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
```

### Explicit includes — never lazy loading

```csharp
// GOOD — explicit include
public async Task<Order?> GetByIdWithItemsAsync(
    Guid orderId,
    CancellationToken cancellationToken = default)
{
    return await _appDbContext.Orders
        .AsNoTracking()
        .Include(order => order.Items)
        .FirstOrDefaultAsync(
            order => order.Id == orderId,
            cancellationToken);
}

// BAD — relies on lazy loading
public async Task<Order?> GetByIdAsync(Guid orderId)
{
    return await _appDbContext.Orders.FindAsync(orderId);
    // order.Items will trigger N+1 if accessed
}
```

### Projections — select only what you need

```csharp
// GOOD — project to DTO, only fetch needed columns
public async Task<List<ProductSummaryDto>> GetAllSummariesAsync(
    CancellationToken cancellationToken = default)
{
    return await _appDbContext.Products
        .AsNoTracking()
        .Select(product => new ProductSummaryDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price
        })
        .ToListAsync(cancellationToken);
}

// BAD — fetches entire entity graph when only summary is needed
public async Task<List<Product>> GetAllAsync()
{
    return await _appDbContext.Products.ToListAsync();
}
```

### Pagination

```csharp
public async Task<PagedResult<Product>> GetPagedAsync(
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
{
    IQueryable<Product> query = _appDbContext.Products.AsNoTracking();

    int totalCount = await query.CountAsync(cancellationToken);

    List<Product> items = await query
        .OrderBy(product => product.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    return new PagedResult<Product>
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
```

### Filtering

```csharp
// GOOD — composable filters
public async Task<List<Product>> GetByStatusAsync(
    ProductStatus status,
    CancellationToken cancellationToken = default)
{
    return await _appDbContext.Products
        .AsNoTracking()
        .Where(product => product.Status == status)
        .ToListAsync(cancellationToken);
}

// GOOD — string search with EF Core translation
public async Task<List<Product>> SearchByNameAsync(
    string searchTerm,
    CancellationToken cancellationToken = default)
{
    return await _appDbContext.Products
        .AsNoTracking()
        .Where(product => product.Name.Contains(searchTerm))
        .ToListAsync(cancellationToken);
}
```

### IN clause with Contains

```csharp
// GOOD — translates to WHERE Id IN (...)
public async Task<List<Product>> GetByIdsAsync(
    List<Guid> ids,
    CancellationToken cancellationToken = default)
{
    return await _appDbContext.Products
        .AsNoTracking()
        .Where(product => ids.Contains(product.Id))
        .ToListAsync(cancellationToken);
}
```

### ExecuteUpdateAsync / ExecuteDeleteAsync (EF Core 7+)

Use for bulk operations without loading entities into the change tracker.

```csharp
// GOOD — single SQL UPDATE, no entity loading
public async Task<int> ActivateProductsByCategoryAsync(
    string category,
    CancellationToken cancellationToken = default)
{
    return await _appDbContext.Products
        .Where(product => product.Category == category)
        .ExecuteUpdateAsync(
            setters => setters.SetProperty(p => p.Status, ProductStatus.Active),
            cancellationToken);
}

// BAD — loads all entities, updates each, then saves
var products = await _appDbContext.Products
    .Where(p => p.Category == category)
    .ToListAsync(cancellationToken);

foreach (var product in products)
{
    product.Status = ProductStatus.Active;
}

await _appDbContext.SaveChangesAsync(cancellationToken);
```

### Query tags for debugging

```csharp
// GOOD — query appears in logs with tag for easy identification
public async Task<List<Product>> GetActiveProductsAsync(
    CancellationToken cancellationToken = default)
{
    return await _appDbContext.Products
        .AsNoTracking()
        .TagWith("GetActiveProducts")
        .Where(product => product.Status == ProductStatus.Active)
        .ToListAsync(cancellationToken);
}
```

### Split queries for multiple collections

Use `AsSplitQuery()` when including multiple collection navigations to avoid Cartesian explosion.

```csharp
// GOOD — separate SQL queries for each collection
public async Task<Order?> GetByIdWithDetailsAsync(
    Guid orderId,
    CancellationToken cancellationToken = default)
{
    return await _appDbContext.Orders
        .AsNoTracking()
        .AsSplitQuery()
        .Include(order => order.Items)
        .Include(order => order.Payments)
        .Include(order => order.Shipments)
        .FirstOrDefaultAsync(
            order => order.Id == orderId,
            cancellationToken);
}
```

### Navigation properties — never manual joins

```csharp
// GOOD — uses navigation property, EF Core generates optimal JOIN
public async Task<List<Product>> GetProductsBySupplierNameAsync(
    string supplierName,
    CancellationToken cancellationToken = default)
{
    return await _appDbContext.Products
        .AsNoTracking()
        .Where(product => product.Supplier.Name == supplierName)
        .ToListAsync(cancellationToken);
}

// BAD — manual join, harder to read, same result
public async Task<List<Product>> GetProductsBySupplierNameAsync(
    string supplierName,
    CancellationToken cancellationToken = default)
{
    return await _appDbContext.Products
        .AsNoTracking()
        .Join(
            _appDbContext.Suppliers,
            product => product.SupplierId,
            supplier => supplier.Id,
            (product, supplier) => new { product, supplier })
        .Where(x => x.supplier.Name == supplierName)
        .Select(x => x.product)
        .ToListAsync(cancellationToken);
}
```

## Performance rules

- **Always** use `AsNoTracking()` for read-only queries.
- **Always** use `Select` to project to DTOs when you do not need the full entity.
- **Always** use `Include` explicitly — never rely on lazy loading.
- **Always** use `CancellationToken` in async LINQ methods.
- **Always** use `DateTime.UtcNow` in queries — `DateTime.Now` may not translate correctly to SQL.
- **Always** use `AnyAsync()` instead of `CountAsync() > 0` — `AnyAsync` stops at the first match.
- **Always** use `AsSplitQuery()` when including 2+ collection navigations.
- **Always** use `ExecuteUpdateAsync` / `ExecuteDeleteAsync` for bulk operations.
- **Never** use `.ToList()` before filtering — filter in the database, not in memory.
- **Never** use `.AsEnumerable()` before filtering — it pulls all data into memory.
- **Never** use client-side methods in LINQ expressions that EF Core cannot translate (e.g., custom methods, `DateTime.Now`).
- **Never** use `Contains` on large in-memory collections (> 10,000 items) — use a database-side join or temporary table.
- **Never** use manual `.Join()` — always use navigation properties instead.
- **Never** use `DateTime.Now` in queries — always use `DateTime.UtcNow`.

## Query composition pattern

```csharp
// GOOD — build queries incrementally
public async Task<List<Product>> GetFilteredAsync(
    string? searchTerm,
    ProductStatus? status,
    CancellationToken cancellationToken = default)
{
    IQueryable<Product> query = _appDbContext.Products.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        query = query.Where(product => product.Name.Contains(searchTerm));
    }

    if (status.HasValue)
    {
        query = query.Where(product => product.Status == status.Value);
    }

    return await query
        .OrderBy(product => product.Name)
        .ToListAsync(cancellationToken);
}
```
