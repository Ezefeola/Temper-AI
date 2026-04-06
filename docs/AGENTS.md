# Agents Reference

Agents are specialized AI sub-agents that handle specific phases of the SDD workflow. Each agent has a defined role, workflow, and set of skills it loads.

---

## temper-init

**Phase:** 1 — Initialization
**Skills:** `dotnet-csharp`, `prd-analyzer`

**Role:** Read or build the PRD, ask clarifying questions, generate `.temper/constitution.md`.

**Workflow:**
1. Check if `PRD.md` exists.
2. If yes, read and analyze for ambiguities.
3. If no, build collaboratively with the user.
4. Ask ALL necessary questions before writing any file.
5. Generate `.temper/constitution.md`.
6. Show summary and request approval.

**Output:** `.temper/constitution.md`

---

## temper-spec

**Phase:** 2 — Specification
**Skills:** `dotnet-csharp`, `prd-analyzer`

**Role:** Generate user stories, acceptance criteria, edge cases, and non-functional requirements.

**Workflow:**
1. Read `.temper/constitution.md`.
2. Identify user stories from the constitution.
3. Define acceptance criteria for each story.
4. Document edge cases and error cases.
5. Define non-functional requirements.
6. Generate `.temper/spec.md`.
7. Request approval.

**Output:** `.temper/spec.md`

---

## temper-design

**Phase:** 3 — Architecture Design
**Skills:** `dotnet-csharp`, `architecture/[chosen]` + `backend/dotnet/api`

**Role:** Design the complete architecture — entities, endpoints, database schema, components.

**Workflow:**
1. Read `constitution.md` and `spec.md`.
2. Design domain entities with properties, relationships, factory methods.
3. Design API endpoints with HTTP methods, routes, DTOs.
4. Design database schema.
5. Design Blazor components (if applicable).
6. Generate `.temper/design.md`.
7. Request approval.

**Output:** `.temper/design.md`

---

## temper-tasks

**Phase:** 4 — Task Breakdown
**Skills:** None

**Role:** Break the design into atomic, trackable implementation tasks.

**Workflow:**
1. Read `constitution.md`, `spec.md`, and `design.md`.
2. Create atomic tasks (1-3 files each).
3. Define dependencies between tasks.
4. Assign each task to an agent (backend, frontend, tester, devops).
5. Define completion criteria for each task.
6. Generate `.temper/tasks.md`.
7. Request approval.

**Output:** `.temper/tasks.md`

---

## temper-plan

**Phase:** 5 — Build Planner
**Skills:** None

**Role:** Analyze task dependencies, group tasks for parallel execution, and generate `.temper/build-plan.md`.

**Workflow:**
1. Read `tasks.md` and `design.md`.
2. Build dependency graph.
3. Group independent tasks for parallel execution.
4. Assign agents to each group (backend, frontend, tester, devops).
5. Estimate token cost per group.
6. Generate `.temper/build-plan.md`.
7. Request approval.

**Output:** `.temper/build-plan.md`

---

## Build Execution (handled by `temper-orchestrator`)

**Phase:** 5 (execution step)
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
1. Read `tasks.md` and filter for pending backend tasks.
2. Take ONE task at a time.
3. Read `design.md` for the relevant section.
4. Implement following TemperAI conventions strictly.
5. Show code and request approval.
6. Mark task as `done`.
7. Proceed to next task.

---

## temper-frontend

**Phase:** 5b — Frontend Implementation
**Skills:** `dotnet-csharp` + `frontend/blazor`

**Role:** Implement Blazor components, pages, and services (Server or WebAssembly).

**Workflow:**
1. Read `tasks.md` and filter for pending frontend tasks.
2. Take ONE task at a time.
3. Read `design.md` for the relevant component design.
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
1. Read `tasks.md` and filter for pending tester tasks.
2. Take ONE task at a time.
3. Read `spec.md` for acceptance criteria and edge cases.
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
1. Read `tasks.md` and filter for pending devops tasks.
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
1. Read `spec.md` and `design.md`.
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
