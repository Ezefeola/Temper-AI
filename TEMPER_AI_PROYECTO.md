# TemperAI — Documentación Completa del Proyecto

> **Para cualquier IA que lea esto:** Este documento describe un proyecto en desarrollo activo. Leelo completo antes de responder cualquier pregunta o continuar el trabajo. Contiene decisiones de arquitectura, convenciones de código, y el estado actual del proyecto. Respetá TODAS las decisiones tomadas — no las cuestionés a menos que el usuario lo pida explícitamente.

---

## Tabla de contenidos

1. [Qué es TemperAI](#1-qué-es-temperai)
2. [Stack tecnológico](#2-stack-tecnológico)
3. [Arquitectura del sistema](#3-arquitectura-del-sistema)
4. [Estructura del repositorio](#4-estructura-del-repositorio)
5. [Los Skills — estándares de código](#5-los-skills--estándares-de-código)
6. [El sistema multi-agente SDD](#6-el-sistema-multi-agente-sdd)
7. [NeuralCore — memoria persistente](#7-neuralcore--memoria-persistente)
8. [Convenciones de código C#](#8-convenciones-de-código-c)
9. [Estado actual y próximos pasos](#9-estado-actual-y-próximos-pasos)

---

## 1. Qué es TemperAI

TemperAI es un **configurador de ecosistema AI** para desarrolladores .NET. Inspirado en [gentle-ai](https://github.com/Gentleman-Programming/gentle-ai) de Gentleman Programming, pero construido completamente en C# y .NET 10, orientado al stack .NET + Blazor.

**Lo que hace:**
- Instala Skills (archivos `.md` con estándares de código) en el directorio de tu agente AI (GitHub Copilot CLI, Claude Code, OpenCode)
- Instala agentes especializados que siguen un workflow SDD (Spec-Driven Development)
- Provee un sistema de memoria persistente (llamado **NeuralCore** — nuestro equivalente a Engram) implementado con .NET + EF Core + SQLite + MCP

**Lo que NO es:**
- No es un agente de IA en sí mismo
- No reemplaza a GitHub Copilot, Claude Code, ni OpenCode
- No tiene su propio LLM — usa el agente que ya tenés instalado

**Nombre del producto:** TemperAI
- "Templar" = proceso de darle dureza, forma y resistencia al metal con precisión
- Metáfora: tomar un PRD y forjar código robusto, bien estructurado, con los estándares más altos

---

## 2. Stack tecnológico

### TemperAI Core + Installer
- **Lenguaje:** C# 14 / .NET 10
- **CLI framework:** Spectre.Console + Spectre.Console.Cli
- **Distribución:** `dotnet publish` con `PublishSingleFile=true` + `SelfContained=true` → `.exe` único sin dependencias
- **Assets embebidos:** `EmbeddedResource` en `.csproj` — todos los Skills y agentes van dentro del binario

### Skills y Agentes
- **Formato:** Markdown puro (`.md`) con YAML frontmatter
- **Compatibilidad:** GitHub Copilot CLI, Claude Code, OpenCode, Cursor (cualquier agente que soporte el estándar Agent Skills)

### NeuralCore (memoria persistente — construido)
- **Lenguaje:** C# / .NET 10
- **Arquitectura:** Clean Architecture
- **Base de datos:** SQLite via EF Core
- **Protocolo:** MCP (Model Context Protocol) — expuesto como MCP server
- **HTTP API:** REST endpoints en puerto configurable
- **Inspirado en:** [Engram](https://github.com/Gentleman-Programming/engram) de Gentleman Programming

### Proyectos generados por TemperAI
- **Backend:** .NET 10, C#, EF Core, Clean Architecture / Hexagonal / Vertical Slice / Onion
- **Frontend:** Blazor (Server o WebAssembly)
- **Testing:** xUnit, bUnit
- **DevOps:** Docker, GitHub Actions

---

## 3. Arquitectura del sistema

### El configurador (lo que ya existe)

```
temper-ai.exe install
    ↓
Muestra menú interactivo (Spectre.Console)
    ↓
Usuario elige agente (Copilot CLI / Claude Code / OpenCode)
    ↓
¿Queres instalar NeuralCore? → Sí/No
    ↓ (Si Sí)
    → ¿NeuralCore publicado? → No → Publica automáticamente
    → Configura MCP en el agente seleccionado
    ↓
Copia Skills de assets embebidos → ~/.config/opencode/skills/
Copia Agentes de assets embebidos → ~/.config/opencode/agent/
    ↓
El agente AI del usuario ahora tiene Skills, agentes y NeuralCore configurado
```

### Menú interactivo (`temper-ai`)

```
temper-ai
    ↓
Muestra opciones:
  install    — Instala skills, agentes y NeuralCore
  update     — Actualiza instalaciones
  status     — Estado de instalación
  neuralcore — Gestiona NeuralCore (status, test, publish, install, logs)
  budget     — Token usage
  snapshot   — Snapshots/rollback
  incremental— Detección de cambios
  skill      — Skills custom
  setup      — Instala temper-ai.exe en PATH global
```

### Submenú de NeuralCore (`temper-ai neuralcore`)

```
🔍 status   — Verifica si NeuralCore está publicado y configurado
🧪 test     — Prueba de conectividad end-to-end
📦 publish  — Compila NeuralCore como ejecutable standalone
⚙️  install  — Configura MCP en los agentes AI
📜 logs     — Muestra logs del servidor
```

### Instalación y auto-actualización

- **Instalación limpia:** Usar `.\install.ps1` desde la raíz del repositorio. Compila todo de cero e instala sin conflictos.
- **Auto-actualización (`temper-ai setup`):** Si el ejecutable está en uso (Windows lo bloquea), genera un script PowerShell temporal que copia el archivo después de que el proceso termine.
- **Separación de comandos:** El comando `neuralcore` es para gestión. El servidor MCP se activa internamente con el flag `--mcp` (invisible para el usuario).

### El sistema multi-agente SDD (construido)

```
Usuario escribe PRD.md
    ↓
/temper-init → Lee PRD, hace preguntas, genera .temper/constitution.md
    ↓ (aprobación del usuario)
/temper-spec → Genera .temper/specs/ (user stories individuales + INDEX.md)
    ↓ (aprobación del usuario)
/temper-design → Genera .temper/design.md (arquitectura, entidades, endpoints)
    ↓ (aprobación del usuario)
/temper-tasks → Genera .temper/tasks/ (tareas atómicas por user story + INDEX.md)
    ↓ (aprobación del usuario)
/temper-plan → Genera .temper/build-plan.md (grupos paralelos, agentes)
    ↓ (aprobación del usuario)
[orchestrator ejecuta el build] → Spawnea sub-agentes por grupo (efímero)
    ├── Group 1 → orchestrator spawnea agentes → actualiza state.md → termina
    ├── Group 2 → nueva sesión → orchestrator lee state.md → spawnea agentes → actualiza state.md → termina
    └── Group N → ... → build completo
    ↓
/temper-review → Valida código contra specs/
    ↓ (aprobación del usuario)
/temper-docs → Genera README, API docs, decisiones de arquitectura
    ↓
✅ Workflow completo
```

**Anti-lobotomía:** El contexto NO vive en la conversación. Cada agente lee archivos `.temper/` al arrancar, hace su trabajo, y escribe el resultado en disco. Si el agente se compacta (lobotomía), simplemente se reinicia y lee los archivos del disco — sin perder nada.

**Orchestrator efímero:** El orchestrator NO acumula contexto entre fases. Ejecuta UNA fase o UN grupo de build, actualiza `.temper/orchestrator-state.md`, y termina. La próxima invocación es una instancia fresca que lee el state file y continúa exactamente donde quedó.

**Minimización de context window:** Cada subagente carga SOLO las Skills de su dominio. El agente de backend .NET no sabe que existe Blazor. El de frontend no sabe que existe EF Core.

---

## 4. Estructura del repositorio

```
temper-ai/
│
├── TemperAI.slnx
│
├── src/
│   ├── TemperAI.Core/              ← Modelos, assets embebidos, configuración
│   │   ├── TemperAI.Core.csproj
│   │   ├── Assets/
│   │   │   └── EmbeddedAssets.cs   ← Lee archivos de skills/agents embebidos
│   │   ├── Configuration/
│   │   │   └── AgentTargets.cs     ← Configuración de agentes AI soportados
│   │   ├── Models/
│   │   │   ├── AgentTarget.cs
│   │   │   ├── InstallResult.cs
│   │   │   └── SaveResult.cs
│   │   ├── Snapshots/
│   │   │   ├── SnapshotService.cs  ← Lógica de snapshot/rollback
│   │   │   ├── SnapshotInfo.cs
│   │   │   └── SnapshotResult.cs
│   │   ├── Incremental/
│   │   │   ├── IncrementalUpdateService.cs  ← Detección de cambios entre fases
│   │   │   ├── IncrementalResult.cs
│   │   │   └── PhaseDependency.cs
│   │   └── Skills/
│   │       ├── SkillCreatorService.cs ← Creación/instalación de skills
│   │       ├── SkillInfo.cs
│   │       ├── SkillMetadata.cs
│   │       └── SkillResult.cs
│   │
│   ├── TemperAI.Installer/         ← Lógica de instalación de Skills
│   │   ├── TemperAI.Installer.csproj
│   │   └── InstallerService.cs
│   │
│   └── TemperAI.NeuralCore/        ← Memoria persistente (MCP + HTTP API)
│       ├── TemperAI.NeuralCore.csproj
│       ├── Program.cs
│       ├── Domain/
│       │   ├── Common/
│       │   │   └── Primitives/
│       │   │       └── Entity.cs
│       │   └── Entities/
│       │       ├── Sessions/
│       │       │   ├── Session.cs
│       │       │   └── Enums/
│       │       │       └── SessionStatus.cs
│       │       └── Observations/
│       │           ├── Observation.cs
│       │           └── Enums/
│       │               └── ObservationType.cs
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
│       │   │   ├── SaveResult.cs
│       │   │   ├── Configurations/
│       │   │   │   ├── SessionConfiguration.cs
│       │   │   │   └── ObservationConfiguration.cs
│       │   │   └── Repositories/
│       │   │       ├── ISessionRepository.cs
│       │   │       ├── SessionRepository.cs
│       │   │       ├── IObservationRepository.cs
│       │   │       └── ObservationRepository.cs
│       │   └── DependencyInjection.cs
│       ├── Api/
│       │   └── Controllers/
│       └── Mcp/
│           ├── McpServer.cs
│           └── Tools/
│               ├── MemSaveTool.cs
│               ├── MemSearchTool.cs
│               ├── MemContextTool.cs
│               └── MemSessionSummaryTool.cs
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
├── assets/                         ← Se embeben dentro del binario en build time
│   ├── skills/
│   │   ├── README.md
│   │   ├── dotnet-csharp/
│   │   │   └── SKILL.md            ← Estándares universales de C# / .NET 10 ✅
│   │   ├── prd-analyzer/
│   │   │   └── SKILL.md            ← Análisis de PRD y generación de constitución ✅
│   │   ├── token-budget/
│   │   │   └── SKILL.md            ← Gestión de presupuesto de tokens ✅
│   │   ├── backend/
│   │   │   ├── dotnet/
│   │   │   │   ├── api/
│   │   │   │   │   └── SKILL.md    ← Estándares ASP.NET Core API ✅
│   │   │   │   ├── ef-core/
│   │   │   │   │   └── SKILL.md    ← EF Core, repositorios, DbContext ✅
│   │   │   │   ├── linq/
│   │   │   │   │   └── SKILL.md    ← Patrones de consultas LINQ ✅
│   │   │   │   ├── ddd/
│   │   │   │   │   └── SKILL.md    ← Domain-Driven Design ✅
│   │   │   │   └── testing/
│   │   │   │       └── SKILL.md    ← xUnit, Moq, bUnit ✅
│   │   │   └── architecture/
│   │   │       ├── shared/
│   │   │       │   └── SKILL.md    ← Reglas comunes a TODAS las arquitecturas ✅
│   │   │       ├── clean/
│   │   │       │   └── SKILL.md    ← Clean Architecture + DDD ✅
│   │   │       ├── hexagonal/
│   │   │       │   └── SKILL.md    ← Hexagonal (Ports & Adapters) ✅
│   │   │       ├── vertical-slice/
│   │   │       │   └── SKILL.md    ← Vertical Slice para CRUDs ✅
│   │   │       └── onion/
│   │   │           └── SKILL.md    ← Onion Architecture ✅
│   │   ├── frontend/
│   │   │   ├── blazor/
│   │   │   │   └── SKILL.md        ← Estándares Blazor (Server y WASM) ✅
│   │   │   └── bunit/
│   │   │       └── SKILL.md        ← Testing de componentes Blazor ✅
│   │   └── devops/
│   │       ├── docker/
│   │       │   └── SKILL.md        ← Docker multi-stage, compose ✅
│   │       ├── github-actions/
│   │       │   └── SKILL.md        ← CI/CD workflows ✅
│   │       └── ci-cd/
│   │           └── SKILL.md        ← Estrategias de despliegue ✅
│   │
│   ├── agents/
│   │   ├── README.md
│   │   ├── temper-orchestrator.agent.md  ← Orquestador principal ✅
│   │   ├── temper-init.agent.md          ← Fase 1: PRD + Constitución ✅
│   │   ├── temper-spec.agent.md          ← Fase 2: User Stories ✅
│   │   ├── temper-design.agent.md        ← Fase 3: Arquitectura ✅
│   │   ├── temper-tasks.agent.md         ← Fase 4: Tareas atómicas ✅
│   │   ├── temper-plan.agent.md          ← Fase 5: Plan de build ✅
│   │   ├── temper-backend.agent.md       ← Fase 5a: Backend ✅
│   │   ├── temper-frontend.agent.md      ← Fase 5b: Frontend ✅
│   │   ├── temper-tester.agent.md        ← Fase 5c: Testing ✅
│   │   ├── temper-devops.agent.md        ← Fase 5d: DevOps ✅
│   │   ├── temper-review.agent.md        ← Fase 6: Code Review ✅
│   │   └── temper-docs.agent.md          ← Fase 7: Documentación ✅
│   │
│   ├── commands/
│   │   ├── temper-init.md
│   │   ├── temper-next.md
│   │   └── temper-status.md
│   │
│   └── config/
│       └── README.md
│
├── docs/
│   ├── ARCHITECTURE.md               ← Arquitectura técnica detallada
│   ├── CONVENTIONS.md                ← Convenciones de código completas
│   ├── CLI.md                        ← Referencia de comandos CLI
│   ├── AGENTS.md                     ← Referencia de agentes
│   ├── SKILLS.md                     ← Referencia de skills
│   ├── WORKFLOW.md                   ← Workflow SDD paso a paso
│   └── GETTING_STARTED.md            ← Instalación y primer uso
│
├── install.ps1                       ← Script de instalación global del CLI
├── AGENTS.md                         ← Índice de routing de agentes (lee la IA)
├── README.md                         ← Documentación principal del proyecto
├── TEMPER_AI_ARCHITECTURE.md         ← Arquitectura y orquestación de agentes
└── TEMPER_AI_PROYECTO.md             ← Este archivo
```

---

## 5. Los Skills — estándares de código

Los Skills son archivos `.md` con YAML frontmatter que el agente AI lee cuando es relevante para la tarea. El `description` en el frontmatter es lo que el agente usa para decidir si carga ese Skill.

### Formato de un Skill

```markdown
---
name: nombre-del-skill
description: >
  Cuándo usar este skill — el agente lo lee para decidir si es relevante.
  Sé específico sobre el contexto de uso.
---

# Contenido del Skill
... instrucciones para el agente ...
```

### Todos los skills — estado actual

#### Backend Skills

| Skill | Estado | Cubre |
|---|---|---|
| `dotnet-csharp` | ✅ Completo | Estándares universales de C# / .NET 10 |
| `backend/dotnet/api` | ✅ Completo | ASP.NET Core API, controllers, DI, logging |
| `backend/dotnet/ef-core` | ✅ Completo | EF Core, repositorios, UnitOfWork, DbContext |
| `backend/dotnet/linq` | ✅ Completo | Patrones LINQ, includes, paginación |
| `backend/dotnet/ddd` | ✅ Completo | Entidades, Value Objects, Domain Events |
| `backend/dotnet/testing` | ✅ Completo | xUnit, Moq, bUnit |

#### Architecture Skills

| Skill | Estado | Cubre |
|---|---|---|
| `backend/architecture/shared` | ✅ Completo | Reglas comunes a TODAS las arquitecturas |
| `backend/architecture/clean` | ✅ Completo | Clean Architecture + DDD |
| `backend/architecture/hexagonal` | ✅ Completo | Hexagonal (Ports & Adapters) |
| `backend/architecture/vertical-slice` | ✅ Completo | Vertical Slice para CRUDs/MVPs |
| `backend/architecture/onion` | ✅ Completo | Onion Architecture |

#### Frontend Skills

| Skill | Estado | Cubre |
|---|---|---|
| `frontend/blazor` | ✅ Completo | Blazor Server y WebAssembly |
| `frontend/bunit` | ✅ Completo | Testing de componentes Blazor |

#### DevOps Skills

| Skill | Estado | Cubre |
|---|---|---|
| `devops/docker` | ✅ Completo | Docker multi-stage, docker-compose |
| `devops/github-actions` | ✅ Completo | CI/CD workflows |
| `devops/ci-cd` | ✅ Completo | Estrategias de despliegue |

#### Utility Skills

| Skill | Estado | Cubre |
|---|---|---|
| `prd-analyzer` | ✅ Completo | Análisis de PRD, generación de constitución |
| `token-budget` | ✅ Completo | Gestión de presupuesto de tokens |

---

## 6. El sistema multi-agente SDD

### Todos los agentes — estado actual

| Archivo | Fase | Descripción | Estado |
|---|---|---|---|
| `temper-orchestrator.agent.md` | — | Orquestador principal — evalúa complejidad y decide path | ✅ Completo |
| `temper-init.agent.md` | Fase 1 | Lee PRD, hace preguntas, genera `constitution.md` | ✅ Completo |
| `temper-spec.agent.md` | Fase 2 | User stories, criterios de aceptación, `specs/` | ✅ Completo |
| `temper-design.agent.md` | Fase 3 | Arquitectura, entidades, endpoints, `design.md` | ✅ Completo |
| `temper-tasks.agent.md` | Fase 4 | Rompe design en tareas atómicas por user story, `tasks/` | ✅ Completo |
| `temper-plan.agent.md` | Fase 5 | Genera build-plan.md con grupos paralelos | ✅ Completo |
| `temper-backend.agent.md` | Fase 5a | Genera código .NET 10 según task file asignado | ✅ Completo |
| `temper-frontend.agent.md` | Fase 5b | Genera código Blazor | ✅ Completo |
| `temper-tester.agent.md` | Fase 5c | Genera tests xUnit/bUnit | ✅ Completo |
| `temper-devops.agent.md` | Fase 5d | Docker, GitHub Actions | ✅ Completo |
| `temper-review.agent.md` | Fase 6 | Valida código contra specs/ | ✅ Completo |
| `temper-docs.agent.md` | Fase 7 | README, API docs, decisiones | ✅ Completo |

### Comandos slash

| Comando | Acción |
|---|---|
| `/temper-init` | Arranca el workflow SDD |
| `/temper-next` | Avanza a la siguiente fase |
| `/temper-status` | Muestra en qué fase estás |

### Estructura `.temper/` en cada proyecto

```
tu-proyecto/
└── .temper/
    ├── constitution.md        ← stack, arquitectura, estándares del proyecto
    ├── specs/                 ← user stories individuales
    │   ├── INDEX.md           ← índice rápido de todas las user stories
    │   └── US-001-*.md        ← cada user story en su propio archivo
    ├── design.md              ← entidades, endpoints, estructura de carpetas
    ├── tasks/                 ← tareas atómicas organizadas por user story
    │   ├── INDEX.md           ← índice rápido de todas las tareas
    │   └── US-001/            ← tareas de la user story US-001
    │       └── T001-*.md      ← cada tarea en su propio archivo
    ├── build-plan.md          ← plan de ejecución con grupos paralelos
    ├── orchestrator-state.md  ← estado persistente del orchestrator (checkpoint)
    ├── budget.md              ← tracking de uso de tokens por fase
    └── .snapshots/            ← snapshots automáticos para rollback
```

### Formato de agente (`.agent.md`)

Para GitHub Copilot CLI, los agentes van en `~/.copilot/agents/` con extensión `.agent.md`:

```markdown
---
name: temper-init
description: >
  Agente de inicialización de TemperAI. Usar cuando el usuario
  ejecuta /temper-init o quiere iniciar un nuevo proyecto desde un PRD.
mode: agent
allowed-tools: read_file, write_file, ask_followup_question
---

# temper-init — Agente de Inicialización

## Tu rol
Sos el primer agente del workflow SDD de TemperAI...
```

---

## 7. NeuralCore — memoria persistente

### Qué es

NeuralCore es el sistema de memoria persistente de TemperAI, inspirado en [Engram](https://github.com/Gentleman-Programming/engram) de Gentleman Programming, reimplementado en .NET con Clean Architecture.

### Estado: Construido ✅

NeuralCore ya tiene implementado:
- **Domain:** Entidades `Session` y `Observation` con enums, Entity base class
- **Application:** Result pattern, UseCases, DependencyInjection
- **Infrastructure:** EF Core + SQLite, repositorios, UnitOfWork, configuraciones
- **Api:** Controllers REST
- **Mcp:** MCP Server con herramientas `mem_save`, `mem_search`, `mem_context`, `mem_session_summary`
- **Tests:** Tests unitarios de dominio para Session y Observation

### Cómo funciona

1. El agente completa trabajo significativo (bugfix, decisión de arquitectura, etc.)
2. El agente llama `mem_save` con un resumen estructurado
3. Se persiste en SQLite con EF Core
4. En la próxima sesión, el agente llama `mem_search` para recuperar contexto relevante
5. Después de cualquier compactación/lobotomía, el agente llama `mem_context` para recuperar el estado

### Formato de memoria (What/Why/Where/Learned)

```
title: "Elegimos Clean Architecture para el módulo de pagos"
type: decision | bugfix | architecture | discovery | pattern | config | preference
content:
  What: Qué se hizo en una oración
  Why: Qué lo motivó
  Where: Archivos o paths afectados
  Learned: Gotchas, edge cases, cosas que sorprendieron (omitir si no hay)
```

### Schema de base de datos

```sql
-- Sesiones de trabajo
CREATE TABLE sessions (
    id TEXT PRIMARY KEY,
    project TEXT,
    directory TEXT,
    started_at TEXT,
    ended_at TEXT,
    summary TEXT,
    status TEXT
);

-- Las memorias reales
CREATE TABLE observations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT REFERENCES sessions(id),
    type TEXT,
    title TEXT,
    content TEXT,
    project TEXT,
    scope TEXT,
    topic_key TEXT,
    revision_count INTEGER,
    created_at TEXT,
    updated_at TEXT,
    deleted_at TEXT
);
```

---

## 8. Convenciones de código C#

Estas convenciones aplican a TODO el código del proyecto, incluyendo TemperAI y los proyectos que genera.

### Reglas absolutas — NUNCA se rompen

```
✗ NUNCA primary constructors — siempre constructor explícito
✗ NUNCA return expression => en métodos — siempre llaves {}
✗ NUNCA DataAnnotations en entidades
✗ NUNCA nvarchar(max) ni varchar(max)
✗ NUNCA .Update() de EF Core
✗ NUNCA async void — siempre async Task
✗ NUNCA .Result ni .Wait() — causa deadlocks
✗ NUNCA lazy loading — includes explícitos
✗ NUNCA throw para validaciones de negocio
✗ NUNCA saltarse warnings con ! (null-forgiving operator) sin justificación
✗ NUNCA using static — siempre using explícitos
✗ NUNCA global usings — siempre per-file using directives
✗ NUNCA var — siempre declaración explícita de tipo
✗ NUNCA named usings — renombrar la entidad o usar carpeta plural

✅ SIEMPRE nombres de variables igual al tipo — SaveResult saveResult, Product product
✅ SIEMPRE CancellationToken en métodos async públicos
✅ SIEMPRE sealed en clases que no se heredan
✅ SIEMPRE llaves {} inclusive en if de una sola línea
✅ SIEMPRE sufijo Dto en DTOs
✅ SIEMPRE prefijo To en extension methods de mapeo
✅ SIEMPRE GetByIdAsync (con tracking) vs GetByIdAsNoTrackingAsync (sin tracking)
✅ SIEMPRE varchar para ASCII, nvarchar para Unicode
✅ SIEMPRE longitud de columnas desde Entity.Rules
✅ SIEMPRE un IEntityTypeConfiguration<T> por entidad
✅ SIEMPRE código en inglés (excepto mensajes de error al usuario)
```

### Nomenclatura

| Elemento | Convención | Ejemplo |
|---|---|---|
| Casos de uso | Sin sufijo, PascalCase | `CreateProduct`, `UpdateProduct` |
| Interfaces de UC | Prefijo `I` | `ICreateProduct`, `IUpdateProduct` |
| DTOs de entrada | Sufijo `RequestDto` | `CreateProductRequestDto` |
| DTOs de salida | Sufijo `ResponseDto` | `CreateProductResponseDto` |
| Extension mapeos | Prefijo `To` + nombre DTO | `ToCreateProductResponseDto()` |
| Repositorios | `I` + nombre + `Repository` | `IProductRepository` |
| Eventos de dominio | Sufijo `Event` | `ProductCreatedEvent` |
| Configs EF | Nombre entidad + `Configuration` | `ProductConfiguration` |

### Estructura de proyectos generados

Para Clean Architecture:
```
src/
├── ProjectName.Api/
│   ├── Controllers/
│   ├── Middlewares/
│   ├── Extensions/
│   │   └── ResultExtensions.cs
│   └── Program.cs
├── ProjectName.Application/
│   ├── Contracts/
│   │   └── Services/
│   │       └── IEventPublisher.cs
│   ├── UseCases/
│   │   └── Products/
│   │       ├── ProductMappingExtensions.cs
│   │       ├── CreateProduct/
│   │       │   ├── ICreateProduct.cs
│   │       │   ├── CreateProduct.cs
│   │       │   ├── CreateProductRequestDto.cs
│   │       │   └── CreateProductResponseDto.cs
│   │       └── UpdateProduct/
│   │           ├── IUpdateProduct.cs
│   │           ├── UpdateProduct.cs
│   │           ├── UpdateProductRequestDto.cs
│   │           └── UpdateProductResponseDto.cs
│   ├── Common/
│   │   └── Result.cs
│   └── DependencyInjection.cs
├── ProjectName.Domain/
│   ├── Entities/
│   │   └── Product/
│   │       ├── Product.cs
│   │       ├── ValueObjects/
│   │       ├── Enums/
│   │       └── Events/
│   ├── Common/
│   │   └── Primitives/
│   │       ├── Entity.cs
│   │       └── IDomainEvent.cs
│   └── Errors/
└── ProjectName.Infrastructure/
    ├── Persistence/
    │   ├── Configurations/
    │   ├── Migrations/
    │   ├── Repositories/
    │   ├── UnitOfWork.cs
    │   └── AppDbContext.cs
    ├── Services/
    └── DependencyInjection.cs
```

---

## 9. Estado actual y próximos pasos

### ✅ Completado

- [x] `TemperAI.Core` — modelos, assets embebidos, configuración, snapshots, incremental, skills
- [x] `TemperAI.Installer` — lógica de instalación de Skills
- [x] Ejecutable standalone funcional (`temper-ai.exe`)
- [x] Estructura de assets embebidos funcionando
- [x] Skill `architecture/clean/SKILL.md` — Clean Architecture + DDD completo
- [x] Skill `architecture/hexagonal/SKILL.md` — Hexagonal completo
- [x] Skill `architecture/vertical-slice/SKILL.md` — Vertical Slice completo
- [x] Skill `architecture/onion/SKILL.md` — Onion completo
- [x] Skill `architecture/shared/SKILL.md` — Reglas comunes a todas las arquitecturas
- [x] Skill `dotnet-csharp/SKILL.md` — Estándares universales de C#
- [x] Skill `backend/dotnet/api/SKILL.md` — ASP.NET Core API
- [x] Skill `backend/dotnet/ef-core/SKILL.md` — EF Core
- [x] Skill `backend/dotnet/linq/SKILL.md` — LINQ
- [x] Skill `backend/dotnet/ddd/SKILL.md` — DDD
- [x] Skill `backend/dotnet/testing/SKILL.md` — Testing
- [x] Skill `frontend/blazor/SKILL.md` — Blazor
- [x] Skill `frontend/bunit/SKILL.md` — bUnit
- [x] Skill `devops/docker/SKILL.md` — Docker
- [x] Skill `devops/github-actions/SKILL.md` — GitHub Actions
- [x] Skill `devops/ci-cd/SKILL.md` — CI/CD
- [x] Skill `prd-analyzer/SKILL.md` — PRD Analysis
- [x] Skill `token-budget/SKILL.md` — Token Budget
- [x] Los 12 agentes SDD completos (orchestrator, init, spec, design, tasks, build, backend, frontend, tester, devops, review, docs)
- [x] Comandos slash (temper-init, temper-next, temper-status)
- [x] Documentación completa (README, ARCHITECTURE, CONVENTIONS, CLI, AGENTS, SKILLS, WORKFLOW, GETTING_STARTED)
- [x] `TemperAI.NeuralCore` — memoria persistente MCP en .NET con Clean Architecture
  - [x] Domain entities (Session, Observation)
  - [x] EF Core + SQLite + UnitOfWork + Repositories
  - [x] MCP Server con herramientas (mem_save, mem_search, mem_context, mem_session_summary)
  - [x] HTTP API Controllers
  - [x] Tests unitarios de dominio

### 📋 Backlog ordenado

1. [ ] Agregar comando `temper-ai update` — actualiza Skills desde versión nueva
2. [ ] Agregar comando `temper-ai status` — muestra qué está instalado
3. [ ] Agregar comando `temper-ai budget` — view token usage tracking
4. [ ] Agregar comando `temper-ai snapshot` — manage snapshots for rollback
5. [ ] Agregar comando `temper-ai incremental` — detect which phases need re-running
6. [ ] Agregar comando `temper-ai skill` — create, install, and discover custom skills
7. [ ] Agregar comando `temper-ai setup` — install CLI to global PATH
8. [ ] Agregar tests de integración para NeuralCore API
9. [ ] Agregar tests unitarios para InstallerService
10. [ ] Dockerizar NeuralCore
11. [ ] Agregar CI/CD pipeline para TemperAI

---

## Prompt de continuación para otras IAs

Si estás leyendo esto para continuar el trabajo, usá este prompt como contexto inicial:

```
Estoy trabajando en TemperAI, un configurador de ecosistema AI para desarrolladores .NET.
Lee el archivo TEMPER_AI_PROYECTO.md que te voy a pasar — contiene toda la documentación
del proyecto, las decisiones de arquitectura tomadas, las convenciones de código, y el
estado actual.

IMPORTANTE:
- Todo el código es C# / .NET 10
- Usamos Spectre.Console para la TUI del CLI
- Las convenciones de código están en la sección 8 — respétalas siempre
- El próximo paso está en la sección 9
- No uses primary constructors en nada
- No uses return expression => en métodos
- Los casos de uso no tienen sufijo UseCase
- Los DTOs siempre son sealed record con propiedades explícitas y sufijo Dto

El próximo paso es: [DESCRIBÍ QUÉ QUERÉS HACER]
```

---

*Documento actualizado el 05/04/2026. Actualizar este archivo cuando se completen items del backlog.*
