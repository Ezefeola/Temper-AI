# TemperAI

> The AI-powered Software Development workflow for .NET developers.

TemperAI is an open-source toolkit that installs a complete ecosystem of **agents** and **skills** directly into [OpenCode](https://opencode.ai), turning your AI coding assistant into a structured, opinionated software delivery engine — from requirements all the way to production-ready code.

---

## What is TemperAI?

When you work with an AI coding assistant, you're usually writing prompts from scratch every time. TemperAI changes that. It provides:

- **Agents** — specialized AI personas (analyst, architect, backend developer, tester, DevOps engineer, and more) that are automatically available in OpenCode as sub-agents.
- **Skills** — reusable instruction libraries covering .NET architecture patterns, DDD, EF Core, Blazor, testing, CI/CD, and more. Agents load the right skill at the right moment without you having to explain it.
- **An orchestrator (FRIDAY)** — an intelligent workflow coordinator that routes your requests to the right specialist, tracks state across sessions, and enforces approval gates before any code is written.
- **NeuralCore** — an optional local MCP server that gives your agents persistent memory across sessions using a local SQLite database.

The result is a **structured SDD (Software-Driven Development) workflow** where each piece of work — requirements, architecture, tasks, implementation, tests, review — is handled by the right specialist, with the right context, in the right order.

---

## The SDD Workflow

```
Your idea
    │
    ▼
┌─────────────────────────────────────────────────────────┐
│                     FRIDAY (Orchestrator)                │
│  Classifies requests · Routes to specialists            │
│  Enforces approvals · Preserves workflow state          │
└──────────────────────────┬──────────────────────────────┘
                           │
    ┌──────────────────────┼───────────────────────────┐
    ▼                      ▼                           ▼
temper-analyst      temper-architect            temper-tasks
Phase 1: PRD        Architecture design         Break work into
Phase 2: Specs      Tech stack · Patterns       atomic tasks
                    DDD docs · Config files
                           │
    ┌──────────────────────┼───────────────────────────┐
    ▼                      ▼                           ▼
temper-backend      temper-frontend           temper-tester
.NET domain         Blazor components         xUnit · Moq
Application layer   State management          bUnit · Integration
EF Core · APIs      UI behavior               tests
                           │
    ┌──────────────────────┼───────────────────────────┐
    ▼                      ▼                           ▼
temper-devops       temper-review             temper-docs
Docker              Quality gate              README
GitHub Actions      Convention audit          ARCHITECTURE.md
CI/CD pipelines     Risk analysis             API.md · CHANGELOG
```

---

## Agents

| Agent | Role |
|---|---|
| `temper-friday` | Intelligent orchestrator. Routes requests, proposes plans, tracks workflow state, enforces approval gates. Never implements work directly. |
| `temper-analyst` | Senior functional analyst. Phase 1: elicits requirements and produces a PRD. Phase 2: generates user stories and acceptance criteria from the approved PRD. |
| `temper-architect` | Senior software architect. Designs the system architecture, chooses the stack and patterns, produces backend/frontend config and DDD documentation. |
| `temper-tasks` | Build planner. Converts approved specs and architecture into atomic, trackable implementation tasks organized by user story. |
| `temper-plan` | Execution planner. Analyzes task dependencies and produces an optimized build plan with parallel execution groups. |
| `temper-backend` | .NET backend implementer. Writes production-quality C# following the architecture pattern decided by the architect. |
| `temper-frontend` | Blazor frontend implementer. Implements components, pages, state, forms, and API consumption. |
| `temper-tester` | Test implementer. Writes xUnit unit tests, integration tests, and bUnit component tests. |
| `temper-devops` | DevOps implementer. Generates Dockerfiles, docker-compose, and GitHub Actions CI/CD pipelines. |
| `temper-review` | Quality reviewer. Audits generated code against TemperAI conventions and the approved specifications. |
| `temper-docs` | Documentation generator. Produces README, ARCHITECTURE.md, API.md, and CHANGELOG after the build is complete. |
| `temper-jarvis` | Alternative orchestrator (legacy). |

---

## Skills

Skills are instruction libraries that agents load on demand. They encode conventions, patterns, and rules so every agent follows the same standards without you needing to repeat yourself.

### Backend

| Category | Skills |
|---|---|
| Architecture patterns | Clean Architecture, Hexagonal Architecture, Vertical Slice Architecture, Onion Architecture, Shared architecture rules |
| .NET / C# | C# conventions, ASP.NET Core API, DDD patterns, EF Core (setup, queries, entity config, repository, bulk ops), LINQ, Testing |
| Shared patterns | Result pattern, SOLID & Clean Code, DTO conventions, Use case patterns |
| API documentation | Scalar, Swagger |

### Frontend

| Category | Skills |
|---|---|
| Blazor | Blazor WebAssembly, Blazor Server, bUnit component testing |
| UI libraries | MudBlazor, Tailwind CSS |

### DDD

| Category | Skills |
|---|---|
| Domain modeling | DDD document generation, Ubiquitous language |

### DevOps

| Category | Skills |
|---|---|
| Infrastructure | Docker, GitHub Actions, CI/CD strategy |

### Workflow

| Category | Skills |
|---|---|
| FRIDAY | State schema, Analyst communication, Architect communication, Implementation delegation, Prompt excellence |
| Analyst | Functional analysis, PRD template, Report formats, Analyst reasoning, Spec generator |
| Architect | Proposal formats, Document templates |
| Project management | Token budget, Setup tasks, PRD analyzer |

---

## Requirements

- **Windows** (macOS/Linux support coming soon)
- **OpenCode** installed and configured
- No .NET SDK required — the CLI is self-contained

## Community release model

TemperAI community releases follow a remote-first distribution model:

- The public install is a **global self-contained `temper-ai` CLI**.
- Community installations use **remote assets by default**.
- **Local source mode** is for contributors and development against a checked-out repository.
- `temper-ai update` is the **single user-facing update action** for both the CLI and assets.
- Every public release ships with a **mandatory manifest** that defines the matching CLI and assets pair.
- **`stable`** is the initial public release channel.
- Running `temper-ai` **without arguments** opens the interactive menu as the primary user experience.

See [docs/community-release-model.md](docs/community-release-model.md) for the public release and update model.

---

## Installation

### Quick install (recommended)

Use the published **one-line PowerShell bootstrap command** from the latest [Releases](https://github.com/ezefeDev/temper-ai/releases) page.

That bootstrap flow is the supported public install path. It resolves the current **stable** release manifest, installs the self-contained CLI globally for the current user, and configures TemperAI to use **remote assets by default**.

No repository clone or .NET SDK is required for community installation.

Restart your terminal and verify:

```powershell
temper-ai --version
```

Then start TemperAI with:

```powershell
temper-ai
```

Running `temper-ai` with no arguments opens the interactive menu, which is the primary community user experience.

---

## First Steps

### Install agents and skills into OpenCode

```powershell
temper-ai install
```

This installs TemperAI into OpenCode using the configured release assets. For community installs, the default source is the published **remote assets** for the selected stable release.

### Local source mode for contributors

If you are developing TemperAI itself, you can work against a local repository checkout instead of published remote assets. This **local source mode** is intended for maintainers and contributors only; it is not the default community installation model.

### Optional: Install NeuralCore (persistent memory)

NeuralCore is a local MCP server that gives your agents memory across sessions. It stores observations, decisions, and project context in a local SQLite database.

```powershell
temper-ai neuralcore --install
```

Once installed, NeuralCore is automatically started whenever OpenCode invokes TemperAI agents.

---

## CLI Commands

| Command | Description |
|---|---|
| `temper-ai install` | Install all agents and skills into OpenCode |
| `temper-ai update` | Update the installed CLI and assets to the latest stable release |
| `temper-ai status` | Show what is installed and what needs updating |
| `temper-ai neuralcore` | Manage NeuralCore MCP server (install, status, test) |
| `temper-ai neural` | Save and recall project observations (requires NeuralCore) |
| `temper-ai doctor` | Diagnose and auto-repair installation issues |
| `temper-ai skill` | Create, install, and discover custom skills |
| `temper-ai snapshot` | Manage workflow snapshots for rollback |
| `temper-ai incremental` | Detect which workflow phases need re-execution after changes |
| `temper-ai budget` | Show token usage for the current project |
| `temper-ai uninstall` | Remove TemperAI completely |

Run any command with `--help` for full options:

```powershell
temper-ai install --help
temper-ai update --help
```

---

## Using TemperAI in OpenCode

Once installed, open OpenCode in any project and start a conversation with FRIDAY:

```
/temper-friday
```

FRIDAY will guide you through the full SDD workflow:

1. Describe your project idea — FRIDAY delegates to the analyst.
2. The analyst elicits requirements and produces a PRD (Product Requirements Document).
3. Once you approve the PRD, the analyst generates user stories with acceptance criteria.
4. FRIDAY delegates to the architect, who designs the system architecture.
5. Tasks are generated and organized into a build plan.
6. Backend, frontend, tester, and DevOps agents implement the work task by task.
7. The reviewer audits the output.
8. The docs agent generates project documentation.

Each step requires explicit approval before proceeding. FRIDAY never generates code without an approved plan.

---

## NeuralCore — Persistent Agent Memory

NeuralCore is an optional local MCP server that enables your agents to remember things across sessions.

**What it stores:**
- Architecture decisions
- Bug fixes and root causes
- Technical observations
- Session context and project notes

**How it works:**

NeuralCore runs as a local process and communicates with OpenCode via the MCP protocol. It uses a local SQLite database stored at:

```
%LOCALAPPDATA%\Programs\TemperAI\NeuralCore\neural.db
```

**CLI commands:**

```powershell
# Save an observation
temper-ai neural --save --title "Fixed null ref in UserService" --content "..." --type Bugfix

# Recall recent observations
temper-ai neural --recall --limit 20

# Recall by topic
temper-ai neural --recall --topic-filter "authentication"

# View session context
temper-ai neural --session --project MyProject
```

---

## Custom Skills

You can create and install your own skills:

```powershell
# Create a new skill scaffold
temper-ai skill --create --name my-skill --category backend

# Install a skill from a local path
temper-ai skill --install /path/to/my-skill

# List installed skills
temper-ai skill --list
```

Skills are Markdown files that follow the TemperAI skill format. Once installed, they are available to any agent that loads them.

---

## Project Structure

```
temper-ai/
├── assets/
│   ├── agents/          # Source agent definition files used for development and packaging
│   └── skills/          # Source skill files used for development and packaging
│       ├── backend/     # .NET, EF Core, architecture patterns
│       ├── frontend/    # Blazor, MudBlazor, Tailwind
│       ├── ddd/         # Domain modeling, ubiquitous language
│       ├── devops/      # Docker, GitHub Actions, CI/CD
│       └── workflow/    # FRIDAY, analyst, architect workflow skills
├── src/
│   ├── TemperAI.Cli/        # CLI entry point (Spectre.Console)
│   ├── TemperAI.Core/       # Shared models, configuration, asset loading
│   ├── TemperAI.Installer/  # Install/update/uninstall logic
│   └── TemperAI.NeuralCore/ # MCP server for persistent memory
├── install.ps1              # Local installer/bootstrap helper for repository workflows
└── temper-ai.exe            # Pre-built self-contained binary
```

---

## Updating

To update your TemperAI installation to the latest public stable release:

```powershell
temper-ai update
```

This is the single user-facing update action. It updates both:

- the installed CLI
- the configured release assets

Release resolution is manifest-driven, so public updates move to the matching CLI/assets pair published for that release.

---

## Uninstalling

```powershell
temper-ai uninstall
```

This removes all installed agents, skills, NeuralCore, the CLI binary, and the PATH entry.

---

## Contributing

Contributions are welcome. To build from source you need the .NET 10 SDK:

```powershell
# Build and run locally
dotnet run --project src/TemperAI.Cli

# Publish a self-contained binary
dotnet publish src/TemperAI.Cli/TemperAI.Cli.csproj -c Release -o ./publish --self-contained true -p:PublishSingleFile=true
```

Contributor workflows may use local source mode to validate unreleased asset changes from the checked-out repository. Public/community releases, however, are documented and supported through manifest-backed stable releases with remote assets by default.

---

## License

MIT — see [LICENSE](LICENSE) for details.
