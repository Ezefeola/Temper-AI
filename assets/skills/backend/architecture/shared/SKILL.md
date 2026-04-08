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

**CRITICAL: This is the ONLY Result pattern allowed. NEVER create variations, alternatives, or simplified versions.**

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

**Usage rules — NEVER broken:**

1. **HttpStatusCode is MANDATORY** — Every `Success()` and `Failure()` call MUST include an HttpStatusCode parameter.
   - ✅ `Result<UserDto>.Success(HttpStatusCode.Created)`
   - ✅ `Result<UserDto>.Failure(HttpStatusCode.NotFound)`
   - ❌ `Result<UserDto>.Success()` — NEVER omit HttpStatusCode
   - ❌ `Result<UserDto>.Success(201)` — NEVER use numeric codes

2. **Common HttpStatusCode values:**
   - `HttpStatusCode.Created` — New resource created
   - `HttpStatusCode.OK` — Successful query/update
   - `HttpStatusCode.BadRequest` — Validation errors
   - `HttpStatusCode.NotFound` — Resource not found
   - `HttpStatusCode.Conflict` — Business rule violation
   - `HttpStatusCode.InternalServerError` — Unexpected error

3. **Use case returns Result with HttpStatusCode. Controller calls `result.ToActionResult()`. NOTHING ELSE.**

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
- **Mappers ONLY transform data from one type to another. They NEVER check status codes, NEVER decide HTTP responses, NEVER contain conditional logic based on HttpStatusCode or Result state.**
- **If a mapper needs to handle an error case, it should return null or throw — the calling code handles the Result pattern.**

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

**ANTI-PATTERNS — NEVER DO THIS:**

```csharp
// ❌ NEVER check status codes in mappers
public static IActionResult MapToResponse(this Result<Product> result)
{
    if (result.HttpStatusCode == HttpStatusCode.NotFound)
        return NotFound();
    // ...
}

// ❌ NEVER create conditional mapping based on Result state
public static object ToResponseDto(this Result<Product> result)
{
    if (!result.IsSuccess)
        return new { error = result.Description };
    return result.Payload.ToDto();
}

// ✅ CORRECT: Simple data transformation only
public static ProductDto ToProductDto(this Product product)
{
    return new ProductDto
    {
        Id = product.Id,
        Name = product.Name
    };
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
- **NEVER check `result.IsSuccess` to decide status codes — ResultExtensions.ToActionResult() handles this automatically.**
- **NEVER create custom error mapping in controllers — the Result pattern with HttpStatusCode is the single source of truth.**
- **NEVER use switch/if on HttpStatusCode in controllers — the extension method already handles all cases.**

```csharp
// ✅ CORRECT — minimal controller, delegates to ResultExtensions
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateProductRequestDto request, [FromServices] ICreateProduct useCase, CancellationToken ct)
{
    Result<CreateProductResponseDto> result = await useCase.ExecuteAsync(request, ct);
    return result.ToActionResult();
}

// ❌ NEVER DO THIS — manual status code checking
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateProductRequestDto request, [FromServices] ICreateProduct useCase, CancellationToken ct)
{
    Result<CreateProductResponseDto> result = await useCase.ExecuteAsync(request, ct);
    
    if (result.HttpStatusCode == HttpStatusCode.NotFound)
        return NotFound(result.Description);
    
    if (!result.IsSuccess)
        return BadRequest(result.Errors);
    
    return Ok(result.Payload);
}
```

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
            Title = "One or more errors occurred.",
            Detail = result.Description
        };

        problemDetails.Extensions["errors"] = result.Errors;

        return new ObjectResult(problemDetails)
        {
            StatusCode = (int)result.HttpStatusCode
        };
    }
}
```

### Use case patterns

- `sealed class` without `UseCase` suffix — `CreateProduct`, `UpdateProduct`.
- Interface in the same folder — `ICreateProduct`, `IUpdateProduct`.
- Explicit constructor injection — never primary constructor.
- Domain events published explicitly after `CompleteAsync` — never automatic in SaveChanges.
- **Result pattern with HttpStatusCode — ALWAYS use `Result<TResponse>.Success(HttpStatusCode.Created)` or `Result<TResponse>.Failure(HttpStatusCode.NotFound)`. NEVER omit the HttpStatusCode parameter.**
- **NEVER create custom status code logic in use cases — the HttpStatusCode passed to Result.Success/Failure is the single source of truth for HTTP responses.**
- **The use case returns a Result with HttpStatusCode. The controller calls `result.ToActionResult()`. That's it. No additional status code checks, no custom error mapping.**

### Entity patterns

- Entities are `sealed class` with `private` constructor.
- Factory method returns `(List<string> Errors, Entity? Entity)`.
- Update methods return `(List<string> Errors, bool Updated)`.
- Nested `Rules` class with constraint constants.
- `UpdatedAt` set explicitly on every update method.
- Update methods validate invariants AND check if the value actually changed.

### Null safety in use cases

**CRITICAL: Never validate error count. Always validate nullability.**

When a factory method returns `(List<string> errors, Entity? entity)`, the use case MUST check `if (entity is null)`, NOT `if (errors.Count > 0)`.

```csharp
// ❌ WRONG — validates error count, then uses ! (null-forgiving operator)
(List<string> errors, TodoItem? item) = TodoItem.Create(request.Title);
if (errors.Count > 0)
{
    return Result<TodoItemDto>.Failure(HttpStatusCode.BadRequest).WithErrors(errors);
}
await _repo.AddAsync(item!, ct);  // ⚠️ DANGER — uses ! operator
```

```csharp
// ✅ CORRECT — validates nullability of the entity
(List<string> errors, TodoItem? item) = TodoItem.Create(request.Title);
if (item is null)
{
    return Result<TodoItemDto>.Failure(HttpStatusCode.BadRequest).WithErrors(errors);
}
await _repo.AddAsync(item, ct);
```

**Rules:**
1. **Never use the null-forgiving operator (`!`)** — if you need it, your validation logic is wrong.
2. **Always use `if (entity is null)`** — never `if (errors.Count > 0)`.
3. **The factory method pattern** `(List<string> errors, T? entity)` requires checking the entity's nullability.
4. **The `!` operator is a code smell** — it suppresses the compiler's warning about potential null references, hiding a bug rather than fixing it.

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

- Never throw exceptions — use Result pattern for all error handling.
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
