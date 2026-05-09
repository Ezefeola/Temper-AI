---
name: analyst-report-formats
description: >
  All structured report formats used by the temper-analyst agent across
  Phase 1 (PRD) and Phase 2 (Spec). Contains every report template the
  analyst emits during its workflow: startup, input synthesis, delta analysis,
  gap reports, resolution status, contradictions, completeness checklist,
  and phase completion reports.
---

# Analyst Report Formats

Every output the analyst produces is a **structured report**. Never informal conversation.
Use the exact formats below for each workflow step.

---

## 1. Startup Report — Phase 1 (PRD)

Emitted when the analyst is activated for Phase 1.

```
🔍 temper-analyst activated — Phase 1: PRD
   Role: Senior Functional Analyst
   Mission: Elicit requirements → generate .temper/prd.md
   Skill loaded: prd-analyzer
   Existing PRD: [yes — will perform delta analysis first | no — full elicitation required]
   Input received: [one-line summary of what the orchestrator passed]
```

---

## 2. Input Synthesis Report (Phase 1.1)

Emitted after ingesting and synthesizing input, before any gap report.

```
📋 Input synthesis report

Understood goal:
  [One paragraph — what the system should do, in functional terms]

What I already know:
  Roles identified: [list, or "none yet"]
  Capabilities mentioned: [list, or "none yet"]
  Constraints mentioned: [list, or "none yet"]
  Integrations mentioned: [list, or "none yet"]

What I still need:
  Gaps detected: [N]
  Contradictions detected: [N]
  Ambiguities detected: [N]

→ Proceeding to [delta analysis | gap report].
```

---

## 3. Delta Analysis Report (Phase 1.2)

Emitted only if `.temper/prd.md` already exists. Compares existing PRD with new input.

```
📊 Delta analysis report — .temper/prd.md exists

➕ Additions ([N]):
  - [capability]

➖ Removals ([N]):
  - [capability]

✏️ Modifications ([N]):
  - [what it was] → [what it should be now]

⚠️ Conflicts requiring resolution ([N]):
  - [description of the contradiction and its impact]

→ Awaiting orchestrator confirmation to proceed.
```

> If no PRD exists, skip this report and go directly to the Gap Report.

---

## 4. Gap Report (Phase 1.3)

The primary elicitation report. Emits a single structured report covering everything needed.

**NEVER include gaps about technology, architecture, database, frontend, authentication,
or infrastructure.** Those belong to other agents.

Structure by category. For each gap, specify its ID, severity, question to surface, and why it matters.

```
❓ Gap report — orchestrator action required

── Category A: Purpose and actors ──────────────────────────────

GAP-001 [BLOCKING]
  Surface to user: "What specific problem does this system solve, and who is affected by it?"
  Why it matters: Without a clear problem statement, scope cannot be validated or bounded.

GAP-002 [BLOCKING]
  Surface to user: "Who are all the people or systems that will interact with this application?
                   Consider: end users, admins, supervisors, external parties, automated processes."
  Why it matters: Capabilities cannot be defined without knowing their actors.

GAP-003 [IMPORTANT]
  Surface to user: "What does success look like for each type of user?"
  Why it matters: Defines acceptance criteria implicitly and helps prioritize capabilities.

── Category B: Functional capabilities ─────────────────────────

GAP-004 [BLOCKING]
  Surface to user: "For each user type identified, what should they be able to DO in this system?"
  Why it matters: Core of the functional scope — nothing can be built without this.

[continue for all detected gaps...]

── Category C: Scope boundaries ───────────────────────────────

GAP-00N [IMPORTANT]
  Surface to user: "What is explicitly out of scope for this version?"
  Why it matters: Prevents scope creep and sets clear expectations for the architect.

── Category D: Business rules and constraints ──────────────────

[gaps about rules, validations, status workflows, conditions...]

── Category E: External interactions ───────────────────────────

[gaps about third-party services, compliance, regulations...]

─────────────────────────────────────────────────────────────
Total gaps: [N] — BLOCKING: [N] | IMPORTANT: [N] | CLARIFYING: [N]

→ Please return this report with answers filled in for each gap.
→ I will not proceed to PRD generation until all BLOCKING gaps are resolved.
```

---

## 5. Resolution Status Report (Phase 1.4)

Emitted when the orchestrator returns answers to a gap report.

```
📬 Gap resolution status

✅ Resolved ([N]):
  GAP-001: [one-line summary of the answer received]
  GAP-002: [one-line summary of the answer received]

⚠️ Partially resolved — follow-up needed ([N]):
  GAP-004: [what was answered] / [what is still unclear]
  → Follow-up: [specific question to surface to the user]

❌ Unresolved ([N]):
  GAP-00N: No answer received.
  → Still blocking PRD generation.

⚠️ New contradictions detected ([N]):
  [Description of contradiction, what caused it, impact]

→ [Proceeding to completeness validation | Emitting follow-up gap report for remaining items]
```

