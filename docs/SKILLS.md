# Skills Reference

Skills are markdown files that teach AI agents coding standards, patterns, and conventions. They are loaded on-demand based on the task.

---

## Backend Skills

### `backend/dotnet/api`

Universal .NET 10 API standards. Loaded for any backend work.

**Covers:**
- Async/await standards (no `async void`, no `.Result`, always `CancellationToken`)
- FluentValidation for request DTOs
- Global error handling middleware with `ProblemDetails`
- Program.cs structure with extension methods
- Logging with `ILogger<T>` (when to use each level)
- C# naming conventions
- Nullable reference types
- appsettings.json structure
- Never `using static`, never named usings, never global usings, never `var`

### `backend/dotnet/ef-core`

Entity Framework Core standards. Loaded when working with data access.

**Covers:**
- Entity configuration with Fluent API (no DataAnnotations)
- `IEntityTypeConfiguration<T>` per entity
- Repository pattern with tracking/no-tracking naming
- UnitOfWork as single entry point
- `AppDbContext` setup
- Value Objects with `OwnsOne`
- Never `.Update()` — change tracker detects changes
- Never `nvarchar(max)` or `varchar(max)`

### `backend/dotnet/linq`

LINQ query standards. Loaded when writing queries.

**Covers:**
- Tracking vs NoTracking queries
- Explicit includes (no lazy loading)
- Projections to DTOs
- Pagination
- Filtering patterns
- Query composition
- Performance best practices

### `backend/dotnet/ddd`

Domain-Driven Design standards. Loaded when creating domain entities.

**Covers:**
- Entity design (sealed class, private constructor, factory methods)
- Value Objects (sealed record, factory methods)
- Domain Events (sealed record, explicit publication)
- Enums (start at 0, explicit values)
- Nested `Rules` class with constraint constants
- Update methods returning `(Errors, bool Updated)`

### `backend/dotnet/testing`

Testing standards. Loaded when writing tests.

**Covers:**
- xUnit test structure
- Test naming: `Method_Scenario_Result`
- Entity tests (factory happy path, validation failures, update methods)
- Use case tests (happy path, validation, business rules, edge cases)
- Mocking conventions with Moq
- Integration tests with `WebApplicationFactory`
- bUnit component tests

---

## Architecture Skills

### `backend/architecture/shared`

Rules common to ALL architecture patterns. Always loaded alongside any architecture skill.

**Covers:**
- Result pattern with `HttpStatusCode`
- DTO conventions (`sealed record`, explicit properties, `Dto` suffix)
- Mapping conventions (extension methods with `To` prefix)
- Use case naming (no `UseCase` suffix)
- Controller conventions (`[FromServices]`, `ToActionResult()`)
- DI conventions (private methods per responsibility)
- Naming conventions table
- Absolute rules (never primary constructors, never `=>` on methods, etc.)

### `backend/architecture/clean`

Clean Architecture with DDD. For complex business domains.

**Structure:**
```
src/
├── Api/
├── Application/ (use cases, contracts, DTOs)
├── Domain/ (entities, value objects, events)
└── Infrastructure/ (EF Core, repositories)
```

**When to use:** Complex business rules, enterprise systems, long-lived applications.

### `backend/architecture/hexagonal`

Hexagonal Architecture (Ports & Adapters). For multiple input channels.

**Structure:**
```
src/
├── Core/ (domain, ports, use cases)
├── Adapter.WebApi/ (REST controllers)
├── Adapter.SqlServer/ (persistence)
└── Adapter.RabbitMQ/ (messaging)
```

**When to use:** Multiple input channels, need to test domain in isolation, adapters change frequently.

### `backend/architecture/vertical-slice`

Vertical Slice Architecture. For CRUDs and MVPs.

**Structure:**
```
src/
├── Domain/ (entities, enums)
├── Persistence/ (DbContext, configurations)
├── Shared/ (Result, extensions)
└── Features/ (endpoint, handler, DTOs, validator per feature)
```

