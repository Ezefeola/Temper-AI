---
name: clean-architecture
description: >
  Structure and rules for .NET projects using Clean Architecture with DDD.
  Use when the project has complex business domain, multiple use cases,
  need to test business logic in isolation, or when the system
  is enterprise, e-commerce, ERP, CRM, or any rich domain.
  Do not use for simple CRUDs without logic — prefer Vertical Slice in that case.
  Also load architecture/shared for common conventions.
  For implementation details, load backend/dotnet/ef-core or your chosen data access skill.
---

# Clean Architecture + DDD — TemperAI Standards

> Also load `architecture/shared` for Result pattern, DTO conventions, naming, and controller rules.
> For data access implementation, load `backend/dotnet/ef-core` or your chosen data access skill.

## When to use

- Complex business domain with rules that change frequently
- Need to test business logic without database or HTTP
- Long-lived enterprise systems where maintainability is critical
- Medium or large teams

## When NOT to use

- Simple CRUDs without business logic → use Vertical Slice
- Prototypes or MVPs with limited time → use Vertical Slice

---

## API and Frontend separation — never in the same solution

If the project includes a Blazor Frontend, it **must be in a separate solution** from the API.

```
TodoManagerApi/                          ← Backend API solution
├── TodoManagerApi.sln
├── src/
│   ├── TodoManagerApi.Api/
│   ├── TodoManagerApi.Application/
│   ├── TodoManagerApi.Domain/
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
```
src/
├── YourProject.Api/
│   ├── Controllers/
│   ├── Middlewares/
│   └── Program.cs
│
├── YourProject.Application/
│   ├── Contracts/
│   │   ├── Services/
│   │   │   └── IEventPublisher.cs
│   │   └── Repositories/
│   │       ├── IProductRepository.cs
│   │       └── IUnitOfWork.cs
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
│   │   └── Product/
│   │       ├── Product.cs
│   │       ├── ValueObjects/
│   │       │   └── Money.cs
│   │       ├── Enums/
│   │       │   └── ProductStatus.cs
│   │       └── Events/
│   │           └── ProductCreatedEvent.cs
│   ├── Common/
│   │   ├── ValueObjects/
│   │   │   └── Address.cs
│   │   └── Primitives/
│   │       ├── Entity.cs
│   │       └── IDomainEvent.cs
│   └── Errors/
│
└── YourProject.Infrastructure/
    ├── Persistence/
    │   ├── Configurations/
    │   │   └── ProductConfiguration.cs
    │   ├── Migrations/
    │   ├── Repositories/
    │   │   └── ProductRepository.cs
    │   └── UnitOfWork.cs
    ├── Services/
    │   └── EventPublisher.cs
    └── DependencyInjection.cs
```

---

## Dependency rules — never broken

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
// Entity.cs — clean base, no event logic
public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;
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

### Value Object pattern

- `sealed record` with explicit properties — never `[ComplexType]` or DataAnnotations.
- Factory method returning `(List<string> Errors, ValueObject? ValueObject)`.
- Configured by the data access layer (e.g., `OwnsOne` in EF Core).

See `backend/dotnet/ddd` for the complete Value Object implementation pattern.

### Domain Event pattern

- `sealed record` with data only — no behavior.
- Never registered on the entity or dispatched automatically in SaveChanges.
- Published explicitly in the UseCase after the persistence operation completes.

See `backend/dotnet/ddd` for the complete Domain Event implementation pattern.

---

## Application — contracts and patterns

### Repository contracts

Defined in `Application/Contracts/Repositories/`. These are the ports that Infrastructure implements.

```csharp
// IProductRepository.cs
public interface IProductRepository
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

// IUnitOfWork.cs
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
    public string ErrorMessage { get; init; } = string.Empty;
}
```

### External service contracts

```csharp
// IEventPublisher.cs — in Application/Contracts/Services/
// Implementation in Infrastructure points to RabbitMQ, MassTransit, etc.
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        TEvent domainEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}
```

### Use case pattern

Use cases are `sealed class` without `UseCase` suffix. They depend on abstractions (`IUnitOfWork`, `IEventPublisher`), never on concrete implementations.

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

// CreateProduct.cs — structure example
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
        // 1. Check business rules (e.g., uniqueness)
        // 2. Create entity via factory method
        // 3. Persist via UnitOfWork
        // 4. Publish domain events explicitly
        // 5. Return Result with response DTO
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

## Infrastructure — contracts and patterns

### What lives here

- **Data access implementations** — repositories, DbContext, configurations (see `backend/dotnet/ef-core`)
- **External service implementations** — `EventPublisher`, email services, third-party API clients
- **DI setup** — `AddInfrastructure`, `AddDatabase`, `AddRepositories`, `AddUnitOfWork`

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
        // Configure database — see backend/dotnet/ef-core for EF Core implementation
        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, ProductRepository>();
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

## Rules specific to Clean Architecture

- `Domain` zero external dependencies — pure C# only
- Entities always `sealed class` with `private` constructor
- `Entity<TId>` base is clean — no event logic or automatic auditing
- Factory method always returns `(List<string> Errors, Entity? Entity)`
- Update methods always validate invariants, check if value changed, set `UpdatedAt`
- Domain Events are contracts only — `sealed record` with data, no behavior
- Event publication always explicit in the UseCase — never automatic in SaveChanges
- `UnitOfWork` is the single entry point to all repositories
- For data access implementation details, load `backend/dotnet/ef-core`