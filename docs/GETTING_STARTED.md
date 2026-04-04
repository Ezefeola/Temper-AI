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
1. Publish the CLI to `%LOCALAPPDATA%\Programs\TemperAI\`
2. Add that directory to your user PATH
3. Verify the installation

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

This copies all skills and agent files to your AI assistant's configuration directory.

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

## Step 3: Start Your First Project

### 3.1 Open Your AI Assistant

Open your preferred AI assistant (OpenCode, Copilot, or Claude Code) in an **empty project directory**.

### 3.2 Initialize the Project

Type in your AI assistant:

```
@temper-init

I want to build a [describe your project].

[Describe the features, users, and requirements]
```

The `temper-init` agent will:
1. Analyze your description
2. Ask clarifying questions
3. Generate `.temper/constitution.md` with project decisions
4. Ask for your approval

### 3.3 Generate Specifications

After approving the constitution:

```
@temper-spec
```

This generates `.temper/spec.md` with user stories, acceptance criteria, and edge cases.

### 3.4 Design the Architecture

```
@temper-design
```

This generates `.temper/design.md` with entity definitions, API endpoints, database schema, and folder structure.

### 3.5 Break Into Tasks

```
@temper-tasks
```

This generates `.temper/tasks.md` with atomic, trackable implementation tasks.

### 3.6 Build the Project

```
@temper-build
```

This executes the tasks — the backend agent writes C# code, the frontend agent writes Blazor components, the tester writes tests, and the devops agent creates Docker and CI/CD files.

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
