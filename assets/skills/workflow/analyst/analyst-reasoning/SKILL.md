---
name: analyst-reasoning
description: >
  Internal self-questioning framework for the TemperAI analyst agent. Activates
  at specific checkpoints across Phase 1 and Phase 2 to ensure no hidden
  stakeholder, implicit requirement, failure mode, or logical gap survives
  into a deliverable. This skill never produces visible output — it shapes
  the quality of every report, gap, PRD, and spec the analyst emits.
  Loaded by temper-analyst at session start alongside functional-analysis.
---

# Analyst Reasoning — Self-Questioning Framework

## Purpose

This is the analyst's **internal reasoning engine**. It does not produce files,
reports, or user-facing output. It activates at specific checkpoints in the
workflow to stress-test the analyst's own understanding before any deliverable
leaves the session.

A competent analyst asks the user good questions.
An unbeatable analyst asks **itself** better questions first.

Every dimension below is a lens through which the analyst examines its own
mental model. If a self-question reveals a gap that the user's input does not
answer, that gap becomes a candidate for the next gap report. If it reveals a
contradiction, it surfaces immediately. If it confirms coverage, the analyst
proceeds with confidence.

This skill is loaded once at session start and remains active throughout the
session. Dimensions are activated at specific phase checkpoints as documented
in the Activation Points section below.

---

## The 10 Self-Questioning Dimensions

### D1 — Hidden Stakeholders

**When activated:** Phase 1.1 (before Input Synthesis), Phase 1.3 (before Gap Report)

The person describing the system is never the only person affected by it.
Before synthesizing, the analyst must ask itself who else exists in the shadows.

**Self-questions:**

1. Who will use the system's output but never touches the system itself?
   — Example: A manager who reads exported reports but never logs in.
2. Who is affected by the system's decisions but has no voice in requirements?
   — Example: End customers whose orders are auto-rejected by a rule.
3. Does the system produce data, notifications, or artifacts consumed by
   another system or role not yet mentioned?
4. Are there regulatory or compliance stakeholders who impose rules but are
   not users? (auditors, legal teams, data protection officers)
5. Will this system replace or change an existing process? Who currently
   performs that process and how does their role change?

**If a gap is revealed:** Add a new entry to Category A gaps asking the user
to confirm or deny each suspected stakeholder. Never assume a stakeholder
exists without confirmation, but always propose candidates.

---

### D2 — Implicit Requirements

**When activated:** Phase 1.1 (before Input Synthesis), Phase 1.3 (before Gap Report)

Users state what they care about. They rarely state what they assume is obvious.
The analyst must detect what "goes without saying" — because it will not go
without building.

**Self-questions:**

1. What does the user assume the system will obviously do but has not stated?
   — Example: The user describes an order system but never mentions that
     orders must be unique. They assume it.
2. What data lifecycle is implied? (creation → editing → archiving/deletion)
   If the user describes creation, what happens afterward?
3. Is there an implicit sorting, filtering, or searching capability attached
   to every list or collection the user mentioned?
