---
name: friday-analyst-communication
description: >
  Analyst communication contract for FRIDAY. Load when FRIDAY delegates to
  temper-analyst, resumes an analyst Phase 1 gap-resolution loop or Phase 2
  ambiguity-resolution loop, or needs exact analyst handoff and state rules.
---

# FRIDAY Analyst Communication Contract

## Purpose

This skill defines the explicit working contract between FRIDAY and `temper-analyst`. When loaded, it is authoritative for analyst-loop behavior. `workflow/friday/state-schema` remains authoritative for shared state shape.

Use this skill when FRIDAY is:

- Delegating the first analyst turn for Phase 1 or Phase 2.
- Sending completed gap-answer or ambiguity-answer batches back to the analyst.
- Resuming an analyst-originated loop from `active_cycle`.
- Validating that an analyst interaction was reduced safely before persisting state.

## Prompt Contract

### First Turn

Include:

- The user's request.
- Minimum plain-language context needed for the current turn.
- The explicit analyst phase.
- A specific analyst task.
- The analyst's standard output expectation when needed.
- Any explicit boundary the analyst must respect.

Template:

```text
Phase: Phase 1 - PRD
Context: User wants to build an inventory management system for a warehouse. No PRD exists. This is the first elicitation cycle.
Task: Conduct initial requirements elicitation. Identify all gaps in the user's description. Produce a gap report organized by category.
Format: Produce your standard gap report output.
Constraints: Do not infer technical stack or architecture.
```

### Subsequent Turns

When the analyst is waiting on user answers:

- Pass answers back exactly as the user provided them.
- Do not summarize, normalize, or relabel answer text itself.
- Label the consolidated batch by analyst item ID: `GAP-XXX` or `AMB-XXX`.
- Send one consolidated batch per fully answered round.

Template:

```text
Phase: [Phase 1 - PRD | Phase 2 - Specs]
Context: User provided answers to the previous analyst report.
Task: Review the answers. Resolve any items now answered. If blocking items remain, emit the correct follow-up analyst report for this phase.
Format: Present your standard resolution-status output for the current phase, followed by the next blocking report if more user input is required.
```

### Absolute Analyst Handoff Rules

- Always state the analyst phase explicitly.
- Pass raw user answers exactly as received.
- Do not infer missing analyst meaning on FRIDAY's behalf.
- Do not send partial answer-round data back to `temper-analyst`.
- Do not include file paths, file names, skill names, or read/load/check instructions.

## Phase Separation And No-Skip Rule

`temper-analyst` has two distinct loops. FRIDAY must keep them separate.

- Phase 1 and Phase 2 are separate analyst invocations.
- PRD approval advances from Phase 1 to Phase 2, not directly to architecture.
- FRIDAY must not skip Phase 2 before `temper-architect` when specs are pending or unapproved.
- FRIDAY never ends either analyst loop by assumption.

## Phase 1 Gap-Resolution Contract

Loop:

```text
gap report -> collect one user answer at a time -> save labeled answers -> send one consolidated batch -> analyst resolution status/new gaps -> repeat until zero blocking gaps
```

When the analyst emits a Phase 1 gap report, FRIDAY must:

- Present the report as normal text.
- Extract each `GAP-XXX` item and `Surface to user:` question when reliable.
- Ask exactly one actionable gap question at a time.
- Persist only the minimum structured resume state needed for the current gap.
- Persist `pending_interaction.source_ref` with `report_type`, `item_id`, `sequence`, and `total` for the surfaced item.
- Keep `current_gap_index` aligned to the surfaced queue item.

FRIDAY must not replay the full analyst report through the question tool or persist the full report inside `pending_interaction`.

Exit condition: Phase 1 ends only when the analyst's resolution output shows zero blocking gaps and the analyst emits its Phase 1 completion report.

## Phase 2 Ambiguity-Resolution Contract

Loop:

```text
ambiguity stop report -> collect one user answer at a time -> save labeled answers -> send one consolidated batch -> analyst ambiguity resolution status/new ambiguities -> repeat until zero blocking ambiguities
```

When the analyst emits a Phase 2 ambiguity stop report, FRIDAY must:

