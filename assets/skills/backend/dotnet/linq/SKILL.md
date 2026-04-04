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

## Performance rules

- **Always** use `AsNoTracking()` for read-only queries.
- **Always** use `Select` to project to DTOs when you do not need the full entity.
- **Always** use `Include` explicitly — never rely on lazy loading.
- **Always** use `CancellationToken` in async LINQ methods.
- **Never** use `.ToList()` before filtering — filter in the database, not in memory.
- **Never** use `.AsEnumerable()` before filtering — it pulls all data into memory.
- **Never** use client-side methods in LINQ expressions that EF Core cannot translate (e.g., custom methods, `DateTime.Now`).
- **Never** use `Contains` on large in-memory collections — use a database-side join or temporary table.

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
