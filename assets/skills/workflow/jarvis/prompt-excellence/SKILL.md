---
name: prompt-excellence
description: >
  Prompt engineering techniques for the TemperAI orchestrator. Teaches JARVIS
  how to construct high-quality delegation prompts for sub-agents. Loaded
  alongside state-schema when JARVIS needs to write or validate delegation prompts.
  Covers prompt anatomy, context management, multi-turn patterns, error recovery,
  and domain language reformulation. Does NOT duplicate state-schema rules.
---

# Prompt Excellence — Delegation Prompt Engineering

## Purpose

This skill teaches JARVIS HOW to construct delegation prompts that work.
State-schema defines WHAT to include in a delegation prompt (format, rules,
prohibitions). This skill teaches the craft of building prompts that agents
actually understand and execute correctly.

A delegation prompt is not "everything the agent needs." It is "exactly what
the agent does not already have." These are different things.

---

## Prompt Anatomy

Every delegation prompt has four parts. Most prompts only need two.

### 1. Role-Context

What the agent needs to know about the situation.
Not the domain — the agent's own context window.

```
Context: The user wants to add order cancellation. A PRD exists at
.temper/prd.md. Current step is task breakdown.
```

**When needed**: Analyst, architect, or when the agent has no prior context.
**When to omit**: Implementation agents that have their task file.

### 2. Task

What the agent is being asked to do. Must be a specific action verb.

```
Task: Analyze the order cancellation requirements and produce a gap report.
```

```
Task: Implement task T001: Add Product to Inventory (US-001)
```

**When needed**: Always.
**Format**: Imperative mood. One task per prompt. No compound tasks.

### 3. Format

How the agent should deliver the output.
Often implicit — state-schema defines standard formats per agent type.

```
Format: Present the gap report exactly as received from the analyst.
```

**When needed**: Non-standard formats, unusual delivery expectations,
or when the agent's standard format is insufficient.
**When to omit**: Standard agent tasks (agents know their own formats).

### 4. Constraints

Boundaries on the output or behavior. Negative framing only.

```
Constraints: Do not propose any technical architecture. Return only gaps.
```

**When needed**: When there are explicit boundaries the agent must respect.
**When to omit**: When the agent's skill set already enforces the boundary.

### The Two-Part Default

For most implementation agents, only two parts are needed:

```
Task: Implement task T001: Add Product to Inventory (US-001)
```

The task file contains all context the agent needs. Adding anything else
is noise.

For analyst and architect, four parts are usually needed:

```
Context: User wants to build an order management system for a small
warehouse. No PRD exists yet.
Task: Conduct requirements elicitation and produce a PRD.
Format: Follow the PRD template in workflow/analyst/prd-template.
Constraints: Do not infer technical stack or architecture.
```

---

## The Minimal Delegation Principle

### The Rule

Give the agent only what it cannot derive from its own inputs.
Everything else is noise.

### What Implementation Agents Already Have

Every implementation agent (backend, frontend, tester, devops):
- Reads its task file directly
- Knows which user story it belongs to
- Loads its own skills based on its agent definition
- Has access to project context through its skills

When you send:
```
Implement task T001: Add Product to Inventory (US-001)
```

The agent derives from that single line:
- What to implement (from task file)
- Which user story (from task file)
- Which skills to load (from its own agent definition)
- Where to put the code (from architecture skill)
- What conventions to follow (from dotnet-csharp, etc.)

You do not need to tell it any of this.

### What Implementation Agents Do NOT Have

- The specific task ID and title from the plan
- Confirmation that this is the next step to execute

That is all. Everything else is redundant.

### The Cost of Extra Information

Adding context to an implementation prompt does not help. It:

1. **Consumes context window** — the agent processes your extra text
2. **Creates ambiguity** — "did they want me to do something different?"
3. **Introduces inconsistency** — your context may contradict the task file
4. **Slows the agent** — more tokens to read and discard

### When Minimal Is Not Enough

Minimal delegation is insufficient when:
- The agent has no task file (bugfix, direct request)
- The user explicitly specified files or context
- The agent is in a multi-turn loop and needs prior output

For bugfixes (no task file):
```
Fix bug: Order total calculates incorrectly when discount applies
Affected area: Order total calculation
Expected behavior: Discounted orders show correct total after tax
```

For analyst/architect: always include user's request + available context.

---

## Context Window Management

Different agents have different context needs. Matching correctly prevents
both context overload and context starvation.

### Agent Context Requirements

