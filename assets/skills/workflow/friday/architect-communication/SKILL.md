---
name: friday-architect-communication
description: >
  Architect communication contract for FRIDAY. Load when FRIDAY delegates to
  temper-architect, resumes an architect loop, or needs exact architect handoff
  and state rules.
---

# FRIDAY Architect Communication Contract

## Purpose

This skill defines the explicit working contract between FRIDAY and `temper-architect`. When loaded, it is authoritative for architect-loop behavior. `workflow/friday/state-schema` remains authoritative for shared state shape.

Use this skill when FRIDAY is:

- Delegating the first architect turn.
- Passing user clarification or change requests back to the architect.
- Resuming an architect-originated loop from `active_cycle`.
- Validating that an architect interaction was reduced safely before persisting state.

## Prompt Contract

### First Turn

Include:

- The raw user request.
- Relevant approved plain-language requirements context only when needed.
- Known preferences or constraints exactly as provided.
- The specific architect task.
- The architect's standard output expectation when needed.

Template:

```text
Context: User request: [raw user request]. Relevant approved requirements context: [plain-language excerpt only when needed].
Task: Analyze the requirements. Propose a technical architecture that supports the domain. Produce an architectural proposal.
Format: Produce your standard architectural proposal output.
```

### Change-Request Turn

Template:

```text
Context: User requested changes to the proposal: [user feedback, exact].
Task: Incorporate the requested changes. Produce an updated proposal.
Format: Produce your standard updated proposal output.
```

### Absolute Architect Handoff Rules

- Pass user feedback and preferences exactly as received.
- Do not pre-negotiate technical decisions on the architect's behalf.
- Do not interpret vague feedback before passing it through.
- Do not include file paths, internal artifact references, implementation hints, class names, or layer names.
- Do not tell the architect what file to read.

## Loop Contract

The architect loop covers clarification, proposal, confirmation, document selection, generation, and completion.

Stages:

- `mode-clarification`: determine whether the architect is designing architecture or solving a technical problem.
- `context-clarification`: gather missing technical context needed to make a valid recommendation.
- `preference-clarification`: ask for user preferences only where they affect architectural choices; `no preference` is valid.
- `problem-clarification`: narrow a technical problem before solution proposals.
- `proposal-confirmation`: ask the user to approve the proposal or request changes.
- `document-selection`: ask which offered documents to generate when the architect offers optional documents.
- `manual-review`: fallback when FRIDAY cannot safely reduce the architect report.
- `none`: no active architect wait remains.

Stage-to-report expectations:

- Mode clarification normally follows a mode report.
- Context, preference, or problem clarification normally follows a clarification request, domain analysis, or problem analysis.
- Proposal confirmation follows an architectural proposal, architectural plan, or updated proposal.
- Document selection follows a document offer.
- Completion follows a completion report and clears the active cycle.

## Question Reduction Rules

FRIDAY must:

- Present architect reports exactly as received.
- Use the question tool only for the minimal next clarification or decision prompt.
- Ask one short clarification or one explicit decision at a time.
- Name the relevant decision bucket when applicable: `architecture pattern`, `stack`, or `external dependencies`.
- Make `no preference` an explicit acceptable answer for preference checkpoints.

FRIDAY must not:

- Replay a full proposal, plan, ambiguity report, or document offer through the question tool.
- Persist the full architect report inside `pending_interaction`.
- Treat intermediate architect decisions as generic plan approval.

## Turn Handling Rules

- Store only the minimal prompt and metadata needed to resume.
- After the user responds, pass that response back to `temper-architect` exactly as received.
- If the architect returns another actionable interaction, persist the next minimal interaction and stop with `status: awaiting-agent-cycle`.
- The loop ends only when the architect emits its completion report.
- Never treat proposal confirmation, change requests, or document selection as generic FRIDAY plan approval.
- Persist `pending_interaction.source_ref.report_type`, `sequence`, and `total` for the current decision prompt when available.

## Parse Fallback

If the architect interaction cannot be reduced reliably to one actionable prompt:

- Present the report as plain text.
- Set `waiting_for: "manual-review"`.
- Set `pending_interaction.interaction_type: "parse-fallback"`.
- Set `pending_interaction.surface_via: "plain-text"`.
- Set `pending_interaction.source_ref.report_type` to the architect report type that could not be reduced.
- Store `resume_hint` and `fallback_reason`.
- Wait for the user's direct reply or rerun decision.

Use parse fallback when the report contains multiple unresolved decision paths, no clear decision prompt, conflicting proposal states, ambiguous document options, or missing mode/context needed to classify the next architect turn.

## Required Architect Cycle State

When `active_cycle.agent` is `temper-architect`, persist:

- `cycle_type: "architect-loop"`.
- `mode: "architectural-design" | "problem-solving"`.
- `waiting_for`.
- `last_report_type`.
- `question_origin: "architect"`.
- `pending_interaction` fields needed to resume.
- `cycle_count`.

Resume rules:

- If `status` is `awaiting-agent-cycle` and `question_origin` is `architect`, resume the architect loop, not normal step execution.
- Use `waiting_for` to decide which minimal clarification or decision prompt is valid next.
- Validate that `active_cycle.agent`, `cycle_type`, `mode` when required, `waiting_for`, `last_report_type`, `question_origin`, `pending_interaction`, and `cycle_count` exist before resuming.
- Validate that `pending_interaction.interaction_type`, `prompt_text` or `resume_hint`, `expected_reply`, and `source_ref.report_type` exist.
- For `mode-clarification`, ask only for mode or present the available mode choices from the architect.
- For `context-clarification`, ask only for the missing technical context item requested by the architect.
- For `preference-clarification`, ask only for the blocking bucket or buckets and make `no preference` explicit.
- For `problem-clarification`, ask only for the missing problem detail requested by the architect.
- For `proposal-confirmation`, phrase the minimal prompt in decision buckets when relevant.
- For `document-selection`, ask only which offered documents to generate and preserve the user's selection exactly.
- For `manual-review`, remind the user with `pending_interaction.resume_hint` and wait for the user's direct reply or rerun decision.
- When the architect emits a completion report, clear `pending_interaction`, clear `active_cycle`, and continue normal post-execution flow.

If resume validation fails, do not infer the missing architect decision. Present the safest fallback prompt or ask for repair/reset approval.
