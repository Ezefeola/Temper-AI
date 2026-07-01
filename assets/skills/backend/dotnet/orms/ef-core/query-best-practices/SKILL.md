---
name: dotnet-ef-core-query-best-practices
description: >
  Canonical EF Core query best practices for TemperAI: how each query method
  behaves, how to write performant queries, and the highest-value EF Core tricks.
  Load whenever a task writes EF Core queries — inside a repository OR directly
  against AppDbContext in a use case. Pattern-agnostic: the query rules are the
  same regardless of whether the project uses repositories or a direct DbContext.
requires: [backend-dotnet-csharp, dotnet-linq]
produces: [ef-core-queries, async-materialization, tracking-queries, query-performance]
---

# EF Core Query Best Practices — TemperAI

The single source of truth for writing EF Core queries. Every rule here applies
identically whether the query lives in a repository implementation or directly in
a use case that injects `AppDbContext`. Where you write the query is decided by the
project's data-access pattern (see `repository-usage` / `dbcontext-usage`); HOW you
write it is decided here.

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **ALWAYS use `AsNoTracking()`** on read-only queries (anything you will not modify + save)
2. **NEVER use lazy loading** — always explicit `Include()` / `ThenInclude()` when you need related data
3. **ALWAYS `await` an async materializer** (`ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, …) — never `.ToList()` / `.First()` on a DB query
4. **ALWAYS pass `CancellationToken`** to every async EF Core call
5. **NEVER filter, sort, or paginate after materialization** — do it on `IQueryable` so it runs in SQL
6. **ALWAYS use `AnyAsync(...)` instead of `CountAsync(...) > 0`** for existence checks
7. **ALWAYS project with `Select` when you don't need the full entity** — never fetch columns you won't use
8. **NEVER use `DateTime.Now`** in queries — use `DateTime.UtcNow`
9. **NEVER call a method EF cannot translate inside `Where`/`OrderBy`** — it forces client-side evaluation over the whole table

## Load when

- Writing ANY EF Core query — in a repository method or directly in a use case
- Adding a query method to an existing repository
- Reviewing tracking behavior, includes, projections, pagination, or query performance

---

## Mental model — the one thing to internalize

`IQueryable<T>` is a **query description**, not data. Nothing hits the database until you
materialize it (`ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `foreach`, …). Everything you
chain **before** materialization (`Where`, `OrderBy`, `Skip`, `Take`, `Select`) is translated to
SQL and runs on the server. Everything you do **after** runs in memory on the app server.

```csharp
// ✅ Filters in SQL — the DB returns only active products
List<Product> active = await _appDbContext.Products
    .Where(product => product.Status == ProductStatus.Active)   // still IQueryable → SQL WHERE
    .ToListAsync(cancellationToken);

// ❌ Filters in memory — the DB returns EVERY product, then C# throws most away
List<Product> wrong = (await _appDbContext.Products.ToListAsync(cancellationToken))
    .Where(product => product.Status == ProductStatus.Active)   // now IEnumerable → runs in app memory
    .ToList();
```

The moment you call `ToListAsync()`, `AsEnumerable()`, or `.ToList()`, you leave the database.
Keep the query as `IQueryable` for as long as there is still work the database can do.

---

## Tracking — the first decision on every read

The change tracker exists so EF can detect what you modified and generate `UPDATE`/`DELETE` on
`SaveChanges`. If you are not going to modify + save the entity, tracking is pure overhead
(extra memory, slower materialization, identity-map bookkeeping).

| Intent | Tracking | How |
|---|---|---|
| Load → modify → save | ON | plain query (do **not** add `AsNoTracking()`) |
| Load → delete → save | ON | plain query |
| Load → read / project / return | OFF | `AsNoTracking()` |
| Check existence | OFF | `AnyAsync()` |
| Read-only but need reference identity across joins | OFF + identity map | `AsNoTrackingWithIdentityResolution()` |

```csharp
// Tracked — will be modified and saved
Product? tracked = await _appDbContext.Products
    .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

// Read-only — no tracking overhead
Product? readOnly = await _appDbContext.Products
    .AsNoTracking()
    .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
```

`AsNoTrackingWithIdentityResolution()` — use only when a no-tracking query has duplicate parent
rows (e.g. a collection `Include`) and you need the same parent to be one shared instance instead
of many copies. It costs a little more than plain `AsNoTracking()` but far less than full tracking.

---

