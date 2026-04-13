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

### `temper-ai doctor`

Diagnostica problemas de instalación y ofrece reparaciones automatizadas.

**Opciones:**
| Flag | Descripción |
|---|---|
| `--fix` | Aplica las reparaciones automáticamente |
| `--check` | Solo muestra el diagnóstico sin reparar |

**Qué verifica:**
- Archivos de skills faltantes o corruptos
- Archivos de agentes faltantes o corruptos
- Configuración de NeuralCore MCP
- Ejecutable del CLI instalado
- PATH del sistema

**Ejemplos:**
```cmd
temper-ai doctor              # Menú interactivo
temper-ai doctor --check      # Solo mostrar diagnóstico
temper-ai doctor --fix      # Reparar automáticamente
```

---

### `temper-ai neural`

Guarda y recupera observaciones del proyecto usando NeuralCore MCP.

**Opciones:**
| Flag | Descripción |
|---|---|
| `--save` | Guardar una observación |
| `--recall` | Buscar observaciones |
| `--session` | Crear resumen de sesión |
| `--title` | Título de la observación |
| `--content` | Contenido de la observación |
| `--type` | Tipo (Bugfix, Feature, Decision, etc.) |
| `--limit` | Límite de resultados |
| `--topic-filter` | Filtrar por tema |

**Ejemplos:**
```cmd
temper-ai neural --save --title "Fix null ref" --content "Fixed..." --type Bugfix
temper-ai neural --recall --limit 20
temper-ai neural --recall --topic-filter "null-ref-fix"
temper-ai neural --session --project MyProject
```

---

### `temper-ai menu`

Abre un menú interactivo con todos los comandos disponibles.

**Ejemplo:**
```cmd
temper-ai menu
```

Muestra una interfaz interactiva donde podés navegar con flechas, escribir para filtrar, y presionar Enter para seleccionar.

---

### `temper-ai uninstall`

Desinstala TemperAI completamente (CLI, NeuralCore, skills, agentes).

**Opciones:**
| Flag | Descripción |
|---|---|
| `--dry-run` | Simula la desinstalación sin escribir archivos |
| `--force` | Desinstala sin pedir confirmación |

**Ejemplos:**
```cmd
temper-ai uninstall             # Desinstalación interactiva
temper-ai uninstall --dry-run  # Simular
temper-ai uninstall --force      # Sin confirmación
```

---

### `temper-ai setup`

Instala el CLI ejecutable al PATH global y configura NeuralCore MCP.

**Qué hace:**
1. Copia el ejecutable actual a `%LOCALAPPDATA%\Programs\TemperAI\temper-ai.exe`
   - Si el archivo está en uso (ejecutándose), usa un script diferido de PowerShell para copiar después de que el proceso termine.
2. Agrega ese directorio a la variable PATH del usuario.
3. Configura NeuralCore MCP en los archivos de configuración de OpenCode y Copilot CLI.
4. Publica NeuralCore como ejecutable separado (si no está ya publicado).

**Manejo de auto-actualización:** Si ejecutás `temper-ai setup` desde la versión instalada, detecta que el archivo está en uso y programa la actualización via un script temporal de PowerShell que se ejecuta después de que el proceso termine.

**Después de ejecutar:** Cerrá y reabri tu terminal para que los cambios en el PATH surtan efecto.

**Recomendación:** Para una instalación limpia, usá `.\install.ps1` desde la raíz del repositorio en lugar de `temper-ai setup`.

---

## Exit Codes

| Code | Meaning |
|---|---|
| 0 | Success |
| 1 | Error (details in output) |
