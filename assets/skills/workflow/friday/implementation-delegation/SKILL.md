---
name: friday-implementation-delegation
description: >
  Implementation-agent delegation contract for FRIDAY. Load when FRIDAY
  delegates to temper-backend, temper-frontend, temper-tester, temper-devops, or
  another implementation agent, including task-driven, direct-action, bugfix,
  and recovery turns.
---

# FRIDAY Implementation Agent Delegation Contract

## Purpose

This skill defines the execution-agent delegation contract for FRIDAY.

When loaded, it is authoritative for:

- Task-file execution prompts.
- Approved direct-action implementation prompts.
- Bugfix prompts without a task file.
- Implementation-agent recovery prompts.
- Prompt-failure correction for implementation agents.
- Implementation-agent pre-delegation checks.

Use this skill when FRIDAY delegates to `temper-backend`, `temper-frontend`, `temper-tester`, `temper-devops`, or another implementation agent.

`workflow/friday/state-schema` remains authoritative for universal delegation prohibitions.

## Task-File Execution Contract

For normal task-driven implementation, the prompt must contain only:

```text
Implement task T001: Add Product to Inventory (user-story US-001, Backend)
```

That is the whole prompt.

FRIDAY must not add:

- File paths.
- File names.
- Skill names.
- Load/read/check instructions.
- Acceptance criteria.
- Task summaries.
- Class, DTO, interface, or method names.
- Architecture or layer guidance.

The implementation agent resolves its task metadata from `Plan/INDEX.md`, reads the task file at `Location`, reads the parent work item source file, and loads its own skills.

## Bugfix Contract

When there is no task file and the user asks for a direct bugfix, delegate in plain domain language:

```text
Fix bug: Order total calculates incorrectly when discount applies.
Affected area: Order total calculation.
Expected behavior: Discounted orders show correct total after tax.
```

Rules:

- Include `Affected area:` only when the user provided it or it is necessary to avoid ambiguity.
- Keep the description behavioral, not technical.
- Do not convert the bug into implementation instructions.

## Approved Direct-Action Contract

When backend implementation is approved without task artifacts, delegate in plain domain language and state the mode explicitly:

```text
Approved direct action: Implement backend change for order discount validation.
Affected area: Discount validation during order updates.
Expected behavior: Invalid discounts are rejected and valid discounts are preserved.
Constraints: No task file or Plan artifacts exist for this request.
```

Rules:

- Use this only when the user explicitly approved direct action.
- Use this only when normal task/work-item artifacts are absent or intentionally bypassed.
- Keep the request behavioral, not technical.
- Do not include file paths, class names, method names, skill names, or implementation instructions.
- Include `Affected area:` only when it helps disambiguate scope.
- Include `Constraints:` only when FRIDAY must preserve the no-task direct-action boundary or another user-approved limit.
- Do not pretend a task ID, work item ID, or artifact exists when it does not.

## Recovery Contract

When an implementation agent failed after creating partial work, re-delegate with the minimum continuation context:

```text
Task: Implement task T003: Add Order Status Validation (user-story US-002, Backend)
Error: Agent ran out of context before completing the handler.
Existing work: Status enum updated, validator created, interface created.
Instruction: Continue from where the previous attempt stopped. Do not regenerate completed work. Finish the remaining scope.
```

Rules:

- State what the agent was doing.
- State the actual failure or unusable output.
- State what already exists.
- Instruct the agent to continue, not restart.
- Keep the recovery prompt focused on remaining scope only.

## Prompt-Failure Contract

If output is unusable because FRIDAY's original prompt missed necessary clarification, re-delegate with only that missing clarification added.

```text
Task: [original task]
Clarification: [exact missing context]
Instruction: Complete the task using this clarification.
```

Do not resend large restated context if one missing clarification is enough.

## Pre-Delegation Checklist

- The target agent is a task-driven implementation or execution step.
- For task-file execution, the prompt is exactly `Implement task [T###]: [title] ([work item type] [work item id], [category])`.
- For approved backend direct action without task artifacts, the prompt starts with `Approved direct action:` and stays in plain behavioral language.
- For bugfixes, the prompt stays in behavioral domain language.
- No file paths, skill names, or internal instructions are included.
- No acceptance criteria, class names, DTO names, or layer guidance are included.
- If this is recovery, the prompt tells the agent to continue from existing work.

If any check fails, rewrite to the smallest prompt that preserves the missing information.

## Routing Boundary

This skill is not for analyst or architect loops.

For those agents, load their dedicated Friday workflow contracts:

- `workflow/friday/analyst-communication`.
- `workflow/friday/architect-communication`.
