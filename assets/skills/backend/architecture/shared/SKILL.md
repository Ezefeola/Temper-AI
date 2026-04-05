---
name: architecture-shared
description: >
  Shared rules that apply to ALL architecture patterns in TemperAI projects.
  Load this skill regardless of whether you use Clean, Hexagonal, Vertical Slice,
  or Onion Architecture. Contains the Result pattern, DTO conventions,
  naming standards, and cross-cutting concerns common to all patterns.
  For data access implementation, load backend/dotnet/ef-core.
---

# Architecture Shared Rules — TemperAI

> For data access implementation (UnitOfWork class, DbContext, repositories, entity configurations), load `backend/dotnet/ef-core` or your chosen data access skill. This skill defines the contracts and conventions only.

## These rules apply to EVERY architecture pattern

### Result pattern with HttpStatusCode

```csharp
public sealed class Result<TResponse>
{
    public bool IsSuccess { get; private set; }
    public HttpStatusCode HttpStatusCode { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public List<string> Errors { get; private set; } = [];
    public TResponse? Payload { get; private set; }

    private Result(bool isSuccess, HttpStatusCode httpStatusCode)
    {
        IsSuccess = isSuccess;
        HttpStatusCode = httpStatusCode;
    }

    public Result<TResponse> WithDescription(string description)
    {
        Description = description;
        return this;
    }

    public Result<TResponse> WithErrors(List<string> errors)
    {
        Errors = errors;
        return this;
    }

    public Result<TResponse> WithPayload(TResponse payload)
    {
        Payload = payload;
        return this;
    }

    public static Result<TResponse> Success(HttpStatusCode httpStatusCode)
    {
        return new(true, httpStatusCode);
    }

    public static Result<TResponse> Failure(HttpStatusCode httpStatusCode)
    {
        return new(false, httpStatusCode);
    }
}
```

### DTO conventions

- Always `sealed record` with explicit properties — never primary constructors.
- Always suffix with `Dto` — `CreateProductRequestDto`, `CreateProductResponseDto`.
- String properties default to `string.Empty`.

```csharp
// GOOD — explicit properties, no primary constructor
public sealed record CreateProductRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
}

// BAD — primary constructor (NEVER DO THIS)
public sealed record CreateProductRequestDto(string Name, string Description, decimal Price);

// GOOD — explicit properties with defaults
public sealed record CreateProductResponseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Status { get; init; } = string.Empty;
}

// BAD — primary constructor with no defaults (NEVER DO THIS)
public sealed record CreateProductResponseDto(Guid Id, string Name, decimal Price, string Status);
```

```csharp
public sealed record CreateProductRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
}
```

### Mapping conventions

- Extension methods in `[Entity]MappingExtensions.cs`.
- Method name: `To[DtoName]` — exact match with DTO name.
- Located at the use case or feature level.

```csharp
public static class ProductMappingExtensions
{
    public static CreateProductResponseDto ToCreateProductResponseDto(this Product product)
    {
        return new CreateProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Status = product.Status.ToString()
        };
    }
}
```

### Use case naming

- No `UseCase` suffix — `CreateProduct`, `UpdateProduct`.
- Interface in the same folder — `ICreateProduct`, `IUpdateProduct`.
- `sealed class` with explicit constructor.

### Controller conventions

- No general constructor — use `[FromServices]` per endpoint.
- Always explicit `[FromBody]`, `[FromRoute]`, `[FromQuery]`.
- Always return `result.ToActionResult()` — never build responses manually.
- Errors always as `ProblemDetails` with `errors` field.

```csharp
public static class ResultExtensions
{
    public static IActionResult ToActionResult<TResponse>(this Result<TResponse> result)
    {
        if (result.IsSuccess)
        {
            return new ObjectResult(result.Payload)
            {
                StatusCode = (int)result.HttpStatusCode
            };
        }

        ProblemDetails problemDetails = new()
        {
            Status = (int)result.HttpStatusCode,
            Title = ResolveTitle(result.HttpStatusCode),
            Detail = result.Description
        };

        problemDetails.Extensions["errors"] = result.Errors;

        return new ObjectResult(problemDetails)
        {
            StatusCode = (int)result.HttpStatusCode
        };
    }

    private static string ResolveTitle(HttpStatusCode httpStatusCode)
    {
        return httpStatusCode switch
        {
            HttpStatusCode.BadRequest => "Invalid request",
            HttpStatusCode.NotFound => "Resource not found",
            HttpStatusCode.Conflict => "Conflict with current state",
            HttpStatusCode.InternalServerError => "Internal server error",
            HttpStatusCode.Unauthorized => "Unauthorized",
            HttpStatusCode.Forbidden => "Access denied",
            _ => "Error"
        };
    }
}
```

### DI conventions

- Private methods per responsibility — `AddDatabase`, `AddRepositories`, `AddUnitOfWork`.
- `AddApplication` → `AddUseCases` → `AddProductUseCases`, etc.

### Naming conventions

| Element | Convention | Example |
|---|---|---|
| Interfaces | Prefix `I` + PascalCase | `IProductRepository` |
| Use cases | No suffix, PascalCase | `CreateProduct`, `UpdateProduct` |
| Use case interfaces | Prefix `I` | `ICreateProduct`, `IUpdateProduct` |
| Request DTOs | Suffix `RequestDto` | `CreateProductRequestDto` |
| Response DTOs | Suffix `ResponseDto` | `CreateProductResponseDto` |
| Mapping extensions | Prefix `To` + DTO name | `ToCreateProductResponseDto()` |
| Repositories | `I` + name + `Repository` | `IProductRepository` |
| Domain events | Suffix `Event` | `ProductCreatedEvent` |
| EF configs | Entity name + `Configuration` | `ProductConfiguration` |

### Absolute rules — never broken in any architecture

For all general C# conventions (syntax, usings, naming, async, DTOs), see `dotnet-csharp`.

- Never `DataAnnotations` on entities or Value Objects.
- Never `nvarchar(max)` or `varchar(max)` — always length from rules.
- Never `.Update()` from EF Core — change tracker detects changes.
- Never lazy loading — explicit includes always.
- Never throw for business validations.
- Always `varchar` for ASCII, `nvarchar` for Unicode.
- Always one `IEntityTypeConfiguration<T>` per entity.
- Always `GetByIdAsync` with tracking, `GetByIdAsNoTrackingAsync` without tracking.
- **Never use hardcoded numbers in validators** — always reference `Entity.Rules` constants (e.g., `.MaximumLength(Product.Rules.NAME_MAX_LENGTH)`). This centralizes constraint management and prevents inconsistencies.

---

## Agent startup announcement

Every agent MUST announce its loaded skills at the start of its execution:

```
🔧 [AgentName] starting
   Skills loaded: [skill1, skill2, ...]
   Context files: [file1, file2, ...]
```

This gives the user full visibility into what the agent knows and what conventions it will follow.
