---
name: dotnet-linq
description: >
  Pure LINQ standards for .NET 10 projects. Covers language operators,
  query composition, projection patterns, filtering, grouping, and
  performance best practices over IEnumerable<T> and IQueryable<T>.
  Load when writing or reviewing LINQ expressions in any layer.
  DO NOT load expecting EF Core-specific methods (ToListAsync, AsNoTracking,
  Include, ExecuteUpdateAsync) — those belong to dotnet-ef-core-queries.
requires: [dotnet-csharp]
produces: [linq-expressions, query-compositions, in-memory-projections]
---

# LINQ — TemperAI Standards

## What this skill covers

LINQ operators and patterns that work over **any** `IEnumerable<T>` or `IQueryable<T>` —
regardless of whether the source is a database, a list, an array, or any other collection.

**This skill does NOT cover:**
- `ToListAsync`, `AnyAsync`, `FirstOrDefaultAsync` — these are EF Core extensions, not LINQ
- `AsNoTracking`, `Include`, `AsSplitQuery`, `TagWith` — EF Core query behavior
- `ExecuteUpdateAsync`, `ExecuteDeleteAsync` — EF Core bulk operations
- Tracking vs no-tracking strategy — EF Core concern

For all of the above, load `dotnet-ef-core-queries`.

---

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **NEVER filter after `.ToList()` or `.AsEnumerable()`** — always filter before materializing
2. **NEVER use manual `.Join()`** — use navigation properties or `SelectMany`
3. **NEVER chain multiple `Where` clauses when one suffices** — compose predicates explicitly
4. **ALWAYS use `Any()` instead of `Count() > 0`** — stops at first match

---

## When NOT to load this skill

- Task is purely about EF Core query behavior (tracking, includes, async materialization) → load `dotnet-ef-core-queries`
- Task has no collection or query logic at all

---

## Core operators — quick reference

| Operator | Use for | Returns |
|---|---|---|
| `Where` | Filtering | `IQueryable<T>` / `IEnumerable<T>` |
| `Select` | Projection | `IQueryable<TResult>` |
| `SelectMany` | Flatten nested collections | `IQueryable<TResult>` |
| `OrderBy` / `ThenBy` | Sorting | `IOrderedQueryable<T>` |
| `GroupBy` | Grouping | `IQueryable<IGrouping<TKey,T>>` |
| `Any` | Existence check | `bool` |
| `All` | Universal check | `bool` |
| `Count` | Count items (use `Any` for existence) | `int` |
| `First` / `FirstOrDefault` | First match | `T` / `T?` |
| `Single` / `SingleOrDefault` | Exactly one match | `T` / `T?` |
| `Skip` / `Take` | Pagination | `IQueryable<T>` |
| `Distinct` | Remove duplicates | `IQueryable<T>` |
| `Contains` | IN-style check | `bool` |

---

## Projection — always project to what you need

Never return more data than required. Project to DTOs or anonymous types as close to the source as possible.

```csharp
// ✅ CORRECT — project early, only fetch needed fields
IEnumerable<ProductSummaryDto> summaries = products
    .Select(product => new ProductSummaryDto
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price
    });

// ❌ WRONG — returns full entities, maps later
List<Product> all = products.ToList();
List<ProductSummaryDto> summaries = all
    .Select(p => new ProductSummaryDto { ... })
    .ToList();
```

---

## Filtering — always before materialization

```csharp
// ✅ CORRECT — filter in the query, not in memory
IEnumerable<Product> active = products
    .Where(product => product.Status == ProductStatus.Active);

// ❌ WRONG — materializes everything, then filters in memory
List<Product> all = products.ToList();
List<Product> active = all
    .Where(product => product.Status == ProductStatus.Active)
    .ToList();
```

---

## Composable queries — build incrementally

When filters are conditional, compose the query step by step instead of duplicating predicates.

