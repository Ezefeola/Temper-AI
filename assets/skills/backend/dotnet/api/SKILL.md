---
name: dotnet-api
description: >
  Universal .NET 10 standards that apply to ANY backend project regardless of
  architecture. Covers async/await, FluentValidation, error handling middleware,
  Program.cs setup, logging, naming conventions, nullable reference types,
  and appsettings structure. For EF Core specifics, load backend/dotnet/ef-core.
  Load this skill for any .NET backend work.
---

# .NET 10 Universal Standards — TemperAI

> These standards apply to ALL .NET 10 backend projects regardless of architecture pattern.
> For EF Core specifics (entity configuration, repository patterns, DbContext), load `backend/dotnet/ef-core`.

---

## Async/await standards

### Absolute rules

- **Never `async void`** — always `async Task`. `async void` crashes the process on unhandled exceptions.
- **Never `.Result` or `.Wait()`** — causes deadlocks in ASP.NET Core. Always use `await`.
- **Always `CancellationToken`** on public async methods — enables request cancellation and graceful shutdown.
- **Always pass `CancellationToken`** to downstream async calls — do not swallow it.
- **Always name async methods with `Async` suffix** — `GetByIdAsync`, `SaveChangesAsync`.

```csharp
// GOOD
public async Task<Result<ProductResponseDto>> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
{
    ProductResponseDto product = await _service.GetByIdAsync(id, cancellationToken);

    if (product is null)
    {
        return Result<ProductResponseDto>
            .Failure(HttpStatusCode.NotFound)
            .WithDescription("Product not found");
    }

    return Result<ProductResponseDto>
        .Success(HttpStatusCode.OK)
        .WithPayload(product);
}

// BAD — async void
public async void DoSomething() { }

// BAD — .Result
Product product = _service.GetById(id).Result;

// BAD — no CancellationToken
public async Task<Product> GetByIdAsync(Guid id)
{
    return await _repository.FirstAsync(p => p.Id == id);
}
```

### ConfigureAwait

- Do not use `ConfigureAwait(false)` in ASP.NET Core applications — the synchronization context is not captured.
- Use `ConfigureAwait(false)` only in library code that may be consumed by UI applications.

---

## FluentValidation

- **Always use FluentValidation** for request DTO validation — never DataAnnotations on DTOs.
- **One validator per DTO** — named `[DtoName]Validator`.
- **Register validators** with `AddValidatorsFromAssembly`.
- **Never use hardcoded numbers** — always reference `Entity.Rules` constants for lengths, ranges, and constraints.

```csharp
public sealed class CreateProductRequestDtoValidator : AbstractValidator<CreateProductRequestDto>
{
    public CreateProductRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Product.Rules.NAME_MAX_LENGTH);

        RuleFor(x => x.Description)
            .MaximumLength(Product.Rules.DESCRIPTION_MAX_LENGTH);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(Product.Rules.MIN_PRICE);
    }
}
```

```csharp
// Program.cs
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
```

---

## Global error handling middleware

All unhandled exceptions must be caught and returned as `ProblemDetails`.

```csharp
// Program.cs
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.ContentType = "application/problem+json";

        ExceptionHandlerPathFeature exceptionHandlerPathFeature =
            context.Features.Get<IExceptionHandlerPathFeature>();

        Exception exception = exceptionHandlerPathFeature?.Error;

        ProblemDetails problemDetails = exception switch
        {
            NotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Detail = notFound.Message
            },
            ValidationException validation => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "One or more validation errors occurred",
                Extensions = { ["errors"] = validation.Errors }
            },
            ConflictException conflict => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = conflict.Message
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal server error",
                Detail = "An unexpected error occurred"
            }
        };

        ILogger<Program> logger = context.RequestServices
            .GetRequiredService<ILogger<Program>>();

        if (exception is not NotFoundException && exception is not ValidationException)
        {
            logger.LogError(exception, "Unhandled exception occurred");
        }

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});
```

### Custom exception types

```csharp
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, object id)
        : base($"{entityName} with id {id} was not found")
    {
    }
}

public sealed class ValidationException : Exception
{
    public List<string> Errors { get; }

    public ValidationException(List<string> errors)
        : base("One or more validation errors occurred")
    {
        Errors = errors;
    }
}

public sealed class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
```

---

## Program.cs — structure and extension methods

