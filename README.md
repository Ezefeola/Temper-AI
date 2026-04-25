# TemperAI

> AI-powered Spec-Driven Development ecosystem for .NET 10 developers.

TemperAI transforms how developers build .NET applications by combining **Spec-Driven Development** with **specialized AI agents**. Instead of "ask and pray", TemperAI structures AI work as an assembly line of experts — each agent handles a specific phase with fresh context, minimal token usage, and maximum precision.

---

## Quick Start

```powershell
# 1. Install the CLI globally (includes NeuralCore MCP server)
powershell -ExecutionPolicy Bypass -File .\install.ps1

# 2. Install skills, agents, and configure NeuralCore into your AI assistant
temper-ai install

# 3. Open your AI assistant (OpenCode, Copilot, Claude Code)
# 4. Start a new project
@temper-analyst

I want to build a task management system...
```

---

## Development Workflow

After making changes to the CLI, agents, or skills:

```powershell
# From the source directory (temper-ai)
cd C:\Users\...\Projects\AI\temper-ai

# Run install.ps1
.\install.ps1

# Now you're ready to continue working
temper-ai update   # Updates agents with latest changes
```

The CLI automatically detects when you're in the source directory and warns if the installed version is outdated:

---

## Core Philosophy

### The Problem

Traditional AI coding assistants accumulate context across long conversations. By phase 5 of a project, the agent is carrying 17,500+ tokens of context — the original PRD, constitution, spec, design, tasks, and all previous Q&A. The result: the agent starts ignoring rules and generating generic code.

### The Solution

**Spec-Driven Development (SDD)** — a structured workflow where:

1. **Ephemeral orchestrator** — each phase runs in a fresh session with zero accumulated context
2. **State persistence** — `.temper/orchestrator-state.md` tracks progress between sessions
3. **Specialized agents** — each agent handles one specific task with only the context it needs
4. **Quality gates** — each phase requires user approval before proceeding
5. **Token efficiency** — quick path for simple changes, full pipeline for complex features
6. **Persistent memory** — NeuralCore MCP server saves observations between sessions

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
               │ Direct     │      │ analyst → architect → │
               │ execution  │      │ spec → design → tasks → │
               │            │      │ plan → [orch.  │
               │            │      │  executes] →    │
               └────────────┘      │ review → docs   │
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
│   │   ├── README.md
│   │   ├── dotnet-csharp/
│   │   │   └── SKILL.md            ← Universal C# / .NET 10 standards
│   │   ├── prd-analyzer/
│   │   │   └── SKILL.md            ← PRD analysis & constitution generation
│   │   ├── token-budget/
│   │   │   └── SKILL.md            ← Token budget management
│   │   ├── backend/
│   │   │   ├── dotnet/
│   │   │   │   ├── api/            ← ASP.NET Core API standards
│   │   │   │   ├── ef-core/        ← EF Core entity config, repositories
│   │   │   │   ├── linq/           ← LINQ query patterns
│   │   │   │   ├── ddd/            ← Domain-Driven Design patterns
│   │   │   │   └── testing/        ← xUnit, Moq, bUnit standards
│   │   │   └── architecture/
│   │   │       ├── shared/         ← Rules common to ALL architectures
│   │   │       ├── clean/          ← Clean Architecture structure
│   │   │       ├── hexagonal/      ← Hexagonal (Ports & Adapters)
│   │   │       ├── vertical-slice/ ← Vertical Slice for CRUDs
│   │   │       └── onion/          ← Onion Architecture
│   │   ├── frontend/
│   │   │   ├── blazor/             ← Blazor component standards (Server & WASM)
│   │   │   └── bunit/              ← Blazor component testing
│   │   └── devops/
│   │       ├── docker/             ← Docker multi-stage, compose
│   │       ├── github-actions/     ← CI/CD workflows
│   │       └── ci-cd/              ← Deployment strategies
│   │
│   ├── agents/                     ← Agent definitions
│   │   ├── README.md
│   │   ├── temper-orchestrator.agent.md  ← Main orchestrator
│   │   ├── temper-analyst.agent.md       ← Phase 1: Functional analysis
│   │   ├── temper-architect.agent.md     ← Phase 2: Technical architecture
│   │   ├── temper-spec.agent.md          ← Phase 3: User Stories
│   │   ├── temper-design.agent.md        ← Phase 4: Architecture Design
│   │   ├── temper-tasks.agent.md         ← Phase 5: Task Breakdown
│   │   ├── temper-plan.agent.md           ← Phase 6: Build Planner
│   │   ├── temper-backend.agent.md       ← Phase 6a: Backend Implementation
│   │   ├── temper-frontend.agent.md       ← Phase 6b: Frontend Implementation
│   │   ├── temper-tester.agent.md         ← Phase 6c: Test Implementation
│   │   ├── temper-devops.agent.md         ← Phase 6d: DevOps Implementation
│   │   ├── temper-review.agent.md         ← Phase 7: Code Review
│   │   └── temper-docs.agent.md           ← Phase 8: Documentation
│   │
│   ├── commands/                   ← Slash commands for AI agents
│   │   ├── temper-init.md
│   │   ├── temper-next.md
│   │   └── temper-status.md
│   │
│   └── config/
│       └── README.md
│
├── src/
│   ├── TemperAI.Core/              ← Shared core library
│   │   ├── Assets/
│   │   │   └── EmbeddedAssets.cs   ← Reads embedded skill/agent files
│   │   ├── Configuration/
│   │   │   └── AgentTargets.cs     ← Supported AI agents config
│   │   ├── Models/
│   │   │   ├── AgentTarget.cs
│   │   │   ├── InstallResult.cs
│   │   │   └── SaveResult.cs
│   │   ├── Snapshots/
│   │   │   ├── SnapshotService.cs  ← Snapshot/rollback logic
│   │   │   ├── SnapshotInfo.cs
│   │   │   └── SnapshotResult.cs
│   │   ├── Incremental/
│   │   │   ├── IncrementalUpdateService.cs ← Change detection
│   │   │   ├── IncrementalResult.cs
│   │   │   └── PhaseDependency.cs
│   │   └── Skills/
│   │       ├── SkillCreatorService.cs ← Skill creation/installation
│   │       ├── SkillInfo.cs
│   │       ├── SkillMetadata.cs
│   │       └── SkillResult.cs
│   │
│   ├── TemperAI.Installer/         ← Installation engine
│   │   ├── InstallerService.cs     ← Copies assets to agent directories
│   │   └── TemperAI.Installer.csproj
│   │
│   └── TemperAI.NeuralCore/        ← Session tracking & MCP memory server
│       ├── Domain/
│       │   ├── Common/
│       │   │   └── Primitives/
│       │   │       └── Entity.cs
│       │   └── Entities/
│       │       ├── Sessions/
│       │       │   ├── Session.cs
│       │       │   └── Enums/
│       │       └── Observations/
│       │           ├── Observation.cs
│       │           └── Enums/
│       ├── Application/
│       │   ├── Common/
│       │   │   └── Result.cs
│       │   ├── UseCases/
│       │   └── DependencyInjection.cs
│       ├── Infrastructure/
│       │   ├── Persistence/
│       │   │   ├── NeuralCoreDbContext.cs
│       │   │   ├── IUnitOfWork.cs
│       │   │   ├── UnitOfWork.cs
│       │   │   ├── Configurations/
│       │   │   └── Repositories/
│       │   └── DependencyInjection.cs
│       ├── Api/
│       │   └── Controllers/
│       ├── Mcp/
│       │   ├── McpServer.cs
│       │   └── Tools/
│       │       ├── MemSaveTool.cs
│       │       ├── MemSearchTool.cs
│       │       ├── MemContextTool.cs
│       │       └── MemSessionSummaryTool.cs
│       └── Program.cs
│
├── tests/
│   ├── TemperAI.NeuralCore.Domain.UnitTests/
│   │   └── Entities/
│   │       ├── Observations/
│   │       │   └── ObservationTests.cs
│   │       └── Sessions/
│   │           └── SessionTests.cs
│   └── TemperAI.Installer.UnitTests/
│
├── install.ps1                    ← Global CLI installer script
├── AGENTS.md                      ← Agent routing index (read by AI)
├── TEMPER_AI_ARCHITECTURE.md      ← Architecture documentation
├── TEMPER_AI_PROYECTO.md          ← Complete project documentation (Spanish)
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
| `prd.md` | Functional requirements — scope, business rules, capabilities |
| `backend-config.md` | Technical decisions — architecture, database, auth |
| `frontend-config.md` | Frontend decisions — Blazor type, API URL |
| `specs/INDEX.md` | Fast-lookup index of all user stories |
| `specs/US-XXX-*.md` | Individual user story files |
| `design.md` | Architecture, entities, endpoints, database schema |
| `tasks/INDEX.md` | Fast-lookup index of all tasks |
| `tasks/US-XXX/T###-*.md` | Individual task files grouped by user story |
| `build-plan.md` | Build execution plan with parallel groups |
| `orchestrator-state.md` | Persistent state for the ephemeral orchestrator |
| `budget.md` | Token usage tracking per phase |
| `.snapshots/` | Automatic snapshots for rollback |

