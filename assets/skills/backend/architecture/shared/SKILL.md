---
name: architecture-shared
description: >
  Shared rules that apply to ALL architecture patterns in TemperAI projects.
  Load this skill for Result pattern, DTO conventions, and use case patterns.
  For data access implementation, load backend/dotnet/ef-core.
---

# Architecture Shared Rules — TemperAI

> For data access implementation (UnitOfWork, DbContext, repositories, entity configurations), load `backend/dotnet/ef-core`.

## Files in this skill

| File | When to load |
|---|---|
| `RESULT_PATTERN.md` | **Always** — Result<T> is universal |
| `DTO_CONVENTIONS.md` | **Always** — DTOs are needed in most backend tasks |
| `USE_CASE_PATTERNS.md` | **Always** — Use cases are needed in most backend tasks |
| `SOLID_CLEAN_CODE.md` | **Always** — SOLID principles and Clean Code standards |

## How to load this skill

When an agent determines it needs this skill, load ALL four sub-files:

```
read_file('backend/architecture/shared/RESULT_PATTERN.md')
read_file('backend/architecture/shared/DTO_CONVENTIONS.md')
read_file('backend/architecture/shared/USE_CASE_PATTERNS.md')
read_file('backend/architecture/shared/SOLID_CLEAN_CODE.md')
```

This skill does NOT have optional sub-files. Load all four always.

## Quick Reference — Always loaded

### Result pattern (universal)

Load `RESULT_PATTERN.md` for complete implementation. Key rules:

- **HttpStatusCode is MANDATORY** — `Result<T>.Success(HttpStatusCode.Created)` or `.Failure(HttpStatusCode.NotFound)`
- **Never omit HttpStatusCode** — Always pass it to Success/Failure
- **Never numeric codes** — Use `HttpStatusCode.Created`, not `201`

### DTOs (always loaded)

Load `DTO_CONVENTIONS.md` for complete rules. Key rules:

- Always `sealed record` with explicit properties
- Always `Dto` suffix — `CreateProductRequestDto`, `CreateProductResponseDto`
- String properties default to `string.Empty`
- Never primary constructors

### Use cases (always loaded)

Load `USE_CASE_PATTERNS.md` for complete patterns. Key rules:

- No `UseCase` suffix — `CreateProduct`, `UpdateProduct`
- Interface in same folder — `ICreateProduct`
- `sealed class` with explicit constructor
- Result pattern with HttpStatusCode

### SOLID & Clean Code (always loaded)

Load `SOLID_CLEAN_CODE.md` for complete rules. Key rules:

- Methods ≤ ~30 lines — extract private methods with descriptive names
- Nesting ≤ 3 levels — use early returns
- Branches ≤ ~7 per method — extract or simplify
- New behavior = new file — never modify an existing use case
- No dead code, no boolean parameters, comments only for WHY

## Absolute rules — never broken

- Never throw exceptions — use Result pattern for all error handling
- Never `DataAnnotations` on entities
- Never primary constructors on DTOs
- Never `using static` — always explicit `using` directives
- Never global usings — always per-file `using` directives

For general C# conventions (syntax, usings, naming, async), see `dotnet-csharp`.