# The SDD Workflow — Step by Step

## Overview

The TemperAI SDD (Spec-Driven Development) workflow consists of 7 phases plus a build execution step handled by the orchestrator. Each phase produces a specific artifact and requires user approval before proceeding.

```
Phase 1: Init      → constitution.md
Phase 2: Spec      → spec.md
Phase 3: Design    → design.md
Phase 4: Tasks     → tasks.md
Phase 5: Plan      → build-plan.md
         Build     → Generated code (orchestrator spawns sub-agents per group)
Phase 6: Review    → Review report
Phase 7: Docs      → README, ARCHITECTURE, API docs, CHANGELOG
```

---

## Phase 1: Initialization (`temper-init`)

**Input:** User's project description (PRD or natural language)
**Output:** `.temper/constitution.md`
**Skills loaded:** `prd-analyzer`

### What happens

1. The agent reads the user's project description.
2. It identifies domain entities, relationships, and business rules.
3. It asks clarifying questions grouped by category (vision, features, technical, standards).
4. It recommends an architecture based on complexity.
5. It generates `constitution.md` with:
   - Project summary
   - Problem statement
   - End users
   - Scope (core features + out of scope)
   - Business rules
   - External integrations
   - Technology stack
   - Architecture choice with justification
   - Code standards
   - Decisions made
   - Pending questions

### User action

Review the constitution and approve or request changes.

---

## Phase 2: Specification (`temper-spec`)

**Input:** `.temper/constitution.md`
**Output:** `.temper/spec.md`
**Skills loaded:** `prd-analyzer`

### What happens

1. The agent reads the constitution.
2. It generates user stories in the format: "As a [role], I want to [action], so that [benefit]."
3. For each user story, it defines:
   - Priority (High/Medium/Low)
   - Acceptance criteria (verifiable conditions)
   - Edge cases
   - Error cases
4. It documents non-functional requirements (performance, security, scalability, reliability, usability, maintainability).
5. It identifies dependencies between user stories.

### User action

Review the spec and approve or request changes.

---

## Phase 3: Design (`temper-design`)

**Input:** `.temper/constitution.md` + `.temper/spec.md`
**Output:** `.temper/design.md`
**Skills loaded:** `architecture/[chosen]` + `backend/dotnet/api`

### What happens

1. The agent reads the constitution and spec.
2. It designs the complete architecture:
   - Domain entities with properties, relationships, factory methods, and update methods
   - Value Objects and Enums
   - API endpoints (HTTP method, route, request DTO, response DTO, error responses)
   - Database schema (tables, columns, keys, indexes, foreign keys)
   - Use cases (interface, input, output)
   - Blazor components (if applicable)
   - External integrations
3. It produces a complete folder structure for the project.

### User action

Review the design and approve or request changes.

---

## Phase 4: Task Breakdown (`temper-tasks`)

**Input:** `.temper/constitution.md` + `.temper/spec.md` + `.temper/design.md`
**Output:** `.temper/tasks.md`
**Skills loaded:** None

### What happens

1. The agent reads all three documents.
2. It breaks the design into atomic tasks:
   - Each task affects 1-3 files maximum
   - Each task has a clear completion criterion
   - Each task has dependencies listed
   - Each task is assigned to an agent (backend, frontend, tester, devops)
3. Tasks are ordered by dependency — foundational tasks first.

### User action

Review the task list and approve or request changes.

---

## Phase 5: Planning (`temper-plan`)

**Input:** `.temper/tasks.md` + `.temper/design.md`
**Output:** `.temper/build-plan.md`
**Skills loaded:** None

### What happens

1. The agent reads the task list and builds a dependency graph.
2. It groups independent tasks that can run in parallel.
3. It produces `build-plan.md` with:
   - Execution groups (tasks that can run in parallel)
   - Agent assignments per group (backend, frontend, tester, devops)
   - Estimated token cost per group
   - Verification steps (`dotnet build` / `dotnet test`) between groups
4. The plan is shown to the user for approval.

### User action

Review the build plan and approve or request changes.

---

## Build Execution (handled by `temper-orchestrator`)

**Input:** `.temper/build-plan.md`
**Output:** Generated source code
**Skills loaded:** Depends on sub-agent type

### How it works

After the build plan is approved, the **orchestrator** (`temper-orchestrator`) executes it:

