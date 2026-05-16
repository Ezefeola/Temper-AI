---
name: architect-communication
description: >
  Architect communication contract for the TemperAI orchestrator. Load when
  JARVIS delegates to temper-architect, resumes an architect loop, or needs
  the exact architect handoff and state rules.
---

# JARVIS <-> Architect Communication Contract

## Purpose

This skill defines the explicit working contract between JARVIS and
`temper-architect`.

When this skill is loaded, it is the authoritative contract for architect-loop
behavior. `assets/agents/temper-jarvis.agent.md` keeps only the high-level
routing, ordering, and phase-transition rules.

Use it only when JARVIS is:
- delegating the first architect turn
- passing user clarification or change requests back to the architect
- resuming an architect-originated loop from `active_cycle`
- validating that an architect interaction was reduced safely before persisting state

`workflow/jarvis/state-schema` remains authoritative for the state file shape.
This skill defines the architect-specific communication and loop behavior that
uses that state.

---

## Prompt contract

### First turn

Include:
- the raw user request
- relevant approved plain-language requirements context only when needed
- known preferences or constraints exactly as provided
- the specific architect task
- the architect's standard output expectation when needed

Template:

```
Context: User request: [raw user request]. Relevant approved requirements
context: [plain-language excerpt only when needed].
Task: Analyze the requirements. Propose a technical architecture that supports
the domain. Produce an architectural proposal.
Format: Produce your standard architectural proposal output.
```

### Change-request turn

Template:

```
Context: User requested changes to the proposal: [user feedback, exact].
Task: Incorporate the requested changes. Produce an updated proposal.
Format: Produce your standard updated proposal output.
```

### Absolute architect handoff rules

- Pass user feedback and preferences exactly as received.
- Do not pre-negotiate technical decisions on the architect's behalf.
- Do not interpret vague feedback before passing it through.
- Do not include file paths, internal artifact references, implementation hints, class names, or layer names.
- Do not tell the architect what file to read.

---

## Loop contract

The architect loop is one contract covering clarification, proposal,
confirmation, document selection, and completion.

### Architect-driven interaction stages

Stages:
- `mode-clarification`
- `context-clarification`
- `preference-clarification`
- `problem-clarification`
- `proposal-confirmation`
- `document-selection`
- `manual-review`
- `none`

These stages cover the full architect loop:
- clarification stage: mode, context, preference, or problem clarification
- proposal stage: architectural proposal or architectural plan
- document selection stage: document offer after confirmation
- generation stage: no question tool unless the architect returns another explicit decision point
- completion stage: architect completion report ends the loop

### Question reduction rules

JARVIS must:
- present architect reports exactly as received
- use the question tool only for the minimal next clarification or decision prompt
- ask one short clarification or one explicit decision at a time
- name the relevant decision bucket when applicable: `architecture pattern`, `stack`, `external dependencies`
- make `no preference` an explicit acceptable answer for preference checkpoints

JARVIS must not:
- replay a full proposal, plan, ambiguity report, or document offer through the question tool
- persist the full architect report inside `pending_interaction`

### Turn handling rules

- For clarification reports, proposal confirmations, and document selections, store only the minimal prompt and metadata needed to resume.
- After the user responds, pass that response back to `temper-architect` exactly as received.
- If the architect returns another actionable interaction, persist the next minimal interaction and stop with `status: awaiting-agent-cycle`.
- The loop ends only when the architect emits its completion report.

State shape for an active architect round:

```json
"active_cycle": {
  "agent": "temper-architect",
  "cycle_type": "architect-loop",
  "mode": "architectural-design | problem-solving",
  "waiting_for": "mode-clarification | context-clarification | preference-clarification | problem-clarification | proposal-confirmation | document-selection | manual-review | none",
  "last_report_type": "clarification-request | architectural-proposal | architectural-plan | updated-proposal | document-offer | completion-report",
  "question_origin": "architect",
  "pending_interaction": {
    "surface_via": "question | plain-text",
    "interaction_type": "architect-clarification | architect-decision | parse-fallback",
    "prompt_text": "single actionable clarification or decision prompt with decision bucket named when relevant",
    "expected_reply": "mode details | clarification answer | preference bucket answer | proposal confirmation/change request | document selection | manual reply",
    "source_ref": {
      "report_type": "clarification-request | architectural-proposal | architectural-plan | updated-proposal | document-offer",
      "item_id": null,
      "sequence": 1,
      "total": 1
    },
    "resume_hint": "one-line reminder of the pending architect decision",
    "fallback_reason": null
  },
  "cycle_count": 1
}
```

### Parse fallback

If the architect interaction cannot be reduced reliably to one actionable prompt:
- present the report as plain text
- set `waiting_for: "manual-review"`
- set `pending_interaction.interaction_type: "parse-fallback"`
- set `pending_interaction.surface_via: "plain-text"`
- store `resume_hint` and `fallback_reason`
- wait for the user's direct reply or rerun decision

---

## Required architect cycle state

When `active_cycle.agent` is `temper-architect`, the required working contract is:

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

Resume rules:
- If `status` is `awaiting-agent-cycle` and `question_origin` is `architect`, resume the architect loop, not normal step execution.
- Use `waiting_for` to decide which minimal clarification or decision prompt is valid next.
- For `preference-clarification`, ask only for the blocking bucket or buckets and make `no preference` explicit.
- For `proposal-confirmation`, when relevant, phrase the minimal prompt in decision buckets such as architecture pattern, stack, and external dependencies.
- For `mode-clarification`, `context-clarification`, `preference-clarification`, and `problem-clarification`, use the question tool only with `pending_interaction.prompt_text`.
- For `proposal-confirmation` and `document-selection`, present the architect report in plain text if needed, then ask only the minimal saved prompt.
- For `manual-review`, remind the user with `pending_interaction.resume_hint`, do not replay the full architect report through the question tool, and wait for the user's direct reply or rerun decision.
- When the architect emits a completion report, set `waiting_for: "none"`, clear `pending_interaction`, clear `active_cycle`, and continue normal post-execution flow.
