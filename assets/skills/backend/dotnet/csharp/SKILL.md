---
name: dotnet-csharp
description: >
  Universal C# / .NET 10 standards that apply to ANY .NET project regardless
  of architecture or layer. Covers syntax, usings, naming, async patterns,
  null safety, and DTO conventions.
  ALWAYS load this skill for ANY agent that writes C# code.
  DO NOT load for tasks that only read or analyze existing code without writing.
requires: []
produces: [c#-conventions, naming, async-patterns, null-safety]
---

# C# Universal Standards — TemperAI

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

The following rules have **ZERO tolerance for violations**.
Code that breaks ANY of these will be **rejected immediately**:

1. **DTOs MUST be `sealed record` with explicit properties** — NEVER `class`, NEVER primary constructors
2. **NEVER magic strings** — use enums or constants for ALL repeated strings
3. **NEVER named usings** (`using X = Y;`) — rename the type instead
4. **NEVER `!` null-forgiving operator** — fix your validation logic
5. **NEVER `async void`** — always return `Task`

If you're unsure about a rule, **ASK before writing code**. Do NOT assume "it's probably fine."

> These standards apply to ALL C# code in TemperAI projects: backend, frontend, and tests.
> Never duplicate these rules in architecture-specific or layer-specific skills.

---

## When NOT to apply this skill

- You are only reading or analyzing existing code without writing new code
- You are writing configuration files (JSON, YAML, XML) — not C#

---

## Syntax & Structure

### 1. Never primary constructors
```csharp
// ❌ WRONG
public class Product(string name) { }

// ✅ CORRECT
public class Product
{
    public Product(string name) { ... }
}
```

### 2. Never expression-bodied methods for bodies
```csharp
// ❌ WRONG
public string GetName() => Name;

// ✅ CORRECT
public string GetName()
{
    return Name;
}
```

### 3. Never break short lines unnecessarily
```csharp
// ❌ WRONG
Result<ProductDto> result =
    await handler.HandleAsync(req, ct);

// ✅ CORRECT
Result<ProductDto> result = await handler.HandleAsync(req, ct);
```

### 4. Always organize usings before namespace
```csharp
// ❌ WRONG
using System;
namespace P;
using System.Collections;
class X {}

// ✅ CORRECT
using System;
using System.Collections;

namespace P;

class X {}
```

---

## Imports & Usings

### 5. Never `using static`
```csharp
// ❌ WRONG
using static System.Console;
WriteLine("Hi");

// ✅ CORRECT
using System;
Console.WriteLine("Hi");
```

### 6. NEVER named usings — rename the type instead

**NEVER use `using X = Y;`**. If you have a name collision, rename the conflicting class itself.

```csharp
// ❌ WRONG
using ProductEntity = MyApp.Domain.Entities.Product;
using DomainTask = TodoApp.Domain.Task;

// ✅ CORRECT
using MyApp.Domain.Entities;
Product product = new();
```

**Collision resolution — rename at the source:**
```csharp
// BEFORE — collision with System.Threading.Tasks.Task
// Domain/Tasks/Task.cs
public class Task { ... }  // ❌ Collides

// AFTER — rename the domain type
// Domain/WorkItems/WorkItem.cs
public class WorkItem { ... }  // ✅ No collision
```

**Rules:**
- ZERO named usings — not even "just this once"
- Rename conflicting types at the source
- File names must match class names exactly

### 7. Never global usings
```csharp
// ❌ WRONG
global using System;

// ✅ CORRECT — per file
using System;
```

### 8. Never fully qualified type names in code
```csharp
// ❌ WRONG
public Projects.Enums.Status Status { get; set; }
FluentValidation.Results.ValidationResult result = ...;
System.Collections.Generic.List<string> list = ...;

// ✅ CORRECT — use proper usings
using Projects.Enums;
using FluentValidation.Results;

public Status Status { get; set; }
ValidationResult result = ...;
List<string> list = ...;
```

---

## Naming & Conventions

### 9. Always use explicit types — never `var`
```csharp
// ❌ WRONG
var result = await _service.GetAsync(id);

// ✅ CORRECT
ProductDto result = await _service.GetAsync(id);
```

### 10. Entity folders always plural
```csharp
// ❌ WRONG
Domain/Product/Product.cs

// ✅ CORRECT
Domain/Products/Product.cs
```

### 11. Always write code in English
```csharp
// ❌ WRONG
public bool EstaActivo { get; set; }

// ✅ CORRECT
public bool IsActive { get; set; }
```

### 12. Always `sealed` on non-inherited classes
```csharp
// ❌ WRONG
public class ProductRepository { }

// ✅ CORRECT
public sealed class ProductRepository { }
```

### 13. NEVER magic strings — ALWAYS use constants or enums

```csharp
// ❌ WRONG
if (product.Status == "active") { ... }
string connection = Configuration["ConnectionStrings:Default"];

// ✅ CORRECT — enums for states
public enum ProductStatus { Active, Inactive, Pending }
if (product.Status == ProductStatus.Active) { ... }

// ✅ CORRECT — constants for config keys
public static class ConfigKeys
{
    public const string DefaultConnection = "ConnectionStrings:Default";
}
string connection = Configuration[ConfigKeys.DefaultConnection];
```

**Decision rule:**
- State / status / type → enum
- Config key / route / repeated literal → `const string`
- Same string appears twice → extract to constant immediately

---

## Null Safety

### 14. Never use the null-forgiving operator (`!`)

The `!` operator **MUST NEVER BE USED**. If you feel you need it, your validation logic is wrong.

```csharp
// ❌ WRONG — validates error count, then suppresses nullable warning with !
(List<string> errors, TodoItem? item) = TodoItem.Create(request.Title);
if (errors.Count > 0)
{
    return Result<TodoItemDto>.Failure(HttpStatusCode.BadRequest).WithErrors(errors);
}
await _repo.AddAsync(item!, ct);  // ❌ DANGEROUS

// ✅ CORRECT — validate the object's nullability, not the error count
(List<string> errors, TodoItem? item) = TodoItem.Create(request.Title);
if (item is null)
{
    return Result<TodoItemDto>.Failure(HttpStatusCode.BadRequest).WithErrors(errors);
}
await _repo.AddAsync(item, ct);  // ✅ Compiler-safe
```

**Rules:**
- Never use `!` — fix validation logic instead
- Always validate `if (entity is null)` — not `if (errors.Count > 0)`
- Factory methods return `(List<string> errors, T? entity)` — check the entity, not the list

---

## Async & Threading

### 15. Never `async void`
```csharp
// ❌ WRONG
public async void DoWork() { }

// ✅ CORRECT
public async Task DoWork() { }
```

### 16. Never `.Result` or `.Wait()`
```csharp
// ❌ WRONG
Product product = _service.Get(id).Result;

// ✅ CORRECT
Product product = await _service.Get(id);
```

### 17. Always `CancellationToken` on public async methods
```csharp
// ❌ WRONG
public async Task<Product> GetByIdAsync(Guid id) { }

// ✅ CORRECT
public async Task<Product> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { }
```

### 18. Never throw exceptions — use Result pattern
```csharp
// ❌ WRONG
if (!IsValid) throw new ValidationException("Invalid");

// ✅ CORRECT
if (!IsValid) return Result<T>.Failure(HttpStatusCode.BadRequest).WithErrors(["Invalid"]);
```

**Rules:**
- Never throw in application code — only unhandled infrastructure errors reach the global handler
- Never create custom exceptions — Result pattern handles all error flows
- Always use `Result<T>.Success()` / `Result<T>.Failure()` with appropriate HTTP status codes

---

## DTOs

### 19. DTOs must be `sealed record` with explicit properties and `Dto` suffix

```csharp
// ❌ WRONG — class, primary constructor
public class CreateProductRequest(string name, decimal price) { }

// ✅ CORRECT
public sealed record CreateProductRequestDto
{
    public required string Name { get; init; }
    public decimal Price { get; init; }
}
```

**Quick rules:**
- Always `sealed record` with explicit properties
- Always `Dto` suffix — `CreateProductRequestDto`, `ProductResponseDto`
- Request DTOs: `{ get; init; }` — allows object initializers
- Non-nullable DTO properties use `required` instead of `!`
- Use nullable properties only when null is semantically valid
- File name must match DTO name exactly

For complete DTO rules, naming, mapping patterns, and anti-patterns:
→ load `backend/dotnet/shared/dto-conventions/SKILL.md`
