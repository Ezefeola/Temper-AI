---
name: analyst-communication
description: >
  Analyst communication contract for the TemperAI orchestrator. Load when
  JARVIS delegates to temper-analyst, resumes an analyst Phase 1 gap-resolution
  loop or Phase 2 ambiguity-resolution loop, or needs the exact analyst
  handoff and state rules.
---

# JARVIS <-> Analyst Communication Contract

## Purpose

This skill defines the explicit working contract between JARVIS and
`temper-analyst`.

When this skill is loaded, it is the authoritative contract for analyst-loop
behavior. `assets/agents/temper-jarvis.agent.md` keeps only the high-level
routing and phase-transition rules.

Use it only when JARVIS is:
- delegating the first analyst turn for Phase 1 or Phase 2
- sending a completed gap-answer or ambiguity-answer batch back to the analyst
- resuming an analyst-originated loop from `active_cycle`
- validating that an analyst interaction was reduced safely before persisting state

`workflow/jarvis/state-schema` remains authoritative for the state file shape.
This skill defines the analyst-specific communication and loop behavior that
uses that state.

---

## Prompt contract

### First turn

Include:
- the user's request
- the minimum plain-language context needed for the current turn
- the explicit analyst phase
- a specific analyst task
- the analyst's standard output expectation when needed
- any explicit boundary the analyst must respect

Template:

```
Phase: Phase 1 - PRD
Context: User wants to build an inventory management system for a warehouse.
No PRD exists. This is the first elicitation cycle.
Task: Conduct initial requirements elicitation. Identify all gaps in the
user's description. Produce a gap report organized by category.
Format: Produce your standard gap report output.
Constraints: Do not infer technical stack or architecture.
```

### Subsequent turns

When the analyst is waiting on user answers in either phase:
- pass answers back exactly as the user provided them
- do not summarize, normalize, or relabel the answer text itself
- do label the consolidated batch by the analyst item ID (`GAP-XXX` or `AMB-XXX`)

Template:

```
Phase: [Phase 1 - PRD | Phase 2 - Specs]
Context: User provided answers to the previous analyst report.
Task: Review the answers. Resolve any items now answered. If blocking items
remain, emit the correct follow-up analyst report for this phase.
Format: Present your standard resolution-status output for the current phase,
followed by the next blocking report if more user input is required.
```

### Absolute analyst handoff rules

- Pass raw user answers exactly as received.
- Do not infer missing analyst meaning on JARVIS's behalf.
- Do not send partial answer-round data back to `temper-analyst`.
- Send one labeled consolidated batch per fully answered round.
- Do not include file paths, file names, skill names, or read/load/check instructions.
- Always state the analyst phase explicitly when delegating or resuming.

---

## Loop contract

`temper-analyst` has two distinct user-answer loops. JARVIS must keep them separate.

The orchestrator invariants are:
- Phase 1 and Phase 2 are separate analyst invocations.
- PRD approval advances from Phase 1 to Phase 2, not to the architect.
- JARVIS must not skip the explicit Phase 2 step before `temper-architect` when specs are still pending or unapproved.
- JARVIS never ends either analyst loop by assumption.

### Contract A - Phase 1 gap-resolution

The loop is:

`gap report -> collect one user answer at a time -> save labeled answers -> send one consolidated batch -> analyst resolution status/new gaps -> repeat until zero blocking gaps`

When the analyst emits a Phase 1 gap report, JARVIS must:
- present the report as normal text
- extract each `GAP-XXX` item and `Surface to user:` question when reliable
- ask exactly one actionable gap question at a time
- persist only the minimum structured resume state needed for the current gap

JARVIS must not:
- replay the full analyst report through the question tool
- persist the full report inside `pending_interaction`

Gap collection rules:

