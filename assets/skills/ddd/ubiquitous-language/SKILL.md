---
name: ddd-ubiquitous-language
description: >
  Ubiquitous Language concepts for TemperAI. Teaches implementation agents
  how to understand, extract, and use domain terminology consistently when
  reading specs, tasks, and DDD documentation.
  Load this skill when implementing any backend or frontend task.
requires: []
produces: []
---

# Ubiquitous Language — TemperAI Standards

This skill teaches the concept of Ubiquitous Language and how to apply it
when reading domain documentation, specs, and tasks.

## What is Ubiquitous Language?

Ubiquitous Language is the shared vocabulary of the domain. It is the terms,
definitions, and relationships that everyone on the team — developers,
domain experts, analysts, testers — use to communicate about the system.

The goal is simple: **every term means exactly one thing, and everyone uses
the same word for the same concept.**

If the analyst says "Order" and the developer codes "PurchaseOrder" without
a mapping, confusion follows. Ubiquitous Language eliminates that gap.

---

## Why This Matters for Implementation Agents

You receive work in the form of:
- User stories and specs (from temper-spec)
- Tasks with business rules (from temper-tasks)
- DDD documentation (from temper-architect, if generated)

These documents use domain terminology. If you don't understand the terms
correctly, you implement the wrong thing.

**Example of the problem:**

```
Spec says: "A customer cannot order items that are out of stock"

You read: "customer" → assume it's the entity named Customer
          "items" → assume it's Product entities
          "out of stock" → assume it's a boolean field on Product

You code: Customer.HasItemsInStock(PurchaseOrder) → WRONG
```

**What actually meant:**

```
Spec says: "A customer cannot place an Order when the requested Quantity
           of each OrderItem exceeds the available Inventory quantity
           for that Product in the Warehouse."

Correct interpretation:
  - customer → Person entity, role in this process
  - order items → OrderItem aggregate (not "items")
  - out of stock → Inventory.WarehouseStockLevel below requested Quantity
  - cannot → a domain rule enforced by the Order aggregate
```

The gap came from not understanding the domain vocabulary correctly.

---

## Core Principles

### 1. One term, one meaning

A term always means the same thing, regardless of context.

```
✅ "Order" always means the Order aggregate
❌ "Order" sometimes means Order aggregate, sometimes means PurchaseOrder
```

If a term has multiple meanings in different bounded contexts, note the
difference — but never use the same word for two different meanings within
the same context.

### 2. Use domain terms, not technical terms

When reading specs and tasks, translate technical shorthand into domain terms.

| Technical shorthand | Domain meaning |
|---|---|
| "CRUD for products" | "Product management" |
| "button to cancel order" | "CancelOrder action" |
| "update the status" | "Transition OrderStatus" |
| "delete the record" | "Remove Entity" |

### 3. Verify terms against available documentation

If `DDD-Vocabulary.md` exists, use it as the authoritative source for term definitions.

If it does not exist, extract term meanings from:
- Specs (Section 4 — Functional Scope uses consistent terms)
- Task business rules (explicit terms are intentional)
- Design document entity definitions

### 4. When a term is ambiguous, stop and verify

Never assume a term's meaning. If a task says "the item" and you don't know
which entity "item" refers to, ask or flag it.

Signs of ambiguity:
- A noun that could refer to multiple entities
- A term used differently in adjacent tasks
- A spec that uses two different terms for what seems like the same concept

---

## How to Extract Terms from Specs

### Step 1: Identify the nouns

In specs and tasks, nouns are typically domain terms.

```
"As a warehouse manager, I want to confirm shipment of an Order
 so that inventory levels are updated correctly."
```

Nouns: warehouse manager, shipment, Order, inventory levels

### Step 2: Classify each noun

| Type | How to recognize | Example |
|---|---|---|
| Entity | Has identity and lifecycle | Order, Product, Customer |
| Aggregate | Entity that owns other entities | Order (owns OrderItem) |
| Value Object | Defined by attributes, no identity | Money, Address |
| Enum | Fixed set of values | OrderStatus, ProductCategory |
| Service | An action or operation | InventoryService |

### Step 3: Check relationships

If spec says "Order has items", the term is `OrderItem` (child entity).

```
✅ "order items" → OrderItem (or OrderLine)
❌ "order items" → Products
```

### Step 4: Validate against vocabulary

If DDD-Vocabulary.md exists:
- Every term in the spec should appear in the vocabulary
- Every term in the task should match the vocabulary's definition

---

## How to Read Tasks with Domain Terms

Tasks embed business rules using domain terms. Your job is to:

### 1. Map terms before implementing