Keep `Program.cs` clean. All DI setup goes through extension methods.

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Framework services
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Application layer
builder.Services.AddApplication();

// Infrastructure layer
builder.Services.AddInfrastructure(builder.Configuration);

// Validation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

WebApplication app = builder.Build();

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();
```

---

## API documentation — Scalar (never Swagger)

**Always use Scalar** for API documentation. Never use Swagger/Swashbuckle.

Scalar is faster, cleaner, and has a better UX. It consumes the OpenAPI document generated by .NET 10's built-in `AddOpenApi()`.

### Required package

```xml
<PackageReference Include="Scalar.AspNetCore" Version="2.+" />
```

### Configuration

```csharp
// Program.cs
builder.Services.AddOpenApi();

// ...

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("YourProject API");
        options.WithTheme(ScalarTheme.Kepler);
    });
}
```

### Accessing the docs

- OpenAPI JSON: `/openapi/v1.json`
- Scalar UI: `/scalar/v1`

### Endpoint documentation

Use `[EndpointDescription]` and XML comments for endpoint documentation:

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpPost]
    [EndpointDescription("Creates a new product with the provided data.")]
    [ProducesResponseType(typeof(CreateProductResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequestDto request,
        [FromServices] ICreateProduct createProduct,
        CancellationToken cancellationToken)
    {
        Result<CreateProductResponseDto> result =
            await createProduct.ExecuteAsync(request, cancellationToken);

        return result.ToActionResult();
    }
}
```

### Scalar rules

- **Never use Swagger/Swashbuckle** — Scalar is the TemperAI standard.
- **Always add `[EndpointDescription]`** to every endpoint for clear documentation.
- **Always include `[ProducesResponseType]`** for all success and error responses.
- **Only expose Scalar in Development** — use `if (app.Environment.IsDevelopment())`.
- **Always set a meaningful title** with `WithTitle("ProjectName API")`.

### Extension method pattern

```csharp
// Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddDatabase(configuration)
            .AddRepositories()
            .AddUnitOfWork();

        return services;
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // See backend/dotnet/ef-core for EF Core implementation
        return services;
    }

    private static IServiceCollection AddRepositories(
        this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, ProductRepository>();
        return services;
    }

    private static IServiceCollection AddUnitOfWork(
        this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
```

---

## Logging with ILogger<T>

### When to use each level

| Level | When to use | Example |
|---|---|---|
| `LogTrace` | Detailed diagnostic info, only in development | Request payload, raw SQL queries |
| `LogDebug` | Information useful for debugging during development | Cache hit/miss, branch taken |
| `LogInformation` | Normal application flow events | User created, order placed, migration applied |
| `LogWarning` | Unexpected but recoverable situations | Deprecated API called, retry attempt, slow query |
| `LogError` | Failures that break a single operation but not the app | Use case failed, external service timeout |
| `LogCritical` | System-wide failures that require immediate attention | Database unreachable, out of memory |

### Structured logging

Always use structured logging with named placeholders — never string interpolation in log messages.

```csharp
// GOOD — structured logging
_logger.LogInformation(
    "Product {ProductId} created by user {UserId}",
    productId,
    userId);

// BAD — string interpolation
_logger.LogInformation($"Product {productId} created by user {userId}");
```

### Logging in use cases

```csharp
public sealed class CreateProduct : ICreateProduct
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateProduct> _logger;

    public CreateProduct(IUnitOfWork unitOfWork, ILogger<CreateProduct> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CreateProductResponseDto>> ExecuteAsync(
        CreateProductRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // ... logic ...

        _logger.LogInformation(
            "Product {ProductId} created with name {ProductName}",
            product.Id,
            product.Name);

        return Result<CreateProductResponseDto>
            .Success(HttpStatusCode.Created)
            .WithPayload(product.ToCreateProductResponseDto());
    }
}
```

---

## C# naming conventions

| Element | Convention | Example |
|---|---|---|
| Classes | `PascalCase` | `Product`, `CreateProductHandler` |
| Interfaces | `I` + `PascalCase` | `IProductRepository`, `ICreateProduct` |
| Methods | `PascalCase` | `GetByIdAsync`, `CreateAsync` |
| Properties | `PascalCase` | `Name`, `CreatedAt` |
| Fields (private) | `_camelCase` | `_unitOfWork`, `_logger` |
| Local variables | `camelCase` — match type name | `SaveResult saveResult`, `Product product` |
| Parameters | `camelCase` | `cancellationToken`, `createProductRequestDto` |
| Constants | `PascalCase` in nested `Rules` class | `Rules.NAME_MAX_LENGTH` |
| DTOs | `PascalCase` + `Dto` suffix | `CreateProductRequestDto` |
| Use cases | `PascalCase` — no `UseCase` suffix | `CreateProduct`, `UpdateProduct` |
| Events | `PascalCase` + `Event` suffix | `ProductCreatedEvent` |
| Enums | `PascalCase` | `ProductStatus`, `OrderType` |

