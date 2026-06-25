# TemperAI Skills Catalog

> Human-readable catalog for the current supported FRIDAY-centered model.
> Scope: active skills referenced by supported agent contracts.

## Workflow — FRIDAY

| Skill | Description |
|---|---|
| `friday-state-schema` | State schema, resume safety, and shared delegation rules for FRIDAY |
| `friday-prompt-excellence` | Prompt construction and recovery guidance for FRIDAY |
| `friday-analyst-communication` | FRIDAY ↔ analyst loop contract |
| `friday-architect-communication` | FRIDAY ↔ architect loop contract |
| `friday-implementation-delegation` | FRIDAY ↔ implementation-agent delegation contract |
| `friday-session-mode-recommendation` | Recommends `clean session` vs `continue here` |

## Analyst

| Skill | Description |
|---|---|
| `functional-analysis` | Functional requirements analysis |
| `analyst-reasoning` | Internal self-questioning for better analysis |
| `analyst-report-formats` | Structured analyst report formats |
| `analyst-prd-template` | PRD generation template |
| `spec-generator` | User-story/spec generation from an approved PRD |

## Architect

| Skill | Description |
|---|---|
| `architect-proposal-formats` | Structured architecture proposal and clarification formats |
| `architect-document-templates` | Templates for generated architecture and config documents |

## Backend

All .NET backend skills live under the `backend/dotnet/` technology root.
Architecture and shared skills are grouped under `backend/dotnet/`; the EF Core
data-access leaves are grouped under `backend/dotnet/orms/ef-core/`.

### `backend/dotnet/` — language, API, domain

| Skill (source path) | Description |
|---|---|
| `backend/dotnet/csharp` | Universal C# / .NET 10 conventions |
| `backend/dotnet/api` | ASP.NET Core API conventions |
| `backend/dotnet/api-docs/scalar` | Scalar API documentation provider rules |
| `backend/dotnet/api-docs/swagger` | Swagger API documentation provider rules |
| `backend/dotnet/ddd` | DDD rules for domain entities and aggregates |
| `backend/dotnet/linq` | LINQ rules |

### `backend/dotnet/architecture/` — architecture patterns

| Skill (source path) | Description |
|---|---|
| `backend/dotnet/architecture/clean` | Clean Architecture rules |
| `backend/dotnet/architecture/hexagonal` | Hexagonal Architecture rules |
| `backend/dotnet/architecture/vertical-slice` | Vertical Slice Architecture rules |
| `backend/dotnet/architecture/onion` | Onion Architecture rules |

### `backend/dotnet/shared/` — cross-cutting backend rules

| Skill (source path) | Description |
|---|---|
| `backend/dotnet/shared/dto-conventions` | DTO conventions |
| `backend/dotnet/shared/result-pattern` | `Result<T>` pattern |
| `backend/dotnet/shared/use-case-patterns` | Use-case conventions |
| `backend/dotnet/shared/solid-clean-code` | SOLID and clean code rules |

### `backend/dotnet/orms/ef-core/` — EF Core data access

| Skill (source path) | Description |
|---|---|
| `backend/dotnet/orms/ef-core/entity-configuration` | EF Core entity configuration rules |
| `backend/dotnet/orms/ef-core/repository-pattern` | Repository and UnitOfWork rules |
| `backend/dotnet/orms/ef-core/repository-usage` | Correct use of existing repositories |
| `backend/dotnet/orms/ef-core/dbcontext-setup` | DbContext setup rules |
| `backend/dotnet/orms/ef-core/queries` | EF Core query rules |
| `backend/dotnet/orms/ef-core/bulk-operations` | Bulk insert and batch operation guidance |

### Domain language

| Skill (source path) | Description |
|---|---|
| `ddd/ubiquitous-language` | Ubiquitous language understanding |

## Frontend

| Skill | Description |
|---|---|
| `blazor` | Blazor WebAssembly standards |
| `blazor-server` | Blazor Server standards |
| `mudblazor` | MudBlazor standards |
| `tailwind` | Tailwind styling standards |
| `bunit` | Blazor component testing |
| `angular` | Angular standards |
| `angular-material` | Angular Material standards |
| `scss` | SCSS standards |

## Testing and DevOps

| Skill | Description |
|---|---|
| `dotnet-testing` | Backend and component testing guidance |
| `docker` | Docker and compose standards |
| `github-actions` | GitHub Actions standards |
| `setup-tasks` | Foundational setup-task generation for new projects |

## Notes

- This catalog covers the supported FRIDAY-centered model only.
- Legacy orchestrator skills are intentionally excluded from this human-facing catalog.
