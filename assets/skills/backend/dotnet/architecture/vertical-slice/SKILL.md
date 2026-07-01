---
name: vertical-slice-architecture
description: >
  Structure and rules for .NET projects using Vertical Slice Architecture.
  Use for CRUDs, MVPs, rapid prototypes, or simple systems where
  Clean Architecture would be overkill. Do not use for complex business
  domains — prefer Clean Architecture in that case.
  For data access implementation, load the required `backend/dotnet/orms/ef-core/*/SKILL.md` leaf skill(s) or your chosen data access skill.
---

# Vertical Slice Architecture — TemperAI Standards

> For data access implementation, load the required `backend/dotnet/orms/ef-core/*/SKILL.md` leaf skill(s) or your chosen data access skill.

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **ALWAYS keep a single project** for the backend application code in `src/`
2. **NEVER introduce repositories or UnitOfWork** for normal feature handlers
3. **ALWAYS use `DbContext` directly in handlers** for normal feature data access
4. **ALWAYS keep each feature self-contained** with endpoint/controller, handler, DTOs, and validator together
5. **NEVER let generic API or use-case guidance override Vertical Slice endpoint and handler structure**

## Project root folder naming — CRITICAL

**This is the ONLY correct structure for new projects:**

```
MyProjectApi/                    ← Folder root with "Api" suffix
├── MyProjectApi.sln            ← Solution file named after folder
├── src/
│   └── MyProjectApi/           ← Single project inside src/
│       ├── Domain/
│       ├── Persistence/
│       ├── Features/
│       └── Program.cs
└── tests/
```

**NEVER create this wrong structure:**

```
MyProjectApi/                    ← WRONG: extra folder inside
├── src/
│   └── MyProjectApi/           ← WRONG: duplicate folder
│       ├── Domain/
│       └── ...
```

**Rules:**
1. Root folder name: `[ProjectName]Api/` for API projects
2. Solution file: `[ProjectName]Api.sln` (same as folder name)
3. `src/` folder contains the single project directly — NO extra project folder inside src/
4. Project named with prefix: `MyProjectApi`

---

## When to use

- CRUDs without complex business logic
- MVPs and rapid prototypes
- Simple systems with limited scope
- Small teams or solo developers
- Systems where features are unlikely to change their data access patterns

## When NOT to use

- Complex business domain with changing rules → use Clean Architecture
- Enterprise systems with long lifespan → use Clean Architecture
- Multiple input channels for the same domain → use Hexagonal Architecture
- Need to swap data access technology without touching business logic → use Hexagonal Architecture

---

## API and Frontend separation — never in the same solution

If the project includes a Blazor Frontend, it **must be in a separate solution** from the API.

```
TodoManagerApi/                          ← Backend API solution
├── TodoManagerApi.sln
├── src/
│   └── TodoManagerApi/
│       ├── Domain/
│       ├── Persistence/
│       ├── Features/
│       └── Program.cs
└── tests/

TodoManagerFront/                        ← Blazor WASM frontend solution
├── TodoManagerFront.sln
├── src/
│   └── TodoManagerFront/
│       ├── Components/
│       ├── Services/
│       └── Program.cs
└── tests/
```

- **Never** put the API and Frontend in the same `.sln` file.
- **Always** create separate solutions — `TodoManagerApi.sln` and `TodoManagerFront.sln`.
- **Always** keep them as sibling directories — never nested.
- The Frontend communicates with the API **only via HTTP** — no shared projects, no project references.

---

## Mandatory folder structure

There is a **single project** — no layer separation. Features are organized vertically, and shared concerns live in dedicated root-level folders.

