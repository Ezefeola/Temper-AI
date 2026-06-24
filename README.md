# TemperAI

> AI-orchestrated software delivery for .NET teams, centered on FRIDAY.

TemperAI installs a structured set of agents and skills into [OpenCode](https://opencode.ai) so work moves through a defined workflow instead of ad-hoc prompting. In the current supported model, **FRIDAY** is the orchestrator: it classifies requests, proposes the next specialist step, waits for explicit approval, delegates one specialist at a time, and tracks workflow state in `.temper/friday-state.json`.

This repository contains more than just prompts. It defines:

- **FRIDAY** as the supported orchestrator
- **Specialist agents** for analysis, architecture, planning, implementation, testing, review, DevOps, and documentation
- **Skills** that encode the standards each agent must follow
- **CLI tooling** for installation, updates, diagnostics, and optional NeuralCore memory

> Supported documentation scope: **FRIDAY-centered workflow only**. Legacy JARVIS assets are not part of the supported documentation model.

---

## Start here

- [FRIDAY workflow overview](docs/friday-workflow.md)
- [Active agents](docs/agents.md)
- [Active skills and agent-to-skill mapping](docs/skills.md)
- [Human-readable skill catalog](assets/docs/skills-catalog.md)

---

## What TemperAI is

TemperAI is an opinionated Software-Driven Development workflow for .NET projects.

Instead of asking one general AI assistant to do everything, TemperAI splits the work across specialists:

- `temper-analyst` defines requirements
- `temper-architect` designs the solution
- `temper-tasks` and `temper-plan` prepare execution
- implementation agents build the system
- `temper-review` checks quality
- `temper-docs` produces final documentation

FRIDAY coordinates that flow and enforces approval gates between steps.

---

## End-to-end model

```text
Idea / request
    ↓
FRIDAY
    ↓
Analyst
    ↓
Architect
    ↓
Tasks
    ↓
Plan
    ↓
Backend / Frontend / Tester / DevOps
    ↓
Review
    ↓
Docs
```

Key rules in the supported model:

- FRIDAY never implements work directly.
- FRIDAY delegates **one specialist per session**.
- Medium and complex work requires **explicit approval** before delegation.
- Requirements and architecture happen before normal implementation.
- Skills, not ad-hoc prompt style, define execution standards.

---

## Active agents

| Agent | Role |
|---|---|
| `temper-friday` | Supported orchestrator. Routes work, manages approvals, persists workflow state, and delegates specialists. |
| `temper-analyst` | Produces the PRD and the user-story/spec set. |
| `temper-architect` | Produces architecture proposals and project design documents. |
| `temper-tasks` | Breaks approved work into atomic implementation tasks. |
| `temper-plan` | Turns tasks into an execution plan with dependency groups. |
| `temper-backend` | Implements .NET backend work. |
| `temper-frontend` | Implements Blazor or Angular frontend work. |
| `temper-tester` | Implements tests. |
| `temper-devops` | Implements Docker and CI/CD work. |
| `temper-review` | Reviews generated output against TemperAI conventions. |
| `temper-docs` | Produces final project documentation. |

Full details: [docs/agents.md](docs/agents.md)

---

## Active skill families

TemperAI skills are reusable instruction contracts. Agents load them only when needed.

Active skill families in the FRIDAY model:

- **FRIDAY workflow skills** — state, delegation, communication loops, session-mode guidance
- **Analyst skills** — functional analysis, report formats, PRD generation, spec generation
- **Architect skills** — proposal/report formats and document templates
- **Backend skills** — architecture patterns, .NET API, DDD, EF Core, DTOs, Result pattern, clean code
- **Frontend skills** — Blazor, Blazor Server, Angular, Angular Material, SCSS, Tailwind, MudBlazor, bUnit
- **Testing skills** — .NET testing conventions
- **DevOps skills** — Docker and GitHub Actions

Full details: [docs/skills.md](docs/skills.md)

---

## Installation

### Quick install

Use the published PowerShell bootstrap command from the latest [Releases](https://github.com/Ezefeola/temper-ai/releases) page.

Then verify:

```powershell
temper-ai --version
```

Install TemperAI assets into OpenCode:

```powershell
temper-ai install
```

---

## How to use TemperAI

1. Install TemperAI into OpenCode.
2. Open your project in OpenCode.
3. Start with FRIDAY:

   ```text
   /temper-friday
   ```

4. Describe the project, change, bug, or documentation request.
5. Review FRIDAY's proposed plan.
6. Approve the next step explicitly when required.
7. Review each specialist's output before moving to the next specialist.

The primary supported usage model is **through FRIDAY orchestration**, not by manually bypassing it.

Full workflow: [docs/friday-workflow.md](docs/friday-workflow.md)

---

## CLI commands

| Command | Description |
|---|---|
| `temper-ai install` | Install agents and skills into OpenCode |
| `temper-ai update` | Update the installed CLI and assets |
| `temper-ai status` | Show installed status |
| `temper-ai neuralcore` | Manage NeuralCore |
| `temper-ai neural` | Save or recall NeuralCore observations |
| `temper-ai doctor` | Diagnose installation issues |
| `temper-ai skill` | Create, install, and discover skills |
| `temper-ai snapshot` | Manage workflow snapshots |
| `temper-ai incremental` | Detect phases that need re-execution |
| `temper-ai budget` | Show token usage |
| `temper-ai uninstall` | Remove TemperAI |

---

## NeuralCore

NeuralCore is an optional local MCP server that gives agents persistent memory across sessions.

Install it with:

```powershell
temper-ai neuralcore --install
```

---

## Project structure

```text
temper-ai/
├── assets/
│   ├── agents/          # Agent definitions
│   ├── skills/          # Skill definitions
│   └── docs/            # Human-readable internal reference docs
├── docs/                # Public workflow documentation
├── src/                 # CLI, core libraries, installer, NeuralCore
└── README.md
```

---

## Contributing

To build from source you need the .NET 10 SDK:

```powershell
dotnet run --project src/TemperAI.Cli
```

---

## License

MIT — see [LICENSE](LICENSE).