| Agent | Context to include | Context to omit |
|---|---|---|
| `temper-backend` | Task reference only | Domain summary, file paths, skill names, class names |
| `temper-frontend` | Task reference only | Tech stack details, file paths, component names |
| `temper-tester` | Task reference + test scope (if user specified) | Implementation details, internal APIs |
| `temper-devops` | Task reference only | Architecture, language-specific details |
| `temper-analyst` | User's request + any existing PRD or gap report | Technical stack, architecture, file paths |
| `temper-architect` | User's request + PRD (if exists) + specs (if exist) | Implementation hints, class names, layer names |
| `temper-tasks` | PRD + specs (if analyst preceded) | Technical details, code structure |
| `temper-plan` | Tasks INDEX + domain model | Implementation specifics |
| `temper-review` | What was built + what to validate against | How it was built |

### Context Overflow Signals

The agent's output will signal context overflow:
- Ignoring a specific instruction you gave
- Asking about something you already explained
- Producing output that contradicts the task file
- Inconsistent with other agent outputs from the same session

If you see these patterns: reduce context next time. You gave too much.

### Context Starvation Signals

The agent will ask clarifying questions about obvious things:
- "Which folder should I put this in?"
- "Should I create a new file or edit an existing one?"
- "What naming convention should I use?"

If you see these: you omitted necessary context. Add the minimum required.

---

## Multi-Turn Prompt Patterns

Agent loops (analyst, architect) require structured prompts across turns.
The pattern differs from single-turn delegation.

### Analyst Gap Resolution Loop

The loop is: gap report → user answers → pass to analyst → repeat until no blocking gaps.

**Prompt to analyst (first turn):**
```
Context: User wants to build an inventory management system for a warehouse.
No PRD exists. This is the first elicitation cycle.
Task: Conduct initial requirements elicitation. Identify all gaps in the
user's description. Produce a gap report organized by category.
Format: Follow gap report format in workflow/analyst/report-formats.
```

**Prompt to analyst (subsequent turns — pass answers back exactly as received):**
```
Context: User provided answers to the previous gap report.
Task: Review the answers. Update the gap report. Resolve any gaps that are
now answered. Flag any new gaps created by the answers.
Format: Present resolution status report followed by updated gap report if
blocking gaps remain.
```

**Key principle**: Pass answers back exactly as the user provided them.
Do not reformat, summarize, or filter. The analyst's skill is in interpreting
raw user input.

### Architect Proposal Loop

**Prompt to architect (first turn):**
```
Context: User described a system with these requirements: [summary from PRD].
Existing PRD: .temper/prd.md
Task: Analyze the requirements. Propose a technical architecture that supports
the domain. Produce an architectural proposal.
Format: Follow proposal format in workflow/architect/proposal-formats.
```

**Prompt to architect (if changes requested):**
```
Context: User requested changes to the proposal: [user's feedback, exact].
Task: Incorporate the requested changes. Produce an updated proposal.
Format: Follow proposal format in workflow/architect/proposal-formats.
```

**Key principle**: Present the user's feedback exactly as received. Do not
interpret "they wanted X" — pass the raw text. The architect's job is to
interpret and implement.

### Turn Transition Rules

Before moving to the next turn:

1. **Verify** the agent produced the expected output type
2. **Present** the output exactly as received (no reformatting)
3. **Wait** for the user's response before proceeding
4. **Pass** the user's response back exactly (no summarization)

If the user says "I don't understand the output":
- Do not explain the output yourself
- Ask the agent to rephrase in different terms
- Re-delegate with the user's request for clarification

---

## Error Recovery Prompts

When an agent fails or returns unclear output, re-delegate with explicit
error context. The goal: tell the agent what exists and what to do next.

### Required Elements

Every error recovery prompt must contain:

1. **What the agent was trying to do** — task reference
2. **What exactly failed** — the error message or the unclear output
3. **What exists** — files created, partial outputs, state before failure
4. **What to do next** — continue from where it stopped, not restart from scratch

### Template

```
Task: [original task]
Error: [exact error or description of unclear output]
Existing work: [files created, partial outputs, state]
Instruction: Continue from where the previous attempt stopped. Do not
regenerate what already exists. Complete the remaining scope.
```

### Examples

**Example 1 — Backend agent fails mid-implementation:**

Prompt sent:
```
Task: Implement task T003: Add Order Status Validation (US-002)
Error: Agent ran out of context before completing the Handle method in
CancelOrderHandler.
Existing work: OrderStatus.cs enum updated, CancelOrderValidator created,
ICancelOrderHandler interface created.
Instruction: Complete the Handle method in CancelOrderHandler. The interface
and validator already exist. Add only the Handle implementation.
```

**Example 2 — Analyst returns incomplete gap report:**

Prompt sent:
```
Task: Produce the complete gap report for inventory management requirements.
Error: Gap report only covered Category A gaps (actors and purpose). Category
B (functional capabilities) and Category C (scope boundaries) are missing.
Existing work: Input synthesis complete. Category A gaps documented.
Instruction: Continue from Category B. Produce Category B and C gaps.
Do not regenerate Category A gaps.
```

**Example 3 — Architect proposal is ambiguous:**

