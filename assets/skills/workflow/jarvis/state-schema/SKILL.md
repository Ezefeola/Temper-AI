---
name: jarvis-state-schema
description: >
  State file JSON schema, status values, and delegation rules for the temper-jarvis
  orchestrator. Load this skill whenever jarvis needs to read, write, or validate
  .temper/jarvis-state.json, or when constructing delegation prompts for sub-agents.
---

# JARVIS State Schema & Delegation Rules

## State file JSON schema

`.temper/jarvis-state.json` is the orchestrator's only persistent memory.

```json
{
  "last_updated": "ISO timestamp",
  "status": "in-progress | awaiting-approval | awaiting-task-approval | awaiting-agent-cycle | complete | blocked",
  "request_summary": "one line description",
  "context": {
    "project": "project name or description",
    "architecture": "Clean Architecture | Vertical Slice | etc.",
    "stack": "EF Core, Blazor, etc.",
    "notes": "any other relevant context"
  },
  "approved_plan": [
    {
      "step": 1,
      "agent": "temper-analyst",
      "description": "one line description",
      "status": "complete | pending | in-cycle",
      "output": "file path or null"
    }
  ],
  "current_step": 2,
  "total_steps": 4,
  "current_agent": "temper-backend",
  "current_task": "T001",
  "task_title": "one line description",
  "total_tasks": 6,
  "completed_tasks": [
    { "task_id": "T002", "agent": "temper-backend", "title": "description", "status": "complete" }
  ],
  "pending_tasks": [
    { "task_id": "T001", "agent": "temper-backend", "title": "description" }
  ],
  "active_cycle": {
    "agent": "temper-analyst",
    "cycle_type": "gap-resolution | proposal-confirmation",
    "unresolved_blocking_gaps": 3,
    "cycle_count": 1
  },
  "block_reason": null,
  "next_action": "what the next session should do"
}
```

---

## Status values

| Status | Meaning |
|---|---|
| `in-progress` | Actively working on a step |
| `awaiting-approval` | Plan proposed, waiting for user to approve |
| `awaiting-task-approval` | Agent completed, waiting for user to confirm output |
| `awaiting-agent-cycle` | Sub-agent is in a multi-turn loop (analyst, architect), waiting for next input |
| `complete` | All steps done |
| `blocked` | Cannot proceed, needs user intervention |

---

## Delegation rules — domain language only

**You never tell an agent HOW to build something. You only tell them WHAT to build.**

For prompt engineering techniques — how to construct the actual delegation
prompt, context window management, multi-turn patterns, error recovery, and
domain language reformulation — refer to `workflow/jarvis/prompt-excellence`.

For the delegation format rules and prohibitions below, those are the
absolute constraints that apply to every prompt regardless of technique.

### ABSOLUTE PROHIBITIONS — never include in a delegation prompt

If you violate any of these, you have failed as orchestrator:

- **NEVER** mention file paths: `.temper/`, `.md` files, `.cs` files, folder locations
- **NEVER** mention skill names: "dotnet-csharp", "ef-core", "ddd", etc.
- **NEVER** describe domain, summarize tasks, or copy acceptance criteria
- **NEVER** mention class names, DTO names, interface names, method names
- **NEVER** say "Read...", "Load...", "Check...", or "See file..."
- **NEVER** describe layers: "Domain layer", "Application layer", "Infrastructure..."

### Correct delegation format

The ONLY thing you send to an implementation agent is:

```
Implement task T001: Add Product to Inventory (US-001)
```

That is literally all. No punctuation, no extra text, no context.

If you catch yourself typing anything after `Implement task [ID]: [title]` — DELETE IT.

### Domain language comparison

| Correct — what to build | Wrong — how to build it |
|---|---|
| "The Order entity has a status: Pending, Confirmed, Cancelled" | "Create an `OrderStatus` enum in `Domain/Enums/`" |
| "An order belongs to one customer and can have multiple items" | "Add a `CustomerId` FK and `OrderItems` navigation property" |
| "The endpoint returns a paginated list of orders filtered by status" | "Create a `GetOrdersQuery` with a `Handle` method returning `PagedResult<OrderDto>`" |
| "An order cannot be cancelled if already shipped" | "Throw `DomainException` in `Cancel()` if `Status == Shipped`" |

### Pre-delegation checklist

Before sending ANY prompt to a sub-agent, verify ALL of these:

- [ ] The prompt contains ONLY: `Implement task [T###]: [title]`
- [ ] No file paths are mentioned (no `.temper/`, no `.md`, no `.cs`)
- [ ] No skill names or load instructions
- [ ] No domain summary, acceptance criteria, or layer descriptions
- [ ] No class names, DTO names, or interface names

For the full quality checklist including prompt anatomy, context management,
and reformulation examples, refer to `workflow/jarvis/prompt-excellence`.

If any check fails → STOP and rewrite to the minimal form:

```
Implement task [T###]: [task title]
```

**For analyst/architect only:**

- [ ] Passing user's request and/or context files as appropriate
- [ ] Speaking in domain language, not implementation language

Implementation agents read their own files. You never tell them what to read unless the user explicitly specifies.
