# Active Agents — FRIDAY-Centered TemperAI

> Supported scope: FRIDAY-centered model only.

## Agent map

| Agent | Type | Primary purpose | Main outputs |
|---|---|---|---|
| `temper-friday` | Orchestrator | Routes work, manages approvals, tracks state | Plans, delegations, checkpoints, `.temper/friday-state.json` |
| `temper-analyst` | Specialist | Functional analysis and specs | `Docs/Functional-Analysis/PRD.md`, `Plan/User-Stories/` |
| `temper-architect` | Specialist | Technical architecture and design docs | architecture proposal, config docs, architecture docs |
| `temper-tasks` | Specialist | Task breakdown | task files under `Plan/` and updated `Plan/INDEX.md` |
| `temper-plan` | Specialist | Execution planning | `Plan/BUILD.md` |
| `temper-backend` | Specialist | .NET backend implementation | backend source changes |
| `temper-frontend` | Specialist | Frontend implementation | Blazor or Angular source changes |
| `temper-tester` | Specialist | Test implementation | test source changes |
| `temper-devops` | Specialist | Docker and CI/CD implementation | Docker and workflow files |
| `temper-review` | Specialist | Quality review | review report |
| `temper-docs` | Specialist | Final documentation | README and `Docs/*` outputs |

## 1. `temper-friday`

**Role:** supported orchestrator.

FRIDAY is responsible for:

- request classification
- routing
- approval gates
- state persistence
- specialist loop resumption
- post-specialist checkpointing

FRIDAY does **not** implement, review, or document the project itself.

### When FRIDAY is used

- new project workflow
- continuation of an existing TemperAI workflow
- change-direction handling
- recovery after failed specialist runs
- direct orchestration questions

### Key boundaries

- never writes implementation artifacts
- never runs multiple specialists in one session
- never skips required workflow gates silently

### Main skills

- `friday-state-schema`
- `friday-prompt-excellence`
- `friday-analyst-communication`
- `friday-architect-communication`
- `friday-implementation-delegation`
- `friday-session-mode-recommendation`

## 2. `temper-analyst`

**Role:** functional analyst.

### Phases

- **Phase 1:** requirements elicitation and PRD generation
- **Phase 2:** spec generation from the approved PRD

### Main outputs

- `Docs/Functional-Analysis/PRD.md`
- `Plan/User-Stories/`

### Key boundaries

- no technology decisions
- no architecture decisions
- no implementation work
- structured reports only

### Main skills

- `functional-analysis`
- `analyst-reasoning`
- `analyst-report-formats`
- `analyst-prd-template`
- `spec-generator`

## 3. `temper-architect`

**Role:** software architect.

### Modes

- **Architectural Design**
- **Problem Solving**

### Main outputs

- architecture proposals
- backend/frontend config docs
- optional architecture documentation set

### Key boundaries

- does not implement code
- does not change functional scope
- does not read specs in normal architectural-design mode

### Main skills

- `architect-proposal-formats`
- `architect-document-templates`

## 4. `temper-tasks`

**Role:** task breakdown agent.

### Main outputs

- atomic task files under `Plan/`
- updated `Plan/INDEX.md`

### Key boundaries

- no code
- no architecture design
- no implementation detail prescriptions

### Main skills

- none by default
- `setup-tasks` for new-project setup generation

## 5. `temper-plan`

**Role:** build planner.

### Main output

- `Plan/BUILD.md`

### Key boundaries

- planner only
- no code
- no task generation

### Main skills

- none

## 6. `temper-backend`

**Role:** .NET backend implementation agent.

### What it does

- resolves the assigned task from `Plan/INDEX.md`
- reads its task file and parent work item
- loads the exact required backend skills
- implements production backend changes

### Key boundaries

- no architectural re-design
- no functional scope changes
- no speculative skill loading

### Always-on skill core

- `backend-dotnet-csharp`
- chosen architecture skill: `clean-architecture` / `hexagonal-architecture` / `vertical-slice-architecture` / `onion-architecture`
- `result-pattern`
- `ddd-ubiquitous-language`
- `solid-clean-code`

### Conditional skill examples

- `dto-conventions`
- `use-case-patterns`
- `dotnet-api`
- `api-docs-scalar` or `api-docs-swagger`
- `dotnet-ddd`
- `entity-configuration`
- `repository-pattern`
- `dbcontext-setup`
- `backend-dotnet-orms-ef-core-queries`
- `repository-usage`
- `dotnet-linq`
- `bulk-operations`

## 7. `temper-frontend`

**Role:** frontend implementation agent.

### Supported stacks

- Blazor / .NET 10
- Angular

### Key behavior

- detects the active frontend stack
- loads only stack-matching skills
- avoids cross-stack skill mixing unless explicitly required

### Main skills

Blazor path:

- `backend-dotnet-csharp`
- `blazor`
- `blazor-server`
- `mudblazor`
- `tailwind`
- `bunit`

Angular path:

- `angular`
- `angular-material`
- `scss`

## 8. `temper-tester`

**Role:** testing implementation agent.

### Main outputs

- unit tests
- integration tests
- component tests where applicable

### Main skills

- `backend-dotnet-csharp`
- `dotnet-testing`

## 9. `temper-devops`

**Role:** infrastructure and pipeline implementation agent.

### Main outputs

- Dockerfiles
- `docker-compose`
- GitHub Actions workflows
- related infrastructure config

### Main skills

- `docker`
- `github-actions`

## 10. `temper-review`

**Role:** quality review agent.

### What it checks

- build and test gates
- coding convention violations
- architecture-rule violations
- coverage of planned behavior

### Main skills

- `backend-dotnet-csharp`
- `dotnet-api`
- `result-pattern`
- `dto-conventions`
- `use-case-patterns`
- `solid-clean-code`
- chosen architecture skill

## 11. `temper-docs`

**Role:** final documentation agent.

### Main outputs

- `README.md`
- `Docs/SYSTEM.md`
- `Docs/ARCHITECTURE.md`
- `Docs/API.md`
- `Docs/CHANGELOG.md`

### Key boundaries

- no code
- no review work
- no duplication of architect reference documents

### Main skills

- none

## How agents relate to each other

The default progression is:

1. `temper-friday`
2. `temper-analyst`
3. `temper-architect`
4. `temper-tasks`
5. `temper-plan`
6. implementation agents (`temper-backend`, `temper-frontend`, `temper-tester`, `temper-devops`)
7. `temper-review`
8. `temper-docs`

FRIDAY manages the transitions and approval gates between those steps.
