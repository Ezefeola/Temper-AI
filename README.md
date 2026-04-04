# TemperAI

> AI-powered Spec-Driven Development ecosystem for .NET 10 developers.

TemperAI transforms how developers build .NET applications by combining **Spec-Driven Development** with **specialized AI agents**. Instead of "ask and pray", TemperAI structures AI work as an assembly line of experts — each agent handles a specific phase with fresh context, minimal token usage, and maximum precision.

---

## Quick Start

```powershell
# 1. Install the CLI globally
powershell -ExecutionPolicy Bypass -File .\install.ps1

# 2. Install skills and agents into your AI assistant
temper-ai install

# 3. Open your AI assistant (OpenCode, Copilot, Claude Code)
# 4. Start a new project
@temper-init

I want to build a task management system...
```

---

## Core Philosophy

### The Problem

Traditional AI coding assistants accumulate context across long conversations. By phase 5 of a project, the agent is carrying 17,500+ tokens of context — the original PRD, constitution, spec, design, tasks, and all previous Q&A. The result: the agent starts ignoring rules and generating generic code.

### The Solution

**Spec-Driven Development (SDD)** — a structured workflow where:

1. **Each phase starts fresh** — no accumulated context from previous phases
2. **Specialized agents** — each agent handles one specific task with only the context it needs
3. **Quality gates** — each phase requires user approval before proceeding
4. **Token efficiency** — quick path for simple changes, full pipeline for complex features

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
              │            │      │ build → review → │
              └────────────┘      │ docs            │
                                  └─────────────────┘