## Async materialization methods — what each one does

| Method | Returns | Use for |
|---|---|---|
| `ToListAsync(ct)` | `List<T>` | a set of rows |
| `FirstOrDefaultAsync(pred, ct)` | `T?` | first match or null — the safe default for "get one" |
| `SingleOrDefaultAsync(pred, ct)` | `T?` | exactly one expected; throws if 2+ (use for unique keys when you want the guarantee) |
| `AnyAsync(pred, ct)` | `bool` | existence — stops at the first matching row |
| `CountAsync(pred, ct)` | `int` | you genuinely need the number |
| `ToDictionaryAsync(keySel, ct)` | `Dictionary<K,V>` | in-memory lookup by key |
| `ToArrayAsync(ct)` | `T[]` | fixed-size result you won't grow |
| `SumAsync/MaxAsync/MinAsync/AverageAsync` | scalar | aggregate in SQL, not in memory |

```csharp
List<Product> list      = await query.ToListAsync(cancellationToken);
Product? first          = await query.FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
bool exists             = await query.AnyAsync(product => product.Name == name, cancellationToken);
decimal totalStockValue = await query.SumAsync(product => product.Price * product.Stock, cancellationToken);

// ❌ WRONG — synchronous blocking call on a DB query
List<Product> syncList = query.ToList();
```

`First` vs `Single`: `FirstOrDefaultAsync` returns the first row and stops. `SingleOrDefaultAsync`
asks the DB for up to two rows to prove uniqueness and throws if it finds two — slightly more work,
but it turns a silent data bug into a loud exception. Use `Single*` when the predicate is on a key
you believe is unique and you want that invariant enforced.

---

## Explicit includes — never lazy loading

Lazy loading turns `order.Items` into a hidden query per access → classic N+1. Always load related
data explicitly and intentionally.

```csharp
Order? order = await _appDbContext.Orders
    .AsNoTracking()
    .Include(current => current.Items)
        .ThenInclude(item => item.Product)
    .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);
```

### Filtered & ordered includes — don't over-fetch a collection

```csharp
Order? order = await _appDbContext.Orders
    .AsNoTracking()
    .Include(current => current.Items
        .Where(item => item.Quantity > 0)
        .OrderBy(item => item.CreatedAt))
    .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);
```

### Split queries — avoid the Cartesian explosion

`Include`-ing two or more **collections** in one SQL statement multiplies rows (10 items × 5
payments = 50 rows carrying duplicated order columns). `AsSplitQuery()` issues one SQL round-trip
per collection instead, trading extra round-trips for far less duplicated data.

```csharp
Order? order = await _appDbContext.Orders
    .AsNoTracking()
    .AsSplitQuery()
    .Include(current => current.Items)
    .Include(current => current.Payments)
    .Include(current => current.Shipments)
    .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);
```

Rule of thumb: **one** collection include → single query is fine. **Two or more** collection
includes → `AsSplitQuery()`. (Reference includes / `ThenInclude` on a single related row do not
cause the explosion.)

---

## Projection — the biggest single performance win

Selecting into a DTO tells the database to return **only the columns you use**, skips change
tracking entirely, and never materializes the full entity graph. Prefer projection for every
read-only path.

```csharp
List<ProductSummaryDto> summaries = await _appDbContext.Products
    .AsNoTracking()
    .Where(product => product.Status == ProductStatus.Active)
    .Select(product => new ProductSummaryDto
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price,
        SupplierName = product.Supplier.Name        // translated to a JOIN — no Include needed
    })
    .ToListAsync(cancellationToken);
```

Projection also replaces `Include` for reads: pulling `product.Supplier.Name` inside `Select`
generates the JOIN and returns just that column. You only need `Include` when you materialize the
**entity** and want its navigations populated.

---

## Pagination

### Offset pagination (`Skip`/`Take`) — simple, fine for early pages

```csharp
IQueryable<Product> query = _appDbContext.Products
    .AsNoTracking()
    .Where(product => product.Status == ProductStatus.Active);

int totalCount = await query.CountAsync(cancellationToken);

List<ProductSummaryDto> items = await query
    .OrderBy(product => product.Name)
    .ThenBy(product => product.Id)                  // deterministic tiebreaker — required for stable paging
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

Always order by something unique (or add a tiebreaker) — otherwise `Skip`/`Take` can repeat or drop
rows between pages. `Skip(N)` gets slower as `N` grows because the DB still scans the skipped rows.

### Keyset / seek pagination — scales to deep pages

For large datasets or infinite scroll, page by the last-seen key instead of an offset. Cost stays
constant no matter how deep you page.

```csharp
IQueryable<Product> query = _appDbContext.Products
    .AsNoTracking()
    .OrderBy(product => product.Name)
    .ThenBy(product => product.Id);

