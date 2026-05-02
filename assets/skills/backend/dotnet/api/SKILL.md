---
name: dotnet-api
description: >
  ASP.NET Core API standards for .NET 10 projects. Covers controllers,
  middleware, routing, error handling, DI setup, logging, FluentValidation,
  nullable reference types, and appsettings structure.
  Load when creating or modifying controllers, middleware, Program.cs, or validators.
  DO NOT load for domain or repository tasks — load dotnet-ef-core or dotnet-ddd instead.
  For general C# conventions, dotnet-csharp must be loaded first.
requires: [dotnet-csharp]
produces: [controllers, middleware, validators, program-cs, appsettings, logging]
---

# ASP.NET Core API Standards — TemperAI

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **ONLY Controllers** — NEVER Minimal APIs. All endpoints must inherit `ControllerBase` with `[ApiController]`
2. **NEVER Swagger/Swashbuckle** — always Scalar for API documentation
3. **NEVER DataAnnotations on DTOs** — always FluentValidation
4. **NEVER hardcoded numbers in validators** — always reference `Entity.Rules` constants
5. **NEVER string interpolation in log messages** — always structured logging with named placeholders

> For general C# conventions (syntax, usings, naming, async, DTOs): `dotnet-csharp` must be loaded.
> For EF Core specifics (entities, repositories, DbContext): load `dotnet-ef-core`.

---

## When NOT to apply this skill

- You are working exclusively on domain entities, repositories, or use cases with no controller changes
- You are working on infrastructure configuration unrelated to the API layer

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
builder.Services.AddOpenApi();
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
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();
```

**Rules:**
- All DI setup through `AddApplication()` and `AddInfrastructure()` extension methods
- Never register services directly in `Program.cs` — always in extension methods
- Middleware order is non-negotiable: HTTPS → Auth → Controllers

---

## API documentation — Scalar

**Always use Scalar. Never use Swagger/Swashbuckle.**

```xml
<!-- .csproj -->
<PackageReference Include="Scalar.AspNetCore" Version="2.+" />
```

```csharp
// Program.cs
builder.Services.AddOpenApi();

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

- OpenAPI JSON: `/openapi/v1.json`
- Scalar UI: `/scalar/v1`
- Only expose in Development — never in production

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
// ✅ Non-nullable string — always initialize
public string Name { get; init; } = string.Empty;

// ✅ Nullable string — always check before use
public string? Description { get; init; }
if (!string.IsNullOrWhiteSpace(Description)) { ... }

// ❌ WRONG — unjustified null-forgiving
Product product = await _repository.GetByIdAsync(id)!;
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