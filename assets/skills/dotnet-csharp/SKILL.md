---
name: dotnet-csharp
description: >
  Universal C# / .NET 10 standards that apply to ANY .NET project regardless
  of architecture or layer. Covers syntax, usings, naming, async patterns,
  and DTO conventions. Load this skill for ANY agent that writes C# code.
---

# C# Universal Standards — TemperAI
## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

The following rules have **ZERO tolerance for violations**. Code that breaks ANY of these will be **rejected immediately**:

1. **DTOs MUST be `sealed record` with explicit properties** — NEVER `class`, NEVER primary constructors
2. **NEVER magic strings** — use enums or constants for ALL repeated strings
3. **NEVER named usings** (`using X = Y;`) — rename the type instead
4. **NEVER `!` null-forgiving operator** — fix your validation logic
5. **NEVER `async void`** — always return `Task`

If you're unsure about a rule, ASK before writing code. Do NOT assume "it's probably fine."

> These standards apply to ALL C# code in TemperAI projects: backend, frontend, and tests.
> Never duplicate these rules in architecture-specific or layer-specific skills.

---

## Syntax & Structure

### 1. Never primary constructors
❌ `public class Product(string name) { }`
✅ `public class Product { public Product(string name) { ... } }`

### 2. Never expression-bodied methods (`=>`) for bodies
❌ `public string GetName() => Name;`
✅ `public string GetName() { return Name; }`

### 3. Never break short lines unnecessarily
❌ `Result<ProductDto> result =\n    await handler.HandleAsync(req, ct);`
✅ `Result<ProductDto> result = await handler.HandleAsync(req, ct);`

### 4. Always organize file structure consistently
❌ `using System;\nnamespace P;\nusing System.Collections;\nclass X {}`
✅ `using System;\nusing System.Collections;\n\nnamespace P;\n\nclass X {}`

---

## Imports & Usings

### 5. Never `using static`
❌ `using static System.Console; WriteLine("Hi");`
✅ `using System; Console.WriteLine("Hi");`

### 6. NEVER use named usings (aliases) — rename the type instead

**NEVER use `using X = Y;` to create aliases.** If you have a name collision, **rename the conflicting class itself**, don't work around it with an alias.

**❌ NEVER use aliases:**
```csharp
using ProductEntity = MyApp.Domain.Entities.Product;  // ⚠️ WRONG
using DomainTask = TodoApp.Domain.Task;  // ⚠️ WRONG

ProductEntity product = new();  // Confusing — what is ProductEntity?
```

**✅ ALWAYS use normal imports:**
```csharp
using MyApp.Domain.Entities;

Product product = new();  // Clear — it's a Product
```

**If there's a name collision (e.g., `Task` vs `System.Threading.Tasks.Task`):**
1. **Rename your domain type** — `WorkItem` instead of `Task`
2. **Change the file name** — `WorkItem.cs` instead of `Task.cs`
3. **Update all references** — no aliases needed

**Example collision resolution:**
```csharp
// BEFORE (collision with System.Threading.Tasks.Task):
// Domain/Tasks/Task.cs
public class Task { ... }  // ⚠️ Collides with System.Threading.Tasks.Task

// AFTER (resolved by renaming):
// Domain/WorkItems/WorkItem.cs
public class WorkItem { ... }  // ✅ No collision

// Usage:
using TodoApp.Domain.WorkItems;
using System.Threading.Tasks;

WorkItem item = new();  // ✅ Clear
Task asyncTask = Task.Run(...);  // ✅ Clear
```

**Rules:**
1. **ZERO named usings allowed** — not even "just this once"
2. **Rename conflicting types** at the source
3. **File names must match class names** exactly

### 7. Never global usings
❌ `global using System;`
✅ `using System;` (por archivo)

### 8. Never fully qualified type names in code
❌ `public Projects.Enums.Status Status { get; set; }`
✅ `using Projects.Enums;\npublic Status Status { get; set; }`

### 8.1 Never write namespace path in variable declarations
❌ `FluentValidation.Results.ValidationResult result = ...;`
✅ `using FluentValidation.Results;\nValidationResult result = ...;`

**Same applies to:**
- ❌ `System.Collections.Generic.List<string> list = ...;`
- ✅ `List<string> list = ...;` (with proper using)

- ❌ `Microsoft.Extensions.Logging.ILogger<Program> logger = ...;`
- ✅ `ILogger<Program> logger = ...;` (with proper using)

---

## Naming & Conventions

### 9. Always variable names matching their type
❌ `var result = await _service.GetAsync(id);`
✅ `ProductDto result = await _service.GetAsync(id);`

### 10. Entity folders always plural
❌ `Domain/Product/Product.cs`
✅ `Domain/Products/Product.cs`

