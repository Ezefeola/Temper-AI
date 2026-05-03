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
| Gather functional requirements | `temper-analyst` | None |
| Define technical architecture | `temper-architect` | None |
| Generate user stories | `temper-spec` | `prd-analyzer` |
| Design architecture | `temper-design` | `dotnet-csharp` + `backend/architecture/[chosen]` + `backend/dotnet/api` |
| Break into tasks | `temper-tasks` | None — **NO access to design.md** |
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

## Approval Rules — NEVER assume approval

- **ALWAYS ask for explicit approval** after every phase output and every sub-agent result.
- **NEVER assume the user approved** because they ran `/temper-next` or started a new session.
- **Before proceeding to the next phase**, the orchestrator MUST:
  1. Show the user a summary of what was generated/changed.
  2. Ask explicitly: "Do you approve these changes? Reply 'yes' to proceed or describe what needs to change."
  3. Wait for the user's explicit "yes" (or equivalent approval).
  4. Only then update the state file and proceed.
- **If the user requests changes**, the orchestrator must spawn the appropriate agent with the feedback, show the revised output, and ask for approval again.
- **If the user does not explicitly approve**, the orchestrator MUST NOT proceed. Set `Status: awaiting-approval` in the state file.
- This rule applies to: phase outputs (spec, design, tasks, plan, review, docs), sub-agent results during build, and quick-path results.

## Recovery Rules — Continue from failure point

- **If a sub-agent fails during build execution**, the orchestrator MUST attempt recovery before reporting to the user:
  1. **Assess what was completed**: Check which files were created/modified by the failed agent before it errored.
  2. **Identify the failure point**: Determine exactly which task or step failed and why.
  3. **Spawn a recovery agent**: Spawn a new sub-agent (same type or different if appropriate) with:
     - The original task file.
     - The corresponding user story spec.
     - The error message from the previous attempt.
     - A clear instruction: "The previous attempt failed at [specific point]. Files already created: [list]. Continue from where it left off. Do NOT regenerate what already exists."
     - All files that were successfully created by the previous attempt (so the recovery agent knows what's already done).
  4. **If the recovery agent also fails**: Only then report to the user with full error details and recommended manual action.
- **If a phase agent fails**, retry once with the error context. If it fails again, report to the user — do NOT attempt the phase yourself.
- **Always preserve partial work**: Never discard files that were successfully created before a failure. The recovery agent must build on top of them.
- **Update the state file** with recovery attempt details: `Last build status: recovery-attempted`, `Recovery error: [error]`, `Recovery attempt: N`.