---

## NeuralCore — Persistent Memory

NeuralCore is an MCP (Model Context Protocol) server that provides persistent memory across AI sessions. It allows agents to save and recall observations, decisions, and patterns between sessions.

### How it works

1. **Auto-start:** NeuralCore starts automatically when you open your AI assistant (OpenCode, Claude Code, Copilot).
2. **Auto-stop:** NeuralCore shuts down when you close your AI session.
3. **Persistent storage:** Observations are saved to SQLite and indexed for fast retrieval.
4. **MCP tools:** Agents use `mem_save`, `mem_search`, `mem_context`, and `mem_session_summary` tools.

### Setup

```powershell
# During installation, say "yes" to NeuralCore
temper-ai install

# Or manage NeuralCore separately
temper-ai neuralcore
```

The `neuralcore` menu provides:
- **Status** — Check if NeuralCore is published and configured
- **Test** — Run connectivity test
- **Publish** — Compile NeuralCore as a standalone executable
- **Install** — Configure MCP in your AI agents
- **Logs** — View server logs

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
| `temper-ai install` | Install skills, agents, and optionally NeuralCore |
| `temper-ai update` | Update installed skills and agents |
| `temper-ai status` | Show installation status |
| `temper-ai neuralcore` | Manage NeuralCore MCP server (publish, install, status, test) |
| `temper-ai neural` | Save/recall observations with NeuralCore |
| `temper-ai doctor` | Diagnose and repair installation issues |
| `temper-ai budget` | View token usage tracking |
| `temper-ai snapshot` | Manage snapshots for rollback |
| `temper-ai incremental` | Detect which phases need re-running |
| `temper-ai skill` | Create, install, and discover custom skills |
| `temper-ai menu` | Interactive menu with all commands |
| `temper-ai uninstall` | Uninstall completely |
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
- **Never** named usings — rename the entity or use a plural folder name
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
