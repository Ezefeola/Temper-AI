# FRIDAY Workflow — Supported TemperAI Model

> This document explains the current supported TemperAI workflow.
> Scope: **FRIDAY-centered model only**.

## 1. What FRIDAY is

FRIDAY is the orchestrator for TemperAI.

It does not implement features, write architecture, or review code itself. Its job is to:

- understand the user's request
- classify the request
- choose the right specialist
- propose a plan
- wait for explicit approval when required
- delegate one specialist at a time
- persist minimal workflow state in `.temper/friday-state.json`
- checkpoint after every specialist completion

## 2. What TemperAI is intended to do

TemperAI is designed for structured software delivery in OpenCode and Claude Code.

The intended usage model is:

1. Start with FRIDAY.
2. Let FRIDAY route the work to specialists.
3. Approve specialist steps explicitly.
4. Progress through requirements, architecture, planning, implementation, review, and docs in order.
5. Use saved state to continue safely across sessions.

TemperAI is not designed around one giant prompt. It is designed around **workflow contracts**.

## 3. Core operating rules

These are the main rules encoded in the current FRIDAY model:

- FRIDAY never implements specialist work directly.
- FRIDAY delegates exactly **one specialist per session**.
- Reading plus doing is never treated as a trivial action.
- Medium or complex work requires explicit approval before delegation.
- Analyst Phase 1 and Phase 2 are separate and Phase 2 cannot be skipped in the normal flow.
- Normal architecture work happens after analyst specs are complete.
- Implementation normally starts only after tasks and plan artifacts exist.

## 4. End-to-end workflow

### Phase A — Entry through FRIDAY

The user starts with:

```text
/temper-friday
```

FRIDAY then:

1. checks `.temper/friday-state.json` when relevant
2. announces current workflow state
3. classifies the request
4. proposes the leanest valid plan
5. asks for approval if execution is needed

### Phase B — Functional definition

FRIDAY delegates to `temper-analyst`:

- **Phase 1:** create `Docs/Functional-Analysis/PRD.md`
- **Phase 2:** create `Plan/User-Stories/`

FRIDAY manages analyst gap and ambiguity loops using saved cycle state.

### Phase C — Architecture

FRIDAY delegates to `temper-architect` to:

- analyze the approved requirements context
- propose architecture decisions
- gather technical clarifications only when needed
- generate architecture documents after confirmation

### Phase D — Tasking and planning

FRIDAY delegates to:

- `temper-tasks` to create task files under `Plan/`
- `temper-plan` to generate `Plan/BUILD.md`

This transforms approved design into executable work.

### Phase E — Implementation

FRIDAY delegates task execution to one of:

- `temper-backend`
- `temper-frontend`
- `temper-tester`
- `temper-devops`

These agents resolve task context from `Plan/INDEX.md` and their task files.

### Phase F — Review and documentation

After implementation:

- `temper-review` validates output against TemperAI rules and planned behavior
- `temper-docs` produces final project documentation

## 5. Approval model

FRIDAY requires explicit approval before starting work that creates or modifies artifacts in normal Medium or Complex flows.

Examples of approval words recognized by FRIDAY include:

- `approve`
- `approved`
- `yes`
- `proceed`
- `go ahead`
- `execute`

FRIDAY does not infer approval from vague agreement.

## 6. State model

FRIDAY uses one persistent orchestration file:

```text
.temper/friday-state.json
```

That file stores only the minimum state needed to resume safely, including:

- workflow status
- approved plan
- current step
- active specialist cycle
- next action
- compact session metrics used for session-mode recommendations

FRIDAY does not use that file as a transcript dump.

## 7. Session continuation model

After a specialist finishes, FRIDAY checkpoints the result and stops.

If a meaningful approved continuation exists, FRIDAY may recommend either:

- `continue here`
- `clean session`

That recommendation is based on session-risk signals such as specialist count, recovery history, long outputs, and scope confusion.

## 8. Typical user journey

### New project

1. Start FRIDAY.
2. Describe the product idea.
3. Approve analyst work.
4. Approve specs.
5. Approve architecture.
6. Approve task generation.
7. Approve build planning.
8. Approve execution steps as FRIDAY advances through implementation.
9. Review output.
10. Generate final docs.

### Existing project change or bug

1. Start FRIDAY.
2. Describe the change or bug.
3. Let FRIDAY classify whether the request needs analysis, architecture, direct execution, review, or documentation.
4. Approve the proposed step.
5. Continue step by step.

## 9. What is in scope for supported documentation

This supported workflow documentation covers:

- FRIDAY
- the active TemperAI specialist agents
- the active skills those agents use
- the intended OpenCode usage model

It intentionally excludes legacy orchestrator documentation.

## 10. Related references

- [Active agents](agents.md)
- [Active skills](skills.md)
- [Human-readable skill catalog](../assets/docs/skills-catalog.md)
