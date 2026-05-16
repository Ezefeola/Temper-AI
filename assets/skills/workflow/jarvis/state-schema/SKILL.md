---
name: jarvis-state-schema
description: >
  State file JSON schema, status values, and delegation rules for the temper-jarvis
  orchestrator. Load this skill whenever jarvis needs to read, write, or validate
  .temper/jarvis-state.json, or when constructing delegation prompts for sub-agents.
---

# JARVIS State Schema & Delegation Rules

## State file JSON schema

`.temper/jarvis-state.json` is the orchestrator's only persistent memory.

```json
{
  "last_updated": "ISO timestamp",
  "status": "fresh | in-progress | awaiting-approval | awaiting-task-approval | awaiting-agent-cycle | complete | blocked",
  "request_summary": "one line description",
  "context": {
    "project": "project name or description",
    "architecture": "Clean Architecture | Hexagonal Architecture | Vertical Slice Architecture | Onion Architecture | unknown",
    "stack": "EF Core, Blazor, etc.",
    "notes": "any other relevant context"
  },
  "approved_plan": [
    {
      "step": 1,
      "agent": "temper-analyst",
      "description": "one line description",
      "status": "complete | pending | in-cycle",
      "output": "file path or null"
    }
  ],
  "current_step": 2,
  "total_steps": 4,
  "current_agent": "temper-backend",
  "current_task": "T001",
  "task_title": "one line description",
  "total_tasks": 6,
  "completed_tasks": [
    { "task_id": "T002", "agent": "temper-backend", "title": "description", "status": "complete" }
  ],
  "pending_tasks": [
    { "task_id": "T001", "agent": "temper-backend", "title": "description" }
  ],
    "active_cycle": {
      "agent": "temper-analyst | temper-architect",
      "cycle_type": "gap-resolution | architect-loop",
      "mode": "architectural-design | problem-solving | null",
      "waiting_for": "gap-answer | gap-batch-send | mode-clarification | context-clarification | problem-clarification | proposal-confirmation | document-selection | manual-review | none",
      "last_report_type": "gap-report | resolution-status | mode-report | clarification-request | domain-analysis | problem-analysis | architectural-proposal | architectural-plan | updated-proposal | document-offer | completion-report",
      "question_origin": "analyst | architect | null",
      "pending_interaction": {
        "surface_via": "question | plain-text",
        "interaction_type": "analyst-gap | architect-clarification | architect-decision | parse-fallback",
        "prompt_text": "single actionable prompt or fallback instruction",
        "expected_reply": "single gap answer | mode details | clarification answer | proposal confirmation/change request | document selection | manual reply",
        "source_ref": {
          "report_type": "gap-report | resolution-status | clarification-request | architectural-proposal | architectural-plan | updated-proposal | document-offer",
          "gap_id": "GAP-001 | null",
          "sequence": 1,
          "total": 3
        },
        "resume_hint": "one-line reminder of what the user still needs to answer or decide",
        "fallback_reason": "null or short explanation"
      } | null,
      "gap_queue": [
        {
          "gap_id": "GAP-001",
          "severity": "BLOCKING | IMPORTANT | CLARIFYING",
          "question_text": "single actionable analyst question"
        }
      ] | null,
      "collected_gap_answers": {
        "GAP-001": "user answer captured exactly as provided"
      } | null,
      "current_gap_index": 0,
      "unresolved_blocking_gaps": 3,
      "cycle_count": 1
    },
  "block_reason": null,
  "next_action": "what the next session should do"
}
```

---

## Status values

| Status | Meaning |
|---|---|
| `fresh` | No active approved plan is in progress; JARVIS should wait for a new request or a reset flow just completed |
| `in-progress` | Actively working on a step |
| `awaiting-approval` | Plan proposed, waiting for user to approve |
| `awaiting-task-approval` | Agent completed, waiting for user to confirm output |
| `awaiting-agent-cycle` | Sub-agent is in a multi-turn loop (analyst, architect), waiting for the next loop action or user input |
| `complete` | All steps done |
| `blocked` | Cannot proceed, needs user intervention |

---

## Delegation rules — domain language only

