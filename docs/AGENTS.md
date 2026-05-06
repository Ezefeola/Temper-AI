# Agents Reference

Agents are specialized AI sub-agents that handle specific phases of the SDD workflow. Each agent has a defined role, workflow, and set of skills it loads.

---

## temper-analyst

**Phase:** 1 — Functional Analysis
**Skills:** None

**Role:** First contact point in the SDD workflow. Gathers functional requirements by asking business-focused questions only (never technology or architecture), identifies scope deltas if a PRD already exists, and generates `.temper/prd.md` as the single source of truth.

**Workflow:**
1. Read the user's project description.
2. If `.temper/prd.md` exists, read it and identify scope deltas.
3. Ask functional questions only: project purpose, user capabilities, scope boundaries, business rules, external integrations.
4. Iterate until everything is clear.
5. Generate `.temper/prd.md` with functional scope, business rules, and status workflows.

**Questions asked:**
- Project basics (problem, end users, core features)
- Functional capabilities (what users should be able to DO)
- Scope boundaries (what's in, what's out, MVP definition)
- Business rules and validation constraints
- External integrations from user perspective

**Output:** `.temper/prd.md` + `.temper/specs/INDEX.md` + `.temper/specs/US-XXX-*.md`

---

## temper-architect

**Phase:** 2 — Technical Architecture
**Skills:** None

**Role:** Reads `.temper/prd.md`, asks ONLY technical questions (database, architecture pattern, frontend type, auth), and generates `.temper/backend-config.md` and `.temper/frontend-config.md` for implementation agents. NEVER changes functional scope.

**Workflow:**
1. Read `.temper/prd.md` to understand functional scope.
2. Ask technical questions only: architecture pattern, database, frontend type, authentication, API documentation.
3. Recommend based on PRD complexity when user doesn't know.
4. Generate config files with project-specific technical decisions.

**Output:** `.temper/backend-config.md` + `.temper/frontend-config.md` (if applicable)

---

## temper-tasks

**Phase:** 3 — Task Breakdown
**Skills:** None

**Role:** Break the design into atomic, trackable implementation tasks.

**Workflow:**
1. Read `prd.md`, `specs/`, and `Docs/domain-model.md`.
2. Create atomic tasks (feature-centric, not component-centric).
3. Define dependencies between tasks.
4. Assign each task to an agent (backend, frontend, tester, devops).
5. Define completion criteria for each task.
6. Generate `.temper/tasks/` with per-story folders, individual task files, and `INDEX.md`.
7. Request approval.

**Output:** `.temper/tasks/INDEX.md` + `.temper/tasks/US-XXX/T###-*.md`

---

## temper-plan

**Phase:** 4 — Build Planner
**Skills:** None

**Role:** Analyze task dependencies, group tasks for parallel execution, and generate `.temper/build-plan.md`.

**Workflow:**
1. Read `tasks/INDEX.md` and `Docs/domain-model.md`.
2. Build dependency graph.
3. Group independent tasks for parallel execution.
4. Assign agents to each group (backend, frontend, tester, devops).
5. Estimate token cost per group.
6. Generate `.temper/build-plan.md`.
7. Request approval.

**Output:** `.temper/build-plan.md`

---

## Build Execution (handled by `temper-orchestrator`)

**Phase:** 5 — Build Execution
**Skills:** Varies by sub-agent

**Role:** Execute the build plan by spawning sub-agents one group at a time, each in a separate conversation with clean context.

**Workflow:**
1. Read `build-plan.md` to understand execution groups.
2. For each group, spawn the appropriate sub-agents (backend, frontend, tester, devops).
3. Each sub-agent receives only its specific tasks — not the full task list.
4. Wait for all agents in the group to complete.
5. Ask user to run `dotnet build` to verify.
6. Proceed to next group.
7. After all groups, ask user to run `dotnet test`.

**Why the orchestrator executes:** The orchestrator is the root of the conversation tree. When it spawns sub-agents, each gets a fresh, clean context at level 1 — preventing context window bloat and ensuring consistent quality.

**Sub-agents:**
- `temper-backend` — Backend implementation
- `temper-frontend` — Frontend implementation
- `temper-tester` — Test implementation
- `temper-devops` — DevOps implementation

---

## temper-backend

