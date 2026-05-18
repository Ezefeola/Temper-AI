---
name: architect-proposal-formats
description: >
  Contains all structured proposal and report formats the temper-architect
  agent emits during its workflow. Includes startup report, mode report,
  domain analysis, problem analysis, architectural proposal, architectural
  plan, updated proposal, document offer, required docs completion, and
  final completion report. Load this skill when the architect needs to
  format any of these outputs.
---

# Architect Proposal and Report Formats

These formats define the exact structure of every structured output the architect emits.
Every output is a **structured report** — never informal conversation.

Canonical architecture pattern values for all architect outputs:
- `Clean Architecture`
- `Hexagonal Architecture`
- `Vertical Slice Architecture`
- `Onion Architecture`

---

## 1. Startup Report

Emitted at the very start of execution, before Phase 1.

```
🏗️ temper-architect activated
   Role: Senior Software Architect
   Input received: [one-line summary]
   Context source: Docs/Functional-Analysis/PRD.md [exists | not found]
```

---

## 2. Mode Report

Emitted after Phase 1 — detecting the operating mode.

```
🔍 Mode detected: [Architectural Design | Problem Solving]
   Basis: [one sentence explaining what in the input determined this]
   Context source: [Docs/Functional-Analysis/PRD.md | provided description | existing system | mixed]

→ Proceeding to [Phase 2-A | Phase 2-B].
```

If the mode is genuinely ambiguous, emit the clarification request format below and stop.

---

## 3. Clarification Request Format

Emitted whenever the architect cannot proceed safely without user input.
This is the single format for mode ambiguity, missing design context, technical preference
checkpoint questions, problem ambiguity, or document selection requests that need clarification
before the next phase.

```
❓ [Mode clarification needed | Context needed | Technical preference checkpoint | Problem clarification needed | Document selection needed]

Reason:
  [one sentence describing what is blocking progress]

I need:
  1. [Specific missing information or choice]
  2. [Specific missing information or choice]

Once you answer, I will continue from [Phase X].
```

Rules:
- Keep it specific and minimal
- Ask only for information that is actually blocking the next step
- If only one answer is needed, include only one numbered item
- For technical preference checkpoints, ask only about the blocking decision buckets:
  architecture pattern, stack, external dependencies
- Make it easy for the user to answer `no preference` for any bucket

---

## 4. Domain Analysis Format (Phase 2-A)

Emitted during Architectural Design mode after extracting architectural signals from context.

```
🔍 Domain analysis

Context source: [PRD | provided description | elicited input]

Signals extracted:
  User roles: [N] — [list]
  Workflows with state: [N] — [list entities with lifecycle]
  Business rule complexity: [simple validations | conditional logic | complex invariants]
  External integrations: [list or "none"]
  Implied scale: [internal tool | startup MVP | mid-size system | enterprise]
Existing constraints: [list or "none"]

Technical preference capture:
  Stack / platform: [explicit requirement/constraint | explicit no preference | not stated]
  Architecture pattern: [explicit requirement/constraint | explicit no preference | not stated]
  External dependency constraints: [explicit requirement/constraint | explicit no preference | not stated]
  Notes: [approved/banned vendors, license limits, managed vs self-hosted, security/compliance, or "none"]

External dependency signals (from PRD):
  [PRD requirement that implies a third-party package — e.g. "send email" → MailKit]
  [PRD requirement that implies a third-party package — e.g. "generate Excel" → ClosedXML]
  [If none found: "No external dependencies identified beyond base stack"]

Architectural implications:
  [Signal] → [what it suggests]
  [Signal] → [what it suggests]

Risks identified:
  [Risk 1]
  [Risk 2 — or "None identified"]

→ Proceeding to proposal.
```

---

## 5. Problem Analysis Format (Phase 2-B)

Emitted during Problem Solving mode after understanding the problem.

```
🔍 Problem analysis

Problem statement: [clear description of what is failing or blocking]
Observable symptom: [what the team is seeing]
Suspected root cause: [architectural or design cause — not code-level]
Affected scope: [what parts of the system are involved]
Constraints on solution: [what cannot be changed or broken]

→ Proceeding to architectural plan.
```

If critical information is missing, use the clarification request format above.

---

## 6. Architectural Proposal Format (Phase 3-A)

Emitted during Architectural Design mode. Do NOT generate any files yet.
Do NOT proceed until the proposal is explicitly confirmed.

