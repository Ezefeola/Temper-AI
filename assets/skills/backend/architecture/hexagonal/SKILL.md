---
name: hexagonal-architecture
description: >
  Structure and rules for .NET projects using Hexagonal Architecture (Ports & Adapters).
  Use when the project has multiple input channels (API, CLI, message queue),
  need to test the domain in isolation, or when adapters change frequently.
  Do not use for simple CRUDs without logic — prefer Vertical Slice in that case.
  For implementation details, load the required `backend/dotnet/ef-core/*/SKILL.md` leaf skill(s) or your chosen data access skill.
---

# Hexagonal Architecture — TemperAI Standards

> For data access implementation, load the required `backend/dotnet/ef-core/*/SKILL.md` leaf skill(s) or your chosen data access skill.

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **ALWAYS keep `Core` isolated from adapters**
2. **NEVER let one adapter depend on another adapter**
3. **ALWAYS express persistence and external capabilities through ports**
4. **ALWAYS keep use cases in `Core` depending on abstractions only**
5. **NEVER let generic shared guidance override Hexagonal port-and-adapter boundaries**

## Project root folder naming — CRITICAL

**This is the ONLY correct structure for new projects:**

```
MyProjectApi/                    ← Folder root with "Api" suffix
├── MyProjectApi.sln            ← Solution file named after folder
├── src/
│   ├── Core/
│   ├── Adapter.WebApi/
│   └── Adapter.SqlServer/
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
3. `src/` folder contains all adapter projects directly — NO extra project folder inside src/
4. Each adapter named with suffix: `Adapter.WebApi`, `Adapter.SqlServer`, etc.

---

## When to use

- Multiple input channels (REST API, gRPC, CLI, message queue)
- Need to test the domain logic completely isolated from infrastructure
- Adapters (database, external APIs) change frequently
- Want to swap infrastructure without touching business logic
- Long-lived systems where the core business rules are stable but the I/O layer evolves

## When NOT to use

- Simple CRUDs without business logic → use Vertical Slice
- Single input channel with stable infrastructure → Clean Architecture is simpler
- Prototypes or MVPs with limited time → use Vertical Slice

---

## API and Frontend separation — never in the same solution

If the project includes a Blazor Frontend, it **must be in a separate solution** from the API.

```
TodoManagerApi/                          ← Backend API solution
├── TodoManagerApi.sln
├── src/
│   ├── Core/
│   ├── Adapter.WebApi/
│   └── Adapter.SqlServer/
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

## Mandatory project structure

Each layer is a **separate project** (`.csproj`). Every adapter is independent and only depends on `Core`.

```
src/
├── Core/                           ← The hexagon — domain, ports, use cases
│   ├── Domain/
│   │   ├── Entities/
│   │   │   └── Product/
│   │   │       ├── Product.cs
│   │   │       ├── Enums/
│   │   │       │   └── ProductStatus.cs
│   │   │       └── Events/
│   │   │           └── ProductCreatedEvent.cs
│   │   └── Common/
│   │       └── Primitives/
│   │           ├── Entity.cs
│   │           └── IDomainEvent.cs
│   │   └── Errors/
│   │
│   ├── Contracts/
│   │   ├── Persistence/
│   │   │   ├── Repositories/
│   │   │   │   ├── IProductRepository.cs
│   │   │   │   └── IOrderRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   ├── Ports/
│   │   │   ├── Input/
│   │   │   │   ├── ICreateProduct.cs
│   │   │   │   ├── IUpdateProduct.cs
│   │   │   │   └── IGetProductById.cs
│   │   │   └── Output/
│   │   │       └── IEventPublisher.cs
│   │   └── Dtos/
│   │       ├── CreateProductRequestDto.cs
│   │       ├── CreateProductResponseDto.cs
│   │       ├── UpdateProductRequestDto.cs
│   │       └── UpdateProductResponseDto.cs
│   │
│   ├── UseCases/
│   │   └── Products/
│   │       ├── ProductMappingExtensions.cs
│   │       ├── CreateProduct.cs
│   │       └── UpdateProduct.cs
│   │
│   ├── Common/
│   │   └── Results/
│   │       ├── Result.cs
│   │       └── ResultExtensions.cs
│   │
│   └── DependencyInjection.cs
│
├── Adapter.WebApi/                 ← Driving adapter — REST API
│   ├── Controllers/
│   │   └── ProductsController.cs
│   ├── Middlewares/
│   ├── Extensions/
│   │   └── ResultExtensions.cs
│   ├── appsettings.json
│   ├── Program.cs
│   └── DependencyInjection.cs
│
├── Adapter.SqlServer/              ← Driven adapter — SQL Server persistence
│   ├── Persistence/
│   │   ├── Configurations/
│   │   │   └── ProductConfiguration.cs
│   │   ├── Migrations/
│   │   ├── Repositories/
│   │   │   └── ProductRepository.cs
│   │   ├── UnitOfWork.cs
│   │   └── AppDbContext.cs
│   └── DependencyInjection.cs
│
├── Adapter.RabbitMQ/               ← Driven adapter — message broker
│   ├── Services/
│   │   └── EventPublisher.cs
│   └── DependencyInjection.cs
│
└── Adapter.Cli/                    ← Driving adapter — CLI interface (optional)
    ├── Commands/
    │   └── CreateProductCommand.cs
    └── DependencyInjection.cs
```

