---
name: prompt-excellence
description: >
  Universal prompt engineering techniques for the TemperAI orchestrator.
  Teaches JARVIS how to construct high-quality delegation prompts regardless
  of target agent. Load when JARVIS needs prompt-writing craft, context
  control, error recovery, or domain-language reformulation.
---

# Prompt Excellence — Delegation Prompt Engineering

## Purpose

This skill teaches JARVIS HOW to construct delegation prompts that work.
State-schema defines the hard rules and prohibitions. Agent-specific workflow
skills define specialist communication contracts. This skill stays universal:
it teaches the craft of building prompts that agents actually understand and
execute correctly.

State-schema remains authoritative. If an example in this skill would require a
file path, skill reference, or internal implementation instruction, do not use
that example. Rewrite it in plain domain language.

When a target agent has its own dedicated workflow contract, that contract owns
the exact handoff format. This skill stays universal and does not replace those
agent-specific rules.

A delegation prompt is not "everything the agent needs." It is "exactly what
the agent does not already have." These are different things.

---

## Prompt Anatomy

Every delegation prompt has four parts. Most prompts only need two.

### 1. Role-Context

What the agent needs to know about the situation.
Not the domain — the agent's own context window.

```
Context: The user wants to add order cancellation. An approved PRD already
exists, and the current step is task breakdown.
```

**When needed**: When the agent lacks enough context in its own inputs.
**When to omit**: Task-file driven work where the agent already has the context.

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

### The Minimal Default

Some target agents work best from an intentionally tiny prompt. When a
dedicated workflow contract says the handoff should be one line, follow that
contract exactly instead of expanding the prompt here.

For conversational or discovery-oriented delegations, four parts are usually needed:

```
Context: User wants to define a new warehouse workflow. No prior structured
artifact exists for this request.
Task: Analyze the request and identify what information is still needed.
Format: Produce your standard structured analysis output.
Constraints: Stay within the current problem space.
```

---

## The Minimal Delegation Principle

### The Rule

Give the agent only what it cannot derive from its own inputs.
Everything else is noise.

### What Specialized Agents Already Have

Some specialized agents derive most of their context from their own inputs,
skills, or persisted artifacts. In those cases, JARVIS should pass only the
smallest task statement that the target workflow contract requires.

Do not restate information the target agent can already derive reliably.

### The Cost of Extra Information

Adding context to a tightly scoped execution prompt does not help. It:

1. **Consumes context window** — the agent processes your extra text
2. **Creates ambiguity** — "did they want me to do something different?"
3. **Introduces inconsistency** — your context may contradict the task file
4. **Slows the agent** — more tokens to read and discard

### When Minimal Is Not Enough

Minimal delegation is insufficient when:
- the target workflow contract requires more than a task reference
- the agent is in a multi-turn loop and needs prior output
- the handoff is a bugfix, recovery turn, or clarification turn

For the exact implementation-agent rules in those cases, load
`workflow/jarvis/implementation-delegation`.

For conversation-loop agents, include the user's request plus only the minimum
plain-language context needed for the current turn.

---

## Context Window Management

Different delegation modes have different context needs. Matching correctly
prevents both context overload and context starvation.

### Delegation Context Modes

| Mode | Context to include | Context to omit |
|---|---|---|
| Task-file execution | Only the minimal task reference required by that workflow contract | Restated domain summary, file paths, skill names, class names |
| Conversation or discovery | User request + minimum plain-language context for the current turn | File paths, internal artifact references, implementation hints |
| Validation or review | What exists + what to validate against | How it was built unless explicitly needed |

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

Some delegations are loops rather than one-off executions. The specialist loop
contracts live in dedicated workflow skills. The universal pattern is:

1. Preserve the specialist's meaning exactly.
2. Surface the specialist output without rewriting it into a different artifact.
3. Reduce the next user interaction to the smallest actionable prompt only when that reduction is reliable.
4. Pass the user's next reply back exactly as received.

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

**Example 2 — Specialist returns incomplete structured output:**

Prompt sent:
```
Task: Produce the complete structured analysis for inventory management requirements.
Error: Output only covered actors and purpose. Functional capabilities and scope boundaries are missing.
Existing work: Initial synthesis complete. Actors and purpose already documented.
Instruction: Continue from the missing sections. Do not regenerate the completed section.
```

**Example 3 — Proposal output is ambiguous:**

Prompt sent:
```
Task: Produce an updated proposal incorporating the user's feedback.
Error: Proposal states "use an event-driven approach" but does not explain the event flow concretely enough.
Existing work: Initial proposal produced. User feedback: "clarify how events flow between inventory and orders bounded contexts."
Instruction: Revise the proposal. Add a specific event list and describe the responsibilities for each event.
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
- [ ] If the target agent has a dedicated workflow contract, the prompt matches that contract exactly
- [ ] Conversation/discovery prompts: include context + task + format + constraints when needed
- [ ] Bugfix and recovery turns follow the dedicated execution-agent contract when applicable
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