4. Are there implied permissions? ("Users can see orders" — but ALL orders?
   Their own? Their team's?)
5. Does the user's language imply a hierarchy or ownership model they have
   not explicitly described? (e.g., "the team's projects" implies teams own
   projects, which implies team membership)

**If a gap is revealed:** Add a targeted gap in the appropriate category.
Frame the question as: "You mentioned X. Does this also mean Y?" — never as
"Did you forget Y?"

---

### D3 — Failure Modes

**When activated:** Phase 1.3 (before Gap Report), Phase 1.6 (before Completeness
Validation), Phase 2 (before each user story)

Most requirements describe the happy path. The analyst must independently
generate the unhappy paths — not by guessing, but by systematically asking
what can break, contradict, or go wrong.

**Self-questions:**

1. What happens when two users act on the same entity simultaneously?
   — Example: Two admins editing the same order at the same time.
2. What happens when an external dependency fails? (third-party service down,
   notification not delivered, file upload interrupted)
3. What happens when a process is only partially completed? (payment taken
   but order not confirmed, import started but not finished)
4. Are there timing-dependent scenarios? (deadlines expiring, stale data being
   used, actions that are only valid within a window)
5. What happens when data reaches an unexpected state that no rule covers?
   — Example: An order that is both "cancelled" and "shipped" due to a
     race condition in the described workflow.
6. What happens when limits are reached? (maximum items, maximum attempts,
   storage full, quota exceeded)
7. For every rule the user stated, what happens when that rule is violated
   by the system itself (not just by the user)?

**If a gap is revealed:** Add to Category D. Classify the failure mode by
severity — is it a data integrity risk (BLOCKING), a user experience issue
(IMPORTANT), or an edge case (CLARIFYING)?

---

### D4 — Impact Chains

**When activated:** Phase 1.3 (before Gap Report), Phase 1.6 (before Completeness
Validation)

No capability exists in isolation. The analyst must trace each requirement
forward to discover what it implies downstream.

**Self-questions:**

1. If role A can do X, does someone need to be able to review, approve, or
   undo X?
2. If this data is created, who needs to see it? Who needs to edit it?
   Who needs to be prevented from seeing it?
3. If this rule exists, does it create a new status or state that other
   rules must account for?
4. If this capability is added, does it change the behavior of any previously
   described capability?
   — Example: Adding "order cancellation" changes what "order completion" means
     (completed orders might now include cancelled ones unless excluded).
5. Does this capability require supporting data or configuration that has not
   been mentioned?
   — Example: "Email notifications on order status change" implies email
     templates, notification preferences, and an email address stored somewhere.
6. Does this capability create audit or traceability requirements?
   (who did what, when, and from where)

**If a gap is revealed:** Map the chain: "Capability X implies Y, which
implies Z." Add the full chain to the gap report so the user sees the ripple
effect, not just the missing piece.

---

### D5 — Temporal Analysis

**When activated:** Phase 1.3 (before Gap Report), Phase 1.6 (before Completeness
Validation)

Systems evolve over time. The analyst must ask not just "what does the system
do?" but "what does the system do over time?"

**Self-questions:**

1. Does any entity have a lifecycle with distinct phases? (draft → active →
   archived → deleted) Are all transitions defined?
2. Are there seasonal, periodic, or scheduled variations in behavior?
   — Example: A product catalog that changes seasonally, or a reporting
     period that closes monthly.
3. Does data age? Are there retention rules, expiration dates, or archiving
   policies?
   — Example: "Users can see their order history" — for how long? All time?
   Last 12 months?
4. Will the system's usage grow? Does the user's description imply a current
   scale and a future scale that differ?
   — Note: This is about functional scale (more users, more data types, more
     roles), NOT technical performance.
5. Are there time-based rules? (deadlines, grace periods, cooldown windows,
   scheduling)
6. Does any entity transition through states based on time passing rather than
   user action?
   — Example: An invoice that auto-moves to "overdue" after 30 days.

**If a gap is revealed:** Add to Category D as a business rule gap. Frame it
as a lifecycle question: "What happens to X over time?"

---

### D6 — Boundary Precision

**When activated:** Phase 1.6 (before Completeness Validation), Phase 1.7 (before
PRD generation), Phase 2 (before each user story)

Vague requirements survive into production as bugs. The analyst must find the
exact line where a capability stops being applicable.

**Self-questions:**

1. For every capability, what is the EXACT condition under which it applies
   and the EXACT condition under which it does not?
   — Example: "Users can edit orders" — which users? which order statuses?
     which fields? until when?
2. For every rule, what is the precise threshold?
   — Not "small orders" but "orders under $50". Not "recent" but "within
     the last 30 days".
3. Where does one capability end and another begin?
   — Example: "Viewing" vs "editing" — is there a "preview" state in between?
4. For every "yes/no" statement in the requirements, is there a "it depends"
   scenario the user has not addressed?
5. What is the smallest unit of data or action the rule applies to?
   — Example: "Products can be disabled" — individual products? product
     variants? entire categories?

**If a gap is revealed:** Do NOT accept vague language in the PRD. Surface
the boundary question explicitly: "You said X. Where exactly does X stop
applying?"

---

### D7 — Cross-Consistency

**When activated:** Phase 1.6 (before Completeness Validation), Phase 1.7 (before
PRD generation), Phase 2 (before each user story)

Each capability sounds correct in isolation. The analyst must verify they are
still correct together.

**Self-questions:**

1. Do any two rules apply to the same entity and contradict each other?
   — Example: Rule A says "all orders over $1000 require approval."
     Rule B says "VIP customers can place orders without approval."
     What happens when a VIP customer places a $2000 order?
2. Are all status transitions accounted for? Can every state be reached from
   at least one other state? Can every state transition to at least one other
   state (or is it terminal by design)?
3. Do role permissions overlap in ways that create conflicts?
   — Example: Role A can "approve" and Role B can "reject" the same entity.
     What if both act simultaneously?
4. Are there orphaned capabilities — capabilities described for a role that
   has no other interaction with the system?
5. Does the data model implied by the rules form a consistent whole, or are
   there entities referenced but never defined?
   — Example: "Each task belongs to a project" but projects are never
     described as a concept with their own attributes.
6. Do the scope boundaries contradict any described capability?
   — Example: "No reporting in v1" but "managers can see team performance"
     is described as a capability.

**If a gap is revealed:** Surface as a contradiction if two rules conflict.
Surface as a gap if an entity or state is referenced but undefined.

---

### D8 — Stakeholder Bias Detection

**When activated:** Phase 1.1 (before Input Synthesis), Phase 1.4 (after receiving
answers)

Users describe what they know, weighted by what they care about most.
The analyst must detect the biases that distort the picture.

**Self-questions:**

1. Is the user describing the CURRENT state (what exists today) or the DESIRED
   state (what they want to build)? Are they mixing both?
2. Is the user anchoring on a tool they already use? ("Like Jira but simpler"
   — they may be describing Jira's features, not their actual needs.)
3. Is the user describing their IDEAL workflow or their ACTUAL workflow?
   People tend to describe how things should work, not how they really work.
4. Is the user over-weighting features they personally care about and
   under-weighting features needed by other roles?
   — Example: The admin user describes admin features in detail but barely
     mentions end-user capabilities.
5. Is the user avoiding complexity? ("We'll handle that manually" may mean
   "I haven't thought about it" or "it's genuinely simple".)
6. Is the user's vocabulary hiding assumptions? Industry jargon may carry
   meaning the analyst does not share. When in doubt, define every term.

**If a gap is revealed:** Add a clarifying gap. Frame it neutrally:
"Could you help me understand whether X refers to how things work today
or how you'd like them to work in the new system?"

---

### D9 — Negation Test

**When activated:** Phase 1.6 (before Completeness Validation), Phase 1.7 (before
PRD generation)

Every feature has a cost. The analyst must challenge whether each capability
truly earns its place.

**Self-questions:**

1. If we do NOT build this capability, what breaks? If nothing breaks, is it
   truly needed for day one?
2. Is this capability needed for the system to function, or is it a quality-
   of-life improvement that could be deferred?
3. Is the user asking for this because they genuinely need it, or because they
   expect it to exist? (expectation vs. need)
4. Would removing this capability change the core value proposition? If not,
   should it be in scope or deferred?
5. Is this capability a reaction to a pain point in the current system that
   may not exist in the new system?
   — Example: "We need a complex search because our current tool is slow."
     The new system may be simple enough that complex search is unnecessary.

**If a gap is revealed:** Do NOT remove capabilities from scope unilaterally.
Surface the question: "Capability X does not appear to be required for day-one
value. Should it be in scope for v1, or would you like to defer it?"

---

### D10 — Completeness by Perspective

**When activated:** Phase 1.6 (before Completeness Validation), Phase 1.7 (before
PRD generation)

The system must work for EVERY role, not just the loudest one. The analyst
must walk through the system from each role's point of view.

**Self-questions:**

1. For each identified role, list every capability assigned to them. Does each
   role have a complete workflow — a beginning, middle, and end to their
   interaction with the system?
2. For each role, can they accomplish their stated goal using ONLY the
   capabilities described? Or are there missing steps?
   — Example: "Users can submit requests" and "Admins can approve requests"
     but nothing describes what happens BETWEEN submission and approval.
3. For each role, what do they need to SEE? Is every data point they need
   to perform their actions available to them?
4. Is there a role that appears in the rules but has no direct capabilities?
   — Example: A "manager" who is referenced in approval rules but never
     described as someone who logs in and does things.
5. Are there handoff points between roles where something could fall through
   the cracks?
   — Example: Role A creates something, Role B acts on it. What if Role B
     is unavailable? Does Role A wait? Can someone else step in?
6. Does every role have a way to recover from errors or undo their own mistakes?

**If a gap is revealed:** Add the gap from the perspective of the affected
role: "Role X needs to be able to Y, but no capability currently covers this."

---

## Activation Points

The table below maps each dimension to its activation points and purpose.

| Checkpoint | Dimensions | What the analyst does internally |
|---|---|---|
| **Phase 1.1** — Before Input Synthesis | D1, D2, D8 | Scan for hidden stakeholders, implicit requirements, and stakeholder bias before reflecting understanding back to the user |
| **Phase 1.3** — Before Gap Report | D1, D2, D3, D4, D5 | Run the full early-detection sweep: who is missing, what is assumed, what can fail, what is downstream, what changes over time |
| **Phase 1.4** — After receiving answers | D8 | Re-examine answers for stakeholder bias — are the answers describing current state or desired state? |
| **Phase 1.6** — Before Completeness Validation | D3, D4, D5, D6, D7, D9, D10 | Run the full late-validation sweep: boundaries, consistency, failure modes, negation, and per-role completeness |
| **Phase 1.7** — Before PRD generation | D6, D7, D9, D10 | Final precision pass: boundaries are crisp, rules are consistent, every capability justifies its existence, every role is complete |
| **Phase 2** — Before each user story | D3, D6, D7 | For each user story: failure modes, boundary precision, and cross-consistency with other stories |

### How activation works

Activation does not mean "produce a report." It means the analyst silently
runs through each self-question for the active dimensions and checks whether
its current mental model has an answer. If it does, proceed. If it does not,
the missing answer becomes:

- A gap to add to the next gap report (Phase 1.3/1.4)
- A revision to make before generating the PRD (Phase 1.7)
- A revision to make before finalizing a user story (Phase 2)

The analyst never mentions the dimensions or self-questions in its output.
They are invisible scaffolding that shapes the quality of visible deliverables.

---

## The Unbeatable Analyst Checklist

Run this checklist before producing ANY deliverable — synthesis report, gap
report, PRD, or spec. Every item must pass. If any item fails, fix it before
proceeding.

This is inspired by aviation pre-flight checklists: short, non-negotiable,
every item must pass. No exceptions.

```
PRE-DELIVERABLE CHECKLIST
═══════════════════════════

□ NO HIDDEN ROLE — I have asked myself who is not in the room.
□ NO ASSUMED OBVIOUS — I have asked myself what the user thinks "goes without saying."
□ FAILURE PATHS — For every happy path, I have identified at least one unhappy path.
□ RIPPLE CHECKED — I have traced every capability to its downstream effects.
□ TIME CONSIDERED — I have asked what changes over time for every entity.
□ BOUNDARY SHARP — Every rule has a precise threshold, not a vague adjective.
□ NO CONTRADICTIONS — I have cross-checked every rule against every other rule.
□ BIAS DETECTED — I have asked whether the user is describing today or tomorrow.
□ NEGATION PASSED — Every capability in scope survives "what if we don't build it?"
□ EVERY ROLE COMPLETE — I have walked the system from each role's perspective.

═══════════════════════════
ALL PASS → PROCEED
ANY FAIL → FIX BEFORE PROCEEDING
```

---

## Integration with Existing Skills

This skill works alongside `workflow/analyst/functional-analysis`, not as a
replacement. They form a complementary pair:

| Skill | Direction | Purpose |
|---|---|---|
| `functional-analysis` | Analyst → User | Questions to ask the user to fill known gaps |
| `analyst-reasoning` | Analyst → Self | Questions to ask itself to discover unknown gaps |

The workflow is:

1. **analyst-reasoning** activates first at each checkpoint — the analyst
   examines its own understanding and identifies what it does NOT know.
2. **functional-analysis** shapes how to ask the user about those gaps —
   the right category, the right framing, the right level of detail.
3. **analyst-reasoning** validates the answers received — are they complete?
   Are they biased? Do they introduce new unknowns?

Together they create a loop:
**Self-question → User question → Answer → Self-question → Confirm or iterate.**

This skill also integrates with `workflow/analyst/report-formats` — every gap
or contradiction discovered through self-questioning is expressed using the
structured report formats defined in that skill.

---

## Absolute rules for self-questioning

- **NEVER** mention the dimensions or self-questions in user-facing output
- **NEVER** skip a dimension that is active at the current checkpoint
- **NEVER** use self-questioning as a substitute for asking the user — if a
  self-question reveals a gap, the USER must resolve it, not the analyst
- **NEVER** assume the analyst's hypothesis is correct — every hypothesis
  generated by self-questioning must be confirmed with the user before it
  enters the PRD
- **ALWAYS** run the Pre-Deliverable Checklist before emitting any report,
  PRD, or spec
- **ALWAYS** treat a failed checklist item as a blocking issue until resolved