```
src/
├── YourProject.Api/
│   ├── Domain/
│   │   ├── Products/
│   │   │   ├── Product.cs
│   │   │   ├── Enums/
│   │   │   │   └── ProductStatus.cs
│   │   │   └── Events/
│   │   │       └── ProductCreatedEvent.cs
│   │   ├── Orders/
│   │   │   ├── Order.cs
│   │   │   ├── Enums/
│   │   │   │   └── OrderStatus.cs
│   │   │   └── Events/
│   │   │       └── OrderCreatedEvent.cs
│   │   └── Common/
│   │       └── Entity.cs
│   │
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   └── Configurations/
│   │       ├── ProductConfiguration.cs
│   │       └── OrderConfiguration.cs
│   │
│   ├── Shared/
│   │   └── Results/
│   │       ├── Result.cs
│   │       └── ResultExtensions.cs
│   │
│   ├── Features/
│   │   └── Products/
│   │       ├── CreateProduct/
│   │       │   ├── CreateProductEndpoint.cs
│   │       │   ├── CreateProductHandler.cs
│   │       │   ├── CreateProductRequestDto.cs
│   │       │   ├── CreateProductResponseDto.cs
│   │       │   └── CreateProductValidator.cs
│   │       ├── GetProductById/
│   │       │   ├── GetProductByIdEndpoint.cs
│   │       │   ├── GetProductByIdHandler.cs
│   │       │   └── GetProductByIdResponseDto.cs
│   │       ├── UpdateProduct/
│   │       │   ├── UpdateProductEndpoint.cs
│   │       │   ├── UpdateProductHandler.cs
│   │       │   ├── UpdateProductRequestDto.cs
│   │       │   ├── UpdateProductResponseDto.cs
│   │       │   └── UpdateProductValidator.cs
│   │       ├── ListProducts/
│   │       │   ├── ListProductsEndpoint.cs
│   │       │   ├── ListProductsHandler.cs
│   │       │   └── ListProductsResponseDto.cs
│   │       └── DeleteProduct/
│   │           ├── DeleteProductEndpoint.cs
│   │           └── DeleteProductHandler.cs
│   │
│   ├── Middlewares/
│   └── Program.cs
│
└── tests/
    └── YourProject.Tests/
        └── Features/
            └── Products/
                ├── CreateProductTests.cs
                └── UpdateProductTests.cs
```

---

## Key principles

- Each feature is a **vertical slice** from API endpoint to database — no horizontal layers
- No Repository pattern — `DbContext` is used directly in handlers
- No UnitOfWork — `DbContext.SaveChanges` is called directly in the handler
- No separate projects for Application, Domain, or Infrastructure — everything lives in one project
- `Domain/` contains entities, enums, and events — never inside `Shared/`
- `Persistence/` sits at root level — contains DbContext and EF Core configurations
- `Shared/` contains only cross-cutting concerns: Result pattern, base Entity class, extensions
- Each feature folder is **self-contained** — endpoint, handler, DTOs, validator
- Minimal abstraction — only add interfaces if there is a concrete need to swap implementations

---

## Domain

Each entity lives in its own folder under `Domain/` along with its related enums and events. This keeps everything related to an entity together and avoids a flat, unmanageable folder structure.

### Entity folder structure

```
Domain/
├── Products/
│   ├── Product.cs
│   ├── Enums/
│   │   └── ProductStatus.cs
│   └── Events/
│       └── ProductCreatedEvent.cs
├── Orders/
│   ├── Order.cs
│   ├── Enums/
│   │   └── OrderStatus.cs
│   └── Events/
│       └── OrderCreatedEvent.cs
└── Common/
    └── Entity.cs
```

### Entities

```csharp
// Domain/Products/Product.cs
public sealed class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public ProductStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### Entity rules in Vertical Slice

Entities in Vertical Slice are **simpler** than in Clean Architecture or Hexagonal:

- **When there are no complex invariants:** use simple properties with `set` — validation happens in the handler or validator.
- **When there are business rules:** use the same factory method pattern as Clean Architecture (see `backend/dotnet/ddd`).
- **Default approach:** start simple. Add factory methods and invariants only when the business logic requires it.

```csharp
// Simple entity — no complex invariants
public sealed class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

// Entity with invariants — same pattern as Clean Architecture
public sealed class Product
{
    public class Rules
    {
        public const int NAME_MAX_LENGTH = 100;
        public const decimal MIN_PRICE = 0;
    }

    public Guid Id { get; set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }

    private Product() { }

