---
name: onion-architecture
description: >
  Structure and rules for .NET projects using Onion Architecture.
  Use when the project has a strong domain-centric focus with DDD,
  aggregate roots, and the Specification pattern. All dependencies
  point inward toward the domain core.
  Do not use for simple CRUDs without logic — prefer Vertical Slice in that case.
  For implementation details, load the required `backend/dotnet/orms/ef-core/*/SKILL.md` leaf skill(s) or your chosen data access skill.
---

# Onion Architecture — TemperAI Standards

> For data access implementation, load the required `backend/dotnet/orms/ef-core/*/SKILL.md` leaf skill(s) or your chosen data access skill.

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **ALWAYS keep dependencies pointing inward toward `Domain`**
2. **ALWAYS define persistence contracts in `Domain`** for this architecture
3. **NEVER let `Application` or `Api` bypass Domain-owned rules**
4. **ALWAYS keep aggregates and specifications as Domain concerns**
5. **NEVER let generic shared guidance override Onion circle boundaries**

## Project root folder naming — CRITICAL

**This is the ONLY correct structure for new projects:**

```
MyProjectApi/                    ← Folder root with "Api" suffix
├── MyProjectApi.sln            ← Solution file named after folder
├── src/
│   ├── MyProjectApi.Domain/
│   ├── MyProjectApi.Application/
│   ├── MyProjectApi.Infrastructure/
│   └── MyProjectApi.Api/
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
3. `src/` folder contains all projects directly — NO extra project folder inside src/
4. Each project named with prefix: `MyProjectApi.Domain`, `MyProjectApi.Application`, etc.

---

## When to use

- Domain-driven projects where the domain model is the absolute center
- Systems where business rules are the most stable part of the application
- Teams practicing DDD with strong domain modeling and aggregate design
- Projects that benefit from the Specification pattern for complex queries

## When NOT to use

- Simple CRUDs without business logic → use Vertical Slice
- Prototypes or MVPs with limited time → use Vertical Slice
- When Clean Architecture is already understood by the team — they are very similar

---

## API and Frontend separation — never in the same solution

If the project includes a Blazor Frontend, it **must be in a separate solution** from the API.

```
TodoManagerApi/                          ← Backend API solution
├── TodoManagerApi.sln
├── src/
│   ├── TodoManagerApi.Domain/
│   ├── TodoManagerApi.Application/
│   └── TodoManagerApi.Infrastructure/
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

Onion Architecture organizes the system in concentric circles. Each inner layer defines contracts that outer layers implement.

```
src/
├── YourProject.Api/
│   ├── Controllers/
│   │   └── ProductsController.cs
│   ├── Middlewares/
│   ├── Extensions/
│   │   └── ResultExtensions.cs
│   └── Program.cs
│
├── YourProject.Application/
│   ├── Services/
│   │   └── IEmailService.cs
│   ├── UseCases/
│   │   └── Products/
│   │       ├── ProductMappingExtensions.cs
│   │       ├── CreateProduct/
│   │       │   ├── ICreateProduct.cs
│   │       │   ├── CreateProduct.cs
│   │       │   ├── CreateProductRequestDto.cs
│   │       │   └── CreateProductResponseDto.cs
│   │       └── UpdateProduct/
│   │           ├── IUpdateProduct.cs
│   │           ├── UpdateProduct.cs
│   │           ├── UpdateProductRequestDto.cs
│   │           └── UpdateProductResponseDto.cs
│   ├── Common/
│   │   └── Results/
│   │       ├── Result.cs
│   │       └── ResultExtensions.cs
│   └── DependencyInjection.cs
│
├── YourProject.Domain/
│   ├── Entities/
│   │   └── Products/
│   │       ├── Product.cs
│   │       ├── Enums/
│   │       │   └── ProductStatus.cs
│   │       └── Events/
│   │           └── ProductCreatedEvent.cs
│   ├── Orders/
│   │   ├── Order.cs
│   │   ├── Enums/
│   │   │   └── OrderStatus.cs
│   │   └── Events/
│   │       └── OrderCreatedEvent.cs
│   │
│   ├── Aggregates/
│   │   └── OrderAggregate/
│   │       ├── OrderAggregateRoot.cs
│   │       └── OrderItem.cs
│   │
│   ├── Specifications/
│   │   ├── ISpecification.cs
│   │   ├── ActiveProductsSpecification.cs
│   │   └── ProductsByPriceRangeSpecification.cs
│   │
│   ├── Contracts/
│   │   ├── Persistence/
│   │   │   ├── Repositories/
│   │   │   │   ├── IProductRepository.cs
│   │   │   │   └── IOrderRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │
│   ├── Common/
│   │   └── Primitives/
│   │       ├── Entity.cs
│   │       └── IDomainEvent.cs
│   │
│   └── Errors/
│
└── YourProject.Infrastructure/
    ├── Persistence/
    │   ├── Configurations/
    │   │   ├── ProductConfiguration.cs
    │   │   └── OrderConfiguration.cs
    │   ├── Migrations/
    │   ├── Repositories/
    │   │   ├── ProductRepository.cs
    │   │   └── OrderRepository.cs
    │   ├── UnitOfWork.cs
    │   └── AppDbContext.cs
    ├── Services/
    │   └── EmailService.cs
    └── DependencyInjection.cs
```

