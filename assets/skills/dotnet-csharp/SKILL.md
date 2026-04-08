---
name: dotnet-csharp
description: >
  Universal C# / .NET 10 standards that apply to ANY .NET project regardless
  of architecture or layer. Covers syntax, usings, naming, async patterns,
  and DTO conventions. Load this skill for ANY agent that writes C# code.
---

# C# Universal Standards — TemperAI

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

### 6. Never named usings (aliases)
❌ `using TodoTask = Domain.Task; TodoTask t = new();`
❌ `using DomainTask = Domain.Entities.Tasks.Task;`
✅ `using Domain.Entities.Tasks;` then use the class by its real name.

**If a name collision occurs (e.g., `Task` vs `System.Threading.Tasks.Task`):**
- **NEVER** use an alias to work around it.
- **ALWAYS** rename the file and the class itself to avoid the collision at the source.
- This applies to ANY type: entities, DTOs, services, value objects, enums, etc.
- The new name must be descriptive and unique within the project.
- The file name must always match the class name.

```csharp
// BAD — alias to avoid collision
using DomainTask = TodoManagerApi.Domain.Entities.Tasks.Task;

// GOOD — rename the type itself
// File: Domain/Entities/WorkItems/WorkItem.cs
public sealed class WorkItem : Entity<Guid> { ... }

// Then in the consumer:
using TodoManagerApi.Domain.Entities.WorkItems;
// No collision — WorkItem doesn't clash with System.Threading.Tasks.Task
```

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

---

## Null Safety

### 13. Never use the null-forgiving operator (`!`)

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

### 14. Never `async void`
❌ `public async void DoWork() { }`
✅ `public async Task DoWork() { }`

### 15. Never `.Result` or `.Wait()`
❌ `var product = _service.Get(id).Result;`
✅ `var product = await _service.Get(id);`

### 16. Always `CancellationToken` on public async methods
❌ `public async Task<Product> GetByIdAsync(Guid id) { }`
✅ `public async Task<Product> GetByIdAsync(Guid id, CancellationToken ct = default) { }`

### 17. Never throw exceptions — use Result pattern instead
❌ `if (!IsValid) throw new ValidationException("Invalid");`
✅ `if (!IsValid) return Result<T>.Failure(HttpStatusCode.BadRequest).WithErrors(["Invalid"]);`

**Rules:**
- **Never throw exceptions** in application code — only the global ExceptionHandler can throw for unhandled errors.
- **Never create custom exceptions** — use the Result pattern for all error handling.
- **Always use Result<T>.Success() / Result<T>.Failure()** to return errors with appropriate HTTP status codes.
- **The ExceptionHandler** (middleware) is the only place that catches and converts exceptions to ProblemDetails responses.

---

## DTOs & Patterns

### 18. Always DTOs as `sealed record` with explicit properties and `Dto` suffix
❌ `public record CreateProductDto(string Name, decimal Price);`
✅ `public sealed record CreateProductRequestDto { public string Name { get; init; } = string.Empty; public decimal Price { get; init; } }`
