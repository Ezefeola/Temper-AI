---
name: friday-prompt-excellence
description: >
  Universal prompt engineering techniques for the FRIDAY orchestrator. Load when
  FRIDAY needs prompt-writing craft, context control, recovery prompts, or
  domain-language reformulation.
---

# FRIDAY Prompt Excellence

## Purpose

This skill teaches FRIDAY how to construct delegation prompts that specialists can execute reliably. `friday-state-schema` defines hard prohibitions. Agent-specific Friday workflow skills define exact handoff formats.

A delegation prompt is not everything the specialist might need. It is exactly what the specialist cannot derive from its own role, task context, and workflow contract.

## Prompt Anatomy

Most delegation prompts need only a task statement. Use additional sections only when the target contract requires them.

| Part | Use When | Rule |
| --- | --- | --- |
| Context | The specialist lacks necessary plain-language background. | Keep it domain-level and minimal. |
| Task | Always. | Use one imperative action. |
| Format | Non-standard delivery is needed. | Otherwise trust the specialist's standard output. |
| Constraints | A boundary must be explicit. | State boundaries, not implementation instructions. |

Minimal discovery prompt:

```text
Context: User wants to define a warehouse inventory workflow. No structured requirements exist yet.
Task: Analyze the request and identify missing functional information.
Format: Produce your standard structured analysis output.
Constraints: Do not propose technical architecture.
```

## Minimal Delegation Principle

Give each specialist only what it cannot derive itself. Extra context creates ambiguity, contradictions, and token waste.

Minimal delegation is insufficient only when:

- The target Friday workflow contract requires more than a task reference.
- The specialist is in a multi-turn analyst or architect loop.
- The handoff is a bugfix, recovery turn, or prompt-failure turn.

For implementation-agent details, load `workflow/friday/implementation-delegation`.

## Context Modes

| Mode | Include | Omit |
| --- | --- | --- |
| Task-file execution | Only the task reference required by the contract. | Restated domain summary, file paths, skill names, class names. |
| Conversation or discovery | User request plus minimum plain-language context. | File paths, internal artifact references, implementation hints. |
| Validation or review | What exists and what to validate against. | How it was built unless explicitly needed. |
| Recovery | Original task, exact failure, known partial result, remaining action. | Full regenerated context or unrelated prior work. |

## Multi-Turn Prompt Pattern

For analyst and architect loops:

- Preserve the specialist's meaning exactly.
- Surface specialist output without rewriting it into a different artifact.
- Reduce the next user interaction to the smallest actionable prompt only when reliable.
- Pass the user's next reply back exactly as received.

If the user says they do not understand specialist output, do not explain it yourself. Ask the specialist to rephrase or clarify.

## Prompt Failure Recovery

Prompt failure means FRIDAY's prompt was unclear, incomplete, overbroad, or incorrectly scoped. Correct the prompt with the smallest missing clarification.

Signals:

- The specialist asks for context FRIDAY should have supplied.
- The output solves a broader or different problem than the approved step.
- The specialist mixes phases, such as producing architecture before Phase 2 specs.
- The specialist returns generic advice instead of the expected artifact or report.
- The specialist follows an implementation hint that FRIDAY should not have included.

Template:

```text
Task: [original task]
Clarification: [exact missing context]
Instruction: Complete the task using this clarification.
```

Do not resend large restated context if one missing clarification is enough.

## Agent Failure Recovery

Agent failure means the specialist errored, stopped mid-task, violated scope, or returned unusable output despite a valid prompt.

Signals:

- Tool/runtime error, timeout, or context exhaustion after a valid prompt.
- The agent stops mid-report or mid-task without a valid completion state.
- The agent explicitly says it cannot continue because of its own failure.
- The agent violates its contract even though FRIDAY supplied the correct contract-shaped prompt.

Template:

```text
Task: [original task]
Error: [exact error or unusable behavior]
Existing work: [brief partial result]
Instruction: Continue from where the previous attempt stopped. Do not regenerate completed work. Complete the remaining scope.
```

Ask for approval before re-delegating if recovery can change artifacts, state, or scope.

## Ambiguity Escalation

Ask the user only for domain ambiguity that affects product intent, functional scope, stakeholders, policy, priorities, or user-facing behavior.

Route technical ambiguity to `temper-architect`, including architecture pattern, stack choice, infrastructure, persistence, deployment, integration design, and implementation strategy.

Never escalate and then assume. Either wait for the answer or explicitly document the approved assumption in FRIDAY state.

Signals to ask the user:

- Two possible product behaviors would both be valid but affect users differently.
- A policy, role, permission, or business rule is missing.
- The priority or scope boundary changes which specialist should run next.

Signals to route to architect:

- The uncertainty is about platform, integration, data storage, deployment, architecture pattern, or tradeoff selection.
- The user asks how it should be built rather than what it must do.

## Context Diagnostics

Use these diagnostics before delegation and after specialist output.

Context overflow signals:

- The prompt contains multiple prior reports when one current task is enough.
- The next specialist would inherit unrelated architecture, task, or recovery details.
- The conversation includes several completed agents or long generated artifacts.
- The agent output shows prompt drift, duplicated work, or mixed old/new scope.

Response: save minimal state, recommend `clean session`, and delegate only from state plus the current approved action.

Context starvation signals:

- The specialist cannot identify the user goal, phase, expected output, or approval boundary.
- FRIDAY omitted the raw user answer needed for an active analyst or architect loop.
- Recovery lacks the exact failure or what partial work already exists.

Response: add only the missing domain fact, raw answer, failure detail, or approved boundary. Do not resend the whole history.

## Domain Language Reformulation

Before delegating, convert vague requests into domain language:

- Identify the domain entities, states, actors, and rules.
- Describe user-visible behavior and business constraints.
- Remove implementation terms unless the target is `temper-architect` and the task is explicitly technical.

Example:

```text
Vague: Add order status tracking.
Domain language: An order transitions through Draft, Confirmed, In Preparation, Shipped, and Delivered. An order can be cancelled only while Draft or Confirmed. Once preparation begins, cancellation is no longer allowed.
```

## Pre-Delegation Checklist

- The task is a single action.
- The correct Friday workflow contract is loaded.
- The prompt matches that contract.
- No file paths, skill names, class names, method names, or layer instructions are included.
- Domain language is used where possible.
- Recovery prompts continue from existing work instead of restarting.
- The prompt contains exactly enough information and no more.
