---
name: dotnet-ef-core
description: >
  Entity Framework Core standards for .NET 10 projects.
  Root skill — defines which sub-files to load based on the task type.
  Load when creating or modifying entity configurations, repositories,
  DbContext, UnitOfWork, or migrations from scratch.
  DO NOT load for tasks that only write queries against existing repositories —
  load dotnet-ef-core-queries instead.
  DO NOT load for tasks that only use existing repositories in use cases —
  load REPOSITORY_USAGE.md instead.
requires: [dotnet-csharp]
produces: [entity-configurations, repositories, unit-of-work, dbcontext, migrations]
---

# EF Core — TemperAI Standards

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **NEVER use DataAnnotations** on entities — Fluent API only via `IEntityTypeConfiguration<T>`
2. **NEVER use `nvarchar(max)` or `varchar(max)`** — always specify lengths from `Entity.Rules`
3. **NEVER call `.Update()`** — change tracker detects changes automatically
4. **NEVER use lazy loading** — always explicit `.Include()`
5. **NEVER call `builder.ToTable()`** — EF Core infers from DbSet property name
6. **NEVER call `HasDefaultValueSql()` or `ValueGeneratedOnAdd()`** for primary keys

---

## Which file to load — decide before reading anything else

```
Task involves...                              → Load
─────────────────────────────────────────────────────────────────
Creating entity configuration                → ENTITY_CONFIGURATION.md
Creating a new repository from scratch       → REPOSITORY_PATTERN.md
Creating UnitOfWork from scratch             → REPOSITORY_PATTERN.md
Creating or modifying DbContext              → DBCONTEXT_SETUP.md
Bulk insert (1000+ rows)                     → BULK_OPERATIONS.md (+ above as needed)

Adding query methods to existing repository  → dotnet-ef-core-queries
Writing queries in use cases                 → dotnet-ef-core-queries
Using existing repositories in use cases     → REPOSITORY_USAGE.md
```

**Load only what the task needs.** Never load all sub-files by default.

---

## Sub-files in this skill

| File | Load when | Weight |
|---|---|---|
| `ENTITY_CONFIGURATION.md` | Creating entity Fluent API config | Medium |
| `REPOSITORY_PATTERN.md` | Creating repositories or UnitOfWork from scratch | Heavy |
| `REPOSITORY_USAGE.md` | Using existing repositories in use cases | Light |
| `DBCONTEXT_SETUP.md` | Creating or modifying DbContext | Light |
| `BULK_OPERATIONS.md` | Bulk insert / batch operations (1000+ rows) | Medium |

---

## Related skills

| Need | Load |
|---|---|
| Write queries against EF Core | `dotnet-ef-core-queries` |
| Pure LINQ composition over any collection | `dotnet-linq` |
| Domain entity structure (Rules class, factory methods) | `dotnet-ddd` |
| C# conventions (naming, async, null safety) | `dotnet-csharp` |
| Clean Architecture folder structure | `backend/architecture/clean` |
| Result pattern and DTO conventions | `backend/architecture/shared/DTO_CONVENTIONS.md` |

---

## Quick reference — critical patterns

### Tracking strategy

| Operation | Method | Why |
|---|---|---|
| Load to modify | `GetByIdAsync` | EF tracks changes — `CompleteAsync` saves |
| Load to read | `GetByIdAsNoTrackingAsync` | No tracking overhead |
| Load collection for display | `.AsNoTracking()` on query | Never track what you won't modify |

### Common anti-patterns

```csharp
// ❌ NEVER — DataAnnotations
[Required][MaxLength(100)] public string Name { get; set; }

// ❌ NEVER — nvarchar(max)
builder.Property(p => p.Description).HasColumnType("nvarchar(max)");

// ❌ NEVER — .Update()
_appDbContext.Products.Update(product);

// ❌ NEVER — lazy loading navigation
public virtual ICollection<Order> Orders { get; set; }

// ❌ NEVER — ToTable
builder.ToTable("Products");
```