---

## Dependency rules — never broken

- `Core` depends on nothing — zero external NuGet packages (except language/framework essentials)
- Every `Adapter.*` depends only on `Core`
- Adapters **never** depend on other adapters — `Adapter.SqlServer` does not know about `Adapter.RabbitMQ`
- `Adapter.WebApi` calls Input Ports from `Core` — it does not call repositories directly
- No circular references between any projects

---

## The Hexagonal concept

Hexagonal Architecture organizes the system around a central **Core** (the hexagon), surrounded by independent **Adapters** that communicate through **Ports**.

### Core — the hexagon

Contains everything that matters to the business: domain entities, business rules, use cases, and the ports (interfaces) that define how the outside world interacts with the core.

The Core is completely isolated. It has no idea what database, message broker, or HTTP framework is being used.

### Output Ports — what the Core needs

Defined in `Core/Contracts/Ports/Output/` (for external services) and `Core/Contracts/Persistence/` (for persistence).

- `IProductRepository` — in `Core/Contracts/Persistence/Repositories/`
- `IUnitOfWork` — in `Core/Contracts/Persistence/`
- `IEventPublisher` — in `Core/Contracts/Ports/Output/`

### Input Ports — what the Core offers

Defined in `Core/Contracts/Ports/Input/`. These are the interfaces that driving adapters call.

- `ICreateProduct` — create a product
- `IUpdateProduct` — update a product
- `IGetProductById` — retrieve a product

### Driving Adapters — initiate interactions

Located in `Adapter.*` projects. They call Input Ports to make things happen.

- `Adapter.WebApi` — REST controllers
- `Adapter.Cli` — CLI command handlers
- `Adapter.Grpc` — gRPC service implementations
- `Adapter.MessageConsumer` — message queue consumers

### Driven Adapters — respond to requests

Located in `Adapter.*` projects. They implement Output Ports.

- `Adapter.SqlServer` — implements `IProductRepository`, `IUnitOfWork`
- `Adapter.PostgreSql` — alternative persistence adapter
- `Adapter.RabbitMQ` — implements `IEventPublisher`
- `Adapter.SendGrid` — implements `IEmailSender`

---

## Domain — contracts and patterns

### Base primitives

The domain defines these base types. They are pure C# — no external dependencies.

```csharp
// Entity.cs — clean base, no event logic
public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default;
}

// IDomainEvent.cs — pure contract, no behavior
public interface IDomainEvent { }
```

### Entity pattern

Entities follow a strict pattern:

- `sealed class` with `private` constructor.
- Nested `Rules` class with constraint constants.
- Factory method returning `(List<string> Errors, Entity? Entity)`.
- Update methods returning `(List<string> Errors, bool Updated)`.
- `UpdatedAt` set explicitly on every update method.
- Update methods validate invariants AND check if the value actually changed.
- Never `throw` for business validations.

See `backend/dotnet/ddd` for the complete entity implementation pattern.

### Domain Event pattern

- `sealed record` with data only — no behavior.
- Never registered on the entity or dispatched automatically in SaveChanges.
- Published explicitly in the UseCase after the persistence operation completes.

See `backend/dotnet/ddd` for the complete Domain Event implementation pattern.

---

## Core — ports, use cases, DTOs

### Output Ports — what the Core needs

Defined in `Core/Contracts/Persistence/`. Repository interfaces in `Core/Contracts/Persistence/Repositories/`, UnitOfWork at root level. **All repository interfaces MUST inherit from `IGenericRepository<TEntity>`**.