---

## Nullable reference types

### How to enable

Nullable reference types are enabled by default in .NET 10 projects. Ensure your `.csproj` has:

```xml
<PropertyGroup>
    <Nullable>enable</Nullable>
</PropertyGroup>
```

### How to use correctly

- **`string`** — non-nullable, must be initialized. Use `= string.Empty` as default.
- **`string?`** — nullable, can be null. Must be checked before use.
- **Injected properties** — use `= default!` with `[Inject]` or `[FromServices]`.
- **Never use `!` (null-forgiving operator)** without justification — it suppresses the compiler warning without actual null checking.

```csharp
// GOOD — non-nullable string with default
public string Name { get; init; } = string.Empty;

// GOOD — nullable string, checked before use
public string? Description { get; init; }

if (!string.IsNullOrWhiteSpace(description))
{
    // safe to use
}

// GOOD — injected property
[Inject]
private IProductService ProductService { get; set; } = default!;

// BAD — unjustified null-forgiving
Product product = await _repository.GetByIdAsync(id)!; // why is this safe?
```

---

## Global usings — do not use

Global usings are a **bad practice** in TemperAI projects. Do not use them.

- Nothing guarantees that a globally imported namespace is needed in every file.
- They pollute the IntelliSense and make it unclear where types come from.
- They create hidden dependencies that are hard to track when refactoring.
- They make it harder for new developers to understand the project structure.

Always use explicit `using` directives at the top of each file. Only import what you actually use.

```csharp
// GOOD — explicit usings
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

// BAD — global usings
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.Logging;
```

### `using static` — never

- **Never use `using static`** — always use explicit `using` directives with the namespace, then reference types by their name. Static usings hide the type origin and make code harder to read and navigate.

### Named usings — never

- **Never use named usings** (e.g., `using TodoTask = ...`) — if a name collision occurs, use the fully qualified namespace or rename the entity. Aliases obscure code and make refactoring difficult.

### Line formatting

- **Never break short lines unnecessarily** — keep assignments on a single line if they fit on screen.
- **GOOD:** `Result<CreateProductResponseDto> result = await handler.HandleAsync(request, cancellationToken);`
- **BAD:** breaking a 90-character assignment into 3 lines for no reason.

---

## appsettings.json structure

### appsettings.json (production defaults)

```json
{
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=project_dev;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

### Rules

- **Never commit secrets** to `appsettings.json` — use `appsettings.Development.json` (gitignored) or User Secrets / environment variables.
- **Connection strings in `appsettings.json` should be empty** — filled by environment or secrets in production.
- **Logging levels** — `Information` for production, `Debug` for development. Never `Trace` in production.
- **Use environment variables** for production configuration — `ConnectionStrings__Default`, `ASPNETCORE_ENVIRONMENT`.

---

## Absolute rules

- Never `async void` — always `async Task`.
- Never `.Result` or `.Wait()` — always `await`.
- Never `DataAnnotations` on entities or Value Objects.
- Never `using static` — always use explicit `using` directives.
- Never use named usings — rename the entity or use fully qualified namespace instead.
- Never use global usings — always use explicit per-file `using` directives.
- Never break short lines unnecessarily — keep assignments on one line if they fit.
- Never use `var` — always declare the explicit type. `Product product = ...`, `List<string> errors = ...`. Implicit typing hides the actual type.
- Never commit secrets to `appsettings.json`.
- Never use `!` (null-forgiving operator) without justification.
- Always `CancellationToken` on public async methods.
- Always structured logging with named placeholders.
- Always `ProblemDetails` for error responses.
- Always extension methods for DI setup — keep `Program.cs` clean.
- Always variable names matching their type — `SaveResult saveResult`, `Product product`.
- Always write code in English — class names, methods, properties, enums, enum values, namespaces, and comments. Only user-facing error messages in API responses may be in the user's language.
