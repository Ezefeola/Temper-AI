---
name: jarvis-state-schema
description: >
  State file JSON schema, status values, and generic delegation rules for the
  temper-jarvis orchestrator. Load this skill whenever jarvis needs to read,
  write, or validate .temper/jarvis-state.json.
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
      "phase": "phase-1-prd | phase-2-specs | null",
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
      "phase": "phase-1-prd | phase-2-specs | null",
      "cycle_type": "gap-resolution | spec-ambiguity-resolution | architect-loop",
      "mode": "architectural-design | problem-solving | null",
      "waiting_for": "gap-answer | gap-batch-send | ambiguity-answer | ambiguity-batch-send | mode-clarification | context-clarification | preference-clarification | problem-clarification | proposal-confirmation | document-selection | manual-review | none",
      "last_report_type": "gap-report | resolution-status | ambiguity-stop | ambiguity-resolution-status | mode-report | clarification-request | domain-analysis | problem-analysis | architectural-proposal | architectural-plan | updated-proposal | document-offer | completion-report",
      "question_origin": "analyst | architect | null",
      "pending_interaction": {
        "surface_via": "question | plain-text",
        "interaction_type": "analyst-gap | analyst-ambiguity | architect-clarification | architect-decision | parse-fallback",
        "prompt_text": "single actionable prompt or fallback instruction",
        "expected_reply": "single gap answer | single ambiguity answer | mode details | clarification answer | preference bucket answer | proposal confirmation/change request | document selection | manual reply",
        "source_ref": {
          "report_type": "gap-report | resolution-status | ambiguity-stop | ambiguity-resolution-status | clarification-request | architectural-proposal | architectural-plan | updated-proposal | document-offer",
          "item_id": "GAP-001 | AMB-001 | null",
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
      "ambiguity_queue": [
        {
          "ambiguity_id": "AMB-001",
          "severity": "BLOCKING",
          "question_text": "single actionable analyst question"
        }
      ] | null,
      "collected_ambiguity_answers": {
        "AMB-001": "user answer captured exactly as provided"
      } | null,
      "current_gap_index": 0,
      "current_ambiguity_index": 0,
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
prompt, context window management, error recovery, and domain language
reformulation — refer to `workflow/jarvis/prompt-excellence`.

For agent-specific loop and communication contracts, refer to:
- `workflow/jarvis/implementation-delegation`
- `workflow/jarvis/analyst-communication`
- `workflow/jarvis/architect-communication`

For the delegation format rules and prohibitions below, those are the
absolute constraints that apply to every prompt regardless of technique.

### ABSOLUTE PROHIBITIONS — never include in a delegation prompt

If you violate any of these, you have failed as orchestrator.

These prohibitions apply to every delegated agent, including `temper-analyst`
and `temper-architect`. The analyst/architect exception is about allowing raw
user request and plain-language working context, not about allowing file paths,
skill references, or internal implementation guidance.

- **NEVER** mention file paths: `.temper/`, `.md` files, `.cs` files, folder locations
- **NEVER** mention skill names: "dotnet-csharp", "ef-core", "ddd", etc.
- **NEVER** describe domain, summarize tasks, or copy acceptance criteria
- **NEVER** mention class names, DTO names, interface names, method names
- **NEVER** say "Read...", "Load...", "Check...", or "See file..."
- **NEVER** describe layers: "Domain layer", "Application layer", "Infrastructure..."

### Domain language comparison

| Correct — what to build | Wrong — how to build it |
|---|---|
| "The Order entity has a status: Pending, Confirmed, Cancelled" | "Create an `OrderStatus` enum in `Domain/Enums/`" |
| "An order belongs to one customer and can have multiple items" | "Add a `CustomerId` FK and `OrderItems` navigation property" |
| "The endpoint returns a paginated list of orders filtered by status" | "Create a `GetOrdersQuery` with a `Handle` method returning `PagedResult<OrderDto>`" |
| "An order cannot be cancelled if already shipped" | "Throw `DomainException` in `Cancel()` if `Status == Shipped`" |

### Pre-send rule

Before sending any prompt, load the workflow contract that matches the target
agent and validate the prompt against that contract.

- `workflow/jarvis/implementation-delegation` — task-driven execution agents,
  bugfix turns, and recovery turns
- `workflow/jarvis/analyst-communication` — analyst Phase 1 and Phase 2 loops
- `workflow/jarvis/architect-communication` — architect loop turns

Implementation agents read their own files. Agents that work through
conversation loops may receive plain-language context when needed, but JARVIS
must never delegate by naming files or telling any agent what to read.

---

## Agent-specific cycle contracts

`active_cycle` carries enough shared structure for both specialist loops, but
the authoritative interaction rules live in dedicated JARVIS workflow skills:

- `workflow/jarvis/analyst-communication` — analyst Phase 1 gap-resolution,
  analyst Phase 2 ambiguity-resolution, batching, parse fallback, and resume rules
- `workflow/jarvis/architect-communication` — architect clarification,
  preference, confirmation, document-selection, parse fallback, and resume rules

This split is intentional:
- `assets/agents/temper-jarvis.agent.md` keeps only universal orchestration logic plus high-level routing and transition rules.
- `workflow/jarvis/implementation-delegation` owns task-driven execution handoff rules.
- Specialist loop mechanics, resume behavior, and interaction reduction rules live in the dedicated workflow skills above.

---

## Analyst-specific state notes

`temper-analyst` is modeled as two explicit orchestrator phases, not one generic
step:

- `phase: "phase-1-prd"` — analyst is eliciting requirements and generating the PRD
- `phase: "phase-2-specs"` — analyst is generating specs from an approved PRD

When `active_cycle.agent` is `temper-analyst`, JARVIS must persist both
`phase` and `cycle_type` so resume logic can distinguish:

- Phase 1 loop: `cycle_type: "gap-resolution"`
- Phase 2 loop: `cycle_type: "spec-ambiguity-resolution"`

Resume rules:

- `phase-1-prd` + `waiting_for: "gap-answer"` means ask the next saved `GAP-XXX` question.
- `phase-1-prd` + `waiting_for: "gap-batch-send"` means send the saved labeled `GAP-XXX` batch first.
- `phase-2-specs` + `waiting_for: "ambiguity-answer"` means ask the next saved `AMB-XXX` question.
- `phase-2-specs` + `waiting_for: "ambiguity-batch-send"` means send the saved labeled `AMB-XXX` batch first.

The orchestrator must never infer the analyst phase from the agent name alone.
The phase must be explicit in plan steps and active-cycle state.
