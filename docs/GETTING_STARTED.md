# Getting Started with TemperAI

## Prerequisites

- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **An AI coding assistant** — OpenCode, GitHub Copilot CLI, or Claude Code
- **PowerShell** (Windows) or **bash** (Mac/Linux)

---

## Step 1: Install the CLI

### Option A: PowerShell Script (Recommended)

From the root of the TemperAI repository:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

This will:
1. Publish the CLI to `%LOCALAPPDATA%\Programs\TemperAI\temper-ai.exe`
2. Publish NeuralCore MCP server to `%LOCALAPPDATA%\Programs\TemperAI\TemperAI.NeuralCore.exe`
3. Add that directory to your user PATH
4. Verify the installation

### Option B: From the CLI itself

```powershell
dotnet run --project src/TemperAI.Cli -- setup
```

### Verify Installation

Open a **new** terminal and run:

```cmd
temper-ai
```

You should see the interactive menu with all available commands.

---

## Step 2: Install Skills and Agents

```cmd
temper-ai install
```

This copies all skills and agent files to your AI assistant's configuration directory. You'll be asked if you want to install NeuralCore (persistent memory). If you say yes, it will automatically publish the NeuralCore executable (if needed) and configure MCP for your selected agents.

| AI Assistant | Where files are installed |
|---|---|
| GitHub Copilot CLI | `~/.copilot/skills/` and `~/.copilot/agents/` |
| Claude Code | `~/.claude/skills/` and `~/.claude/agents/` |
| OpenCode | `~/.config/opencode/skills/` and `~/.config/opencode/agent/` |

### Update Existing Installations

If you already have TemperAI installed and want to update to the latest version:

```cmd
temper-ai update
```

This compares your installed files with the latest versions and shows what's outdated.

---

## NeuralCore — Persistent Memory

NeuralCore is an MCP (Model Context Protocol) server that provides persistent memory across AI sessions. It allows agents to save and recall observations, decisions, and patterns between sessions.

### How it works

1. **Auto-start:** NeuralCore starts automatically when you open your AI assistant (OpenCode, Claude Code, Copilot).
2. **Auto-stop:** NeuralCore shuts down when you close your AI session.
3. **Persistent storage:** Observations are saved to SQLite and indexed for fast retrieval.
4. **MCP tools:** Agents use `mem_save`, `mem_search`, `mem_context`, and `mem_session_summary` tools.

### Managing NeuralCore

```cmd
temper-ai neuralcore
```

This opens an interactive menu with the following options:

| Option | Description |
|---|---|
| 🔍 **status** | Check if NeuralCore is published and configured for each agent |
| 🧪 **test** | Run a connectivity test against the MCP server |
| 📦 **publish** | Compile NeuralCore as a standalone executable |
| ⚙️ **install** | Configure MCP in your AI agents (publishes automatically if needed) |
| 📜 **logs** | View the latest server logs |

### CLI Flags

```cmd
temper-ai neuralcore --status     # Check status
temper-ai neuralcore --test       # Run connectivity test
temper-ai neuralcore --publish    # Publish executable
temper-ai neuralcore --install    # Install MCP config
temper-ai neuralcore --logs       # View logs
```

---

## Step 3: Start Your First Project

### 3.1 Open Your AI Assistant

Open your preferred AI assistant (OpenCode, Copilot, or Claude Code) in an **empty project directory**.

### 3.2 Initialize the Project

Type in your AI assistant:

```
@temper-analyst

I want to build a [describe your project].

[Describe the features, users, and requirements]
```

The `temper-analyst` agent will:
1. Analyze your description
2. Ask functional questions until everything is clear
3. Generate `.temper/prd.md` with the functional scope

### 3.3 Define Technical Architecture

After the PRD is approved:

```
@temper-architect
```

This generates `.temper/backend-config.md` and `.temper/frontend-config.md` with all technical decisions.

### 3.4 Generate Specifications

After approving the architecture:

```
@temper-spec
```

This generates `.temper/specs/` with individual user story files and an `INDEX.md` for fast lookup.

### 3.4 Design the Architecture

```
@temper-design
```

This generates `.temper/design.md` with entity definitions, API endpoints, database schema, and folder structure.

### 3.5 Break Into Tasks

```
@temper-tasks
```

This generates `.temper/tasks/` with atomic, per-user-story task files and an `INDEX.md` for fast lookup.

### 3.5 Plan the Build

```
@temper-plan
```

This generates `.temper/build-plan.md` with parallel execution groups and agent assignments.

### 3.6 Build the Project

The ephemeral orchestrator reads the build plan and spawns sub-agents one group at a time. **Each group runs in a fresh session** to maintain clean context:

```
@temper-next
```

This triggers the orchestrator to execute Group 1 — spawning backend, frontend, and devops agents in separate conversations with clean context. After each group, you'll verify with `dotnet build`, then start a new session (`/new` in OpenCode) and run `/temper-next` again for the next group.

### 3.7 Review the Code

```
@temper-review
```

This scans all generated code against TemperAI conventions and produces a review report.

### 3.8 Generate Documentation

```
@temper-docs
```

This generates `README.md`, `ARCHITECTURE.md`, `API.md`, and `CHANGELOG.md`.

---

## Using the Interactive Menu

At any time, you can run the interactive menu from any terminal:

```cmd
temper-ai
```

This shows all available commands with descriptions. Use arrow keys to navigate and type to filter.

---

## Managing Snapshots

Before risky operations, create a snapshot:

```cmd
temper-ai snapshot --create --phase pre-build
```

If something goes wrong, rollback:

```cmd
temper-ai snapshot --latest
```

View all snapshots:

```cmd
temper-ai snapshot
```

---

## Tracking Token Usage

View your project's token budget:

```cmd
temper-ai budget
```

This shows estimated token usage per phase and remaining budget.

---

## Checking for Incremental Updates

After making changes to `.temper/` files, check what needs re-running:

```cmd
temper-ai incremental --check
```

---

## Creating Custom Skills

Create a custom skill for your team's conventions:

```cmd
temper-ai skill --create --name mycompany-standards --category backend
```

This generates a template in `assets/skills/backend/mycompany-standards/SKILL.md` that you can customize.

---

## Troubleshooting

### "temper-ai is not recognized"

The CLI is not in your PATH. Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

Then **close and reopen** your terminal.

### "No supported agents found"

None of the supported AI assistants are installed on your system. Install OpenCode, GitHub Copilot CLI, or Claude Code, then run `temper-ai install` again.

### Skills not loading in AI assistant

Verify the files were installed:

```cmd
temper-ai status
```

If files show as missing, run:

```cmd
temper-ai install --force
```

### Build errors in generated code

Run the review agent:

```
@temper-review
```

It will identify convention violations with exact file and line references.