if (lastName is not null && lastId is not null)
    query = query.Where(product =>
        product.Name > lastName ||
        (product.Name == lastName && product.Id > lastId.Value));

List<Product> pageItems = await query.Take(pageSize).ToListAsync(cancellationToken);
```

---

## Composable filtering — build the query conditionally

Because `IQueryable` is deferred, you can assemble a query across `if` branches and it still
executes as one SQL statement.

```csharp
IQueryable<Product> query = _appDbContext.Products.AsNoTracking();

if (!string.IsNullOrWhiteSpace(searchTerm))
    query = query.Where(product => product.Name.Contains(searchTerm));

if (status.HasValue)
    query = query.Where(product => product.Status == status.Value);

if (minPrice.HasValue)
    query = query.Where(product => product.Price >= minPrice.Value);

List<Product> results = await query.ToListAsync(cancellationToken);
```

---

## Aggregates and existence — let SQL do the math

```csharp
bool exists      = await _appDbContext.Products.AnyAsync(product => product.Sku == sku, cancellationToken);
int activeCount  = await _appDbContext.Products.CountAsync(product => product.Status == ProductStatus.Active, cancellationToken);
decimal maxPrice = await _appDbContext.Products.MaxAsync(product => product.Price, cancellationToken);

// ❌ NEVER — loads every row to count in memory
bool wrong = (await _appDbContext.Products.ToListAsync(cancellationToken)).Any(p => p.Sku == sku);
```

`AnyAsync` compiles to `EXISTS (...)` and stops at the first hit. `CountAsync() > 0` counts the
entire matching set first — always slower for a yes/no question.

---

## `Contains` / IN clauses — watch the parameter count

`list.Contains(x.Id)` translates to SQL `IN (...)`. It's great for small sets, but thousands of
ids blow up the SQL parameter limit and hurt plan caching. For large sets, filter another way
(join against a temp set, batch the ids, or reshape the query).

```csharp
List<Guid> ids = request.ProductIds;                 // keep this bounded

List<Product> products = await _appDbContext.Products
    .AsNoTracking()
    .Where(product => ids.Contains(product.Id))       // → WHERE Id IN (...)
    .ToListAsync(cancellationToken);
```

---

## Set-based writes — `ExecuteUpdateAsync` / `ExecuteDeleteAsync`

Update or delete many rows in **one SQL statement** without loading a single entity into memory.
These run immediately (they do **not** go through `SaveChanges`) and bypass the change tracker.

```csharp
int updated = await _appDbContext.Products
    .Where(product => product.Category == category)
    .ExecuteUpdateAsync(
        setters => setters
            .SetProperty(product => product.Status, ProductStatus.Inactive)
            .SetProperty(product => product.UpdatedAt, DateTime.UtcNow),
        cancellationToken);

int deleted = await _appDbContext.Products
    .Where(product => product.Status == ProductStatus.Discontinued)
    .ExecuteDeleteAsync(cancellationToken);
```

Caveat: because they skip the tracker, they won't fire `SaveChanges`-based logic (auditing,
domain-event dispatch, interceptors). Use them for genuine bulk maintenance, not for the normal
"load aggregate → change → save" path that must run domain rules.

---

## Streaming vs buffering

`ToListAsync()` buffers the whole result in memory. For very large read-only sets you process
row-by-row, stream with `AsAsyncEnumerable()` so only one row is in memory at a time.

```csharp
await foreach (Product product in _appDbContext.Products
    .AsNoTracking()
    .Where(product => product.Status == ProductStatus.Active)
    .AsAsyncEnumerable()
    .WithCancellation(cancellationToken))
{
    // process one product at a time — never fully buffered
}
```

Do not hold the connection open across slow per-row work you could have buffered; stream only when
the set is genuinely too large to hold in memory.

---

## Compiled queries — hot-path micro-optimization

For a query executed extremely often, `EF.CompileAsyncQuery` caches the translation so EF skips
re-building the expression tree each call. Reach for this only after profiling shows the query is
truly hot — it's a niche optimization, not a default.

```csharp
private static readonly Func<AppDbContext, Guid, CancellationToken, Task<Product?>> GetByIdCompiled =
    EF.CompileAsyncQuery((AppDbContext context, Guid id, CancellationToken ct) =>
        context.Products.AsNoTracking().FirstOrDefault(product => product.Id == id));

