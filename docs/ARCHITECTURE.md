# Architecture Deep Dive

## The Problem TemperAI Solves

### Context Accumulation

When an AI agent works through a long conversation, it accumulates context from every previous exchange. By phase 5 of a project, it's carrying:

| Source | Estimated Tokens |
|---|---|
| Original PRD | 2,000 |
| Constitution | 1,500 |
| Specification | 3,000 |
| Design | 4,000 |
| Tasks | 2,000 |
| Previous Q&A | 5,000+ |
| **Total** | **17,500+** |

At this point, the agent starts ignoring rules and generating generic code.

### The Solution: Fresh Context Per Phase

Each phase starts with a clean slate. It only receives:
1. The specific task it needs to complete
2. The relevant design section
3. The skills it needs for that task
4. The TemperAI conventions that apply

It does **not** receive:
- The entire PRD
- All previous phase outputs
- Unrelated code files
- Skills it doesn't need

---

## The Orchestrator

The orchestrator (`temper-orchestrator`) is the brain of the system. It:

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
| Estimated tokens | < 3,500 | > 3,500 |

### Quick Path Examples

| Request | Agent | Context |
|---|---|---|
| "Fix null reference in ProductController" | `temper-backend` | ProductController.cs + CreateProduct.cs |
| "Add Description field to Product" | `temper-backend` | Product.cs |
| "Add test for UpdateName" | `temper-tester` | Product.cs + UpdateProduct.cs |

### Full Pipeline Examples

| Request | Starting Phase |
|---|---|
| "Add user authentication" | `temper-spec` (constitution exists) |
| "Add Order management" | `temper-spec` |
| "Add RabbitMQ for events" | `temper-design` |
| "Start a new project" | `temper-discover` |

---

## Skill System

### How Skills Work

Skills are markdown files that teach agents coding standards. They are loaded on-demand:

```
Agent loads skills → Reads skill content → Applies rules to code generation
```

### Skill Loading

| Agent | Skills |
|---|---|
| `temper-discover` | None |
| `temper-constitution` | `prd-analyzer` |
| `temper-spec` | `prd-analyzer` |
| `temper-design` | `dotnet-csharp`, `architecture/[chosen]` + `backend/dotnet/api` |
| `temper-tasks` | None |
| `temper-plan` | None |
| `temper-backend` | `dotnet-csharp` + `backend/dotnet/api` + `backend/dotnet/ef-core` + `backend/dotnet/linq` + `backend/dotnet/ddd` (on demand) + `architecture/[chosen]` |
| `temper-frontend` | `dotnet-csharp` + `frontend/blazor` |
| `temper-tester` | `dotnet-csharp` + `backend/dotnet/testing` |
| `temper-devops` | `devops/docker` + `devops/github-actions` |
| `temper-review` | `dotnet-csharp` + `backend/dotnet/api` + `architecture/[chosen]` |
| `temper-docs` | None |
| `temper-orchestrator` | None (spawns sub-agents, does not load skills) |

### Skill Structure

Each skill contains:
- **When to use / When NOT to use**
- **Folder structure**
- **Code patterns** with examples
- **Absolute rules** that must never be broken
- **Anti-patterns** to avoid

---

## Token Budget

### Default Limits

| Phase | Max Tokens |
|---|---|
| `temper-discover` | 5,000 |
| `temper-constitution` | 7,000 |
| `temper-spec` | 9,000 |
| `temper-design` | 14,000 |
| `temper-tasks` | 14,000 |
| `temper-plan` | 10,000 |
| Build (per group) | 5,000 |
| `temper-review` | 18,000 |
| `temper-docs` | 16,000 |
| **Total** | **88,000** |

### Quick Path Savings

| Request | Full Pipeline | Quick Path | Savings |
|---|---|---|---|
| Bug fix | 83,000 | 2,300 | 97% |
| Add property | 83,000 | 1,500 | 98% |
| Add test | 83,000 | 3,500 | 96% |

---

## Snapshot & Rollback

### How Snapshots Work

Before each phase completes successfully, a snapshot of all `.temper/` files is saved to `.temper/.snapshots/[timestamp]_[phase]/`.

### What Gets Snapshotted

- `constitution.md`
- `specs/` (entire directory)
- `design.md`
- `tasks/` (entire directory)
- `budget.md`

### Rollback Process

1. User rejects phase output
2. System offers to restore last snapshot
3. User confirms
4. All `.temper/` files are restored from snapshot
5. User can retry the phase or switch approach

---

## Incremental Updates

### How Change Detection Works

1. Compare current `.temper/` files against the last snapshot
2. Identify which files have changed
3. Use the dependency graph to determine affected phases
4. Cascade: if `constitution.md` changes, all downstream phases need re-running

### Dependency Graph

```
constitution.md → specs/ → design.md → tasks/ → build → review → docs
```

### Cascade Rules

| Changed File | Phases That Need Re-running |
|---|---|
| `constitution.md` | specs → design → tasks → build → review → docs |
| `specs/` | design → tasks → build → review → docs |
| `design.md` | tasks → build → review → docs |
| `tasks/` | build → review → docs |

---

## Parallel Task Execution

### How It Works

1. Build orchestrator reads `tasks/INDEX.md` and builds a dependency graph
2. Groups tasks with no mutual dependencies
3. Spawns sub-agents for each group simultaneously
4. Waits for all tasks in the group to complete
5. Proceeds to the next group

### Token Impact

Parallel execution does **not** increase total token usage — it decreases wall-clock time.

| Approach | Total Tokens | Wall-Clock Time |
|---|---|---|
| Sequential | 20,000 | 40 minutes |
| Parallel (3 groups) | 20,000 | 15 minutes |

---

## Custom Skills

Teams can create their own skills for company-specific conventions:

```cmd
temper-ai skill --create --name mycompany-standards --category backend
```

This generates:
```
assets/skills/backend/mycompany-standards/
├── SKILL.md          ← Rules, patterns, templates
├── metadata.json     ← Name, version, author, dependencies
└── README.md         ← Documentation
```

Skills can be shared via GitHub repos or internal registries and installed with:

```cmd
temper-ai skill --install /path/to/skill
```

---

## Project: TemperAI.NeuralCore

NeuralCore is the session tracking and observation system. It records:

- **Sessions** — Work sessions with project, directory, status, summary
- **Observations** — Bugs fixed, decisions made, patterns discovered, configurations changed

### Database

SQLite with tables:
- `Sessions` (Id, Project, Directory, StartedAt, EndedAt, Summary, Status)
- `Observations` (Id, SessionId, Type, Title, Content, Project, TopicKey, RevisionCount, CreatedAt, UpdatedAt)

### Architecture

Clean Architecture with:
- `Domain/` — Entities, Enums, Events
- `Application/` — Use cases, Result pattern
- `Infrastructure/` — EF Core, repositories, UnitOfWork
- `Api/` — Controllers
