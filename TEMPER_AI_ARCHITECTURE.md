# TemperAI — Architecture & Agent Orchestration

## Philosophy: Engineering Applied to AI

Instead of "ask and pray", we structure AI work as an **assembly line of specialists**. Spec-Driven Development applies software engineering principles to AI workflows.

### Core Principles

1. **Divide and conquer** — orchestrator coordinates, sub-agents execute
2. **Fresh context** — each sub-agent starts with a clean slate
3. **Minimal context** — only load what's needed, when it's needed
4. **Lazy spawning** — never create sub-agents unless the task complexity warrants it
5. **Quality gates** — each phase validates before the next begins

---

## The Problem We Solve

### Problem 1: Context Accumulation

When an agent works through a long conversation, it accumulates context from every previous exchange. By phase 5, it's carrying:
- The original PRD (2000 tokens)
- Constitution (1500 tokens)
- Spec (3000 tokens)
- Design (4000 tokens)
- Tasks (2000 tokens)
- All previous Q&A (5000+ tokens)

**Result:** 17,500+ tokens of context before writing a single line of code. The agent starts ignoring rules and generating generic code.

**Solution:** Each sub-agent starts fresh with only what it needs.

### Problem 2: Over-Engineering Simple Tasks

A bug fix that changes one line does not need:
- A spec document
- A design document
- A task breakdown
- A review agent

**Result:** Wasted tokens, wasted time, user frustration.

**Solution:** The orchestrator evaluates complexity and chooses the right path.

### Problem 3: Skill Overloading

Loading all skills (Clean Architecture, EF Core, DDD, Blazor, Docker, Testing) for every request.

**Result:** Massive system prompt, most of it irrelevant to the current task.

**Solution:** Skills are loaded on-demand based on the task type.

---

## Architecture Overview

```
                    ┌─────────────────────┐
                    │   temper-orchestrator│
                    │   (evaluates, decides)│
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │   Complexity Check   │
                    └──────────┬──────────┘
                               │
                    ┌──────────┴──────────┐
                    │                     │
              ┌─────▼─────┐       ┌──────▼──────┐
              │ Quick Path │       │ Full Pipeline│
              │ (1 agent)  │       │ (SDD phases) │
              └─────┬──────┘       └──────┬──────┘
                    │                     │
               ┌─────▼──────┐      ┌──────▼──────────┐
               │ Direct     │      │ init → spec →   │
               │ execution  │      │ design → tasks → │
               │            │      │ plan → [orch.  │
               │            │      │  executes] →    │
               └────────────┘      │ review → docs   │
                                   └─────────────────┘
```

---

## The Orchestrator

The orchestrator (`temper-orchestrator.agent.md`) is the brain. It:

1. **Receives the user request**
2. **Evaluates complexity** using the decision matrix
3. **Chooses the path** — quick or full pipeline
4. **Spawns the right agent** with minimal context
5. **Waits for completion** and reports back

### Decision Matrix

| Criteria | Quick Path | Full Pipeline |
|---|---|---|
| Files affected | 1-2 | 3+ |
| New entities | No | Yes |
| Architectural change | No | Yes |
| Business logic complexity | Low | High |
| User explicitly requests pipeline | No | Yes |

### Quick Path Examples

- "Fix null reference in ProductController" → `temper-backend` directly
- "Add Description field to Product" → `temper-backend` directly
- "Add test for UpdateName" → `temper-tester` directly
- "Change connection string" → `temper-devops` directly
- "How does UnitOfWork work?" → Answer directly

### Full Pipeline Examples

- "Add user authentication" → Full SDD pipeline
- "Add Order management with items and payments" → Full SDD pipeline
- "Add RabbitMQ for order events" → Full SDD pipeline
- "Start a new project from scratch" → Full SDD pipeline

---

## Sub-Agent Context Isolation

### What Each Agent Receives

| Agent | Receives | Does NOT Receive |
|---|---|---|
| `temper-init` | PRD.md (if exists) | Nothing else |
| `temper-spec` | `.temper/constitution.md` | Design, tasks, code |
| `temper-design` | `constitution.md` + `spec.md` | Tasks, code |
| `temper-tasks` | `constitution.md` + `spec.md` + `design.md` | Code |
| `temper-plan` | `tasks.md` + `design.md` | Code |
| `temper-backend` | `tasks.md` + `design.md` + relevant files | Full codebase |
| `temper-review` | `spec.md` + `design.md` + generated code | PRD, constitution |
| `temper-docs` | All `.temper/` files | Implementation details |

### Skill Loading Per Agent

| Agent | Skills Loaded |
|---|---|
| `temper-init` | `prd-analyzer` |
| `temper-spec` | `prd-analyzer` |
| `temper-design` | `architecture/[chosen]` + `backend/dotnet/api` |
| `temper-tasks` | None (reads `.temper/` files) |
| `temper-plan` | None (reads `.temper/` files) |
| `temper-backend` | `backend/dotnet/api` + `backend/dotnet/ef-core` + `architecture/[chosen]` |
| `temper-frontend` | `frontend/blazor` |
| `temper-tester` | `backend/dotnet/testing` |
| `temper-devops` | `devops/docker` + `devops/github-actions` |
| `temper-review` | `backend/dotnet/api` + `architecture/[chosen]` |
| `temper-docs` | None |

---

## Token Budget Management

### Estimated Token Usage Per Phase

