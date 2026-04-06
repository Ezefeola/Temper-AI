# CLI Reference

The TemperAI CLI (`temper-ai`) is a management tool for installing, updating, and monitoring your TemperAI ecosystem.

---

## Installation

### Global Install

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

This publishes the CLI to `%LOCALAPPDATA%\Programs\TemperAI\` and adds it to your PATH.

### Verify

```cmd
temper-ai
```

---

## Commands

### `temper-ai` (no arguments)

Opens the interactive menu. Use arrow keys to navigate, type to filter, Enter to select.

---

### `temper-ai install`

Installs all skills and agents into your AI assistant's configuration directory.

**Options:**
| Flag | Description |
|---|---|
| `--dry-run` | Simulate installation without writing files |
| `-a, --agent <id>` | Install for a specific agent (`copilot`, `claude`, `opencode`) |

**Examples:**
```cmd
temper-ai install
temper-ai install --dry-run
temper-ai install --agent copilot
```

---

### `temper-ai update`

Compares installed files with the latest versions and shows what's outdated.

**Options:**
| Flag | Description |
|---|---|
| `--force` | Update without asking for confirmation |
| `--dry-run` | Show what would be updated without writing |
| `-a, --agent <id>` | Check a specific agent |

**Examples:**
```cmd
temper-ai update
temper-ai update --force
temper-ai update --dry-run
```

---

### `temper-ai status`

Shows the installation status for all supported AI agents.

**Output:**
- Table with each agent's status (installed, partial, not installed)
- Skills count (installed/total)
- Agents count (installed/total)
- Missing files count

**Example:**
```
┌────────────────────┬──────────────┬────────┬────────┬───────────┐
│ Agente             │ Estado       │ Skills │ Agents │ Faltantes │
├────────────────────┼──────────────┼────────┼────────┼───────────┤
│ GitHub Copilot CLI │ parcial      │ 4/17   │ 0/12   │ 25        │
│ Claude Code        │ no instalado │ 0/17   │ 0/12   │ 29        │
│ OpenCode           │ instalado    │ 17/17  │ 12/12  │ -         │
└────────────────────┴──────────────┴────────┴────────┴───────────┘
```

---

### `temper-ai budget`

Shows token usage tracking for the current project.

**Options:**
| Flag | Description |
|---|---|
| `--reset` | Reset the budget tracking |

**Output:**
- Configuration table (max tokens, alert threshold, hard limit)
- Phase usage table (estimated input/output/total, status)
- Summary (total used, remaining, utilization %, pending phases)
- Warning if utilization ≥ 80%

---

### `temper-ai snapshot`

Manages snapshots for automatic rollback.

**Options:**
| Flag | Description |
|---|---|
| `--create` | Create a snapshot of current `.temper/` files |
| `--restore <name>` | Restore a specific snapshot |
| `--latest` | Restore the most recent snapshot |
| `--delete <name>` | Delete a snapshot |
| `--phase <name>` | Phase name for the snapshot (with `--create`) |

**Examples:**
```cmd
temper-ai snapshot                                    # List all snapshots
temper-ai snapshot --create --phase pre-build         # Create snapshot
temper-ai snapshot --latest                           # Restore latest
temper-ai snapshot --restore 20260404-120000_init     # Restore specific
temper-ai snapshot --delete 20260404-120000_init      # Delete snapshot
```

**Snapshot naming:** `[timestamp]_[phase-name]` (e.g., `20260404-120000_design`)

---

### `temper-ai incremental`

Detects which phases need re-running after changes to `.temper/` files.

**Options:**
| Flag | Description |
|---|---|
| `--check` | Show detailed analysis with reasons |
| `--force` | Show all phases without checking changes |

**Output:**
- Table showing each phase's status (Re-run / No changes)
- Reason for each phase (file changed or dependency affected)
- Re-execution order

---

### `temper-ai skill`

Creates, installs, and discovers custom skills.

**Options:**
| Flag | Description |
|---|---|
| `--create` | Create a new skill template |
| `--install <path>` | Install a skill from a source directory |
| `--list` | List all installed skills |
| `--name <name>` | Skill name (with `--create`) |
| `--category <cat>` | Skill category (with `--create`) |
| `--author <name>` | Author name (with `--create`) |
| `--description <text>` | Skill description (with `--create`) |

**Examples:**
```cmd
temper-ai skill --create --name my-standards --category backend
temper-ai skill --install /path/to/skill
temper-ai skill --list
```

---

### `temper-ai neuralcore`

Manages the NeuralCore MCP server for persistent AI memory. Opens an interactive menu with options to check status, test connectivity, install, and update.

**Interactive Menu Options:**
| Option | Description |
|---|---|
| 🔍 **status** | Check if NeuralCore is published and configured for each agent |
| 🧪 **test** | Run a connectivity test against the MCP server |
| 📦 **publish** | Compile NeuralCore as a standalone executable |
| ⚙️ **install** | Configure MCP in your AI agents (publishes automatically if needed) |
| 📜 **logs** | View the latest server logs |

**CLI Flags:**
| Flag | Description |
|---|---|
| `--status` | Check NeuralCore status directly |
| `--test` | Run connectivity test directly |
| `--publish` | Publish NeuralCore executable |
| `--install` | Install MCP configuration for agents |
| `--logs` | Show server logs |
| `-a, --agent <id>` | Target a specific agent (`copilot`, `claude`, `opencode`) |

**Examples:**
```cmd
temper-ai neuralcore                  # Interactive menu
temper-ai neuralcore --status         # Check status
temper-ai neuralcore --test           # Run test
temper-ai neuralcore --publish        # Publish executable
temper-ai neuralcore --install        # Install MCP config
temper-ai neuralcore --install --agent opencode  # Install for OpenCode only
```

**Note:** When running `temper-ai install`, you'll be asked if you want to install NeuralCore. If you say yes, it will automatically publish (if needed) and configure MCP for your selected agents.

**How the test works:** The `--test` command starts NeuralCore and waits 10 seconds. If the process is still running (not crashed), the test passes. Since MCP servers are long-running processes, a "timeout" message actually means the server is working correctly and will be auto-started by your AI assistant when needed.

---

### `temper-ai setup`

Installs the CLI executable to the global PATH and configures NeuralCore MCP.

**What it does:**
1. Copies the current executable to `%LOCALAPPDATA%\Programs\TemperAI\temper-ai.exe`
   - If the file is locked (running), uses a deferred PowerShell script to copy after the process exits.
2. Adds that directory to the user's PATH environment variable.
3. Configures NeuralCore MCP in OpenCode and Copilot CLI configuration files.
4. Publishes NeuralCore as a standalone executable (if not already published).

**Self-update handling:** If you run `temper-ai setup` from the installed version, it detects that the file is in use and schedules the update via a temporary PowerShell script that runs after the process exits.

**After running:** Close and reopen your terminal for PATH changes to take effect.

**Recommendation:** For a clean installation, use `.\install.ps1` from the repository root instead of `temper-ai setup`.

---

## Exit Codes

| Code | Meaning |
|---|---|
| 0 | Success |
| 1 | Error (details in output) |
