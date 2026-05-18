---
name: dotnet-api
description: >
  ASP.NET Core API standards for .NET 10 projects. Covers controllers,
  middleware, routing, error handling, DI setup, logging, FluentValidation,
  nullable reference types, and appsettings structure.
  Load when creating or modifying controllers, middleware, Program.cs, or validators.
  DO NOT load for domain or repository tasks — load dotnet-ddd or the required `backend/dotnet/ef-core/*/SKILL.md` leaf instead.
  For API documentation provider wiring, load exactly one provider skill based on backend config:
  `backend/dotnet/api-docs/scalar/SKILL.md` or `backend/dotnet/api-docs/swagger/SKILL.md`.
  For general C# conventions, dotnet-csharp must be loaded first.
requires: [dotnet-csharp]
produces: [controllers, middleware, validators, program-cs, appsettings, logging]
---

# ASP.NET Core API Standards — TemperAI

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **When this skill governs endpoints, use Controllers only** — endpoints must inherit `ControllerBase` with `[ApiController]`
2. **NEVER improvise the API documentation provider** — use exactly the provider configured in `Docs/Application/Architecture/backend-config.md`
3. **NEVER DataAnnotations on DTOs** — always FluentValidation
4. **NEVER hardcoded numbers in validators** — always reference `Entity.Rules` constants
5. **NEVER string interpolation in log messages** — always structured logging with named placeholders

> For general C# conventions (syntax, usings, naming, async, DTOs): `dotnet-csharp` must be loaded.
> For EF Core specifics (entities, repositories, DbContext): load the required `backend/dotnet/ef-core/*/SKILL.md` leaf skill.
> For API documentation setup: load one provider skill only, based on backend config.

---

## When NOT to apply this skill

- You are working exclusively on domain entities, repositories, or use cases with no controller changes
- You are working on infrastructure configuration unrelated to the API layer
- You are implementing Vertical Slice feature endpoints or handlers and the architecture skill already defines the endpoint style

## Architecture precedence

- `dotnet-csharp` still governs universal C# rules.
- The chosen architecture skill decides endpoint style, project structure, and whether repositories / use cases exist.
- This skill governs host-level API concerns: controllers, middleware, `Program.cs`, API-facing validators, routing metadata, and logging.
- In `Vertical Slice`, do not load this skill for Minimal API endpoint files. Load it only for host-level concerns or existing grouped controllers.

---

## FluentValidation

**Always use FluentValidation** for request DTO validation — never DataAnnotations on DTOs.

```csharp
// ✅ CORRECT
public sealed class CreateProductRequestDtoValidator : AbstractValidator<CreateProductRequestDto>
{
    public CreateProductRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Product.Rules.NAME_MAX_LENGTH);  // ← always Entity.Rules, never magic numbers

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

**Rules:**
- One validator per DTO — named `[DtoName]Validator`
- Register with `AddValidatorsFromAssembly` — never register manually
- Always reference `Entity.Rules` constants — never hardcode lengths or ranges

---

## Controllers

This section applies only when the task is controller-based.

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

**Rules:**
- Always `[ApiController]` and `[Route("api/[controller]")]`
- Always `[EndpointDescription]` on every endpoint
- Always `[ProducesResponseType]` for ALL success and error responses
- Always inject use cases via `[FromServices]` — never use constructor injection in controllers
- Always delegate to a use case — zero business logic in controllers
- Always return `result.ToActionResult()` — never map manually

---

## Global error handling middleware

All unhandled exceptions are caught here and returned as `ProblemDetails`.
Since application code never throws, only unexpected infrastructure errors reach this handler.

```csharp
// Program.cs
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.ContentType = "application/problem+json";

        IExceptionHandlerPathFeature exceptionHandlerPathFeature =
            context.Features.Get<IExceptionHandlerPathFeature>();

        Exception exception = exceptionHandlerPathFeature?.Error;

        ProblemDetails problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal server error",
            Detail = "An unexpected error occurred"
        };

        ILogger<Program> logger = context.RequestServices
            .GetRequiredService<ILogger<Program>>();

        logger.LogError(exception, "Unhandled exception occurred");

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});
```

---

## Program.cs — structure and extension methods

Keep `Program.cs` clean. All DI registration goes through extension methods.

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

WebApplication app = builder.Build();

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
    // API documentation mapping belongs to the configured provider skill.
    // Load exactly one of:
    // - backend/dotnet/api-docs/scalar/SKILL.md
    // - backend/dotnet/api-docs/swagger/SKILL.md
}

app.Run();
```

**Rules:**
- All DI setup through `AddApplication()` and `AddInfrastructure()` extension methods
- Never register services directly in `Program.cs` — always in extension methods
- Middleware order is non-negotiable: HTTPS → Auth → Controllers

---

## API documentation provider selection

This skill defines the host-level API rules, but it does not choose the documentation provider.

- If `Docs/Application/Architecture/backend-config.md` says `Scalar` → also load `backend/dotnet/api-docs/scalar/SKILL.md`
- If `Docs/Application/Architecture/backend-config.md` says `Swagger` → also load `backend/dotnet/api-docs/swagger/SKILL.md`
- If the provider is missing or ambiguous and the task touches `Program.cs` or API docs → stop and ask

Never mix both providers in the same host unless a future skill explicitly allows it.

---

## Logging

### Log level reference

| Level | When | Example |
|---|---|---|
| `LogTrace` | Detailed diagnostics, dev only | Raw SQL, request payload |
| `LogDebug` | Debug during development | Cache hit/miss, branch taken |
| `LogInformation` | Normal flow events | User created, order placed |
| `LogWarning` | Unexpected but recoverable | Retry attempt, slow query |
| `LogError` | Single operation failure | Use case failed, timeout |
| `LogCritical` | System-wide failure | DB unreachable, OOM |

### Structured logging — always named placeholders

```csharp
// ✅ CORRECT — structured logging
_logger.LogInformation(
    "Product {ProductId} created by user {UserId}",
    productId,
    userId);

// ❌ WRONG — string interpolation
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

## Nullable reference types

```xml
<!-- .csproj — enabled by default in .NET 10 -->
<PropertyGroup>
    <Nullable>enable</Nullable>
</PropertyGroup>
```

```csharp
// ✅ Non-nullable property — require initialization
public required string Name { get; init; }

// ✅ Nullable string — always check before use
public string? Description { get; init; }
if (!string.IsNullOrWhiteSpace(Description)) { ... }

// ❌ WRONG — unjustified null-forgiving
Product product = await _repository.GetByIdAsync(id)!;

// ✅ CORRECT — validate nullable result before use
Product? product = await _repository.GetByIdAsync(id);
if (product is null)
{
    return Result<ProductDto>.Failure(HttpStatusCode.NotFound);
}
```

---

## appsettings.json structure

```json
// appsettings.json — production defaults
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

```json
// appsettings.Development.json
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

**Rules:**
- Never commit secrets — use `appsettings.Development.json` (gitignored) or User Secrets
- Connection strings in `appsettings.json` always empty — filled by environment in production
- Never `Trace` in production
- Production config via environment variables: `ConnectionStrings__Default`, `ASPNETCORE_ENVIRONMENT`