Product? product = await GetByIdCompiled(_appDbContext, id, cancellationToken);
```

---

## Global query filters — know when they apply

If entities use global query filters (e.g. soft-delete `IsDeleted == false`), every query silently
applies them. When you deliberately need filtered-out rows (admin/restore/reporting), opt out
explicitly and document why.

```csharp
Product? includingDeleted = await _appDbContext.Products
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
```

---

## Raw SQL — only when LINQ genuinely can't, always parameterized

```csharp
// Entity-returning raw SQL — parameters are interpolated safely (never string-concatenate)
List<Product> products = await _appDbContext.Products
    .FromSql($"SELECT * FROM Products WHERE Category = {category}")
    .AsNoTracking()
    .ToListAsync(cancellationToken);

// Scalar / non-entity result
List<string> names = await _appDbContext.Database
    .SqlQuery<string>($"SELECT Name FROM Products WHERE Price > {minPrice}")
    .ToListAsync(cancellationToken);
```

Use the interpolated `FromSql`/`SqlQuery` overloads so values become SQL parameters. Never build SQL
with string concatenation — that is a SQL-injection hole.

---

## Query tags — make slow queries findable in logs

`TagWith` prepends a SQL comment so you can map a slow query in the DB logs back to the exact call
site. Free to add, invaluable when diagnosing production.

```csharp
List<Product> active = await _appDbContext.Products
    .AsNoTracking()
    .TagWith("GetActiveProductsByCategory")
    .Where(product => product.Status == ProductStatus.Active)
    .ToListAsync(cancellationToken);
```

---

## Client-side evaluation — the silent performance killer

If a `Where`/`OrderBy` calls something EF cannot translate to SQL, older EF threw; EF Core will
evaluate what it can in SQL and warn — but a badly placed custom method can still force scanning the
whole table in memory. Keep predicates to properties and translatable functions.

```csharp
// ❌ Custom method EF can't translate → risks pulling every row to filter in memory
query = query.Where(product => MyHelpers.IsEligible(product));

// ✅ Express the rule with translatable operators
query = query.Where(product =>
    product.Status == ProductStatus.Active && product.Stock > 0);
```

`DateTime` in queries — always `UtcNow`, and compute the cutoff **before** the query so a constant
is sent to SQL:

```csharp
DateTime cutoff = DateTime.UtcNow.AddDays(-30);
List<Order> recent = await _appDbContext.Orders
    .AsNoTracking()
    .Where(order => order.CreatedAt >= cutoff)
    .ToListAsync(cancellationToken);
```

---

## Performance rules summary

| Rule | Why |
|---|---|
| `AsNoTracking()` on read-only | No change-tracking memory or CPU overhead |
| `Select` a DTO when not modifying | Fewer columns, no entity materialization, no tracking |
| Explicit `Include` / project navigations | Kills lazy-loading N+1 |
| `AsSplitQuery()` for 2+ collection includes | Avoids Cartesian row explosion |
| `AnyAsync()` over `CountAsync() > 0` | Stops at first match (`EXISTS`) |
| Aggregate with `SumAsync`/`CountAsync`/`MaxAsync` | Math runs in SQL, not in memory |
| Filter/sort/page on `IQueryable` | Keeps work in SQL, not the app server |
| Keyset over deep `Skip(N)` | Constant cost regardless of page depth |
| `ExecuteUpdate/DeleteAsync` for bulk maintenance | One statement, no entity loading |
| Stream with `AsAsyncEnumerable()` for huge reads | One row in memory at a time |
| `TagWith` on non-trivial queries | Traceable in DB logs |
| Bounded `Contains` sets | Avoids SQL parameter blow-up |
| `CancellationToken` on every async call | Cancellable, no orphaned DB work |

## Related skills

- Writing these queries inside a **repository** → also load `repository-usage`
- Writing these queries directly in a **use case** against `AppDbContext` → also load `dbcontext-usage`
- Bulk insert / batch of 1000+ rows → load `bulk-operations`
- Indexes, keys, and column mapping that make these queries fast → `entity-configuration`
