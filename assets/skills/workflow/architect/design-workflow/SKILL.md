---
name: architect-design-workflow
description: >
  Mode A (Architectural Design) workflow for temper-architect — the full
  sequence from reading the PRD through proposal, confirmation, document offer,
  and generation. Load this skill after the architect detects Architectural
  Design mode in Phase 1, and execute it start to finish.
---

# Architect — Architectural Design Workflow (Mode A)

This is the complete Mode A workflow. The architect loads it after detecting
**Architectural Design** mode and executes every phase in strict order.

Report formats come from `workflow/architect/proposal-formats`.
Document templates come from `workflow/architect/document-templates`.
Never duplicate those contents here — load and reference them.

---

## Phase 2-A — Analyze context

### Step 1 — Read the PRD

Read `Docs/Functional-Analysis/PRD.md` when it exists. This is the preferred design
context source. Do NOT read specs. Do NOT read design.md. Do NOT read any other agent's output.

If `Docs/Functional-Analysis/PRD.md` does not exist, elicit the minimum needed using the
clarification request format from `workflow/architect/proposal-formats` (header `Context needed`),
asking only:
1. What does this system do? (one paragraph is enough)
2. Who uses it? (user types or roles)
3. Are there any known constraints? (existing tech, team preferences, deployment requirements)

Wait for the response before proceeding.

### Step 2 — Extract architectural signals

Once context is available, extract:
- User roles (count and list)
- Workflows with state (entities with lifecycle)
- Business rule complexity (simple validations | conditional logic | complex invariants)
- External integrations
- Implied scale (internal tool | startup MVP | mid-size system | enterprise)
- Existing constraints
- External dependency signals (PRD requirements implying third-party packages — email,
  Excel, PDF, SMS, payment gateway, cloud storage, etc.)

Emit the domain analysis using the format from `workflow/architect/proposal-formats`.

---

## Phase 2-A.5 — Technical preference checkpoint

Before proposing anything, classify each decision bucket as exactly one of:
- `Explicit requirement/constraint` — the user or context already mandates something
- `No preference` — the user explicitly leaves it to your recommendation
- `Unknown but proposal-blocking` — you need one targeted answer before recommending safely

Buckets to classify:
- stack / platform
- architecture pattern
- external dependencies (per detected responsibility)

For external dependencies, account for: approved or banned packages/libraries/vendors;
license or commercial-use restrictions; managed service vs self-hosted requirements;
security, compliance, data residency, or procurement limits.

Only ask follow-up questions for buckets that are truly proposal-blocking. Do not ask
open-ended technical questionnaires. Ask the minimum targeted question that separates a
required constraint from a no-preference, and make `no preference` an easy answer.
Use the clarification request format from `workflow/architect/proposal-formats`
(header `Technical preference checkpoint`).

If all buckets are either explicit or clearly no-preference, proceed directly to Phase 3-A.

---

## Phase 3-A — Present proposal

Based on the domain analysis, form a complete technical proposal and present it.
Do NOT generate any files yet. Do NOT proceed until the proposal is explicitly confirmed.

Your proposal must show where each major decision came from:
- user-required preference or constraint
- architect recommendation because no preference was given

Identify every external dependency from the PRD: any requirement that implies a third-party
package must be proposed with a specific package and an alternative, in the proposal's
"External dependencies" section, for user confirmation — these are architectural decisions,
not implementation details to discover later.

Load `workflow/architect/proposal-formats` and present the architectural proposal using its
exact format. The proposal presents DECISIONS only — the content constraints (no folder
structures, file names, class/method names, or code) are defined in that skill and are
binding here.

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

## Phase 5 — Smart document offer

After the proposal is confirmed, determine which documents are required from the proposal
content. Do NOT generate anything yet.

Required documents (auto-included, generated only after selection is captured):
- `backend-config.md` — auto-included if the proposal has a backend (it almost always does)
- `frontend-config.md` — auto-included only if the proposal includes a frontend
- `DDD-Vocabulary.md` — always auto-included

Emit the document offer using the format from `workflow/architect/proposal-formats`.
The user is selecting which OPTIONAL documents to add; required documents are still generated
automatically after the selection is captured. Wait for selection.

---

## Phase 6 — Generate required documents

Generate the auto-included required documents and write them to `Docs/Application/`:
- `backend-config.md` → `Docs/Application/Architecture/backend-config.md` (if proposal has backend)
- `frontend-config.md` → `Docs/Application/Architecture/frontend-config.md` (if proposal has frontend)
- `DDD-Vocabulary.md` → `Docs/Application/Domain/DDD-Vocabulary.md` (always — load `ddd/documents`)

Load `workflow/architect/document-templates` and generate each document using its exact
template. Then emit the required docs completion report from `workflow/architect/proposal-formats`.

If no optional documents were selected, proceed directly to Phase 8.

---

## Phase 7 — Generate optional documentation

Generate only the optional documents the user explicitly selected, written to the appropriate
`Docs/Application/` subfolder, in this order (skip any not selected):
1. `architecture-decision.md` → `Docs/Application/Architecture/architecture-decision.md`
2. `domain-model.md` → `Docs/Application/Domain/domain-model.md` (load `ddd/documents`)
3. `system-architecture.md` → `Docs/Application/System/system-architecture.md` (load `ddd/documents`)

Load `workflow/architect/document-templates` and generate each selected document using its
exact template. When generating `domain-model.md` or `system-architecture.md`, load the
`ddd/documents` skill first and follow its templates and rules.

After each document, emit a brief progress note: `📄 Generated: [filename]`.
Then proceed to Phase 8.

---

## Phase 8 — Completion report

Emit the Mode A completion report using the format from `workflow/architect/proposal-formats`.