**Proposal content rule — CRITICAL:**
The proposal presents DECISIONS only. It must NEVER contain:
- Folder structures or directory trees
- File names or file path enumerations
- Class names, method names, or namespace suggestions
- Code snippets in any language
- "Structure is X" or "Files go in Y" descriptions

```
📐 Architectural proposal

── Architecture pattern ─────────────────────────────────────────

Pattern: [Clean Architecture | Hexagonal Architecture | Vertical Slice Architecture | Onion Architecture]
Decision source: [user-required | architect recommendation after explicit no preference | architect recommendation after targeted checkpoint]
Justification:
  [Specific signal that justifies this — tied to domain analysis]
  [Second signal if applicable]
Trade-off accepted: [what this pattern costs vs. what it gains here]
Alternatives considered: [what was rejected and why — one line each]

── Backend stack ────────────────────────────────────────────────

Runtime: [value]
  Decision source: [user-required | architect recommendation]
  Reason: [tied to context]
Database: [value]
  Decision source: [user-required | architect recommendation]
  Reason: [tied to domain scale and complexity]
ORM / data access: [value]
  Decision source: [user-required | architect recommendation]
  Reason: [why]
Auth strategy: [value]
  Decision source: [user-required | architect recommendation]
  Reason: [tied to frontend type and stateless/stateful decision]
API documentation: [value]
  Decision source: [user-required | architect recommendation]
  Reason: [why]

Additional components:
  Health checks: [Yes / No] — [reason]
  Messaging: [value] — [reason tied to domain signals, or "not justified by domain"]
  Caching: [value] — [reason tied to domain signals, or "not justified by domain"]
  Logging: [value] — [reason]

── Frontend ─────────────────────────────────────────────────────

Type: [value | None | API Only]
  Decision source: [user-required | architect recommendation]
  Reason: [tied to user roles and interaction patterns]

[If frontend exists:]
  State management: [approach] — [reason]
  Backend communication: [REST | GraphQL | SignalR | combination] — [reason]
  Auth handling: [approach] — [reason]

── External dependencies ────────────────────────────────────────

  Responsibility / Need        Proposed Choice        Constraint Handling
  ───────────────────────────  ─────────────────────  ─────────────────────────────────────────
  [e.g. Send email reports]    [e.g. MailKit]         [approved OSS only | no vendor lock-in]
  [e.g. Generate Excel]        [e.g. ClosedXML]       [commercial license avoided]
  [e.g. Auth provider]         [e.g. self-hosted JWT] [managed not allowed | compliance]
  [e.g. Storage / search]      [e.g. Azure Blob]      [vendor preference | data residency]

  For each row, state whether the choice is:
  - required by user constraint
  - selected from an approved/preferred set
  - recommended by the architect because no preference was given

  If no external dependencies: "No external packages required beyond base stack."

  Explicit constraint categories to account for when relevant:
  - licensing / commercial-use restrictions
  - approved or banned libraries/vendors
  - managed service vs self-hosted requirements
  - security / compliance / procurement / data residency constraints

── Consistency check ────────────────────────────────────────────

  ✅ [Decision A] is consistent with [Decision B]
  ✅ Auth strategy aligns with frontend type and API design
  ✅ External dependencies are compatible with chosen runtime and architecture
  ⚠️ [Any tension — or remove this line if none]

── Risks ────────────────────────────────────────────────────────

  [Risk 1 and mitigation — or "No significant risks identified"]

────────────────────────────────────────────────────────────────

Please confirm this proposal or tell me what you want to change.
If needed, respond by bucket: architecture pattern, stack, external dependencies.
I will update any decision without resistance.
If a change introduces a risk, I will note it once — the decision is yours.
```

---

## 7. Architectural Plan Format (Phase 3-B)

Emitted during Problem Solving mode. Do NOT generate any files yet.

```
📋 Architectural plan

── Root cause ───────────────────────────────────────────────────

[Clear identification of the architectural or design cause of the problem.
Not code-level — structural: wrong layer responsibilities, missing abstraction,
coupling that should not exist, missing boundary, etc.]

── Proposed solution ────────────────────────────────────────────

[What needs to change architecturally, explained in plain terms]

Step 1: [action — what, where, why]
Step 2: [action — what, where, why]
Step N: [action — what, where, why]

── Impact assessment ────────────────────────────────────────────

  What this fixes: [clear outcome]
  What this affects: [parts of the system touched by this plan]
  What this does NOT change: [explicit boundaries of the plan]

── Risks ────────────────────────────────────────────────────────

  [Risk 1 and mitigation]
  [Risk 2 and mitigation — or "No significant risks identified"]

── Alternatives considered ─────────────────────────────────────

  [Alternative 1] — rejected because [reason]
  [Alternative 2] — rejected because [reason — or "No alternatives evaluated"]

────────────────────────────────────────────────────────────────

Please confirm this plan or tell me what you want to change.
I will update any decision without resistance.
If a change introduces a risk, I will note it once — the decision is yours.
```

