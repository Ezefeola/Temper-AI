---
name: friday-state-schema
description: >
  State file JSON schema, status values, and generic delegation rules for the
  FRIDAY orchestrator. Load this skill whenever FRIDAY needs to read, write, or
  validate .temper/friday-state.json.
---

# FRIDAY State Schema & Delegation Rules

## State File JSON Schema

`.temper/friday-state.json` is FRIDAY's only persistent orchestration memory.

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
    },
    "gap_queue": [
      {
        "gap_id": "GAP-001",
        "severity": "BLOCKING | IMPORTANT | CLARIFYING",
        "question_text": "single actionable analyst question"
      }
    ],
    "collected_gap_answers": {
      "GAP-001": "user answer captured exactly as provided"
    },
    "ambiguity_queue": [
      {
        "ambiguity_id": "AMB-001",
        "severity": "BLOCKING",
        "question_text": "single actionable analyst question"
      }
    ],
    "collected_ambiguity_answers": {
      "AMB-001": "user answer captured exactly as provided"
    },
    "current_gap_index": 0,
    "current_ambiguity_index": 0,
    "unresolved_blocking_gaps": 3,
    "cycle_count": 1
  },
  "block_reason": null,
  "next_action": "what the next session should do"
}
```

## Status Values

| Status | Meaning |
| --- | --- |
| `fresh` | No active approved plan is in progress; FRIDAY should wait for a new request or a reset flow just completed. |
| `in-progress` | FRIDAY is actively working on an approved step. |
| `awaiting-approval` | A plan or next specialist step is proposed and waiting for user approval. |
| `awaiting-task-approval` | A task-oriented agent completed and FRIDAY is waiting for the user to confirm the output or next action. |
| `awaiting-agent-cycle` | An analyst or architect loop is active and waiting for the next loop action or user input. |
| `complete` | All approved steps are complete. |
| `blocked` | FRIDAY cannot proceed without user intervention. |

## Startup State Health Check

At startup, FRIDAY must produce a concise operational note containing:

- State file: `found` or `not found`.
- Status: current `status`, or `invalid` with the shortest reason.
- Active plan: current step and total steps, or `none`.
- Next action: the next safe orchestration action.

Do not use ceremonial startup phrases. If state is missing and the user is not asking to continue prior work, report fresh state and proceed with normal classification.

## Required-Field Validation

Before resuming, delegating, or writing state, validate these fields:

- Root object must be valid JSON. Invalid JSON blocks delegation until repaired, reset, or replaced with explicit approval.
- `last_updated`, `status`, `request_summary`, and `next_action` are required for any persisted state.
- `status` must be one of the defined status values.
- `approved_plan` is required when `status` is `in-progress`, `awaiting-approval`, `awaiting-task-approval`, or `complete` for a multi-step plan.
- `current_step` and `total_steps` are required when `approved_plan` exists.
- `current_step` must be an integer from `1` through `total_steps`; if all steps are complete, it may equal `total_steps` with `status: "complete"`.
- Each `approved_plan` item must include `step`, `agent`, `description`, `status`, and `output`.
- `approved_plan[].step` must be unique, sequential, and match its order.
- `approved_plan[].status` must be `complete`, `pending`, or `in-cycle`.
- `current_agent` is required when `status` is `in-progress` or a pending approval targets a specific next agent.
- `block_reason` is required and non-empty when `status` is `blocked`.

If required fields are missing or contradictory, stop delegation, warn that state cannot be resumed safely, and ask for repair/reset approval or one concise clarification.

## Pending Step And Output Validation

When `approved_plan` exists:

- The pending step is the first item with `status: "pending"` or `status: "in-cycle"`; it must match `current_step` unless state is `complete`.
- Do not skip a pending step unless the user explicitly approves a change-direction plan.
- A completed step must have an `output` value when that specialist is expected to produce a durable artifact or completion report.
- If expected output is missing, mark state as blocked or recovery-needed instead of advancing.
- If the next step depends on a previous output, verify the previous step is `complete` and has the expected output reference before delegation.
- `current_task`, `task_title`, `pending_tasks`, and task counters must agree before delegating task-driven implementation.

FRIDAY does not need to read full artifact contents for this check; it validates that required references and workflow statuses exist and are coherent.

## Prerequisite Checks

Before routing between workflow phases, enforce these prerequisites:

- Analyst Phase 2 may start only after analyst Phase 1 is complete and the PRD output has been approved or the user explicitly approves a change-direction exception.
- Architect routing for normal project architecture may start only after Phase 2 specs are complete or the user explicitly changes direction with approval.
- Task generation may start only after required requirements/spec and architecture outputs are complete and approved.
- Plan generation may start only after task generation is complete and approved.
- Implementation agents may start only from an approved plan/task or an explicitly approved direct bugfix/recovery path.

If a prerequisite is missing, do not delegate. Report the missing prerequisite and propose the smallest valid next step.

## Active-Cycle Validation

When `status` is `awaiting-agent-cycle`, `active_cycle` is required and must include:

- `agent` with `temper-analyst` or `temper-architect`.
- `cycle_type` matching the agent.
- `waiting_for` with a valid value for that cycle.
- `last_report_type`.
- `question_origin` matching the agent.
- `pending_interaction` unless `waiting_for` is `none`.
- `cycle_count` as a positive integer.

`pending_interaction` must include:

- `surface_via`.
- `interaction_type`.
- `prompt_text` or a `resume_hint` for manual review.
- `expected_reply`.
- `source_ref.report_type`.
- `source_ref.sequence` and `source_ref.total` when presenting queued items.

For analyst cycles:

- `phase` is required and must be `phase-1-prd` or `phase-2-specs`.
- Phase 1 requires `cycle_type: "gap-resolution"`, `gap_queue`, `collected_gap_answers`, and `current_gap_index`.
- Phase 2 requires `cycle_type: "spec-ambiguity-resolution"`, `ambiguity_queue`, `collected_ambiguity_answers`, and `current_ambiguity_index`.
- Queue index must be within the queue bounds unless the next action is batch send.

For architect cycles:

- `cycle_type` must be `architect-loop`.
- `mode` must be `architectural-design` or `problem-solving` unless the current wait is mode clarification.
- `waiting_for` must match the stage represented by `last_report_type`.

If active-cycle state is incomplete, ask only for the missing resume decision or propose safe parse fallback. Do not infer answers or fabricate queues.

## Blocked Handling And Recovery Warnings

When `status` is `blocked`:

- Do not delegate.
- Show `block_reason` in one sentence.
- Ask for the exact missing input, approval, repair decision, or reset decision.
- Preserve prior approvals unless the recovery changes their scope.

Warn before recovery when any of these are true:

- State JSON was invalid and had to be ignored.
- Required fields are missing from an active plan or active cycle.
- A pending step points to an agent that does not match prerequisites.
- An expected output reference is missing for a completed step.
- The user's request conflicts with saved next action.
- Recovery would supersede approved plan, approved requirements, approved architecture, tasks, or implementation output.

## Delegation Rules - Domain Language Only

FRIDAY never tells an agent how to build something. FRIDAY tells specialists what outcome is needed and lets the specialist load its own context and skills.

For prompt construction, context control, recovery prompts, and domain-language reformulation, load `workflow/friday/prompt-excellence`.

For agent-specific contracts, load:

- `workflow/friday/implementation-delegation`
- `workflow/friday/analyst-communication`
- `workflow/friday/architect-communication`

## Absolute Delegation Prompt Prohibitions

These prohibitions apply to every delegated agent, including `temper-analyst` and `temper-architect`.

- Never mention file paths, `.temper/`, Markdown files, source files, or folder locations.
- Never mention skill names in a delegation prompt.
- Never copy acceptance criteria into implementation prompts.
- Never mention class names, DTO names, interface names, or method names.
- Never say `Read`, `Load`, `Check`, or `See file`.
- Never describe implementation layers or tell the specialist where code belongs.
- Never summarize away user answers that must be passed back exactly.

## Pre-Send Rule

Before sending any prompt, load the workflow contract that matches the target agent and validate the prompt against that contract.

- `workflow/friday/implementation-delegation` for task-driven execution agents, bugfix turns, and recovery turns.
- `workflow/friday/analyst-communication` for analyst Phase 1 and Phase 2 loops.
- `workflow/friday/architect-communication` for architect loop turns.

Implementation agents read their own task context. Conversation-loop agents may receive raw user request and minimum plain-language context when their contract requires it.

## Agent-Specific Cycle Contracts

`active_cycle` carries enough shared structure for analyst and architect loops. The authoritative interaction rules live in dedicated FRIDAY workflow skills:

- `workflow/friday/analyst-communication` for Phase 1 gap-resolution, Phase 2 ambiguity-resolution, batching, parse fallback, and resume rules.
- `workflow/friday/architect-communication` for clarification, preference, confirmation, document-selection, parse fallback, and resume rules.

## Session Mode Recommendation

After a specialist completes and the user approves any next step, FRIDAY must evaluate context load and recommend one of two session modes:

- `clean session`: save or rely on `.temper/friday-state.json`, then continue later from state with focused context.
- `continue here`: proceed in the same conversation because context is still small and useful.

FRIDAY recommends; the user decides. Do not ask passively with neutral wording like "Do you want to continue?" Always give a clear recommendation and the reason.

Recommend `clean session` when any of these are true:

- Multiple specialists have already executed in the current conversation.
- Large generated artifacts, long reports, or extensive file summaries are in context.
- The next specialist needs focused context and should not inherit prior report noise.
- Context noise could cause prompt drift, duplicated work, or scope confusion.
- Analyst or architect loops produced long specialist reports or multiple interaction rounds.
- Recovery loops, failed attempts, or prompt-failure analysis accumulated in context.

Say `continue here` is fine when all of these are true:

- This was the first specialist step or a short isolated step.
- Context is small and directly relevant to the next action.
- The next action is quick, narrow, and isolated.
- No long specialist reports, large artifacts, or recovery history have accumulated.

Use this output format:

```text
Recommendation: clean session | continue here
Reason: [one concise reason tied to context load]
Choices: Reply "clean" to continue from saved state with focused context, or "continue" to proceed in this session.
```

## Analyst-Specific State Notes

`temper-analyst` is modeled as two explicit orchestrator phases:

- `phase: "phase-1-prd"` - requirements elicitation and PRD generation.
- `phase: "phase-2-specs"` - spec generation from an approved PRD.

When `active_cycle.agent` is `temper-analyst`, FRIDAY must persist both `phase` and `cycle_type` so resume logic can distinguish:

- Phase 1 loop: `cycle_type: "gap-resolution"`.
- Phase 2 loop: `cycle_type: "spec-ambiguity-resolution"`.

FRIDAY must never infer analyst phase from the agent name alone.
