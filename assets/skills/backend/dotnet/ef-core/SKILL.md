---
name: dotnet-ef-core
description: >
  Entity Framework Core standards for .NET 10 projects.
  When loading this skill, load ALL required sub-files.
  Only BULK_OPERATIONS.md is optional.
---

# EF Core — TemperAI Standards

## Files in this skill

| File | Always load? | Content |
|---|---|---|
| `ENTITY_CONFIGURATION.md` | ✅ Yes | Fluent API, IEntityTypeConfiguration, column types |
| `REPOSITORY_PATTERN.md` | ✅ Yes | GenericRepository, specific repos, UnitOfWork |
| `DBCONTEXT_SETUP.md` | ✅ Yes | AppDbContext, OnModelCreating, migrations |
| `BULK_OPERATIONS.md` | ❌ Optional | BulkInsert, SqlBulkCopy (only for 1000+ rows) |

## How to load this skill

When an agent determines it needs EF Core, load ALL required sub-files:

```
read_file('backend/dotnet/ef-core/ENTITY_CONFIGURATION.md')
read_file('backend/dotnet/ef-core/REPOSITORY_PATTERN.md')
read_file('backend/dotnet/ef-core/DBCONTEXT_SETUP.md')
```

Only load `BULK_OPERATIONS.md` if the task explicitly mentions bulk insert, batch operations, or high-volume data import:

```
read_file('backend/dotnet/ef-core/BULK_OPERATIONS.md')
```

## Quick Reference — Always loaded

### Absolute rules

- **Never `DataAnnotations`** on entities or Value Objects — Fluent API only
- **Never `nvarchar(max)` or `varchar(max)`** — always specify lengths from `Entity.Rules`
- **Never `.Update()`** from EF Core — change tracker detects changes automatically
- **Always `GetByIdAsync` with tracking** for modifications, `GetByIdAsNoTrackingAsync` for reads
- **Always `AsNoTracking()`** on read-only queries
- **Always one `IEntityTypeConfiguration<T>`** per entity
- **Never lazy loading** — explicit includes always
- **Never call `builder.ToTable()`** — EF Core infers from DbSet property name
- **Never call `HasDefaultValueSql()` or `ValueGeneratedOnAdd()`** for primary keys — EF Core handles this

### Common patterns by task

| Task | Load |
|---|---|
| Creating entity configuration | All required files |
| Creating repository | All required files |
| Creating UnitOfWork | All required files |
| Creating DbContext | All required files |
| Bulk insert (1000+ rows) | All required + BULK_OPERATIONS.md |

## Dependencies

For general C# conventions (syntax, usings, naming, async), see `dotnet-csharp`.

For Result pattern and DTO conventions, see `architecture-shared`.

For Clean Architecture folder structure, see `backend/architecture/clean`.