    public static (List<string> Errors, Product? Product) Create(
        string name, decimal price)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Name is required");
        }
        else if (name.Length > Rules.NAME_MAX_LENGTH)
        {
            errors.Add($"Name cannot exceed {Rules.NAME_MAX_LENGTH} characters");
        }

        if (price < Rules.MIN_PRICE)
        {
            errors.Add("Price must be zero or greater");
        }

        if (errors.Count > 0)
        {
            return (errors, null);
        }

        Product product = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price
        };

        return ([], product);
    }
}
```

### Enums

Each enum lives inside its entity's folder — never in a global Enums folder.

```csharp
// Domain/Products/Enums/ProductStatus.cs
public enum ProductStatus
{
    Active = 0,
    Inactive = 1,
    Discontinued = 2
}
```

### Events

Each domain event lives inside its entity's folder.

```csharp
// Domain/Products/Events/ProductCreatedEvent.cs
public sealed record ProductCreatedEvent
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;

    public ProductCreatedEvent(Guid productId, string productName)
    {
        ProductId = productId;
        ProductName = productName;
    }
}
```

---

## Persistence

### DbContext

```csharp
// Persistence/AppDbContext.cs
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> dbContextOptions)
        : base(dbContextOptions)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

### Entity configuration

```csharp
// Persistence/Configurations/ProductConfiguration.cs
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .HasMaxLength(Product.Rules.NAME_MAX_LENGTH)
            .HasColumnType("varchar");

        builder.Property(product => product.Description)
            .HasMaxLength(Product.Rules.DESCRIPTION_MAX_LENGTH)
            .HasColumnType("nvarchar");

        builder.Property(product => product.Price)
            .HasColumnType("decimal(18,2)");

        builder.Property(product => product.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnType("varchar");

        builder.HasIndex(product => product.Name)
            .IsUnique();
    }
}
```

---

## Feature slices

### Create slice — endpoint, handler, DTOs

```csharp
// Features/Products/CreateProduct/CreateProductRequestDto.cs
public sealed record CreateProductRequestDto
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public decimal Price { get; init; }
}

// Features/Products/CreateProduct/CreateProductResponseDto.cs
public sealed record CreateProductResponseDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public decimal Price { get; init; }
    public required string Status { get; init; }
}
```

#### Endpoint — Minimal API (preferred)

In Vertical Slice, each endpoint is a separate file using Minimal APIs. **Never use Controllers with one file per endpoint.**

```csharp
// Features/Products/CreateProduct/CreateProductEndpoint.cs
public static class CreateProductEndpoint
{
    public static RouteHandlerBuilder MapCreateProduct(this IEndpointRouteBuilder app)
    {
        return app.MapPost("/api/products", async (
            CreateProductRequestDto request,
            CreateProductHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<CreateProductResponseDto> result =
                await handler.HandleAsync(request, cancellationToken);

            return result.ToActionResult();
        });
    }
}
```

#### Alternative — Single Controller per feature

If the existing project already uses Controllers, **group ALL endpoints for a feature into ONE Controller file**. Never create one Controller per endpoint.

```csharp
// Features/Products/ProductsController.cs
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequestDto request,
        [FromServices] CreateProductHandler handler,
        CancellationToken cancellationToken)
    {
        Result<CreateProductResponseDto> result =
            await handler.HandleAsync(request, cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        [FromServices] GetProductByIdHandler handler,
        CancellationToken cancellationToken)
    {
        Result<GetProductByIdResponseDto> result =
            await handler.HandleAsync(id, cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateProductRequestDto request,
        [FromServices] UpdateProductHandler handler,
        CancellationToken cancellationToken)
    {
        Result<UpdateProductResponseDto> result =
            await handler.HandleAsync(id, request, cancellationToken);

        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        [FromServices] DeleteProductHandler handler,
        CancellationToken cancellationToken)
    {
        Result<object> result =
            await handler.HandleAsync(id, cancellationToken);

        return result.ToActionResult();
    }
}
```

#### Handler

The handler contains all the business logic and data access for this feature. It uses `DbContext` directly — no repository, no UnitOfWork.

```csharp
// Features/Products/CreateProduct/CreateProductHandler.cs
public sealed class CreateProductHandler
{
    private readonly AppDbContext _dbContext;

    public CreateProductHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CreateProductResponseDto>> HandleAsync(
        CreateProductRequestDto request,
        CancellationToken cancellationToken = default)
    {
        bool exists = await _dbContext.Products
            .AnyAsync(p => p.Name == request.Name, cancellationToken);

        if (exists)
        {
            return Result<CreateProductResponseDto>
                .Failure(HttpStatusCode.Conflict)
                .WithDescription("A product with that name already exists");
        }

        Product product = new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreateProductResponseDto>
            .Success(HttpStatusCode.Created)
            .WithPayload(new CreateProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Status = product.Status.ToString()
            });
    }
}
```