### 11. Always write code in English
❌ `public bool EstaActivo { get; set; }`
✅ `public bool IsActive { get; set; }`

### 12. Always `sealed` on non-inherited classes
❌ `public class ProductRepository { }`
✅ `public sealed class ProductRepository { }`

### 13. NEVER magic strings — ALWAYS use constants or enums

**NEVER hardcode string literals** in your code. ANY repeated string value must be a constant or enum.

**❌ NEVER hardcode strings:**
```csharp
if (product.Status == "active") { ... }  // ⚠️ WRONG — magic string
string connection = Configuration["ConnectionStrings:Default"];  // ⚠️ WRONG — magic string
```

**✅ ALWAYS use enums for fixed states:**
```csharp
public enum ProductStatus { Active, Inactive, Pending }
if (product.Status == ProductStatus.Active) { ... }
```

**✅ ALWAYS use constants for configuration keys:**
```csharp
public static class ConfigKeys
{
    public const string DefaultConnection = "ConnectionStrings:Default";
}
string connection = Configuration[ConfigKeys.DefaultConnection];
```

**Rules:**
1. **If it's a state/status/type** → Use enum
2. **If it's a config key/route/constant** → Use const string
3. **If you write the same string twice** → Extract to constant
4. **ZERO tolerance for magic strings** — code will be rejected if any are found

---

## Null Safety

### 14. Never use the null-forgiving operator (`!`)

The `!` operator (`null-forgiving operator`) **MUST NEVER BE USED**. It indicates the developer is forcing the compiler to ignore a potential nullable reference, which is a sign that the validation logic is incorrect.

**Problematic case to avoid:**
```csharp
// ❌ WRONG — validates error count, then uses ! to suppress warnings
(List<string> errors, TodoItem? item) = TodoItem.Create(request.Title);
if (errors.Count > 0)
{
    return Result<TodoItemDto>.Failure(HttpStatusCode.BadRequest).WithErrors(errors);
}
await _repo.AddAsync(item!, ct);  // ⚠️ Uses ! — DANGEROUS
```

**Correct pattern:**
```csharp
// ✅ RIGHT — validates the nullability of the object, not the error count
(List<string> errors, TodoItem? item) = TodoItem.Create(request.Title);
if (item is null)
{
    return Result<TodoItemDto>.Failure(HttpStatusCode.BadRequest).WithErrors(errors);
}
await _repo.AddAsync(item, ct);
```

**Rules:**
1. **Never use `!`** — if you need it, your validation logic is wrong.
2. **Always validate `if (entity is null)`** — not `if (errors.Count > 0)`.
3. **Factory methods returning `(List<string> errors, T? entity)`** must check the entity's nullability, not the error count.
4. **The correct pattern** is: `if (entity is null) return failure;` before using the entity.

---

## Async & Threading

### 15. Never `async void`
❌ `public async void DoWork() { }`
✅ `public async Task DoWork() { }`

### 16. Never `.Result` or `.Wait()`
❌ `var product = _service.Get(id).Result;`
✅ `var product = await _service.Get(id);`

### 17. Always `CancellationToken` on public async methods
❌ `public async Task<Product> GetByIdAsync(Guid id) { }`
✅ `public async Task<Product> GetByIdAsync(Guid id, CancellationToken ct = default) { }`

### 18. Never throw exceptions — use Result pattern instead
❌ `if (!IsValid) throw new ValidationException("Invalid");`
✅ `if (!IsValid) return Result<T>.Failure(HttpStatusCode.BadRequest).WithErrors(["Invalid"]);`

**Rules:**
- **Never throw exceptions** in application code — only the global ExceptionHandler can throw for unhandled errors.
- **Never create custom exceptions** — use the Result pattern for all error handling.
- **Always use Result<T>.Success() / Result<T>.Failure()** to return errors with appropriate HTTP status codes.
- **The ExceptionHandler** (middleware) is the only place that catches and converts exceptions to ProblemDetails responses.

---

## DTOs & Patterns

### 19. DTOs must be sealed records with explicit properties and `Dto` suffix

DTOs follow specific conventions defined in `backend/architecture/shared/DTO_CONVENTIONS.md`.

**Quick rules (for complete rules and examples, load the skill):**
- Always `sealed record` with explicit properties — NEVER `class`, NEVER primary constructors
- Always `Dto` suffix — `CreateProductRequestDto`, `ProductResponseDto`
- Request DTOs use `{ get; init; }` — allows object initializers
- String properties default to `string.Empty` — never nullable strings in DTOs
- File name must match DTO name — `CreateProductRequestDto.cs`

For complete DTO rules, naming conventions, mapping patterns, and anti-patterns, load `backend/architecture/shared/DTO_CONVENTIONS.md`.