**When to use:** Simple CRUDs, MVPs, rapid prototypes, small teams.

### `backend/architecture/onion`

Onion Architecture. For strong domain-centric DDD.

**Structure:**
```
src/
├── Api/
├── Application/ (use cases, DTOs)
├── Domain/ (entities, aggregates, specifications, contracts)
└── Infrastructure/ (EF Core, repositories)
```

**When to use:** DDD with aggregates, Specification pattern, domain as the center.

---

## Frontend Skills

### `frontend/blazor`

Blazor WebAssembly standards. Loaded for any frontend work.

**Covers:**
- Project structure (separate solution from API)
- Component naming conventions
- Code-behind separation (>50 lines)
- Component lifecycle (`OnInitializedAsync`, `OnParametersSetAsync`, `OnAfterRenderAsync`)
- Dependency injection (`[Inject]` attribute)
- State management (5 levels: component → EventCallback → cascading → services → Fluxor)
- Forms with FluentValidation
- Routing with typed parameters
- Typed HttpClient for API consumption
- Performance optimization (Virtualize, @key, debounce, lazy loading, AOT)
- Error boundaries
- Accessibility (a11y)
- CSS isolation
- JavaScript interop
- Security best practices
- Testing with bUnit

### `frontend/bunit`

bUnit testing standards. Loaded for Blazor component tests.

**Covers:**
- Test class structure (inherit from `TestContext`)
- Render tests
- Data binding tests
- Event handler tests
- State verification tests

---

## DevOps Skills

### `devops/docker`

Docker standards. Loaded for Docker-related tasks.

**Covers:**
- Multi-stage Dockerfiles for .NET 10
- docker-compose.yml (API + database)
- .dockerignore
- Non-root user in runtime stage
- Database healthchecks
- SQL Server, PostgreSQL, and SQLite variants

### `devops/github-actions`

CI/CD standards. Loaded for GitHub Actions workflows.

**Covers:**
- Build and test workflow
- Docker image publishing to GHCR
- Branch protection
- Secrets management
- Test result artifacts

### `devops/ci-cd`

Deployment strategy standards. Loaded for release planning.

**Covers:**
- Environment management (dev, staging, production)
- Semantic versioning
- Release process
- Pipeline design principles
- Docker image strategy

---

## Utility Skills

### `prd-analyzer`

PRD analysis skill. Loaded when reading or building PRDs.

**Covers:**
- How to read and understand a PRD
- Questions to ask when the PRD is ambiguous
- How to identify the right technology stack
- How to identify domain entities from requirements
- How to detect Clean Architecture vs Vertical Slice
- How to generate config files (backend-config.md, frontend-config.md)
- Token-efficient constitution generation

### `token-budget`

Token budget tracking. Loaded for budget management.

**Covers:**
- Default budget limits per phase
- Budget tracking format (`.temper/budget.md`)
- How to estimate token usage
- Budget optimization tips

---

## Skill Loading Matrix

| Agent | Skills Loaded |
|---|---|
| `temper-analyst` | None |
| `temper-architect` | None |
| `temper-spec` | `prd-analyzer` |
| `temper-design` | `dotnet-csharp`, `architecture/[chosen]` + `backend/dotnet/api` |
| `temper-tasks` | None |
| `temper-plan` | None |
| `temper-backend` | `dotnet-csharp` + `backend/dotnet/api` + `backend/dotnet/ef-core` + `backend/dotnet/linq` + `backend/dotnet/ddd` (on demand) + `architecture/[chosen]` |
| `temper-frontend` | `dotnet-csharp` + (`frontend/blazor` if wasm) or (`frontend/blazor-server` if server) |
| `temper-tester` | `dotnet-csharp` + `backend/dotnet/testing` |
| `temper-devops` | `devops/docker` + `devops/github-actions` |
| `temper-review` | `dotnet-csharp` + `backend/dotnet/api` + `architecture/[chosen]` |
| `temper-docs` | None |
| `temper-orchestrator` | None (spawns sub-agents, does not load skills) |
