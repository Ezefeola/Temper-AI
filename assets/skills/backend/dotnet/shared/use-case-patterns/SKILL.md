---
name: use-case-patterns
description: >
  Canonical use case structure, naming, and DI conventions for backend tasks.
  Load when creating or modifying use cases or controllers that invoke them.
  Do not load for Vertical Slice handlers.
requires: [backend-dotnet-csharp, backend-dotnet-shared-result-pattern]
produces: [use-case-classes, use-case-interfaces]
---

# Use Case Patterns — TemperAI

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **NEVER use a `UseCase` suffix** on use case classes
2. **ALWAYS keep interface and implementation together** in the same folder
3. **ALWAYS use explicit constructor injection**
4. **ALWAYS return `Result<TResponse>` with `HttpStatusCode`**
5. **NEVER place business logic in controllers**

## When NOT to apply this skill

- You are implementing Vertical Slice handlers or Minimal API endpoints
- The chosen architecture does not use use case classes for this task

## Naming

| Element | Convention | Example |
|---|---|---|
| Use case | PascalCase, no suffix | `CreateProduct` |
| Interface | `I` prefix | `ICreateProduct` |
| Mapping extension | `To` + DTO name | `ToCreateProductResponseDto()` |

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

## Result flow

- Use cases always return `Result<TResponse>.Success(HttpStatusCode.X)` or `Failure(HttpStatusCode.X)`
- Controllers call `result.ToActionResult()` and nothing else
- Do not create custom status code branching in use cases or controllers

## Null safety with domain factories

When a factory returns `(List<string> errors, Entity? entity)`, validate the entity reference, not the error count.

```csharp
// ❌ WRONG — validates error count instead of the nullable entity reference
(List<string> errors, TodoItem? item) = TodoItem.Create(request.Title);
if (errors.Count > 0)
    return Result<TodoItemDto>.Failure(HttpStatusCode.BadRequest).WithErrors(errors);

// item may still be null here even though the error count check passed

// ✅ CORRECT — validate nullability directly
(List<string> errors, TodoItem? item) = TodoItem.Create(request.Title);
if (item is null)
    return Result<TodoItemDto>.Failure(HttpStatusCode.BadRequest).WithErrors(errors);

await _repo.AddAsync(item, cancellationToken);
```

Rules:

- Never use the null-forgiving operator (`!`) in use case flows
- Always prefer `if (entity is null)` over `if (errors.Count > 0)` when the factory returns a nullable entity

## Domain entity interaction patterns

When a use case collaborates with domain entities, keep these legacy rules intact:

- Entities are `sealed class` with `private` constructors
- Factory methods return `(List<string> Errors, Entity? Entity)`
- Update methods return `(List<string> Errors, bool Updated)`
- Constraint constants live in a nested `Rules` class
- `UpdatedAt` is set explicitly in every successful update method

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

        return (errors, new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            UpdatedAt = DateTime.UtcNow
        });
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

- Use explicit constructor injection
- Keep `AddApplication()` small and delegate to grouped private registration methods
- Register use cases by interface and implementation pair

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