```csharp
// IProductRepository.cs — in Core/Contracts/Persistence/Repositories/
public interface IProductRepository : IGenericRepository<Product>
{
    // With tracking — for modification operations
    Task<Product?> GetByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    // Without tracking — for read-only queries
    Task<Product?> GetByIdAsNoTrackingAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string productName,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default);
}

// IUnitOfWork.cs — in Core/Contracts/Persistence/
public interface IUnitOfWork : IDisposable
{
    IProductRepository ProductRepository { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task<SaveResult> CompleteAsync(CancellationToken cancellationToken = default);
}

// SaveResult.cs
public sealed class SaveResult
{
    public bool IsSuccess { get; init; }
    public int RowsAffected { get; init; }
    public required string ErrorMessage { get; init; }
}
```

### Generic Repository

For base repository operations, use `IGenericRepository` and `GenericRepository` in `Core/Contracts/Persistence/Repositories/`:

```csharp
// IGenericRepository.cs — in Core/Contracts/Persistence/Repositories/
public interface IGenericRepository<TEntity> where TEntity : class, IEntity
{
    IQueryable<TEntity> Query();

    Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default);

    void Add(TEntity entity);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
}

// GenericRepository.cs — in Core/Contracts/Persistence/Repositories/
public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class, IEntity
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public GenericRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public IQueryable<TEntity> Query()
    {
        return _dbSet.AsQueryable();
    }

    public async Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);
    }

    public void Add(TEntity entity)
    {
        _dbSet.Add(entity);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }
}
```

### External service output ports

```csharp
// IEventPublisher.cs — in Core/Contracts/Ports/Output/
// Implemented by a driven adapter (e.g., Adapter.RabbitMQ)
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        TEvent domainEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}
```

### Input Ports — what the Core offers

Defined in `Core/Contracts/Ports/Input/`.

```csharp
// ICreateProduct.cs
public interface ICreateProduct
{
    Task<Result<CreateProductResponseDto>> ExecuteAsync(
        CreateProductRequestDto createProductRequestDto,
        CancellationToken cancellationToken = default);
}

// IUpdateProduct.cs
public interface IUpdateProduct
{
    Task<Result<UpdateProductResponseDto>> ExecuteAsync(
        UpdateProductRequestDto updateProductRequestDto,
        CancellationToken cancellationToken = default);
}
```

### DTOs

Defined in `Core/Contracts/Dtos/`.

```csharp
public sealed record CreateProductRequestDto
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public decimal Price { get; init; }
}

public sealed record CreateProductResponseDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public decimal Price { get; init; }
    public required string Status { get; init; }
}
```

### Use cases — implement Input Ports

Use cases are `sealed class` in `Core/UseCases/` that implement Input Ports. They depend on Output Ports, never on concrete adapters.

- Explicit constructor injection — never primary constructor.
- Domain events published explicitly after `CompleteAsync`.
- Result pattern with `HttpStatusCode`.

```csharp
// CreateProduct.cs
public sealed class CreateProduct : ICreateProduct
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public CreateProduct(IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<CreateProductResponseDto>> ExecuteAsync(
        CreateProductRequestDto createProductRequestDto,
        CancellationToken cancellationToken = default)
    {
        bool productExists = await _unitOfWork.ProductRepository
            .ExistsByNameAsync(createProductRequestDto.Name, cancellationToken);

        if (productExists)
        {
            return Result<CreateProductResponseDto>
                .Failure(HttpStatusCode.Conflict)
                .WithDescription("A product with that name already exists");
        }

        (List<string> productErrors, Product? product) = Product.Create(
            createProductRequestDto.Name,
            createProductRequestDto.Description,
            createProductRequestDto.Price);

        if (product is null)
        {
            return Result<CreateProductResponseDto>
                .Failure(HttpStatusCode.BadRequest)
                .WithErrors(productErrors);
        }

        await _unitOfWork.ProductRepository.AddAsync(product, cancellationToken);

        SaveResult saveResult = await _unitOfWork.CompleteAsync(cancellationToken);

        if (!saveResult.IsSuccess)
        {
            return Result<CreateProductResponseDto>
                .Failure(HttpStatusCode.InternalServerError)
                .WithDescription(saveResult.ErrorMessage);
        }

        // Explicit event publication
        ProductCreatedEvent productCreatedEvent = new(
            product.Id,
            product.Name,
            product.Price);

        await _eventPublisher.PublishAsync(productCreatedEvent, cancellationToken);

        return Result<CreateProductResponseDto>
            .Success(HttpStatusCode.Created)
            .WithPayload(product.ToCreateProductResponseDto());
    }
}
```

### Core DI setup

