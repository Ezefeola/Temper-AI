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

## Consuming domain entities

A use case orchestrates entities through the contract their factory and update methods expose —
it does NOT author entities. The authoring rules (sealed class, private constructor, nested
`Rules`, update methods, aggregates) live in `backend/dotnet/ddd/SKILL.md`; load it only when
creating or modifying a domain entity.

The contract a use case relies on:

- Factory methods return `(List<string> Errors, Entity? Entity)`
- Update methods return `(List<string> Errors, bool Updated)`

When a factory returns a nullable entity, validate the entity reference, not the error count
(full rationale and example in `backend/dotnet/csharp/SKILL.md` §14 Null Safety):

```csharp
// ✅ CORRECT — validate nullability directly; never check errors.Count, never use `!`
(List<string> errors, TodoItem? item) = TodoItem.Create(request.Title);
if (item is null)
    return Result<TodoItemDto>.Failure(HttpStatusCode.BadRequest).WithErrors(errors);

await _repo.AddAsync(item, cancellationToken);
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