---

## The Onion concept — concentric circles

Onion Architecture organizes the system in layers that all depend **inward** toward the center. The center is the most stable and important part of the system.

### Circle 1 — Domain Model (center)

Entities, Enums, Domain Events, Aggregates, Specifications. Pure business logic with zero external dependencies.

### Circle 2 — Domain Services and Contracts

Repository interfaces, UnitOfWork interface, Specifications, Domain Services. These are contracts that outer circles must implement.

**Key difference from Clean Architecture:** Repository interfaces live in `Domain/Contracts/Persistence/Repositories/`, not in Application. The domain defines what it needs — infrastructure provides it.

### Circle 3 — Application Services

Use cases, DTOs, Application Services. This layer orchestrates the domain to accomplish tasks.

### Circle 4 — Infrastructure and UI (outermost)

Database implementations, external services, controllers, middlewares. This layer depends on everything inside — nothing inside depends on it.

---

## Dependency rules — never broken

- All layers depend inward — toward `Domain`
- `Domain` depends on nothing — zero external NuGet packages
- `Application` depends only on `Domain`
- `Infrastructure` depends on `Application` and `Domain`
- `Api` depends on `Application` — never on `Infrastructure` directly
- No circular references between layers

---

## Domain — contracts and patterns

### Base primitives

The domain defines these base types. They are pure C# — no external dependencies.

```csharp
// Domain/Common/Primitives/Entity.cs
public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default;
}

// Domain/Common/Primitives/IDomainEvent.cs
public interface IDomainEvent { }
```

### Entity pattern

Each entity lives in its own folder under `Domain/Entities/` along with its related enums and events.

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

### Aggregate pattern

Aggregates manage consistency boundaries. The aggregate root is the only entry point.

```csharp
// Domain/Aggregates/OrderAggregate/OrderAggregateRoot.cs
public sealed class OrderAggregateRoot : Entity<Guid>
{
    private readonly List<OrderItem> _items = [];

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private OrderAggregateRoot() { }

    public static (List<string> Errors, OrderAggregateRoot? Order) Create()
    {
        OrderAggregateRoot order = new()
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        return ([], order);
    }

    public (List<string> Errors, bool Added) AddItem(Product product, int quantity)
    {
        List<string> errors = [];

        if (quantity <= 0)
        {
            errors.Add("Quantity must be greater than zero");
        }

        if (errors.Count > 0)
        {
            return (errors, false);
        }

        OrderItem item = new(Id, product.Id, quantity, product.Price);
        _items.Add(item);

        return ([], true);
    }
}
```

### Specification pattern

Specifications encapsulate query logic in the domain. They are evaluated by the infrastructure layer.

```csharp
// Domain/Specifications/ISpecification.cs
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
}

// Domain/Specifications/ActiveProductsSpecification.cs
public sealed class ActiveProductsSpecification : ISpecification<Product>
{
    public Expression<Func<Product, bool>> Criteria
    {
        get
        {
            return product => product.Status == ProductStatus.Active;
        }
    }

    public List<Expression<Func<Product, object>>> Includes
    {
        get
        {
            return [];
        }
    }

    public List<string> IncludeStrings
    {
        get
        {
            return [];
        }
    }
}
```

### Repository contracts — in Domain, not Application

**Key difference from Clean Architecture:** Repository interfaces are defined in `Domain/Contracts/Persistence/Repositories/`. The domain defines what it needs — infrastructure implements it. **All repository interfaces MUST inherit from `IGenericRepository<TEntity>`**.

```csharp
// Domain/Contracts/Persistence/Repositories/IProductRepository.cs — MUST inherit from IGenericRepository
public interface IProductRepository : IGenericRepository<Product>
{
    Task<Product?> GetByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsNoTrackingAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> ListAsync(
        ISpecification<Product> specification,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string productName,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default);
}

// Domain/Contracts/Persistence/IUnitOfWork.cs
public interface IUnitOfWork : IDisposable
{
    IProductRepository ProductRepository { get; }
    IOrderRepository OrderRepository { get; }

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

For base repository operations, use `IGenericRepository` and `GenericRepository` in `Domain/Contracts/Persistence/Repositories/`:

```csharp
// IGenericRepository.cs — in Domain/Contracts/Persistence/Repositories/
public interface IGenericRepository<TEntity> where TEntity : class, IEntity
{
    IQueryable<TEntity> Query();

    Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default);

    void Add(TEntity entity);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
}

