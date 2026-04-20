# TemperAI Agents

## Overview

Agents are specialized sub-agents in the TemperAI SDD workflow. Each agent handles a specific phase or implementation task with fresh context, minimal token usage, and maximum precision.

## Phase Agents

| Agent | Phase | Description |
|-------|-------|-------------|
| `temper-discover` | 1 | Discovery agent — gathers project requirements through questions |
| `temper-constitution` | 2 | Constitution agent — generates project constitution from discovered requirements |
| `temper-spec` | 3 | Specification agent — generates user stories and acceptance criteria |
| `temper-design` | 4 | Design agent — produces architecture design, entities, API endpoints |
| `temper-tasks` | 5 | Task breakdown agent — breaks design into atomic implementation tasks |
| `temper-plan` | 6 | Build planner — creates execution strategy with parallel groups |
| `temper-review` | 7 | Quality review — validates code against conventions and specs |
| `temper-docs` | 8 | Documentation agent — generates README, ARCHITECTURE, API docs |

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