```
Task: "CancelOrder — an order cannot be cancelled if it has been shipped"

Before you code:
  - "order" → Order aggregate (entity or AR?)
  - "cancelled" → OrderStatus.Cancelled?
  - "shipped" → OrderStatus.Shipped?
  - "cannot be cancelled" → invariant enforced by Order aggregate
```

### 2. Identify what entity owns the rule

Business rules belong to the entity that enforces them.

```
"an order cannot be cancelled if it has been shipped"

→ This rule belongs to the Order aggregate
→ The Order aggregate enforces this invariant
→ The implementation goes in the Order entity (or its AR)

NOT in:
  → A service
  → A use case handler
  → The controller
```

### 3. Look for status transitions

When you see "cannot X if Y", the entity has a status field.

```
"Cannot cancel a shipped order" → Order has OrderStatus with states
                                   (Pending, Confirmed, Shipped, Delivered)
```

The rule is enforced during the Cancel transition.

---

## Common Domain Term Pitfalls

### Using synonyms

```
Spec uses "product" → you code with "article" or "item"
```

Problem: Different words for the same thing cause confusion.
Fix: Use the term from the spec. If it feels wrong, flag it — but use it.

### Technical translation

```
Spec says "the user presses submit" → you code "OnClick handler"
```

Problem: Technical terms hide domain meaning.
Fix: "User submits order for processing" → the domain action is "SubmitOrder",
not the UI interaction.

### Assuming relationships

```
Spec mentions "customer" → you assume it links to User entity
```

Problem: Without verifying, you can connect to the wrong entity.
Fix: Check DDD-Vocabulary.md or design.md for relationships.

### Ignoring bounded contexts

```
The term "Account" appears in two different contexts with different meanings
```

Problem: A term that means one thing in one context can mean something
different in another context.
Fix: When reading tasks, note which bounded context the task belongs to.
If the term appears in multiple contexts, verify which meaning applies.

---

## Working with DDD-Vocabulary.md

If `DDD-Vocabulary.md` exists:

### What it contains

```markdown
| Term        | Type      | Definition                              |
|---|---|---|
| Order       | Aggregate | A customer's request for products...    |
| OrderItem   | Entity    | A line in an order representing...      |
| OrderStatus | Enum      | The lifecycle state of an Order         |
```

### How to use it

1. **Read it at the start of the project** to understand all domain terms
2. **Before implementing, check** if the term you're working with appears in it
3. **When you encounter an ambiguous term**, the vocabulary resolves it

### When it does not match the task

If DDD-Vocabulary says one thing but the task uses a different term:

```
Vocabulary: "Product — an item sold by the company"
Task: "article cannot be discontinued if it has pending orders"
```

The task uses "article" but the vocabulary uses "Product".
This is intentional — the task agent translated. But you should code
using "Product" (the vocabulary term), not "Article".

---

## When DDD-Vocabulary Does Not Exist

If the user did not generate DDD docs, you rely on:

1. **Specs** — consistent terminology within a user story
2. **Tasks** — explicit business rules with domain terms
3. **Design** — entity definitions and relationships (if available)

Rules:
- Assume the first term used is correct
- Maintain consistency within the task
- If you see the same concept called two different things, flag it
- When in doubt, stop and ask — do not guess domain meaning

---

## Quick Reference

### Term types

| Type | Implemented as | Example |
|---|---|---|
| Entity | `sealed class EntityName` | `Product`, `Order` |
| Aggregate | `sealed class AggregateName : Entity<TId>` | `Order` (root), `Product` (root) |
| Child Entity | `sealed class ChildName : Entity<TId>` | `OrderItem` inside Order aggregate |
| Value Object | `sealed record ValueName` | `Money`, `Address` |
| Enum | `public enum EnumName` | `OrderStatus`, `ProductCategory` |
| Service | Interface + implementation | `IInventoryService` |

### Key indicators

| Phrase in spec | Domain meaning |
|---|---|
| "cannot be X if Y" | Invariant — belongs to entity |
| "must be" | Validation rule — entity or VO |
| "when state is A" | Status transition — entity with state |
| "update inventory" | Service operation |
| "calculate total" | Calculation rule |

### Golden rule

> When you read a spec or task, translate it word by word into domain concepts
> before you think about implementation. The code will follow naturally.

---

## Absolute Rules

- **NEVER assume a term's meaning without verifying**
- **NEVER use synonyms** — use the exact term from the spec or vocabulary
- **NEVER code a business rule in a service** when it belongs to an entity
- **ALWAYS check DDD-Vocabulary.md first** if it exists
- **ALWAYS identify what entity owns each business rule**
- **ALWAYS note bounded context** when a term appears in multiple contexts
- **ALWAYS flag ambiguous terms** — do not guess, ask