// GenericRepository.cs — in Domain/Contracts/Persistence/Repositories/
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

---

## Application — use cases and DTOs

### Use case pattern

Use cases are `sealed class` without `UseCase` suffix. They depend on abstractions from the Domain layer (`IUnitOfWork`, repository interfaces), never on concrete implementations.

- Interface in the same folder — `ICreateProduct`, `IUpdateProduct`.
- Explicit constructor injection — never primary constructor.
- Domain events published explicitly after `CompleteAsync`.
- Result pattern with `HttpStatusCode`.

```csharp
// ICreateProduct.cs
public interface ICreateProduct
{
    Task<Result<CreateProductResponseDto>> ExecuteAsync(
        CreateProductRequestDto createProductRequestDto,
        CancellationToken cancellationToken = default);
}

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

### DI structure

```csharp
// Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddUseCases();
        return services;
    }

    private static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddProductUseCases();
        return services;
    }

    private static IServiceCollection AddProductUseCases(this IServiceCollection services)
    {
        services.AddScoped<ICreateProduct, CreateProduct>();
        services.AddScoped<IUpdateProduct, UpdateProduct>();
        return services;
    }
}
```

---

## Infrastructure — implementations

### What lives here

- **Repository implementations** — fulfill contracts defined in `Domain/Contracts/Persistence/Repositories/`
- **UnitOfWork implementation** — fulfills `IUnitOfWork` from `Domain/Contracts/Persistence/`
- **EF Core** — DbContext, configurations, migrations
- **External service implementations** — email, messaging, third-party APIs

### Repository implementation

```csharp
// Infrastructure/Persistence/Repositories/ProductRepository.cs
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

    public async Task<IReadOnlyList<Product>> ListAsync(
        ISpecification<Product> specification,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = _appDbContext.Products.AsNoTracking();

        query = query.Where(specification.Criteria);

        foreach (Expression<Func<Product, object>> include in specification.Includes)
        {
            query = query.Include(include);
        }

        foreach (string includeString in specification.IncludeStrings)
        {
            query = query.Include(includeString);
        }

        return await query.ToListAsync(cancellationToken);
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

### DI structure

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
        // Configure database — see the matching backend/dotnet/orms/ef-core/* leaf skill for EF Core implementation
        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        return services;
    }

    private static IServiceCollection AddUnitOfWork(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
```

---

## Code examples — never include namespace

Code examples in skills show ONLY the class/interface code. Never include `namespace X.Y.Z;` — the folder structure shown in the skill defines the namespace implicitly. This prevents the model from making assumptions about where code should be placed.

When generating actual code, the namespace MUST match the folder structure exactly. For example, a Product entity in `Domain/Entities/Products/` must have `namespace YourProject.Domain.Entities.Products;`.

---

## Rules specific to Onion Architecture

- `Domain` zero external dependencies — pure C# only
- **Repository interfaces live in `Domain/Contracts/Persistence/Repositories/`** — not in Application. The domain defines what it needs.
- **UnitOfWork interface lives in `Domain/Contracts/`** — not in Application.
- **Specifications live in `Domain/Specifications/`** — they encapsulate query logic in the domain layer.
- **Aggregate roots** manage consistency within their boundary — no entity outside an aggregate references it directly.
- **Repository per aggregate root**, not per entity — the aggregate root is the only entry point.
- All layers depend inward — toward the domain center.
- Entities always `sealed class` with `private` constructor.
- Factory method always returns `(List<string> Errors, Entity? Entity)`.
- Update methods always validate invariants, check if value changed, set `UpdatedAt`.
- Domain Events are contracts only — `sealed record` with data, no behavior.
- Event publication always explicit in the UseCase — never automatic in SaveChanges.
- `UnitOfWork` is the single entry point to all repositories.
- **All repository interfaces MUST inherit from `IGenericRepository<TEntity>`**
- **All repository implementations MUST inherit from `GenericRepository<TEntity>`**
- **Never use `using static`** — always use explicit `using` directives with the namespace, then reference types by their name. Static usings hide the type origin and make code harder to read and navigate.
- Domain folder names must be **plural** and different from the class name — `Domain/Entities/Products/Product.cs`, never `Domain/Entities/Product/Product.cs` — this avoids namespace collisions that force fully qualified type names.
- For bulk insert operations (1000+ rows), use `BulkInsertOperations` from `backend/dotnet/orms/ef-core/bulk-operations/SKILL.md`
- For data access implementation details, load only the EF Core leaf skill(s) the task actually touches