```csharp
// ✅ CORRECT — composable, clean, no duplication
IQueryable<Product> query = source;

if (!string.IsNullOrWhiteSpace(searchTerm))
    query = query.Where(product => product.Name.Contains(searchTerm));

if (status.HasValue)
    query = query.Where(product => product.Status == status.Value);

if (minPrice.HasValue)
    query = query.Where(product => product.Price >= minPrice.Value);

// Materialize only at the end
List<Product> results = query.ToList();

// ❌ WRONG — duplicated predicates, hard to maintain
if (searchTerm != null && status.HasValue)
    results = source.Where(p => p.Name.Contains(searchTerm) && p.Status == status.Value).ToList();
else if (searchTerm != null)
    results = source.Where(p => p.Name.Contains(searchTerm)).ToList();
// ... and so on
```

---

## Existence check — always `Any`, never `Count > 0`

```csharp
// ✅ CORRECT — stops at first match
bool hasActive = products.Any(product => product.Status == ProductStatus.Active);

// ❌ WRONG — counts all matching items unnecessarily
bool hasActive = products.Count(product => product.Status == ProductStatus.Active) > 0;
```

---

## Flattening nested collections — SelectMany

```csharp
// ✅ CORRECT — flatten order items across all orders
IEnumerable<OrderItem> allItems = orders
    .SelectMany(order => order.Items);

// ✅ CORRECT — flatten with parent context
IEnumerable<(Order Order, OrderItem Item)> pairs = orders
    .SelectMany(
        order => order.Items,
        (order, item) => (order, item));
```

---

## Grouping

```csharp
// ✅ CORRECT — group products by category, project each group
IEnumerable<CategorySummaryDto> grouped = products
    .GroupBy(product => product.Category)
    .Select(group => new CategorySummaryDto
    {
        Category = group.Key,
        Count = group.Count(),
        AveragePrice = group.Average(p => p.Price)
    });
```

---

## Pagination — always Skip then Take, always with OrderBy

Pagination without `OrderBy` produces non-deterministic results.

```csharp
// ✅ CORRECT — deterministic pagination
IEnumerable<Product> page = products
    .OrderBy(product => product.Name)
    .ThenBy(product => product.Id)   // secondary sort for stability
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize);

// ❌ WRONG — no OrderBy, result order is undefined
IEnumerable<Product> page = products
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize);
```

---

## IN-style checks — Contains on a collection

```csharp
// ✅ CORRECT — filter by a set of IDs
List<Guid> targetIds = [id1, id2, id3];

IEnumerable<Product> matching = products
    .Where(product => targetIds.Contains(product.Id));
```

> When used over EF Core `IQueryable<T>`, this translates to `WHERE Id IN (...)`.
> For in-memory collections larger than 10,000 items, use a `HashSet<T>` for O(1) lookup:

```csharp
// ✅ CORRECT — O(1) lookup for large in-memory sets
HashSet<Guid> targetIds = new HashSet<Guid>([id1, id2, id3, /* ... */]);

IEnumerable<Product> matching = products
    .Where(product => targetIds.Contains(product.Id));
```

---

## Null safety in LINQ expressions

```csharp
// ✅ CORRECT — guard against null before LINQ
if (products is null || !products.Any())
    return [];

// ✅ CORRECT — null-conditional in projection
IEnumerable<string> names = products
    .Where(product => product.Name is not null)
    .Select(product => product.Name!);

// ❌ WRONG — null-forgiving inside LINQ
IEnumerable<string> names = products.Select(p => p.Name!);
```

---

## Performance rules summary

| Rule | Reason |
|---|---|
| Always filter before `.ToList()` | Avoids loading unnecessary data into memory |
| Always project with `Select` early | Reduces the data surface at the source |
| Always use `Any()` over `Count() > 0` | Stops evaluation at first match |
| Always `OrderBy` before `Skip/Take` | Deterministic pagination |
| Use `HashSet<T>` for large in-memory `Contains` | O(1) lookup vs O(n) on list |
| Never manual `.Join()` | Use navigation properties or `SelectMany` |
| Never chain `.Where` duplicates | Compose predicates with the composable pattern |