Prompt sent:
```
Task: Produce updated architectural proposal incorporating user's feedback.
Error: Proposal states "use an event-driven approach" but does not specify
which events, which handlers, or how events propagate between bounded contexts.
Existing work: Initial proposal produced. User feedback: "clarify how events
flow between inventory and orders bounded contexts."
Instruction: Revise the proposal. Add a specific event list (event name,
source bounded context, target bounded context, payload) and describe the
handler responsibilities for each event.
```

---

## Ambiguity Escalation

Before sending any prompt, check whether the request contains ambiguity
that the agent cannot resolve. Ambiguity escalated to the user costs less
than ambiguity resolved wrong.

### Escalate to User When

- **Domain concept is unclear**: "What does 'product' mean here? A single
  item or a product family?"
- **Scope boundary is fuzzy**: "Should order cancellation be possible
  after the order is partially fulfilled, or only before any fulfillment?"
- **Priority or preference is unknown**: "The user mentioned both speed and
  accuracy as goals. Which takes priority if they conflict?"
- **Missing actor**: "You mentioned admin can manage products. Who can
  view them? Everyone or only admins?"

### Resolve Internally When

- **Ambiguity is about format or presentation**: Agent knows standard formats
- **Ambiguity is about convention**: Skills enforce conventions
- **Ambiguity is about priority in execution**: Infer and document as assumption
- **Ambiguity is about scope in a known domain**: Apply domain knowledge

### Escalation Prompt Template

When you must ask the user:

```
I need clarification on: [specific ambiguous point]

Options as I understand them:
A. [interpretation A]
B. [interpretation B]

My assumption if you don't answer: [default interpretation]

Does this match your intent?
```

**Rule**: Never escalate and then assume. Either wait for the answer or
explicitly state your assumption and document it in the state file.

---

## Domain Language Reformulation

Vague prompts produce vague outputs. Reformulating in domain language
before delegation is the single highest-leverage thing you do.

### The Reformulation Process

1. Take the user's raw request or your mental model
2. Ask: "What does this mean in the language of the domain?"
3. Ask: "What are the domain entities, states, and rules?"
4. Ask: "What would a non-technical domain expert recognize as correct?"

### Before / After Examples

**Example 1 — Vague:**
"Implement the product catalog feature"

**Reformulated:**
"The inventory consists of products. Each product has a name, a unit of
measure (each, box, kg), a current stock level, and a reorder threshold.
When stock falls below threshold, a reorder alert is triggered. Products
can be active or inactive."

**Example 2 — Vague:**
"Add order status tracking"

**Reformulated:**
"An order transitions through states: Draft → Confirmed → In Preparation →
Shipped → Delivered. An order can be cancelled only while in Draft or
Confirmed state. Once In Preparation begins, cancellation is no longer
allowed. The confirmed order total is final — no price changes after
confirmation."

**Example 3 — Vague:**
"User permissions system"

**Reformulated:**
"There are three roles: Viewer, Operator, and Admin. Viewers can see all
orders and products but cannot modify anything. Operators can create and
edit orders and adjust stock levels. Admins can do everything including
deleting products and managing other users' permissions. Permissions are
per-role, not per-user — every user with the Operator role has the same
capabilities."

**Example 4 — Vague:**
"Error handling for the form"

**Reformulated:**
"When a user submits an order with invalid data, the system returns the
specific field that failed validation and the reason. Fields validated
before submission: customer reference (required), shipping address (required),
at least one order line (required), quantity per line (must be positive
integer). The form does not submit if any field fails. No error state
persists after the user corrects the input."

**Example 5 — Vague:**
"Customer dashboard"

**Reformulated:**
"Each customer has a dashboard showing their orders. The dashboard displays:
orders in Confirmed or later state (active orders), and orders in
Delivered state within the last 90 days (recent orders). Cancelled orders
are not shown by default. Each order row shows: order reference, current
status, item count, and total amount. Clicking an order opens the order
detail view."

---

## Pre-Delegation Checklist

Before sending any prompt to a sub-agent, verify:

- [ ] Task is a single, specific action (not compound)
- [ ] Implementation agents: prompt contains ONLY task reference
- [ ] Analyst/Architect: prompt includes context + task + format + constraints
- [ ] Bugfix: prompt includes domain description + affected area + expected behavior
- [ ] No file paths, skill names, class names, or layer descriptions
- [ ] Domain language used throughout (no implementation terms)
- [ ] Format specified if non-standard
- [ ] Constraints specified if any boundary exists

If any check fails: fix before delegating. The cost of a bad prompt is
higher than the cost of taking 30 extra seconds to review it.

---

## Quality Audit Question

Before delegating, ask:

*"If this prompt falls in the hands of the best agent in the world,
does it have exactly what it needs to deliver exactly what I asked for,
or does it have to infer something I did not say?"*

If inference is required → rewrite the prompt.
If inference is not required → delegate.
