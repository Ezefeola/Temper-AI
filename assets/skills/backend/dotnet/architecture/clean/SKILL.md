---
name: clean-architecture
description: >
  Structure and rules for .NET projects using Clean Architecture with DDD.
  Use when the project has complex business domain, multiple use cases,
  need to test business logic in isolation, or when the system
  is enterprise, e-commerce, ERP, CRM, or any rich domain.
  Do not use for simple CRUDs without logic — prefer Vertical Slice in that case.
  For implementation details, load the required `backend/dotnet/orms/ef-core/*/SKILL.md` leaf skill(s) or your chosen data access skill.
---

# Clean Architecture + DDD — TemperAI Standards

> For data access implementation, load the required `backend/dotnet/orms/ef-core/*/SKILL.md` leaf skill(s) or your chosen data access skill.

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **ALWAYS preserve the four-layer project split**: `Api`, `Application`, `Domain`, `Infrastructure`
2. **NEVER let `Api` depend directly on `Infrastructure`**
3. **ALWAYS respect the `Data Access` pattern**: under `Repository + UnitOfWork`, keep repositories and UnitOfWork behind Application contracts; under `Direct DbContext`, the `DbContext` lives in Infrastructure and use cases depend on it. Never mix the two.
4. **ALWAYS keep business rules inside Domain entities**
5. **NEVER let generic shared guidance override Clean dependency direction or layer placement**

## Project root folder naming — CRITICAL

**This is the ONLY correct structure for new projects:**

```
MyProjectApi/                    ← Folder root with "Api" suffix
├── MyProjectApi.sln            ← Solution file named after folder
├── src/
│   ├── MyProjectApi.Api/        ← Projects inside src/
│   ├── MyProjectApi.Application/
│   ├── MyProjectApi.Domain/
│   └── MyProjectApi.Infrastructure/
└── tests/
```

**NEVER create this wrong structure:**

```
MyProjectApi/                    ← WRONG: extra folder inside
├── src/
│   └── MyProjectApi/            ← WRONG: duplicate folder
│       ├── Domain/
│       └── ...
```

**Rules:**
1. Root folder name: `[ProjectName]Api/` for API projects
2. Solution file: `[ProjectName]Api.sln` (same as folder name)
3. `src/` folder contains all projects directly — NO extra project folder inside src/
4. Each project named with prefix: `MyProjectApi.Api`, `MyProjectApi.Application`, etc.

---

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

The four-project split (`Api`, `Application`, `Domain`, `Infrastructure`) and the dependency
direction are **fixed** for Clean Architecture regardless of anything else. What changes with the
`Data Access` pattern in `backend-config.md` is only the **persistence layout** — see
"Data access layout by pattern" below. The tree here shows the `Repository + UnitOfWork` layout.

```
src/
├── YourProject.Api/
│   ├── Controllers/
│   ├── Middlewares/
│   └── Program.cs
│
├── YourProject.Application/
│   ├── Contracts/
│   │   ├── Persistence/                         # [Repository + UnitOfWork only]
│   │   │   ├── Repositories/
│   │   │   │   ├── IProductRepository.cs
│   │   │   │   └── IOrderRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   └── Services/
│   │       └── IEventPublisher.cs
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
│   │       ├── Enums/
│   │       │   └── ProductStatus.cs
│   │       └── Events/
│   │           └── ProductCreatedEvent.cs
│   ├── Common/
│   │   └── Primitives/
│   │       ├── Entity.cs
│   │       └── IDomainEvent.cs
│   └── Errors/
│
└── YourProject.Infrastructure/
    ├── Persistence/
    │   ├── AppDbContext.cs
    │   ├── Configurations/
    │   │   └── ProductConfiguration.cs
    │   ├── Migrations/
    │   ├── Repositories/                         # [Repository + UnitOfWork only]
    │   │   └── ProductRepository.cs
    │   └── UnitOfWork.cs                         # [Repository + UnitOfWork only]
    ├── Services/
    │   └── EventPublisher.cs
    └── DependencyInjection.cs
```

---

## Data access layout by pattern

The persistence layout depends on the `Data Access` field in `backend-config.md`. The rest of the
Clean structure (layers, dependency direction, UseCases, Domain, Services) is identical for both.

**`Repository + UnitOfWork`** (the tree above):
- `Application/Contracts/Persistence/Repositories/` — repository interfaces (ports)
- `Application/Contracts/Persistence/IUnitOfWork.cs` — the persistence entry point
- `Infrastructure/Persistence/Repositories/` — repository implementations
- `Infrastructure/Persistence/UnitOfWork.cs`
- Use cases depend on the repository interfaces + `IUnitOfWork`
- For the exact repository / UnitOfWork contract and rules, load `backend/dotnet/orms/ef-core/repository-pattern` (create) or `repository-usage` (consume)

