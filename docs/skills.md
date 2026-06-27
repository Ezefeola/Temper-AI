# Active Skills — FRIDAY-Centered TemperAI

> This catalog lists the skills actively referenced by the current FRIDAY-centered agent contracts.

## 1. How skills work

Skills are reusable instruction modules.

Agents load them to get:

- rules
- output contracts
- architecture conventions
- framework-specific standards
- delegation mechanics

In the supported TemperAI model, skills are part of the runtime contract. They are not optional reading.

## 2. FRIDAY workflow skills

| Skill | Purpose | Used by |
|---|---|---|
| `friday-state-schema` | State schema, resume safety, status rules, delegation prerequisites | `temper-friday` |
| `friday-prompt-excellence` | Delegation prompt quality and recovery prompt craft | `temper-friday` |
| `friday-analyst-communication` | Contract for analyst delegation and analyst loop resumption | `temper-friday` |
| `friday-architect-communication` | Contract for architect delegation and architect loop resumption | `temper-friday` |
| `friday-implementation-delegation` | Contract for task-driven implementation delegation | `temper-friday` |
| `friday-session-mode-recommendation` | Policy for `clean session` vs `continue here` | `temper-friday` |

## 3. Analyst skills

| Skill | Purpose | Used by |
|---|---|---|
| `functional-analysis` | Phase 1 functional analysis guidance | `temper-analyst` |
| `analyst-reasoning` | Internal self-questioning for gap detection and completeness | `temper-analyst` |
| `analyst-report-formats` | Structured reports for startup, gaps, contradictions, completion, and more | `temper-analyst` |
| `analyst-prd-template` | Exact PRD generation structure | `temper-analyst` |
| `spec-generator` | Phase 2 user-story/spec generation | `temper-analyst` |

## 4. Architect skills

| Skill | Purpose | Used by |
|---|---|---|
| `architect-design-workflow` | Mode A workflow — context analysis, proposal, document offer, and generation | `temper-architect` |
| `architect-problem-solving-workflow` | Mode B workflow — problem analysis, plan, and optional plan document | `temper-architect` |
| `architect-proposal-formats` | Structured mode, proposal, clarification, and completion reports | `temper-architect` |
| `architect-document-templates` | Templates for generated architecture/configuration documents | `temper-architect` |

## 5. Tasking and planning skills

| Skill | Purpose | Used by |
|---|---|---|
| `setup-tasks` | Generates required setup work for new projects | `temper-tasks` |

`temper-plan` does not load skills in the current contract.

## 6. Backend skills

### Always-used backend core

| Skill | Purpose |
|---|---|
| `backend-dotnet-csharp` | Universal .NET and C# conventions |
| `clean-architecture` / `hexagonal-architecture` / `vertical-slice-architecture` / `onion-architecture` | Structural rules based on chosen architecture |
| `result-pattern` | Standard result handling |
| `ddd-ubiquitous-language` | Consistent domain language understanding |
| `solid-clean-code` | SOLID and maintainability rules |

### Conditional backend skills

| Skill | Purpose |
|---|---|
| `dto-conventions` | DTO structure and naming |
| `use-case-patterns` | Use-case/controller invocation rules |
| `dotnet-api` | ASP.NET Core API conventions |
| `api-docs-scalar` | Scalar API documentation provider |
| `api-docs-swagger` | Swagger API documentation provider |
| `dotnet-ddd` | Domain entities, aggregates, and events |
| `entity-configuration` | EF Core Fluent API entity configuration |
| `repository-pattern` | Repository and UnitOfWork creation |
| `dbcontext-setup` | DbContext creation and registration |
| `backend-dotnet-orms-ef-core-queries` | EF Core query composition |
| `repository-usage` | Correct use of existing repositories |
| `dotnet-linq` | LINQ rules |
| `bulk-operations` | High-volume insert and batch guidance |

## 7. Frontend skills

### Blazor path

| Skill | Purpose |
|---|---|
| `backend-dotnet-csharp` | C# rules for Blazor work |
| `blazor` | Blazor WebAssembly standards |
| `blazor-server` | Blazor Server / interactive server rendering standards |
| `mudblazor` | MudBlazor component standards |
| `tailwind` | Tailwind styling standards |
| `bunit` | Blazor component testing |

### Angular path

| Skill | Purpose |
|---|---|
| `angular` | Angular component, service, routing, state, and test standards |
| `angular-material` | Angular Material standards |
| `scss` | SCSS styling standards |

## 8. Testing skills

| Skill | Purpose | Used by |
|---|---|---|
| `backend-dotnet-csharp` | C# conventions for test code | `temper-tester` |
| `dotnet-testing` | xUnit, Moq, integration, and component test guidance | `temper-tester` |

## 9. DevOps skills

| Skill | Purpose | Used by |
|---|---|---|
| `docker` | Dockerfiles, compose, and container conventions | `temper-devops` |
| `github-actions` | GitHub Actions workflow conventions | `temper-devops` |

## 10. Review skills

`temper-review` reuses implementation conventions as audit criteria.

| Skill | Purpose |
|---|---|
| `backend-dotnet-csharp` | Core coding rules |
| `dotnet-api` | API review rules |
| `result-pattern` | Result contract review |
| `dto-conventions` | DTO review rules |
| `use-case-patterns` | Use-case review rules |
| `solid-clean-code` | Clean code review rules |
| chosen architecture skill | Architecture-specific review rules |

## 11. Agents with no active skill loading

| Agent | Skill policy |
|---|---|
| `temper-plan` | none |
| `temper-docs` | none |

`temper-tasks` loads no skills by default and uses `setup-tasks` only for new-project setup generation.

## 12. Agent-to-skill relationship summary

| Agent | Direct skills | Conditional skills |
|---|---|---|
| `temper-friday` | all `friday-*` skills | based on routing, resume, recovery, and session state |
| `temper-analyst` | `analyst-report-formats` | `functional-analysis`, `analyst-reasoning`, `analyst-prd-template`, `spec-generator` by phase |
| `temper-architect` | `architect-proposal-formats` | `architect-design-workflow` or `architect-problem-solving-workflow` by mode; `architect-document-templates` when generating documents |
| `temper-tasks` | none | `setup-tasks` for new projects |
| `temper-plan` | none | none |
| `temper-backend` | backend core skills | API, DDD, EF Core, DTO, query, docs-provider, and batch skills as required |
| `temper-frontend` | stack-detection-driven | Blazor or Angular skill set only |
| `temper-tester` | `backend-dotnet-csharp`, `dotnet-testing` | none in current contract |
| `temper-devops` | `docker`, `github-actions` | none in current contract |
| `temper-review` | review baseline skills | chosen architecture skill |
| `temper-docs` | none | none |

## 13. Scope note

This document intentionally describes the active FRIDAY-centered skill model only. It does not document legacy orchestrator skill families.