| Phase | Input Tokens | Output Tokens | Total |
|---|---|---|---|
| `temper-init` | 2,000-4,000 | 1,500-3,000 | 3,500-7,000 |
| `temper-spec` | 1,500-3,000 | 3,000-6,000 | 4,500-9,000 |
| `temper-design` | 3,000-6,000 | 4,000-8,000 | 7,000-14,000 |
| `temper-tasks` | 5,000-10,000 | 2,000-4,000 | 7,000-14,000 |
| `temper-plan` | 3,000-6,000 | 2,000-4,000 | 5,000-10,000 |
| Build (per group) | 1,000-3,000 | 500-2,000 | 1,500-5,000 |
| `temper-review` | 5,000-15,000 | 1,000-3,000 | 6,000-18,000 |
| `temper-docs` | 5,000-10,000 | 3,000-6,000 | 8,000-16,000 |

### Quick Path Token Usage

| Request Type | Input Tokens | Output Tokens | Total |
|---|---|---|---|
| Bug fix | 500-1,500 | 200-800 | 700-2,300 |
| Add property | 500-1,000 | 200-500 | 700-1,500 |
| Add test | 1,000-2,000 | 500-1,500 | 1,500-3,500 |
| Config change | 200-500 | 100-300 | 300-800 |

**Savings:** Quick path uses 80-95% fewer tokens than the full pipeline for simple tasks.

---

## The DAG: Dependency Flow

```
                     ┌─────────┐
                     │  Init   │
                     └────┬────┘
                          │
                     ┌────▼────┐
                     │  Spec   │
                     └────┬────┘
                          │
                     ┌────▼────┐
                     │ Design  │
                     └────┬────┘
                          │
                     ┌────▼────┐
                     │  Tasks  │
                     └────┬────┘
                          │
                     ┌────▼────┐
                     │  Plan   │
                     └────┬────┘
                          │
          ┌───────────────┼───────────────┐
          │               │               │
     ┌────▼────┐     ┌────▼────┐     ┌────▼────┐
     │ Backend │     │Frontend │     │ DevOps  │
     └────┬────┘     └────┬────┘     └────┬────┘
          │               │               │
          └───────────────┼───────────────┘
                          │
                     ┌────▼────┐
                     │ Tester  │
                     └────┬────┘
                          │
                     ┌────▼────┐
                     │ Review  │
                     └────┬────┘
                          │
                     ┌────▼────┐
                     │  Docs   │
                     └─────────┘
```

**Execution model:** The orchestrator reads the build plan and spawns sub-agents one group at a time. Each sub-agent runs in a separate conversation with clean context. Backend, Frontend, and DevOps can run in parallel within a group. Tester runs after the code it tests is built.

---

## Quality Gates

Each phase has a quality gate:

1. **Init gate:** Constitution approved by user
2. **Spec gate:** User stories and acceptance criteria approved
3. **Design gate:** Architecture and API design approved
4. **Tasks gate:** Task breakdown approved
5. **Plan gate:** Build plan approved
6. **Build gate:** Each group verified with `dotnet build`
7. **Review gate:** All convention checks pass
8. **Docs gate:** Documentation approved

**No phase proceeds without passing its gate.**

---

## Memory & State

### Persistent State (`.temper/` directory)

- `constitution.md` — project decisions
- `spec.md` — requirements
- `design.md` — architecture
- `tasks.md` — implementation tasks
- `build-plan.md` — execution plan with parallel groups

### Ephemeral State (conversation context)

- **Cleared between phases** — each phase starts fresh
- **Only relevant files loaded** — not the entire codebase
- **No accumulated Q&A** — previous conversations are not carried forward

---

## Anti-Patterns to Avoid

### 1. The Monolith Prompt

**Bad:** Loading all skills, all phases, all code into one prompt.
**Good:** Loading only what's needed for the current task.

### 2. The Context Snowball

**Bad:** Each phase adds more context to the conversation.
**Good:** Each phase starts fresh with only its required inputs.

### 3. The Over-Engineered Fix

**Bad:** Running the full pipeline for a one-line bug fix.
**Good:** Using the quick path for simple, isolated changes.

### 4. The Skill Dump

**Bad:** Loading Clean Architecture, EF Core, DDD, Blazor, Docker, and Testing for every request.
**Good:** Loading only the skills relevant to the current task.

### 5. The Blind Sub-Agent

**Bad:** Spawning a sub-agent with the entire codebase as context.
**Good:** Giving the sub-agent only the files it needs to complete its task.

---

## Implementation Notes

### How to Enforce Fresh Context

1. **Separate conversations** — each phase runs in its own conversation/session.
2. **File-based state** — all state is in `.temper/` files, not in conversation history.
3. **Explicit loading** — the orchestrator explicitly loads only the files and skills needed.
4. **No implicit context** — sub-agents do not inherit the orchestrator's conversation.

### How to Enforce Lazy Spawning

1. **Complexity check first** — the orchestrator evaluates before spawning.
2. **Quick path by default** — prefer the simplest path that works.
3. **User override** — user can always request the full pipeline explicitly.
4. **Cost awareness** — the orchestrator reports estimated token usage before spawning.

---

## Future Enhancements

1. **Token budget tracking** — track and report token usage per phase.
2. **Automatic rollback** — if a phase fails, rollback to the last known good state.
3. **Parallel task execution** — run independent tasks in parallel sub-agents.
4. **Incremental updates** — only re-run phases affected by a change.
5. **Skill marketplace** — teams can share and discover skills.