```csharp
// Core/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
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

---

## Adapters — driving and driven

### Driving adapter — WebApi

```csharp
// Adapter.WebApi/Controllers/ProductsController.cs
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequestDto createProductRequestDto,
        [FromServices] ICreateProduct createProduct,
        CancellationToken cancellationToken)
    {
        Result<CreateProductResponseDto> result =
            await createProduct.ExecuteAsync(createProductRequestDto, cancellationToken);

        return result.ToActionResult();
    }
}
```

### Driven adapter — persistence

```csharp
// Adapter.SqlServer/Persistence/Repositories/ProductRepository.cs
// Implements IProductRepository (an Output Port defined in Core)
// ALL repositories MUST inherit from GenericRepository
public sealed class ProductRepository : GenericRepository<Product>, IProductRepository
{
    private readonly AppDbContext _appDbContext;

    public ProductRepository(AppDbContext appDbContext)
        : base(appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Product?> GetByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Products
            .FirstOrDefaultAsync(
                product => product.Id == productId,
                cancellationToken);
    }

    public async Task<Product?> GetByIdAsNoTrackingAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                product => product.Id == productId,
                cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string productName,
        CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Products
            .AsNoTracking()
            .AnyAsync(
                product => product.Name == productName,
                cancellationToken);
    }

    public async Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        await _appDbContext.Products.AddAsync(product, cancellationToken);
    }
}
```

### Driven adapter — event publishing

```csharp
// Adapter.RabbitMQ/Services/EventPublisher.cs
// Implements IEventPublisher (an Output Port defined in Core)
public sealed class EventPublisher : IEventPublisher
{
    private readonly IConnection _connection;

    public EventPublisher(IConnection connection)
    {
        _connection = connection;
    }

    public async Task PublishAsync<TEvent>(
        TEvent domainEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        // RabbitMQ publish logic
    }
}
```

### Adapter DI setup

Each adapter registers its own implementations:

```csharp
// Adapter.SqlServer/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddSqlServerAdapter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("Default"));
        });

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}

// Adapter.RabbitMQ/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddRabbitMqAdapter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IConnection>(_ =>
        {
            ConnectionFactory factory = new()
            {
                HostName = configuration["RabbitMQ:Host"]
            };
            return factory.CreateConnection();
        });

        services.AddScoped<IEventPublisher, EventPublisher>();

        return services;
    }
}
```

### Program.cs — wiring everything together

```csharp
// Adapter.WebApi/Program.cs
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Core — use cases and domain
builder.Services.AddCore();

// Driven adapters — infrastructure
builder.Services.AddSqlServerAdapter(builder.Configuration);
builder.Services.AddRabbitMqAdapter(builder.Configuration);

WebApplication app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## Code examples — never include namespace

Code examples in skills show ONLY the class/interface code. Never include `namespace X.Y.Z;` — the folder structure shown in the skill defines the namespace implicitly. This prevents the model from making assumptions about where code should be placed.

When generating actual code, the namespace MUST match the folder structure exactly. For example, a Product entity in `Core/Domain/Entities/Products/` must have `namespace YourProject.Core.Domain.Entities.Products;`.

---

## Rules specific to Hexagonal Architecture

- `Core` has zero external dependencies — pure C# only
- Every adapter is a **separate project** — `Adapter.SqlServer`, `Adapter.RabbitMQ`, `Adapter.WebApi`
- Adapters **never** depend on other adapters — only on `Core`
- Persistence contracts (repositories, UnitOfWork) in `Core/Contracts/Persistence/`
- Output Ports in `Core/Contracts/Ports/Output/` — what the Core needs from the outside (external services)
- Input Ports in `Core/Contracts/Ports/Input/` — what the Core offers to the outside
- Driving adapters call Input Ports — they never call Output Ports directly
- Driven adapters implement Output Ports — they are called by the Core through the ports
- Each driven adapter is completely isolated — you can swap `Adapter.SqlServer` for `Adapter.PostgreSql` without touching anything else
- Entities always `sealed class` with `private` constructor
- Factory method always returns `(List<string> Errors, Entity? Entity)`
- Update methods always validate invariants, check if value changed, set `UpdatedAt`
- Domain Events are contracts only — `sealed record` with data, no behavior
- Event publication always explicit in the UseCase — never automatic in SaveChanges
- `UnitOfWork` is the single entry point to all repositories within a persistence adapter
- **All repository interfaces MUST inherit from `IGenericRepository<TEntity>`**
- **All repository implementations MUST inherit from `GenericRepository<TEntity>`**
- For bulk insert operations (1000+ rows), use `BulkInsertOperations` from `backend/dotnet/ef-core/bulk-operations/SKILL.md`
- For data access implementation details, load only the EF Core leaf skill(s) the task actually touches