If follow-up gaps remain, emit a new Gap Report (format #4) covering only the unresolved items.
Repeat until all BLOCKING gaps are resolved.

---

## 6. Contradiction Report (Phase 1.5)

Emitted when a contradiction between two pieces of information is detected.

```
⚠️ Contradiction — resolution required

ID: CONFLICT-001
Statement A: "[source and exact content]"
Statement B: "[source and exact content]"
Impact: [what breaks architecturally or functionally if this is not resolved]
Options: [if applicable, describe the two possible interpretations]

→ Awaiting orchestrator resolution before proceeding.
```

Do NOT proceed to completeness validation until all contradictions are resolved.
**Exception:** explicit orchestrator override — every unresolved contradiction becomes a BLOCKING RISK entry in the PRD.

---

## 7. Completeness Checklist (Phase 1.6)

Validated internally before generating the PRD. Do NOT proceed to PRD generation if any BLOCKING item is unchecked.

```
Completeness checklist:

Core questions (all four must be answerable):
□ What problem does this system solve?                              [answered / missing]
□ For whom does it solve it?                                        [answered / missing]
□ How will users know the system is working correctly?              [answered / missing]
□ What is the minimum set of capabilities that delivers value?      [answered / missing]

Scope:
□ Problem statement clearly and specifically defined                [✓ / ✗]
□ All user roles and personas identified and described              [✓ / ✗]
□ Every role has at least one confirmed functional capability       [✓ / ✗]
□ Scope exclusions explicitly confirmed (not assumed)               [✓ / ✗]
□ MVP boundary clear and agreed upon                                [✓ / ✗]

Rules and structure:
□ Business rules documented or confirmed as "none"                  [✓ / ✗]
□ Status workflows documented or confirmed as "not applicable"      [✓ / ✗]
□ External integrations confirmed or confirmed as "none"            [✓ / ✗]

Quality:
□ All contradictions resolved (or marked BLOCKING RISK)             [✓ / ✗]
□ All BLOCKING gaps resolved (or marked BLOCKING RISK)              [✓ / ✗]
□ No solution disguised as a requirement remains in scope          [✓ / ✗]
```

If any item marked [✗] is a BLOCKING item, return to Phase 1.3 and emit a focused gap report.

---

## 8. Phase 1 Completion Report (Phase 1.8)

Emitted after `.temper/prd.md` is generated.

```
✅ Phase 1 complete — PRD generated

Summary:
  Project: [name and one-line description]
  User roles identified: [N] — [list names]
  Functional capabilities documented: [N]
  Business rules documented: [N]
  Status workflows: [list entities, or "None"]
  External integrations: [list, or "None"]
  Future scope items deferred: [N]
  Blocking risks carried forward: [N — list if any, or "None"]

Output:
  .temper/prd.md — version [YYYYMMDD-HHMM] — sections 11/11 complete

⚠️ Scope rule: Only the capabilities in Section 4 of the PRD will be implemented.
   Anything not listed is OUT OF SCOPE for this version.

→ Awaiting user approval for PRD.
→ After PRD approval, Phase 2 (Spec generation) will be invoked with clean context.
```

---

## 9. Phase 2 Startup Report (Phase 2.1)

Emitted when the analyst is activated for Phase 2 (Spec generation).

```
📝 temper-analyst (Phase 2: Spec) starting
   Skill loaded: spec-generator
   Input: .temper/prd.md (approved)
   Mission: Generate .temper/specs/ with user stories
```

If the orchestrator did not load the spec-generator skill, emit:

```
⚠️ Phase 2 requires spec-generator skill

The orchestrator did not load the spec-generator skill. Please re-invoke with:
  temper-analyst + spec-generator skill loaded
```

Stop and wait. Do not proceed without the skill.

---

## 10. Phase 2 Completion Report (Phase 2.6)

Emitted after `.temper/specs/` files are generated.

```
✅ Phase 2 complete — Specs generated

Summary:
• User stories: [N] created
• Priority breakdown: [N] High | [N] Medium | [N] Low
• Business rules documented: [N] total
• Files generated: .temper/specs/INDEX.md + [N] user story files
• Non-functional requirements: [list or "none"]
• Open questions: [N — list if any, or "none"]

Output:
  .temper/specs/INDEX.md — version [YYYYMMDD-HHMM]
  .temper/specs/US-001-[slug].md
  .temper/specs/US-002-[slug].md
  [...]

→ Awaiting user approval for Specs.
→ After Spec approval, next agent: temper-architect
```
