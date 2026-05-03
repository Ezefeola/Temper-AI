---
name: dotnet-ef-core-queries
description: >
  EF Core query standards for .NET 10 projects. Covers tracking strategy,
  explicit includes, async materialization, performance patterns, bulk operations,
  and all EF Core-specific query extensions.
  Load when writing queries against DbContext or repositories — whether creating
  a new repository method or adding queries in use cases.
  DO NOT load for tasks that only create repositories or DbContext from scratch —
  load dotnet-ef-core instead.
requires: [dotnet-csharp, dotnet-linq]
produces: [ef-core-queries, async-materializations, tracking-queries, bulk-operations]
---

# EF Core Queries — TemperAI Standards

## What this skill covers

Everything you need to write **correct and performant queries against EF Core** —
whether you are adding a method to an existing repository, writing a query in a use case,
or reviewing existing query code.

**This skill assumes the following already exist:**
- `AppDbContext` with configured `DbSet<T>` properties
- Repository interfaces and implementations following `REPOSITORY_PATTERN.md`
- Entity configurations following `ENTITY_CONFIGURATION.md`

If any of those do not exist yet, load `dotnet-ef-core` first to create them.

---

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **ALWAYS `AsNoTracking()`** on read-only queries — never track what you won't modify
2. **NEVER use lazy loading** — always explicit `.Include()`
3. **NEVER use `DateTime.Now`** in queries — always `DateTime.UtcNow`
4. **NEVER filter after `.ToListAsync()`** — filter in the database, not in memory
5. **ALWAYS use `AnyAsync()`** instead of `CountAsync() > 0`
6. **ALWAYS pass `CancellationToken`** to every async EF Core method

---

## When NOT to load this skill

- Task creates repositories, UnitOfWork, or DbContext from scratch → load `dotnet-ef-core` instead
- Task has no EF Core query logic (pure domain or application logic, no DbContext access)

---

## Tracking strategy — the most important decision

Every query starts with a tracking decision. Make it explicit.

| You will... | Use | Why |
|---|---|---|
| Modify the entity after loading | `GetByIdAsync` (no `AsNoTracking`) | EF tracks changes — `CompleteAsync` saves automatically |
| Only read / display the entity | `GetByIdAsNoTrackingAsync` | No change tracking overhead |
| Load a collection for display | `.AsNoTracking()` on the query | Never track what you won't modify |
| Check existence | `AnyAsync(predicate)` | No entity loaded, no tracking |

```csharp
// ✅ For modification — with tracking
Product? product = await _appDbContext.Products
    .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

// ✅ For read-only — no tracking
Product? product = await _appDbContext.Products
    .AsNoTracking()
    .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

// ✅ Collection — always no tracking
List<Product> products = await _appDbContext.Products
    .AsNoTracking()
    .Where(p => p.Status == ProductStatus.Active)
    .ToListAsync(cancellationToken);
```

---

## Async materialization — EF Core extension methods

These methods come from `Microsoft.EntityFrameworkCore` — they are NOT part of LINQ.
Always use the async versions when querying a database.

```csharp
// ✅ Always async when querying DB
List<Product> list = await query.ToListAsync(cancellationToken);
Product? first = await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
bool exists = await query.AnyAsync(p => p.Name == name, cancellationToken);
int count = await query.CountAsync(cancellationToken);
Dictionary<Guid, Product> dict = await query.ToDictionaryAsync(p => p.Id, cancellationToken);

// ❌ WRONG — synchronous materialization on a DB query
List<Product> list = query.ToList();
Product? first = query.FirstOrDefault(p => p.Id == id);
```

---

## Explicit includes — never lazy loading

Always declare what you need. Never rely on navigation properties loading themselves.

```csharp
// ✅ CORRECT — explicit include
Order? order = await _appDbContext.Orders
    .AsNoTracking()
    .Include(order => order.Items)
    .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

// ✅ Nested include
Order? order = await _appDbContext.Orders
    .AsNoTracking()
    .Include(order => order.Items)
        .ThenInclude(item => item.Product)
    .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

// ❌ WRONG — lazy loading, triggers N+1
Order? order = await _appDbContext.Orders.FindAsync(id);
int itemCount = order.Items.Count;  // triggers a new query
```

---

## Split queries — multiple collection navigations

Use `AsSplitQuery()` when including 2+ collection navigations to avoid Cartesian explosion.

```csharp
// ✅ CORRECT — separate SQL query per collection
Order? order = await _appDbContext.Orders
    .AsNoTracking()
    .AsSplitQuery()
    .Include(order => order.Items)
    .Include(order => order.Payments)
    .Include(order => order.Shipments)
    .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

// ❌ WRONG — single query with multiple collections causes Cartesian explosion
Order? order = await _appDbContext.Orders
    .Include(order => order.Items)
    .Include(order => order.Payments)
    .Include(order => order.Shipments)
    .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
```

