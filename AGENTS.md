# TemperAI — Agent Router

Lightweight index that maps intentions to skills and agents.
Full documentation is in TEMPER_AI_ARCHITECTURE.md.

## Quick Rules

- C# / .NET 10 — always
- Never primary constructors
- Never return expression `=>` on methods — always braces `{}`
- Never `DataAnnotations` on entities
- Never `using static` — always explicit `using` directives
- Never global usings — always per-file `using` directives
- DTOs are always `sealed record` with explicit properties and `Dto` suffix
- Use cases never have `UseCase` suffix
- Entity folders are always plural — `Domain/Products/Product.cs`, never `Domain/Product/Product.cs`

## Agent Routing

| User intention | Agent | Skills loaded |
|---|---|---|
| Start new project / analyze PRD | `temper-init` | `dotnet-csharp`, `prd-analyzer` |
| Generate user stories | `temper-spec` | `dotnet-csharp`, `prd-analyzer` |
| Design architecture | `temper-design` | `dotnet-csharp` + `backend/architecture/[chosen]` + `backend/dotnet/api` |
| Break into tasks | `temper-tasks` | None |
| Generate build plan | `temper-plan` | None |
| **Execute build** | **`temper-orchestrator`** (spawns sub-agents) | Varies by sub-agent |
| Implement backend | `temper-backend` | `dotnet-csharp` + `backend/dotnet/api` + `backend/dotnet/ef-core` + `backend/dotnet/linq` + `backend/architecture/shared` + `backend/architecture/[chosen]` |
| Implement frontend | `temper-frontend` | `dotnet-csharp` + `frontend/blazor` |
| Write tests | `temper-tester` | `dotnet-csharp` + `backend/dotnet/testing` |
| Docker / CI/CD | `temper-devops` | `devops/docker` + `devops/github-actions` |
| Review code | `temper-review` | `dotnet-csharp` + `backend/dotnet/api` + `backend/architecture/shared` + `backend/architecture/[chosen]` |
| Generate docs | `temper-docs` | None |
| Bug fix / small change | `temper-backend` (quick path) | Only what's needed |

## Skill Loading

Skills are loaded on-demand. Never load all skills at once.

| Skill | When to load |
|---|---|
| `dotnet-csharp` | **ALWAYS** — loaded by every agent that writes C# code |
| `prd-analyzer` | Reading or building a PRD |
| `backend/architecture/shared` | **ALWAYS** for backend agents — Result pattern, DTO conventions, naming, controller rules |
| `backend/architecture/clean` | Project uses Clean Architecture |
| `backend/architecture/hexagonal` | Project uses Hexagonal Architecture |
| `backend/architecture/vertical-slice` | Project uses Vertical Slice |
| `backend/architecture/onion` | Project uses Onion Architecture |
| `backend/dotnet/api` | Any .NET backend work |
| `backend/dotnet/ef-core` | EF Core entity config, repositories, DbContext |
| `backend/dotnet/linq` | Writing or reviewing LINQ queries |
| `backend/dotnet/ddd` | Domain entities, value objects, domain events |
| `backend/dotnet/testing` | Writing xUnit tests |
| `frontend/blazor` | Blazor components and pages |
| `frontend/bunit` | Blazor component tests |
| `devops/docker` | Dockerfiles, docker-compose |
| `devops/github-actions` | CI/CD workflows |
| `token-budget` | Token budget tracking and management |

## Context Rules

- Each phase starts fresh — no accumulated context from previous phases
- Only load files the current phase needs — never the entire codebase
- Quick path for 1-2 file changes — full pipeline for 3+ files or architectural changes
- Orchestrator decides — see TEMPER_AI_ARCHITECTURE.md for the decision matrix
