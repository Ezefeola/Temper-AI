---
name: dotnet-ef-core-queries
description: >
  Canonical EF Core query rules for tracking, includes, async materialization,
  projections, and performance-sensitive query composition.
requires: [dotnet-csharp, dotnet-linq]
produces: [ef-core-queries, async-materialization, tracking-queries]
---

# EF Core Queries — TemperAI

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **ALWAYS use `AsNoTracking()`** on read-only queries
2. **NEVER use lazy loading** — always explicit `Include()` / `ThenInclude()` when needed
3. **NEVER use `DateTime.Now`** in queries — use `DateTime.UtcNow`
4. **NEVER filter after `ToListAsync()`** when the database can filter first
5. **ALWAYS use `AnyAsync()` instead of `CountAsync() > 0`**
6. **ALWAYS pass `CancellationToken`** to async EF Core calls

## Load when

- Adding a method to an existing repository
- Writing EF Core queries inside repository implementations
- Reviewing tracking behavior, includes, projections, or pagination

## Tracking strategy — the first decision

| Intent | Tracking | Implementation |
|---|---|---|
| Load -> modify -> save | ON | Do not use `AsNoTracking()` |
| Load -> delete -> save | ON | Do not use `AsNoTracking()` |
| Load -> read / project | OFF | Use `AsNoTracking()` |
| Check existence | OFF | Use `AnyAsync()` |

```csharp
Product? tracked = await _appDbContext.Products
    .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

Product? readOnly = await _appDbContext.Products
    .AsNoTracking()
    .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
```

## Async materialization

```csharp
List<Product> list = await query.ToListAsync(cancellationToken);
Product? first = await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
bool exists = await query.AnyAsync(p => p.Name == name, cancellationToken);
Dictionary<Guid, Product> dict = await query.ToDictionaryAsync(p => p.Id, cancellationToken);

// ❌ WRONG
List<Product> syncList = query.ToList();
```

## Explicit includes — never lazy loading

```csharp
Order? order = await _appDbContext.Orders
    .AsNoTracking()
    .Include(current => current.Items)
        .ThenInclude(item => item.Product)
    .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);
```

## Split queries for multiple collections

```csharp
Order? order = await _appDbContext.Orders
    .AsNoTracking()
    .AsSplitQuery()
    .Include(current => current.Items)
    .Include(current => current.Payments)
    .Include(current => current.Shipments)
    .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);
```

## Projection — select only what you need

```csharp
List<ProductSummaryDto> summaries = await _appDbContext.Products
    .AsNoTracking()
    .Where(product => product.Status == ProductStatus.Active)
    .Select(product => new ProductSummaryDto
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price
    })
    .ToListAsync(cancellationToken);
```

## Pagination

```csharp
IQueryable<Product> query = _appDbContext.Products
    .AsNoTracking()
    .Where(product => product.Status == ProductStatus.Active);

int totalCount = await query.CountAsync(cancellationToken);

List<ProductSummaryDto> items = await query
    .OrderBy(product => product.Name)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .Select(product => new ProductSummaryDto
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price
    })
    .ToListAsync(cancellationToken);
```

## Composable filtering

```csharp
IQueryable<Product> query = _appDbContext.Products.AsNoTracking();

if (!string.IsNullOrWhiteSpace(searchTerm))
    query = query.Where(product => product.Name.Contains(searchTerm));

if (status.HasValue)
    query = query.Where(product => product.Status == status.Value);
```

## Navigation properties — prefer them over manual joins

```csharp
List<Product> results = await _appDbContext.Products
    .AsNoTracking()
    .Where(product => product.Supplier.Name == supplierName)
    .ToListAsync(cancellationToken);
```

## ExecuteUpdateAsync / ExecuteDeleteAsync

```csharp
int updated = await _appDbContext.Products
    .Where(product => product.Category == category)
    .ExecuteUpdateAsync(
        setters => setters.SetProperty(product => product.Status, ProductStatus.Inactive),
        cancellationToken);

int deleted = await _appDbContext.Products
    .Where(product => product.Status == ProductStatus.Discontinued)
    .ExecuteDeleteAsync(cancellationToken);
```

## Query tags

```csharp
List<Product> active = await _appDbContext.Products
    .AsNoTracking()
    .TagWith("GetActiveProductsByCategory")
    .Where(product => product.Status == ProductStatus.Active)
    .ToListAsync(cancellationToken);
```

## DateTime in queries

```csharp
DateTime cutoff = DateTime.UtcNow.AddDays(-30);
List<Order> recent = await _appDbContext.Orders
    .Where(order => order.CreatedAt >= cutoff)
    .ToListAsync(cancellationToken);
```

## Performance rules summary

| Rule | Reason |
|---|---|
| `AsNoTracking()` for read-only | No change tracking overhead |
| `Select` when not modifying | Fetch fewer columns |
| Explicit `Include` | Avoid lazy-loading N+1 |
| `AsSplitQuery()` for 2+ collections | Avoid Cartesian explosion |
| `AnyAsync()` over `CountAsync() > 0` | Stops on first match |
| `ExecuteUpdateAsync/DeleteAsync` for bulk updates | Avoid entity loading overhead |
| No filtering after `ToListAsync()` | Keep filtering in SQL |