**You never tell an agent HOW to build something. You only tell them WHAT to build.**

For prompt engineering techniques — how to construct the actual delegation
prompt, context window management, multi-turn patterns, error recovery, and
domain language reformulation — refer to `workflow/jarvis/prompt-excellence`.

For the delegation format rules and prohibitions below, those are the
absolute constraints that apply to every prompt regardless of technique.

### ABSOLUTE PROHIBITIONS — never include in a delegation prompt

If you violate any of these, you have failed as orchestrator:

- **NEVER** mention file paths: `.temper/`, `.md` files, `.cs` files, folder locations
- **NEVER** mention skill names: "dotnet-csharp", "ef-core", "ddd", etc.
- **NEVER** describe domain, summarize tasks, or copy acceptance criteria
- **NEVER** mention class names, DTO names, interface names, method names
- **NEVER** say "Read...", "Load...", "Check...", or "See file..."
- **NEVER** describe layers: "Domain layer", "Application layer", "Infrastructure..."

### Correct delegation format

The ONLY thing you send to an implementation agent is:

```
Implement task T001: Add Product to Inventory (US-001)
```

That is literally all. No punctuation, no extra text, no context.

If you catch yourself typing anything after `Implement task [ID]: [title]` — DELETE IT.

### Domain language comparison

| Correct — what to build | Wrong — how to build it |
|---|---|
| "The Order entity has a status: Pending, Confirmed, Cancelled" | "Create an `OrderStatus` enum in `Domain/Enums/`" |
| "An order belongs to one customer and can have multiple items" | "Add a `CustomerId` FK and `OrderItems` navigation property" |
| "The endpoint returns a paginated list of orders filtered by status" | "Create a `GetOrdersQuery` with a `Handle` method returning `PagedResult<OrderDto>`" |
| "An order cannot be cancelled if already shipped" | "Throw `DomainException` in `Cancel()` if `Status == Shipped`" |

### Pre-delegation checklist

Before sending ANY prompt to a sub-agent, verify ALL of these:

- [ ] The prompt contains ONLY: `Implement task [T###]: [title]`
- [ ] No file paths are mentioned (no `.temper/`, no `.md`, no `.cs`)
- [ ] No skill names or load instructions
- [ ] No domain summary, acceptance criteria, or layer descriptions
- [ ] No class names, DTO names, or interface names

For the full quality checklist including prompt anatomy, context management,
and reformulation examples, refer to `workflow/jarvis/prompt-excellence`.

If any check fails → STOP and rewrite to the minimal form:

```
Implement task [T###]: [task title]
```

**For analyst/architect only:**

- [ ] Passing user's request and/or context files as appropriate
- [ ] Speaking in domain language, not implementation language

Implementation agents read their own files. You never tell them what to read unless the user explicitly specifies.

---

## Analyst loop state contract

When `active_cycle.agent` is `temper-analyst`, JARVIS must treat `active_cycle` as the
authoritative resume contract for structured gap-resolution interactions.

Required fields:
- `cycle_type: "gap-resolution"`
- `waiting_for: "gap-answer" | "gap-batch-send" | "manual-review"`
- `last_report_type`
- `question_origin: "analyst"`
- `pending_interaction.surface_via`
- `pending_interaction.interaction_type`
- `pending_interaction.prompt_text`
- `pending_interaction.expected_reply`
- `pending_interaction.source_ref.report_type`
- `pending_interaction.source_ref.sequence`
- `pending_interaction.source_ref.total`
- `pending_interaction.resume_hint`
- `pending_interaction.fallback_reason` when `waiting_for: "manual-review"`
- `gap_queue`, `collected_gap_answers`, and `current_gap_index` when `waiting_for: "gap-answer" | "gap-batch-send"`
- `cycle_count`