```

### Decision Matrix

| Criteria | Quick Path | Full Pipeline |
|---|---|---|
| Files affected | 1-2 | 3+ |
| New entities | No | Yes |
| Architectural change | No | Yes |
| Business logic complexity | Low | High |
| Estimated tokens | < 3,500 | > 3,500 |

---

## Project Structure

```
temper-ai/
├── docs/                           ← Documentation
│   ├── ARCHITECTURE.md
│   ├── GETTING_STARTED.md
│   ├── WORKFLOW.md
│   ├── SKILLS.md
│   ├── AGENTS.md
│   ├── CLI.md
│   └── CONVENTIONS.md
│
├── assets/                         ← Embedded resources (installed into AI agents)
│   ├── skills/
│   │   ├── backend/
│   │   │   ├── dotnet/
│   │   │   │   ├── api/           ← Universal .NET API standards
│   │   │   │   ├── ef-core/       ← EF Core entity config, repositories
│   │   │   │   ├── linq/          ← LINQ query patterns
│   │   │   │   ├── ddd/           ← Domain-Driven Design patterns
│   │   │   │   └── testing/       ← xUnit, Moq, bUnit standards
│   │   │   └── architecture/
│   │   │       ├── shared/        ← Rules common to ALL architectures
│   │   │       ├── clean/         ← Clean Architecture structure
│   │   │       ├── hexagonal/     ← Hexagonal (Ports & Adapters)
│   │   │       ├── vertical-slice/← Vertical Slice for CRUDs
│   │   │       └── onion/         ← Onion Architecture
│   │   ├── frontend/
│   │   │   ├── blazor/            ← Blazor WASM component standards
│   │   │   └── bunit/             ← Blazor component testing
│   │   └── devops/
│   │       ├── docker/            ← Docker multi-stage, compose
│   │       ├── github-actions/    ← CI/CD workflows
│   │       └── ci-cd/             ← Deployment strategies
│   │
│   ├── agents/                     ← Agent definitions
│   │   ├── temper-init.agent.md   ← Phase 1: PRD + Constitution
│   │   ├── temper-spec.agent.md   ← Phase 2: User Stories
│   │   ├── temper-design.agent.md ← Phase 3: Architecture Design
│   │   ├── temper-tasks.agent.md  ← Phase 4: Task Breakdown
│   │   ├── temper-build.agent.md  ← Phase 5: Build Orchestrator
│   │   ├── temper-backend.agent.md← Phase 5a: Backend Implementation
│   │   ├── temper-frontend.agent.md← Phase 5b: Frontend Implementation
│   │   ├── temper-tester.agent.md ← Phase 5c: Test Implementation
│   │   ├── temper-devops.agent.md ← Phase 5d: DevOps Implementation
│   │   ├── temper-review.agent.md ← Phase 6: Code Review
│   │   ├── temper-docs.agent.md   ← Phase 7: Documentation
│   │   └── temper-orchestrator.agent.md ← Main Orchestrator
│   │
│   └── commands/                   ← Slash commands for AI agents
│       ├── temper-init.md
│       ├── temper-next.md
│       └── temper-status.md
│
├── src/
│   ├── TemperAI.Cli/              ← CLI application (temper-ai.exe)
│   │   ├── Commands/
│   │   │   ├── InstallCommand.cs
│   │   │   ├── UpdateCommand.cs
│   │   │   ├── StatusCommand.cs
│   │   │   ├── BudgetCommand.cs
│   │   │   ├── SnapshotCommand.cs
│   │   │   ├── IncrementalCommand.cs
│   │   │   ├── SkillCommand.cs
│   │   │   ├── SetupCommand.cs
│   │   │   └── MenuCommand.cs
│   │   └── Program.cs
│   │
│   ├── TemperAI.Core/             ← Shared core library
│   │   ├── Assets/
│   │   │   └── EmbeddedAssets.cs  ← Reads embedded skill/agent files
│   │   ├── Configuration/
│   │   │   └── AgentTargets.cs    ← Supported AI agents config
│   │   ├── Models/
│   │   ├── Snapshots/
│   │   │   ├── SnapshotService.cs ← Snapshot/rollback logic
│   │   │   ├── SnapshotInfo.cs
│   │   │   └── SnapshotResult.cs
│   │   ├── Incremental/
│   │   │   ├── IncrementalUpdateService.cs ← Change detection
│   │   │   ├── IncrementalResult.cs
│   │   │   └── PhaseDependency.cs
│   │   └── Skills/
│   │       ├── SkillCreatorService.cs ← Skill creation/installation
│   │       ├── SkillMetadata.cs
│   │       ├── SkillInfo.cs
│   │       └── SkillResult.cs
│   │
│   ├── TemperAI.Installer/        ← Installation engine
│   │   ├── InstallerService.cs    ← Copies assets to agent directories
│   │   └── InstallResult.cs
│   │
│   └── TemperAI.NeuralCore/       ← Session tracking & observations
│       ├── Domain/
│       ├── Application/
│       ├── Infrastructure/
│       └── Api/
│
├── install.ps1                    ← Global CLI installer script
├── AGENTS.md                      ← Agent routing index (read by AI)
├── TEMPER_AI_ARCHITECTURE.md      ← Architecture documentation
└── TemperAI.slnx                  ← Solution file
```

---

## Key Concepts

### Skills

Skills are markdown files (`SKILL.md`) that teach AI agents coding standards, patterns, and conventions. Each skill contains:

- **When to use** — conditions for loading the skill
- **Folder structure** — expected project layout
- **Code patterns** — examples of correct implementation
- **Absolute rules** — conventions that must never be broken
- **Anti-patterns** — what to avoid

Skills are loaded **on-demand** — only the skills needed for the current task are loaded.

### Agents

Agents are markdown files (`.agent.md`) that define the role, workflow, and behavior of specialized AI sub-agents. Each agent:

- Has a specific responsibility (init, spec, design, build, review, etc.)
- Loads only the skills it needs
- Receives only the context files it needs
- Produces a specific output and stops

### The `.temper/` Directory

Each project gets a `.temper/` directory that tracks the SDD workflow state:

| File | Purpose |
|---|---|
| `constitution.md` | Project decisions — stack, architecture, standards |
| `spec.md` | User stories, acceptance criteria, edge cases |
| `design.md` | Architecture, entities, endpoints, database schema |
| `tasks.md` | Atomic implementation tasks with dependencies |
| `budget.md` | Token usage tracking per phase |
| `.snapshots/` | Automatic snapshots for rollback |

---

## Supported AI Agents

TemperAI installs skills and agents into:

| Agent | Skills Path | Agents Path |
|---|---|---|
| **GitHub Copilot CLI** | `~/.copilot/skills` | `~/.copilot/agents` |
| **Claude Code** | `~/.claude/skills` | `~/.claude/agents` |
| **OpenCode** | `~/.config/opencode/skills` | `~/.config/opencode/agent` |

---

## CLI Commands

| Command | Description |
|---|---|
| `temper-ai` | Interactive menu (default when run without arguments) |
| `temper-ai install` | Install skills and agents into AI assistants |
| `temper-ai update` | Update installed skills and agents |
| `temper-ai status` | Show installation status |
| `temper-ai budget` | View token usage tracking |
| `temper-ai snapshot` | Manage snapshots for rollback |
| `temper-ai incremental` | Detect which phases need re-running |
| `temper-ai skill` | Create, install, and discover custom skills |
| `temper-ai setup` | Install CLI to global PATH |

---

## Code Conventions (Summary)

- **Never** primary constructors — always explicit constructors
- **Never** return expression `=>` on methods — always braces `{}`
- **Never** `DataAnnotations` on entities
- **Never** `throw` for business validations — use Result pattern
- **Never** `.Update()` in EF Core — change tracker detects changes
- **Never** `async void` — always `async Task`
- **Never** `.Result` or `.Wait()` — always `await`
- **Never** `using static` — always explicit `using` directives
- **Never** named usings — rename the entity or use fully qualified namespace
- **Never** global usings — always per-file `using` directives
- **Never** `var` — always explicit type declaration
- **Never** fully qualified type names in code — always use `using` directives
- **Never** hardcoded numbers in validators — always use `Entity.Rules` constants
- **Never** break short lines unnecessarily
- **Always** write code in English (except user-facing error messages)
- **Always** `sealed` on classes that are not inherited
- **Always** `CancellationToken` on public async methods
- **Always** variable names matching their type
- **Always** DTOs as `sealed record` with explicit properties and `Dto` suffix
- **Always** API and Frontend in separate solutions

---

## Documentation Index

| Document | Content |
|---|---|
| [GETTING_STARTED.md](docs/GETTING_STARTED.md) | Installation, setup, first project |
| [WORKFLOW.md](docs/WORKFLOW.md) | Complete SDD workflow step by step |
| [SKILLS.md](docs/SKILLS.md) | All skills reference |
| [AGENTS.md](docs/AGENTS.md) | All agents reference |
| [CLI.md](docs/CLI.md) | CLI commands reference |
| [CONVENTIONS.md](docs/CONVENTIONS.md) | Complete code conventions |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Technical architecture deep dive |