---

## Projections — select only what you need

Project to DTOs directly in the query. Never load full entities when only a subset is needed.

```csharp
// ✅ CORRECT — project in the query, minimal data transfer
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

// ❌ WRONG — loads full entities, maps in memory
List<Product> all = await _appDbContext.Products.ToListAsync(cancellationToken);
List<ProductSummaryDto> summaries = all.Select(p => new ProductSummaryDto { ... }).ToList();
```

---

## Pagination

```csharp
public async Task<PagedResult<ProductSummaryDto>> GetPagedAsync(
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
{
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

    return new PagedResult<ProductSummaryDto>
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
```

---

## Composable filtering

Build queries incrementally. Never duplicate predicates.

```csharp
// ✅ CORRECT — composable, database-side
IQueryable<Product> query = _appDbContext.Products.AsNoTracking();

if (!string.IsNullOrWhiteSpace(searchTerm))
    query = query.Where(product => product.Name.Contains(searchTerm));

if (status.HasValue)
    query = query.Where(product => product.Status == status.Value);

List<ProductDto> results = await query
    .Select(product => new ProductDto { Id = product.Id, Name = product.Name })
    .ToListAsync(cancellationToken);
```

---

## Navigation properties — never manual joins

EF Core translates navigation property access to optimal SQL JOINs automatically.

```csharp
// ✅ CORRECT — navigation property
List<Product> results = await _appDbContext.Products
    .AsNoTracking()
    .Where(product => product.Supplier.Name == supplierName)
    .ToListAsync(cancellationToken);

// ❌ WRONG — manual join, same result but harder to read and maintain
List<Product> results = await _appDbContext.Products
    .AsNoTracking()
    .Join(_appDbContext.Suppliers,
        product => product.SupplierId,
        supplier => supplier.Id,
        (product, supplier) => new { product, supplier })
    .Where(x => x.supplier.Name == supplierName)
    .Select(x => x.product)
    .ToListAsync(cancellationToken);
```

---

## ExecuteUpdateAsync / ExecuteDeleteAsync — bulk without loading entities

Use when you need to update or delete many rows without loading them into the change tracker.

```csharp
// ✅ CORRECT — single SQL UPDATE statement
int updated = await _appDbContext.Products
    .Where(product => product.Category == category)
    .ExecuteUpdateAsync(
        setters => setters.SetProperty(p => p.Status, ProductStatus.Inactive),
        cancellationToken);

// ✅ CORRECT — single SQL DELETE statement
int deleted = await _appDbContext.Products
    .Where(product => product.Status == ProductStatus.Discontinued)
    .ExecuteDeleteAsync(cancellationToken);

// ❌ WRONG — loads all matching entities, updates/deletes in a loop
List<Product> products = await _appDbContext.Products
    .Where(p => p.Category == category)
    .ToListAsync(cancellationToken);
foreach (Product p in products)
    p.Status = ProductStatus.Inactive;
await _appDbContext.SaveChangesAsync(cancellationToken);
```

---

## Query tags — for debugging in logs

```csharp
// ✅ Tag appears in SQL logs — makes debugging much faster
List<Product> active = await _appDbContext.Products
    .AsNoTracking()
    .TagWith("GetActiveProductsByCategory")
    .Where(product => product.Status == ProductStatus.Active)
    .ToListAsync(cancellationToken);
```

---

## DateTime in queries — always UtcNow

```csharp
// ✅ CORRECT
DateTime cutoff = DateTime.UtcNow.AddDays(-30);
List<Order> recent = await _appDbContext.Orders
    .Where(order => order.CreatedAt >= cutoff)
    .ToListAsync(cancellationToken);

// ❌ WRONG — DateTime.Now may not translate correctly to SQL in all providers
List<Order> recent = await _appDbContext.Orders
    .Where(order => order.CreatedAt >= DateTime.Now.AddDays(-30))
    .ToListAsync(cancellationToken);
```

---

## Performance rules summary

| Rule | Reason |
|---|---|
| Always `AsNoTracking()` for read-only | No change tracking overhead |
| Always project with `Select` when not modifying | Fewer columns fetched from DB |
| Always explicit `Include` | Never lazy loading — avoids N+1 |
| Always `AsSplitQuery()` for 2+ collections | Avoids Cartesian explosion |
| Always `AnyAsync()` over `CountAsync() > 0` | Stops at first match |
| Always `CancellationToken` on async methods | Proper cancellation support |
| Always `DateTime.UtcNow` | `DateTime.Now` may not translate to SQL correctly |
| Always `ExecuteUpdateAsync/DeleteAsync` for bulk | No entity loading overhead |
| Never `.ToListAsync()` before filtering | Pulls all data into memory |
| Never manual `.Join()` | Use navigation properties |
| Never lazy loading | Causes N+1 queries |