- User-facing granularity is one `GAP-XXX` question at a time.
- Analyst-facing granularity is one labeled answer batch per completed gap round.
- After each user reply, store the answer under the current `gap_id` exactly as received.
- If unanswered gaps remain, advance to the next gap without contacting the analyst yet.
- When the round is fully answered, set `waiting_for: "gap-batch-send"`, persist, then send the consolidated labeled batch.
- The batch must map every answer explicitly, for example `GAP-001: ...`.

State shape for an active Phase 1 round:

```json
"active_cycle": {
  "agent": "temper-analyst",
  "phase": "phase-1-prd",
  "cycle_type": "gap-resolution",
  "waiting_for": "gap-answer | gap-batch-send | manual-review",
  "last_report_type": "gap-report | resolution-status",
  "question_origin": "analyst",
  "pending_interaction": {
    "surface_via": "question | plain-text",
    "interaction_type": "analyst-gap | parse-fallback",
    "prompt_text": "single current gap question or fallback instruction",
    "expected_reply": "single gap answer | manual reply",
    "source_ref": {
      "report_type": "gap-report | resolution-status",
      "item_id": "GAP-001 | null",
      "sequence": 1,
      "total": 3
    },
    "resume_hint": "one-line reminder of the current unanswered gap",
    "fallback_reason": null
  },
  "gap_queue": [
    {
      "gap_id": "GAP-001",
      "severity": "BLOCKING",
      "question_text": "current actionable question"
    }
  ],
  "collected_gap_answers": {
    "GAP-001": "user answer captured exactly as provided"
  },
  "current_gap_index": 0,
  "cycle_count": 1
}
```

Exit condition:

- The Phase 1 loop ends only when the analyst's resolution output shows zero blocking gaps and Phase 1 can complete.

### Contract B - Phase 2 ambiguity-resolution

The loop is:

`ambiguity stop report -> collect one user answer at a time -> save labeled answers -> send one consolidated batch -> analyst ambiguity resolution status/new ambiguities -> repeat until zero blocking ambiguities`

When the analyst emits a Phase 2 ambiguity stop report, JARVIS must:
- present the report as normal text
- extract each `AMB-XXX` item and `Surface to user:` question when reliable
- ask exactly one actionable ambiguity question at a time
- persist only the minimum structured resume state needed for the current ambiguity

JARVIS must not:
- replay the full analyst report through the question tool
- persist the full report inside `pending_interaction`

Ambiguity collection rules:

- User-facing granularity is one `AMB-XXX` question at a time.
- Analyst-facing granularity is one labeled answer batch per completed ambiguity round.
- After each user reply, store the answer under the current `ambiguity_id` exactly as received.
- If unanswered ambiguities remain, advance to the next ambiguity without contacting the analyst yet.
- When the round is fully answered, set `waiting_for: "ambiguity-batch-send"`, persist, then send the consolidated labeled batch.
- The batch must map every answer explicitly, for example `AMB-001: ...`.

State shape for an active Phase 2 round:

```json
"active_cycle": {
  "agent": "temper-analyst",
  "phase": "phase-2-specs",
  "cycle_type": "spec-ambiguity-resolution",
  "waiting_for": "ambiguity-answer | ambiguity-batch-send | manual-review",
  "last_report_type": "ambiguity-stop | ambiguity-resolution-status",
  "question_origin": "analyst",
  "pending_interaction": {
    "surface_via": "question | plain-text",
    "interaction_type": "analyst-ambiguity | parse-fallback",
    "prompt_text": "single current ambiguity question or fallback instruction",
    "expected_reply": "single ambiguity answer | manual reply",
    "source_ref": {
      "report_type": "ambiguity-stop | ambiguity-resolution-status",
      "item_id": "AMB-001 | null",
      "sequence": 1,
      "total": 2
    },
    "resume_hint": "one-line reminder of the current unresolved ambiguity",
    "fallback_reason": null
  },
  "ambiguity_queue": [
    {
      "ambiguity_id": "AMB-001",
      "severity": "BLOCKING",
      "question_text": "current actionable question"
    }
  ],
  "collected_ambiguity_answers": {
    "AMB-001": "user answer captured exactly as provided"
  },
  "current_ambiguity_index": 0,
  "cycle_count": 1
}
```