Resume rules:
- If `status` is `awaiting-agent-cycle` and `question_origin` is `analyst`, JARVIS resumes the analyst loop, not normal step execution.
- JARVIS must use the saved `waiting_for` value to decide what kind of user response is expected.
- When the analyst emits a gap report, JARVIS must extract each `GAP-XXX` item and each `Surface to user:` question.
- If extraction is reliable, JARVIS must store the extracted items in `gap_queue`, initialize `collected_gap_answers`, point `current_gap_index` at the current gap, and use the question tool for exactly one gap question at a time.
- JARVIS must not persist or replay the full analyst report in `pending_interaction`.
- JARVIS must persist `pending_interaction`, `gap_queue`, `collected_gap_answers`, and `current_gap_index` before stopping on every analyst-originated question turn so a fresh session can resume the same atomic gap deterministically.
- After the user responds, JARVIS must store that answer under the current `gap_id`, preserve the raw answer text inside that labeled field, and continue to the next unanswered gap without contacting `temper-analyst` yet.
- When every gap in the current `gap_queue` has an answer, JARVIS must set `waiting_for: "gap-batch-send"`, persist that state, and on the next loop action or resumed session send one consolidated labeled batch to `temper-analyst` for that round.
- The consolidated payload must identify every answer by `gap_id`; JARVIS must never forward unlabeled raw gap answers as if the analyst can infer the mapping.
- If extraction is not reliable, JARVIS must not dump the full report into question. It presents the report as plain text, sets `waiting_for: "manual-review"`, stores `interaction_type: "parse-fallback"`, `surface_via: "plain-text"`, `resume_hint`, and `fallback_reason`, and waits for the user's direct reply or rerun decision.
- When the analyst emits a resolution status with zero blocking gaps, set `waiting_for: "none"`, clear `pending_interaction`, clear `gap_queue`, clear `collected_gap_answers`, clear `active_cycle`, and continue the normal post-execution flow.

---

## Architect loop state contract

When `active_cycle.agent` is `temper-architect`, JARVIS must treat `active_cycle` as the
authoritative resume contract for structured architect interactions.

Required fields:
- `cycle_type: "architect-loop"`
- `mode: "architectural-design" | "problem-solving"`
- `waiting_for`
- `last_report_type`
- `question_origin: "architect"`
- `pending_interaction.surface_via`
- `pending_interaction.interaction_type`
- `pending_interaction.prompt_text`
- `pending_interaction.expected_reply`
- `pending_interaction.source_ref.report_type`
- `pending_interaction.source_ref.sequence`
- `pending_interaction.source_ref.total`
- `pending_interaction.resume_hint`
- `pending_interaction.fallback_reason` when `waiting_for: "manual-review"`
- `cycle_count`

`waiting_for` values:
- `mode-clarification` — architect cannot classify the request yet
- `context-clarification` — architect needs design context before proposal
- `problem-clarification` — architect needs problem details before plan
- `proposal-confirmation` — architect already emitted proposal/plan and is waiting for confirm-or-change
- `document-selection` — architect already emitted document offer and is waiting for selected docs or required-only confirmation
- `manual-review` — JARVIS could not safely reduce the architect interaction to one actionable prompt and is waiting for a direct user reply or rerun decision
- `none` — architect has completed the loop and JARVIS should clear `active_cycle`

Resume rules:
- If `status` is `awaiting-agent-cycle` and `question_origin` is `architect`, JARVIS resumes the architect loop, not normal step execution.
- JARVIS must use the saved `waiting_for` value to decide what kind of user response is expected.
- JARVIS must present architect reports normally and use question only for the minimal next clarification or decision prompt.
- JARVIS must not persist or replay the full architect report in `pending_interaction`.
- JARVIS must persist `pending_interaction` before stopping on every architect-originated interaction turn so a fresh session can re-ask the same minimal prompt deterministically.
- After the user responds, JARVIS passes that response back to `temper-architect` exactly as received.
- If the architect interaction cannot be reduced reliably to one actionable prompt, JARVIS must not dump the full report into question. It presents the report as plain text, sets `waiting_for: "manual-review"`, stores `interaction_type: "parse-fallback"`, `surface_via: "plain-text"`, `resume_hint`, and `fallback_reason`, and waits for the user's direct reply or rerun decision.
- When the architect emits a completion report, set `waiting_for: "none"`, clear `pending_interaction`, clear `active_cycle`, and continue the normal post-execution flow.