---

## 8. Updated Proposal Format (Phase 4)

Emitted when any decision is changed during confirmation feedback.

```
📝 Proposal updated

Changed: [original] → [new decision]

⚠️ Note: [one sentence about risk or inconsistency, if any — otherwise omit this line]
This is recorded. The decision stands as confirmed.

[Reprint only the sections that changed]

Please confirm the updated proposal or continue adjusting.
```

Rules:
- Accept the change immediately — do NOT argue or re-justify the original decision
- Note any risk or inconsistency the change introduces — once, clearly, then drop it
- Never surface the same objection twice
- Wait for confirmation before proceeding

---

## 9. Document Offer Format (Phase 5)

Emitted after proposal confirmation, before document generation.

**Mode A — Architectural Design:**

```
📄 Proposal confirmed. Here's what I'll generate:

  Required (for implementation agents):
    ✅ Docs/Application/Architecture/backend-config.md  — backend implementation agents need this
    ✅ Docs/Application/Domain/DDD-Vocabulary.md        — backend agent uses this for domain terminology
    [✅ Docs/Application/Architecture/frontend-config.md] — frontend agent needs this (only if frontend exists in proposal)

  Optional documentation:
    [ ] Docs/Application/Architecture/architecture-decision.md  — ADR: full reasoning, trade-offs, alternatives
    [ ] Docs/Application/Domain/domain-model.md                 — DDD model: entities, aggregates, events, Mermaid diagrams
    [ ] Docs/Application/System/system-architecture.md          — bounded contexts, component diagrams, integrations

  Note: These reference documents are the authoritative source for domain and architecture.
  The docs agent will link to them when generating ARCHITECTURE.md and SYSTEM.md.

  After you reply, I will generate the required documents automatically and add any
  optional documents you selected.

  Select any optional documents you want, or confirm to proceed with required only.
```

Required documents are determined automatically:
- `Docs/Application/Architecture/backend-config.md` — auto-included if the proposal has a backend
- `Docs/Application/Architecture/frontend-config.md` — auto-included only if the proposal includes a frontend
- `Docs/Application/Domain/DDD-Vocabulary.md` — always auto-included

**Mode B — Problem Solving:**

```
📄 Plan confirmed. Which documents do you want me to generate?

  [ ] Docs/Application/Architecture/architectural-plan.md — full problem analysis, plan, risks, and alternatives

  Select if needed. If you only needed the analysis, just let me know.
```

---

## 10. Required Docs Completion Report (Phase 6)

Emitted after required documents are generated.

```
✅ Required documents generated

Files created:
  - Docs/Application/Architecture/backend-config.md
  - Docs/Application/Domain/DDD-Vocabulary.md
  [- Docs/Application/Architecture/frontend-config.md]

[If optional documents were selected:]
→ Proceeding to optional documentation.
[If no optional documents were selected:]
→ Architect phase complete.
```

---

## 11. Completion Report (Phase 8)

Emitted at the very end of the architect's execution.

**Mode A — Architectural Design:**

```
✅ temper-architect complete

Mode: Architectural Design
Proposal confirmed: Yes
Required documents generated:
  - Docs/Application/Architecture/backend-config.md
  - Docs/Application/Domain/DDD-Vocabulary.md
  [- Docs/Application/Architecture/frontend-config.md]
Optional documents generated (authoritative reference docs in Docs/Application/):
  [- Docs/Application/Architecture/architecture-decision.md]
  [- Docs/Application/Domain/domain-model.md]
  [- Docs/Application/System/system-architecture.md]
  [or "None requested"]
Version: [YYYYMMDD-HHMM]

Note: After the build is complete, you can request Docs/Application/System/api-contracts.md to be generated
from the built backend code — this is the contract the frontend agent will use.
The docs agent will link to these reference docs when generating ARCHITECTURE.md and SYSTEM.md.
```

**Mode B — Problem Solving:**

```
✅ temper-architect complete

Mode: Problem Solving
Plan confirmed: Yes
Documents generated:
  [- Docs/Application/Architecture/architectural-plan.md]
  [or "None requested"]
Version: [YYYYMMDD-HHMM]
```
