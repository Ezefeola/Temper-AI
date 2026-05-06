# TemperAI Agents

## Overview

Agents are specialized sub-agents in the TemperAI SDD workflow. Each agent handles a specific phase or implementation task with fresh context, minimal token usage, and maximum precision.

## Phase Agents

| Agent | Phase | Description |
|-------|-------|-------------|
| `temper-analyst` | 1 | Functional analyst — Phase 1: PRD. Phase 2: User Stories (after PRD approval). |
| `temper-architect` | 2 | Technical architect — defines stack, architecture, domain model, generates config files and DDD docs |
| `temper-tasks` | 3 | Task breakdown agent — breaks design into atomic implementation tasks |
| `temper-plan` | 4 | Build planner — creates execution strategy with parallel groups |
| `temper-review` | 5 | Quality review — validates code against conventions and specs |
| `temper-docs` | 6 | Documentation agent — generates README, ARCHITECTURE, API docs |

## Build Execution Agents

| Agent | Description |
|-------|-------------|
| `temper-orchestrator` | Main orchestrator — coordinates all phases and spawns sub-agents |
| `temper-backend` | Backend implementation — creates C# code for entities, use cases, endpoints |
| `temper-frontend` | Frontend implementation — creates Blazor components and pages |
| `temper-tester` | Testing implementation — writes xUnit and bUnit tests |
| `temper-devops` | DevOps implementation — generates Dockerfiles, CI/CD workflows |

## Agent Routing

Full routing table available in `AGENTS.md` at project root.

## Context Rules

- Each phase starts fresh — no accumulated context from previous phases
- Only load files the current phase needs — never the entire codebase
- Quick path for 1-2 file changes — full pipeline for 3+ files or architectural changes

## Separation of Concerns

- **temper-analyst** handles ONLY functional/business questions ("What should users be able to do?")
- **temper-architect** handles ONLY technical questions (database, architecture, frontend type, auth)
- Neither agent crosses into the other's domain — analyst never asks about tech, architect never changes scope
