---
name: implementation-delegation
description: >
  Implementation-agent delegation contract for the TemperAI orchestrator. Load
  when JARVIS delegates to temper-backend, temper-frontend, temper-tester,
  temper-devops, or another task-driven execution agent, including bugfix and
  recovery turns.
---

# JARVIS <-> Implementation Agent Delegation Contract

## Purpose

This skill defines the execution-agent delegation contract for JARVIS.

When this skill is loaded, it is the authoritative source for:
- task-file execution prompts
- bugfix prompts without a task file
- implementation-agent recovery prompts
- implementation-agent pre-delegation checks

`assets/agents/temper-jarvis.agent.md` keeps only universal orchestration
logic and the rule that JARVIS must load the relevant workflow skill before
delegating specialist behavior.

Use this skill when JARVIS is:
- delegating to `temper-backend`
- delegating to `temper-frontend`
- delegating to `temper-tester`
- delegating to `temper-devops`
- delegating to another task-driven execution step that should run from a task
  reference instead of a conversational handoff
- re-delegating after execution failure or prompt failure for those agents

`workflow/jarvis/state-schema` remains authoritative for universal prompt
prohibitions. This skill adds the implementation-agent-specific execution
contract that sits on top of those universal rules.

---

## Task-file execution contract

For a normal task-driven implementation step, the prompt must contain only:

```
Implement task T001: Add Product to Inventory (user-story US-001, Backend)
```

That is the whole prompt.

JARVIS must not add:
- file paths
- file names
- skill names
- load/read/check instructions
- acceptance criteria
- task summaries
- class, DTO, interface, or method names
- architecture or layer guidance

The implementation agent resolves its task metadata from `Plan/INDEX.md`, reads the task file at `Location`, reads the parent work item source file, and loads its own skills.

---

## Bugfix contract

When there is no task file and the user is asking for a direct bugfix,
delegate in plain domain language:

```
Fix bug: Order total calculates incorrectly when discount applies
Affected area: Order total calculation
Expected behavior: Discounted orders show correct total after tax
```

Rules:
- Include `Affected area:` only when the user explicitly provided it or it is
  necessary to avoid ambiguity.
- Keep the description behavioral, not technical.
- Do not convert the bug into implementation instructions.

---

## Recovery contract

When an implementation agent failed after creating partial work, re-delegate
with the minimum continuation context:

```
Task: Implement task T003: Add Order Status Validation (user-story US-002, Backend)
Error: Agent ran out of context before completing the handler.
Existing work: Status enum updated, validator created, interface created.
Instruction: Continue from where the previous attempt stopped. Do not
regenerate completed work. Finish the remaining scope.
```

Rules:
- State what the agent was doing.
- State the actual failure or unusable output.
- State what already exists.
- Instruct the agent to continue, not restart.
- Keep the recovery prompt focused on remaining scope only.

---

## Prompt-failure contract

If the implementation agent's output is unusable because the original prompt
was missing a necessary clarification, re-delegate with only that missing
clarification added.

Template:

```
Task: [original task]
Clarification: [exact missing context]
Instruction: Complete the task using this clarification.
```

Do not resend large restated context if one missing clarification is enough.

---

## Pre-delegation checklist

Before sending a prompt to an implementation agent, verify all of these:

- [ ] The target agent really is a task-driven implementation or execution step
- [ ] For task-file execution, the prompt is exactly `Implement task [T###]: [title] ([work item type] [work item id], [category])`
- [ ] For bugfixes, the prompt stays in domain language only
- [ ] No file paths, skill names, or internal instructions are included
- [ ] No acceptance criteria, class names, DTO names, or layer guidance are included
- [ ] If this is recovery, the prompt tells the agent to continue from existing work

If any check fails, rewrite to the smallest prompt that still preserves the
missing information.

---

## Routing boundary

This skill is not for analyst or architect loops.

For those agents, load their dedicated workflow contracts instead:
- `workflow/jarvis/analyst-communication`
- `workflow/jarvis/architect-communication`