1. Reads `.temper/build-plan.md` to understand the execution groups.
2. For each group, spawns the appropriate sub-agents in **separate conversations** with clean context:
   - **`temper-backend`** — Domain entities, EF Core configurations, repositories, use cases, DTOs, controllers
   - **`temper-frontend`** — Blazor pages, components, services, layouts
   - **`temper-tester`** — xUnit tests, bUnit component tests
   - **`temper-devops`** — Dockerfiles, docker-compose, GitHub Actions
3. Each sub-agent receives **only** its specific tasks — not the full task list.
4. After each group completes, the orchestrator asks the user to run `dotnet build`.
5. If build succeeds, proceeds to the next group. If it fails, stops and reports errors.
6. After all groups complete, asks the user to run `dotnet test`.

### Why the orchestrator executes (not a separate agent)

The orchestrator is the **root of the conversation tree**. When it spawns sub-agents, each one gets a **fresh, clean context** — no accumulated history from previous phases. This prevents context window bloat and ensures consistent code quality across all tasks.

If a separate build agent tried to spawn sub-agents, it would create nested conversations (level 2+), which:
- Consumes more resources per level of nesting
- Loses visibility at the orchestrator level
- May not be supported by all AI platforms

### Parallel execution

| Can run in parallel | Cannot run in parallel |
|---|---|
| Backend + Frontend | Two tasks modifying the same file |
| Backend + DevOps | Task B depends on Task A |
| Frontend + DevOps | Tester for unbuilt code |
| Tasks for different entities | |

### User action

Each group's output is verified with `dotnet build` before proceeding to the next group.

---

## Phase 6: Review (`temper-review`)

**Input:** `.temper/spec.md` + `.temper/design.md` + generated code
**Output:** Review report
**Skills loaded:** `backend/dotnet/api` + `architecture/[chosen]`

### What happens

1. The agent scans all generated C# code for convention violations.
2. It checks:
   - No primary constructors
   - No expression-bodied methods
   - No DataAnnotations on entities
   - No `.Update()` in EF Core
   - No `UseCase` suffix on use cases
   - DTOs are `sealed record`
   - Variable names match types
   - No `async void`, `.Result`, `.Wait()`
   - No `nvarchar(max)` or `varchar(max)`
   - No lazy loading
   - No `throw` for business validations
   - All acceptance criteria from spec.md are covered
3. It produces a report with:
   - Critical violations (must fix)
   - Warnings (should fix)
   - Specification coverage matrix
   - Suggestions for improvement

### User action

If critical violations exist, fix them before proceeding. If approved, proceed to docs.

---

## Phase 7: Documentation (`temper-docs`)

**Input:** All `.temper/` files
**Output:** `README.md`, `ARCHITECTURE.md`, `API.md`, `CHANGELOG.md`
**Skills loaded:** None

### What happens

1. The agent generates:
   - **README.md** — Project description, setup instructions, running locally, project structure
   - **ARCHITECTURE.md** — Architecture decisions, domain design, API design, code conventions
   - **API.md** — Complete endpoint documentation with request/response examples
   - **CHANGELOG.md** — Initial release notes with version 0.1.0

### User action

Review and approve the documentation.

---

## Quick Path (for simple changes)

Not every change needs the full pipeline. The orchestrator evaluates complexity:

| Request | Path | Agent |
|---|---|---|
| "Fix null reference in ProductController" | Quick | `temper-backend` directly |
| "Add Description field to Product" | Quick | `temper-backend` directly |
| "Add test for UpdateName" | Quick | `temper-tester` directly |
| "Change connection string" | Quick | `temper-devops` directly |
| "Add Order management with items and payments" | Full | Complete pipeline |
| "Add RabbitMQ for order events" | Full | Complete pipeline |

---

## Automatic Rollback

Before each phase, a snapshot of `.temper/` files is created. If a phase produces unsatisfactory output:

```cmd
temper-ai snapshot --latest
```

This restores all `.temper/` files to the last approved state.

---

## Incremental Updates

After modifying `.temper/` files, check what needs re-running:

```cmd
temper-ai incremental --check
```

This compares current files against the last snapshot and identifies which phases are affected by the change.

| Changed file | Phases that need re-running |
|---|---|
| `constitution.md` | spec → design → tasks → plan → build → review → docs |
| `spec.md` | design → tasks → plan → build → review → docs |
| `design.md` | tasks → plan → build → review → docs |
| `tasks.md` | plan → build → review → docs |
| `build-plan.md` | build → review → docs |