**`Direct DbContext`** — the repository/UnitOfWork folders do **not** exist:
- No `Application/Contracts/Persistence/Repositories/` and no `IUnitOfWork.cs`
- No `Infrastructure/Persistence/Repositories/` and no `UnitOfWork.cs`
- `Infrastructure/Persistence/` holds only `AppDbContext.cs`, `Configurations/`, and `Migrations/`
- Use cases depend on the `DbContext` directly and call `SaveChangesAsync`
- For the exact usage rules, load `backend/dotnet/orms/ef-core/dbcontext-usage`

Either way, load `backend/dotnet/orms/ef-core/query-best-practices` whenever the task writes queries.

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

## Application — contracts and patterns

### Persistence contracts — depend on the Data Access pattern

The persistence layer follows the `Data Access` field in `backend-config.md`. Clean does not
redefine the data-access contracts — the leaf EF Core skills own them so there is a single source
of truth.

**`Repository + UnitOfWork`:**
- Repository interfaces live in `Application/Contracts/Persistence/Repositories/` — the ports Infrastructure implements.
- `IUnitOfWork` lives in `Application/Contracts/Persistence/` and is the single persistence entry point that exposes the repositories and `CompleteAsync`.
- The exact contracts (`IGenericRepository<TEntity>`, the per-entity repository interface, `IUnitOfWork`, `SaveResult`) and their rules are defined once in `backend/dotnet/orms/ef-core/repository-pattern`. Load that skill to create them, or `repository-usage` to consume existing ones. Do not restate the contract here.

**`Direct DbContext`:**
- No repository or `IUnitOfWork` contracts. Use cases depend on the `DbContext` directly and own `SaveChangesAsync`.
- Rules are defined in `backend/dotnet/orms/ef-core/dbcontext-usage`.

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

Use cases are `sealed class` without `UseCase` suffix, with explicit constructor injection. Their
persistence dependency follows the `Data Access` pattern: under `Repository + UnitOfWork` they
inject `IUnitOfWork` (+ repositories) as shown below; under `Direct DbContext` they inject the
`DbContext` instead (see `dbcontext-usage`). Everything else is identical.

- Interface in the same folder — `ICreateProduct`, `IUpdateProduct`.
- Explicit constructor injection — never primary constructor.
- Domain events published explicitly after the write is persisted (`CompleteAsync` under Repository + UnitOfWork, `SaveChangesAsync` under Direct DbContext).
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

- **Data access implementations** — repositories, DbContext, configurations (load the matching `backend/dotnet/orms/ef-core/*/SKILL.md` leaf skill)
- **External service implementations** — `EventPublisher`, email services, third-party API clients
- **DI setup** — `AddInfrastructure`, `AddDatabase`, `AddRepositories`, `AddUnitOfWork`

### DI structure

Under `Direct DbContext`, register only the database — omit `AddRepositories()` and `AddUnitOfWork()`
(there are none). The example below is the `Repository + UnitOfWork` wiring.

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
            .AddRepositories()   // [Repository + UnitOfWork only]
            .AddUnitOfWork();    // [Repository + UnitOfWork only]

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

## Rules specific to Clean Architecture

- `Domain` zero external dependencies — pure C# only
- Entities always `sealed class` with `private` constructor
- `Entity<TId>` base is clean — no event logic or automatic auditing
- Factory method always returns `(List<string> Errors, Entity? Entity)`
- Update methods always validate invariants, check if value changed, set `UpdatedAt`
- Domain Events are contracts only — `sealed record` with data, no behavior
- Event publication always explicit in the UseCase — never automatic in SaveChanges
- Data access follows the `Data Access` pattern in `backend-config.md` — `Repository + UnitOfWork` or `Direct DbContext` — and only one is used per project
- Under `Repository + UnitOfWork`: `UnitOfWork` is the single entry point to all repositories, and repository interfaces/implementations follow `backend/dotnet/orms/ef-core/repository-pattern` (the canonical contract). Repository and UnitOfWork are always used together
- Under `Direct DbContext`: no repositories or UnitOfWork — use cases inject the `DbContext` and follow `backend/dotnet/orms/ef-core/dbcontext-usage`
- For bulk insert operations (1000+ rows), use `BulkInsertOperations` from `backend/dotnet/orms/ef-core/bulk-operations/SKILL.md`
- For data access implementation details, load only the EF Core leaf skill(s) the task actually touches