**Phase:** 5a — Backend Implementation
**Skills:** `dotnet-csharp` + `backend/dotnet/api` + `backend/dotnet/ef-core` + `backend/dotnet/linq` + `backend/dotnet/ddd` (on demand) + `architecture/[chosen]`

**Role:** Implement backend code — entities, use cases, DTOs, repositories, controllers.

**Workflow:**
1. Read `tasks/INDEX.md` and filter for pending backend tasks.
2. Take ONE task at a time.
3. Read `Docs/domain-model.md` for the relevant section.
4. Implement following TemperAI conventions strictly.
5. Show code and request approval.
6. Mark task as `done`.
7. Proceed to next task.

---

## temper-frontend

**Phase:** 5b — Frontend Implementation
**Skills:** `dotnet-csharp` + (`frontend/blazor` if wasm) or (`frontend/blazor-server` if server)

**Role:** Implement Blazor components, pages, and services (Server or WebAssembly).
**Note:** Reads `.temper/frontend-config.md` to determine `blazorType` and loads the appropriate skill.

**Workflow:**
1. Read `tasks/INDEX.md` and filter for pending frontend tasks.
2. Take ONE task at a time.
3. Read `Docs/domain-model.md` for the relevant component design.
4. Implement following Blazor conventions strictly.
5. Show code and request approval.
6. Mark task as `done`.
7. Proceed to next task.

---

## temper-tester

**Phase:** 5c — Test Implementation
**Skills:** `dotnet-csharp` + `backend/dotnet/testing`

**Role:** Write xUnit tests for backend code and bUnit tests for Blazor components.

**Workflow:**
1. Read `tasks/INDEX.md` and filter for pending tester tasks.
2. Take ONE task at a time.
3. Read the corresponding user story spec for acceptance criteria and edge cases.
4. Write tests following naming and structure conventions.
5. Show tests and request approval.
6. Mark task as `done`.
7. Proceed to next task.

---

## temper-devops

**Phase:** 5d — DevOps Implementation
**Skills:** `devops/docker` + `devops/github-actions`

**Role:** Generate Docker and CI/CD infrastructure files.

**Workflow:**
1. Read `tasks/INDEX.md` and filter for pending devops tasks.
2. Take ONE task at a time.
3. Generate Dockerfiles, docker-compose, GitHub Actions workflows.
4. Show files and request approval.
5. Mark task as `done`.
6. Proceed to next task.

---

## temper-review

**Phase:** 6 — Code Review
**Skills:** `dotnet-csharp` + `backend/dotnet/api` + `architecture/[chosen]`

**Role:** Review all generated code against TemperAI conventions and specification coverage.

**Workflow:**
1. Read `specs/` and `Docs/domain-model.md`.
2. Scan all generated C# code for convention violations.
3. Cross-reference code against acceptance criteria.
4. Generate review report with pass/fail items.
5. Show report and recommend action.

**Output:** Review report with critical violations, warnings, and coverage matrix.

---

## temper-docs

**Phase:** 7 — Documentation
**Skills:** None

**Role:** Generate project documentation — README, ARCHITECTURE, API docs, CHANGELOG.

**Workflow:**
1. Read all `.temper/` files.
2. Generate `README.md`.
3. Generate `ARCHITECTURE.md`.
4. Generate `API.md` (if applicable).
5. Generate `CHANGELOG.md`.
6. Show each document and request approval.

---

## temper-orchestrator

**Role:** Main orchestrator — evaluates request complexity, decides between quick path and full pipeline, and **executes the build plan** by spawning sub-agents.

**Workflow:**
1. Receive user request.
2. Evaluate complexity (files affected, new entities, architectural impact).
3. Choose quick path or full pipeline.
4. Spawn the appropriate phase agent with minimal context.
5. During build phase: read `build-plan.md` and spawn sub-agents per group.
6. Wait for completion and report back.

**Decision rules:**
- 1-2 files, no architectural impact → Quick path
- 3+ files, new entities, architectural change → Full pipeline

**Build execution:**
- Reads `.temper/build-plan.md` generated by `temper-plan`.
- Spawns sub-agents (backend, frontend, tester, devops) one group at a time.
- Each sub-agent runs in a separate conversation with clean context.
- Verifies `dotnet build` between groups.