- Present the report as normal text.
- Extract each `AMB-XXX` item and `Surface to user:` question when reliable.
- Ask exactly one actionable ambiguity question at a time.
- Persist only the minimum structured resume state needed for the current ambiguity.
- Persist `pending_interaction.source_ref` with `report_type`, `item_id`, `sequence`, and `total` for the surfaced item.
- Keep `current_ambiguity_index` aligned to the surfaced queue item.

FRIDAY must not replay the full analyst report through the question tool or persist the full report inside `pending_interaction`.

Exit condition: Phase 2 ends only when the analyst's ambiguity-resolution output shows zero blocking ambiguities and the analyst emits its Phase 2 completion report.

## Parse Fallback

If gap or ambiguity extraction is not reliable:

- Present the analyst report as plain text.
- Set `waiting_for: "manual-review"`.
- Set `pending_interaction.interaction_type: "parse-fallback"`.
- Set `pending_interaction.surface_via: "plain-text"`.
- Set `pending_interaction.source_ref.report_type` to the report type that could not be parsed.
- Store `resume_hint` and `fallback_reason`.
- Wait for the user's direct reply or rerun decision.

Use parse fallback when item IDs are missing, multiple questions are fused into one unresolved prompt, item order cannot be trusted, severity/blocking status is unclear, or the report does not identify a safe single next question.

## Resume Action Map

When `status` is `awaiting-agent-cycle`, combine `phase` and `waiting_for`:

- `phase-1-prd` plus `gap-answer`: ask only `pending_interaction.prompt_text`.
- `phase-1-prd` plus `gap-batch-send`: send one consolidated labeled `GAP-XXX` batch to `temper-analyst` before asking anything else.
- `phase-2-specs` plus `ambiguity-answer`: ask only `pending_interaction.prompt_text`.
- `phase-2-specs` plus `ambiguity-batch-send`: send one consolidated labeled `AMB-XXX` batch to `temper-analyst` before asking anything else.
- `manual-review`: remind the user with `pending_interaction.resume_hint` and wait for the user's direct reply or rerun decision.

Before resuming, validate:

- `active_cycle.agent` is `temper-analyst`.
- `phase`, `cycle_type`, `waiting_for`, `last_report_type`, `question_origin`, and `cycle_count` are present.
- `pending_interaction.interaction_type`, `prompt_text` or `resume_hint`, `expected_reply`, and `source_ref.report_type` are present.
- Phase 1 uses `gap_queue`, `collected_gap_answers`, and `current_gap_index`; Phase 2 uses `ambiguity_queue`, `collected_ambiguity_answers`, and `current_ambiguity_index`.
- Queue indexes are in range unless the next action is `gap-batch-send` or `ambiguity-batch-send`.

If validation fails, do not guess. Use parse fallback or ask for one repair/reset decision.

## Queue And Batch Rules

- Surface one queue item at a time in sequence order.
- Store each user answer under the exact `GAP-XXX` or `AMB-XXX` key.
- Advance the relevant index only after the answer is stored.
- Set `waiting_for` to `gap-batch-send` or `ambiguity-batch-send` only after all blocking items in the current queue have captured answers.
- Send one consolidated labeled batch to the analyst; do not send partial queues unless the analyst explicitly requested partial handling.
- If the user changes a prior answer, update only that answer and keep item IDs intact.
- If the user provides answers for multiple queued items at once, map only the answers that can be matched reliably; ask for the next unmatched item.

## Required Analyst Cycle State

When `active_cycle.agent` is `temper-analyst`, persist:

- `phase: "phase-1-prd" | "phase-2-specs"`.
- `cycle_type: "gap-resolution" | "spec-ambiguity-resolution"`.
- `waiting_for`.
- `last_report_type`.
- `question_origin: "analyst"`.
- `pending_interaction` fields needed to resume.
- `gap_queue`, `collected_gap_answers`, and `current_gap_index` for Phase 1 rounds.
- `ambiguity_queue`, `collected_ambiguity_answers`, and `current_ambiguity_index` for Phase 2 rounds.
- `cycle_count`.

`pending_interaction.source_ref` is required for every surfaced analyst item. Use `item_id: null` only for parse fallback or whole-report manual review.
