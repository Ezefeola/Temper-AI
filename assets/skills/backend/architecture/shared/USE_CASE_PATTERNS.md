---
name: use-case-patterns
description: >
  Use case structure, naming conventions, and DI patterns.
  Load when creating use cases, handlers, or controllers.
---

# Use Case Patterns — TemperAI

## Naming conventions

| Element | Convention | Example |
|---|---|---|
| Use cases | No suffix, PascalCase | `CreateProduct`, `UpdateProduct` |
| Use case interfaces | Prefix `I` | `ICreateProduct`, `IUpdateProduct` |
| Request DTOs | Suffix `RequestDto` | `CreateProductRequestDto` |
| Response DTOs | Suffix `ResponseDto` | `CreateProductResponseDto` |
| Mapping extensions | Prefix `To` + DTO name | `ToCreateProductResponseDto()` |

## Structure

- `sealed class` without `UseCase` suffix
- Interface in the same folder
- Explicit constructor injection — never primary constructor

```csharp
public interface ICreateProduct
{
    Task<Result<CreateProductResponseDto>> ExecuteAsync(
        CreateProductRequestDto request,
        CancellationToken cancellationToken);
}

public sealed class CreateProduct : ICreateProduct
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;

    public CreateProduct(
        IUnitOfWork unitOfWork,
        IProductRepository productRepository)
    {
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
    }

    public async Task<Result<CreateProductResponseDto>> ExecuteAsync(
        CreateProductRequestDto request,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

## Result pattern with HttpStatusCode

**ALWAYS use `Result<TResponse>.Success(HttpStatusCode.Created)` or `Result<TResponse>.Failure(HttpStatusCode.NotFound)`.**

- **NEVER omit the HttpStatusCode parameter**
- **NEVER create custom status code logic in use cases**
- **The use case returns a Result with HttpStatusCode. The controller calls `result.ToActionResult()`. That's it.**

## Null safety in use cases

**CRITICAL: Never validate error count. Always validate nullability.**

When a factory method returns `(List<string> errors, Entity? entity)`, check `if (entity is null)`, NOT `if (errors.Count > 0)`.

```csharp
// ❌ WRONG — validates error count, then uses ! (null-forgiving operator)
(List<string> errors, TodoItem? item) = TodoItem.Create(request.Title);
if (errors.Count > 0)
{
    return Result<TodoItemDto>.Failure(HttpStatusCode.BadRequest).WithErrors(errors);
}
await _repo.AddAsync(item!, ct);  // ⚠️ DANGER — uses ! operator

// ✅ CORRECT — validates nullability of the entity
(List<string> errors, TodoItem? item) = TodoItem.Create(request.Title);
if (item is null)
{
    return Result<TodoItemDto>.Failure(HttpStatusCode.BadRequest).WithErrors(errors);
}
await _repo.AddAsync(item, ct);
```

**Rules:**
1. **Never use the null-forgiving operator (`!`)** — if you need it, your validation logic is wrong
2. **Always use `if (entity is null)`** — never `if (errors.Count > 0)`
3. **The `!` operator is a code smell** — it suppresses the compiler's warning about potential null references

## Entity patterns in use cases

- Entities are `sealed class` with `private` constructor
- Factory method returns `(List<string> Errors, Entity? Entity)`
- Update methods return `(List<string> Errors, bool Updated)`
- Nested `Rules` class with constraint constants
- `UpdatedAt` set explicitly on every update method

```csharp
public sealed class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime UpdatedAt { get; private set; }

    private Product() { }

    public static (List<string> Errors, Product? Entity) Create(string name)
    {
        List<string> errors = [];
        
        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Name is required.");
        
        if (name.Length > Rules.NAME_MAX_LENGTH)
            errors.Add($"Name must not exceed {Rules.NAME_MAX_LENGTH} characters.");
        
        if (errors.Count > 0)
            return (errors, null);
        
        return (errors, new Product { Id = Guid.NewGuid(), Name = name, UpdatedAt = DateTime.UtcNow });
    }

    public (List<string> Errors, bool Updated) UpdateName(string newName)
    {
        List<string> errors = [];
        
        if (string.IsNullOrWhiteSpace(newName))
            errors.Add("Name is required.");
        
        if (newName.Length > Rules.NAME_MAX_LENGTH)
            errors.Add($"Name must not exceed {Rules.NAME_MAX_LENGTH} characters.");
        
        if (Name == newName)
            return (errors, false);
        
        if (errors.Count > 0)
            return (errors, false);
        
        Name = newName;
        UpdatedAt = DateTime.UtcNow;
        return (errors, true);
    }

    public static class Rules
    {
        public const int NAME_MAX_LENGTH = 100;
    }
}
```

## DI conventions

- Private methods per responsibility — `AddDatabase`, `AddRepositories`, `AddUnitOfWork`
- `AddApplication` → `AddUseCases` → `AddProductUseCases`, etc.

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddUseCases();
        return services;
    }

    private static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<ICreateProduct, CreateProduct>();
        services.AddScoped<IUpdateProduct, UpdateProduct>();
        return services;
    }
}
```