Exit condition:

- The Phase 2 loop ends only when the analyst's ambiguity-resolution output shows zero blocking ambiguities and spec generation can continue.

### Parse fallback

If gap or ambiguity extraction is not reliable:
- present the analyst report as plain text
- set `waiting_for: "manual-review"`
- set `pending_interaction.interaction_type: "parse-fallback"`
- set `pending_interaction.surface_via: "plain-text"`
- store `resume_hint` and `fallback_reason`
- wait for the user's direct reply or rerun decision

JARVIS does not end either analyst loop by assumption.

### Resume action map

When `status` is `awaiting-agent-cycle`, combine `phase` and `waiting_for`:

- `phase-1-prd` + `gap-answer`: ask only `pending_interaction.prompt_text` with the question tool.
- `phase-1-prd` + `gap-batch-send`: send one consolidated labeled `GAP-XXX` batch to `temper-analyst` before asking anything else.
- `phase-2-specs` + `ambiguity-answer`: ask only `pending_interaction.prompt_text` with the question tool.
- `phase-2-specs` + `ambiguity-batch-send`: send one consolidated labeled `AMB-XXX` batch to `temper-analyst` before asking anything else.
- `manual-review`: remind the user with `pending_interaction.resume_hint`, keep the prior analyst report out of the question tool, and wait for the user's direct reply or rerun decision.

Loop completion signals:
- Phase 1 completes only when zero BLOCKING gaps remain and the analyst emits its Phase 1 completion report.
- Phase 2 completes only when zero BLOCKING ambiguities remain and the analyst emits its Phase 2 completion report.

---

## Required analyst cycle state

When `active_cycle.agent` is `temper-analyst`, the required working contract is:

- `phase: "phase-1-prd" | "phase-2-specs"`
- `cycle_type: "gap-resolution" | "spec-ambiguity-resolution"`
- `waiting_for: "gap-answer" | "gap-batch-send" | "ambiguity-answer" | "ambiguity-batch-send" | "manual-review"`
- `last_report_type`
- `question_origin: "analyst"`
- `pending_interaction.surface_via`
- `pending_interaction.interaction_type`
- `pending_interaction.prompt_text`
- `pending_interaction.expected_reply`
- `pending_interaction.source_ref.report_type`
- `pending_interaction.source_ref.item_id`
- `pending_interaction.source_ref.sequence`
- `pending_interaction.source_ref.total`
- `pending_interaction.resume_hint`
- `pending_interaction.fallback_reason` when `waiting_for: "manual-review"`
- `gap_queue`, `collected_gap_answers`, and `current_gap_index` when `waiting_for: "gap-answer" | "gap-batch-send"`
- `ambiguity_queue`, `collected_ambiguity_answers`, and `current_ambiguity_index` when `waiting_for: "ambiguity-answer" | "ambiguity-batch-send"`
- `cycle_count`

Resume rules:
- If `status` is `awaiting-agent-cycle` and `question_origin` is `analyst`, resume the analyst loop, not normal step execution.
- Use `phase` and `waiting_for` together to decide whether to ask the next saved Phase 1 gap, send the saved Phase 1 batch, ask the next saved Phase 2 ambiguity, send the saved Phase 2 batch, or wait in manual review.
- A resumed session must continue the next unanswered gap if `phase: "phase-1-prd"` and `waiting_for: "gap-answer"`.
- A resumed session must send the saved labeled gap batch first if `phase: "phase-1-prd"` and `waiting_for: "gap-batch-send"`.
- A resumed session must continue the next unanswered ambiguity if `phase: "phase-2-specs"` and `waiting_for: "ambiguity-answer"`.
- A resumed session must send the saved labeled ambiguity batch first if `phase: "phase-2-specs"` and `waiting_for: "ambiguity-batch-send"`.
