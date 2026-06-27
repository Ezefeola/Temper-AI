---
name: architect-problem-solving-workflow
description: >
  Mode B (Problem Solving) workflow for temper-architect — analyzing a bug,
  design issue, or blocking technical decision and producing a confirmed
  architectural plan. Load this skill after the architect detects Problem
  Solving mode in Phase 1, and execute it start to finish.
---

# Architect — Problem Solving Workflow (Mode B)

This is the complete Mode B workflow. The architect loads it after detecting
**Problem Solving** mode and executes every phase in strict order.

Report formats come from `workflow/architect/proposal-formats`.
Document templates come from `workflow/architect/document-templates`.
Never duplicate those contents here — load and reference them.

---

## Phase 2-B — Analyze the problem

### Step 1 — Understand the problem

Extract from the input:
- What is failing or blocking?
- What is the observable symptom?
- What is the suspected or known cause?
- What constraints exist on the solution? (cannot break X, must be done by Y, etc.)

If critical information is missing, ask for it before proceeding using the clarification
request format from `workflow/architect/proposal-formats` (header `Problem clarification needed`).

### Step 2 — Emit problem analysis

Emit the problem analysis using the format from `workflow/architect/proposal-formats`.

---

## Phase 3-B — Present architectural plan

Based on the problem analysis, form a concrete plan and present it for confirmation.
Do NOT generate any files yet.

Load `workflow/architect/proposal-formats` and present the architectural plan using its
exact format. The plan describes structural changes only — the content constraints
(no folder structures, file names, class/method names, or code) are defined in that skill
and are binding here.

---

## Phase 4 — Process confirmation or feedback

**If confirmed as-is:** proceed to Phase 5.

**If any decision is changed:**
1. Accept the change immediately — do NOT argue or re-justify the original decision
2. Note any risk or inconsistency the change introduces — once, clearly, then drop it
3. Wait for confirmation before proceeding

Emit the updated proposal using the format from `workflow/architect/proposal-formats`.
Reprint only the sections that changed.

---

## Phase 5 — Document offer

Mode B has no required documents. Offer one optional document:
- `architectural-plan.md` — generated only if the user selects it

Emit the Mode B document offer using the format from `workflow/architect/proposal-formats`.
Wait for selection.

---

## Phase 7 — Generate optional document

If — and only if — the user selected it, generate `architectural-plan.md` and write it to
`Docs/Application/Architecture/architectural-plan.md`.

Load `workflow/architect/document-templates` and generate it using its exact template.
Emit a brief progress note: `📄 Generated: architectural-plan.md`.

If the user only needed the analysis, generate nothing.

---

## Phase 8 — Completion report

Emit the Mode B completion report using the format from `workflow/architect/proposal-formats`.