### Read slice — query only

```csharp
// Features/Products/GetProductById/GetProductByIdResponseDto.cs
public sealed record GetProductByIdResponseDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public decimal Price { get; init; }
    public required string Status { get; init; }
}

// Features/Products/GetProductById/GetProductByIdHandler.cs
public sealed class GetProductByIdHandler
{
    private readonly AppDbContext _dbContext;

    public GetProductByIdHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetProductByIdResponseDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        Product? product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            return Result<GetProductByIdResponseDto>
                .Failure(HttpStatusCode.NotFound)
                .WithDescription("Product not found");
        }

        return Result<GetProductByIdResponseDto>
            .Success(HttpStatusCode.OK)
            .WithPayload(new GetProductByIdResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Status = product.Status.ToString()
            });
    }
}
```

### Update slice

```csharp
// Features/Products/UpdateProduct/UpdateProductHandler.cs
public sealed class UpdateProductHandler
{
    private readonly AppDbContext _dbContext;

    public UpdateProductHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<UpdateProductResponseDto>> HandleAsync(
        Guid id,
        UpdateProductRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Product? product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            return Result<UpdateProductResponseDto>
                .Failure(HttpStatusCode.NotFound)
                .WithDescription("Product not found");
        }

        product.Name = request.Name;
        product.Price = request.Price;
        product.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateProductResponseDto>
            .Success(HttpStatusCode.OK)
            .WithPayload(new UpdateProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Status = product.Status.ToString()
            });
    }
}
```

---

## Validation

Use FluentValidation per feature:

```csharp
// Features/Products/CreateProduct/CreateProductValidator.cs
public sealed class CreateProductValidator : AbstractValidator<CreateProductRequestDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Product.Rules.NAME_MAX_LENGTH);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(Product.Rules.MIN_PRICE);
    }
}
```

---

## DI setup

```csharp
// Program.cs
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});

// Register handlers
builder.Services.AddScoped<CreateProductHandler>();
builder.Services.AddScoped<GetProductByIdHandler>();
builder.Services.AddScoped<UpdateProductHandler>();
builder.Services.AddScoped<DeleteProductHandler>();
builder.Services.AddScoped<ListProductsHandler>();

// Register validators
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

WebApplication app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## Code examples — never include namespace

Code examples in skills show ONLY the class/interface code. Never include `namespace X.Y.Z;` — the folder structure shown in the skill defines the namespace implicitly. This prevents the model from making assumptions about where code should be placed.

When generating actual code, the namespace MUST match the folder structure exactly. For example, a Product entity in `Domain/Products/` must have `namespace YourProject.Domain.Products;`.

---

## Rules specific to Vertical Slice Architecture

For general C# conventions (syntax, usings, naming, async, DTOs), see `backend/dotnet/csharp/SKILL.md`.

- **Single project** — no layer separation into Api/Application/Domain/Infrastructure
- **No Repository pattern** — `DbContext` is used directly in handlers
- **No UnitOfWork** — `SaveChangesAsync` is called directly in the handler
- **No interfaces for handlers** — handlers are concrete classes, injected directly
- `Domain/` at root level — each entity in its own **plural** folder (`Products/`, `Orders/`) with its enums and events
- `Persistence/` at root level contains DbContext and EF Core configurations
- `Shared/` only contains cross-cutting concerns: Result, base Entity, extensions
- Each feature folder contains **everything** it needs: endpoint, handler, DTOs, validator
- **No `UseCase` suffix** — feature names describe the action: `CreateProduct`, `GetProductById`
- Entities are simple by default — add factory methods and invariants only when business logic requires it
- EF Core configurations in `Persistence/Configurations/`
- **Never use one Controller per endpoint** — either use Minimal APIs (one file per endpoint) OR one Controller per feature (all endpoints grouped). Never create `CreateProductEndpoint : ControllerBase`, `UpdateProductEndpoint : ControllerBase`, etc.
- **Never use hardcoded numbers in validators** — always reference `Entity.Rules` constants
- For data access implementation details, load only the EF Core leaf skill(s) the task actually touches
