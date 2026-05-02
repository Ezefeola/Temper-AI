---
name: prd-analyzer
description: >
  Product Requirements Document analysis skill for TemperAI. Use when reading,
  analyzing, or building PRDs for new projects. Teaches the agent how to extract
  domain entities, choose architecture, identify tech stack, and generate
  config files from a PRD. Also covers how to ask efficient questions
  without wasting tokens.
---

# PRD Analyzer — TemperAI Standards

## What a good PRD contains

- **Problem statement** — clear description of the problem being solved.
- **Target users** — who will use this system.
- **Core features** — the 3-5 most important functionalities.
- **Out of scope** — what this version explicitly does NOT include.
- **Business rules** — any domain-specific rules or constraints.
- **External integrations** — third-party APIs, services, or systems.
- **Non-functional requirements** — performance, security, scalability needs.

---

## How to read and understand a PRD

### Step 1 — Read the entire PRD first

Do not start asking questions or generating files until you have read the complete PRD. Build a mental model of the entire system before analyzing details.

### Step 2 — Identify the nouns

Nouns in the PRD are your **domain entities**. Look for:

- Things the system manages — products, orders, users, invoices.
- Things that have properties — a product has a name, price, status.
- Things that have relationships — an order has many order items, belongs to a customer.
- Things that have a lifecycle — an order goes from draft to confirmed to shipped.

### Step 3 — Identify the verbs

Verbs in the PRD are your **use cases** and **API endpoints**. Look for:

- Actions users perform — create, update, delete, approve, cancel.
- System actions — send email, generate report, calculate total.
- Triggers — when an order is placed, when payment is confirmed.

### Step 4 — Identify the rules

Rules in the PRD are your **business logic** and **validation**. Look for:

- Constraints — a product name cannot be empty, price must be positive.
- Conditions — an order can only be cancelled before shipping.
- Calculations — total = sum of items * tax rate.
- Workflows — order must be approved by manager before processing.

### Step 5 — Map entities and relationships

Create a mental (or written) entity relationship map:

```
Customer (1) ──< Order (M) >── OrderItem (M) >── Product (1)
```

This map becomes the foundation for the domain design.

---

## Questions to ask when the PRD is ambiguous or incomplete

### Token-efficient questioning strategy

**Do NOT ask one question at a time.** Group questions by category and ask them all at once. This minimizes back-and-forth and saves tokens.

**Do NOT ask questions that can be answered by making a reasonable default.** If the PRD does not specify a database, suggest one and ask for confirmation instead of asking "what database do you want?"

**Do NOT ask questions about things that are out of scope.** If the PRD says "no authentication for now," do not ask about auth.

### Group 1 — Blocking questions (must answer before proceeding)

These are questions that prevent you from generating the constitution:

1. What is the primary purpose of the system? (if not clear from PRD)
2. Who are the main users? (if not specified)
3. What are the core entities the system manages? (if ambiguous)
4. Are there any complex business rules, or is it mostly CRUD? (determines architecture)

### Group 2 — Architecture questions

1. Do you have a preference for architecture pattern? If not, I will recommend based on complexity.
2. Will this system need to scale to multiple input channels in the future (API, CLI, message queue)?
3. Is this a long-lived enterprise system or a short-term project?

### Group 3 — Technology questions

1. What database do you prefer? If unsure, I will recommend based on project size.
2. Do you need user authentication? If so, what type?
3. Do you need a frontend, or is API-only sufficient for now?
4. Are there any external service integrations required?

### Group 4 — Standards questions

1. Are there any team-specific coding conventions I should know about?
2. Do you want to follow TemperAI standards for C# / .NET 10? (recommended)

---

## How to identify the right technology stack

### Backend

- **Default:** .NET 10, C# 14 — this is the TemperAI standard. Only suggest alternatives if the PRD explicitly requires something else.

### Database

| PRD signals | Recommendation |
|---|---|
| Simple CRUD, small data, single developer | SQLite |
| Relational data, medium complexity | PostgreSQL |
| Enterprise, existing Microsoft ecosystem | SQL Server |
| Document-based, flexible schema | MongoDB |
| High read throughput, caching layer | Redis (alongside primary DB) |

### Frontend

| PRD signals | Recommendation |
|---|---|
| Any frontend needed | Blazor WebAssembly (default) |
| API only, consumed by mobile/other clients | No frontend |
| SEO-critical public site | Not Blazor — suggest SSR alternative |

### Authentication

| PRD signals | Recommendation |
|---|---|
| Internal tool, no external users | No auth for now |
| Simple user login | JWT with ASP.NET Core Identity |
| Social login required | OAuth2 / OpenID Connect |
| Enterprise SSO | SAML / OIDC with external provider |

### Messaging / Events

| PRD signals | Recommendation |
|---|---|
| Real-time notifications, decoupled processing | RabbitMQ or MassTransit |
| Simple event logging | In-memory event publisher for now |
| No async processing needed | No messaging |

---

## How to identify domain entities from requirements

### Entity detection rules

An entity exists when the PRD mentions something that:

1. **Has identity** — it can be uniquely identified (by ID, code, name).
2. **Has lifecycle** — it is created, modified, and possibly deleted.
3. **Has relationships** — it connects to other things.
4. **Has business rules** — there are constraints on its data or behavior.

### Examples

| PRD text | Entity identified | Properties |
|---|---|---|
| "Users can create products with a name, description, and price" | Product | Name, Description, Price |
| "Each order contains multiple items and tracks its status" | Order, OrderItem | Status, Items |
| "Customers have a name, email, and shipping address" | Customer | Name, Email, Address |
| "Invoices are generated for completed orders" | Invoice | Order reference, Amount, Date |

### Value Object detection

A value object exists when the PRD mentions something that:

1. **Has no identity** — it is defined by its attributes, not an ID.
2. **Is immutable** — once created, it does not change.
3. **Is reusable** — it appears in multiple entities.

Examples: Money (amount + currency), Address (street, city, zip), DateRange (start + end).

### Enum detection

An enum exists when the PRD mentions a fixed set of states or types:

- "Orders can be pending, confirmed, shipped, or delivered" → `OrderStatus` enum.
- "Products have active, inactive, or discontinued status" → `ProductStatus` enum.

---

## How to detect Clean Architecture vs Vertical Slice

### Decision matrix

| Signal | Architecture |
|---|---|
| Complex business rules that change frequently | **Clean Architecture** |
| Multiple entities with rich relationships | **Clean Architecture** |
| Need to test business logic in isolation | **Clean Architecture** |
| Enterprise system with long lifespan | **Clean Architecture** |
| Team of 3+ developers | **Clean Architecture** |
| Mostly CRUD operations | **Vertical Slice** |
| MVP or prototype with limited time | **Vertical Slice** |
| Simple data management (create, read, update, delete) | **Vertical Slice** |
| Solo developer or small team (1-2) | **Vertical Slice** |
| System unlikely to grow in complexity | **Vertical Slice** |

### Default recommendation

If the PRD is unclear about complexity, ask: "Does this system have complex business rules that change often, or is it mostly managing data (create, read, update, delete)?"

- If complex rules → Clean Architecture.
- If mostly data management → Vertical Slice.

---

## How to generate config files from the PRD

### Process

1. Read and analyze the PRD using the steps above.
2. Ask all necessary questions grouped by category (architecture first, then database, frontend, auth, API docs).
3. Wait for answers. If something remains unclear, make a recommendation and ask for confirmation.
4. Generate `.temper/backend-config.md` and `.temper/frontend-config.md` (if applicable) using the exact format below.
5. Show the user a summary and ask for explicit approval.
6. If changes are needed, update and re-ask. If approved, confirm and indicate the next step is `/temper-spec`.

### Token-efficient config file generation

- Do not generate config files before asking questions.
- Do not include sections for things that are out of scope.
- Keep descriptions concise — config files are reference documents, not novels.
- Use tables where possible instead of long paragraphs.

### Exact format of backend-config.md

```markdown
# Backend Configuration

> Generated by TemperAI — temper-architect
> Date: [date]
> Status: Pending approval

---

## Architecture

**Pattern:** [Clean Architecture / Hexagonal / Vertical Slice / Onion]

**Justification:** [Why this architecture was chosen]

## Database

**Engine:** [SQL Server / PostgreSQL / SQLite]

## API Documentation

**Provider:** [Scalar / Swagger / None]

## Authentication

**Type:** [JWT / Identity / OAuth / None]

## Additional

**Health Checks:** [Yes / No]
**Messaging:** [RabbitMQ / MassTransit / None]
**Caching:** [Redis / In-Memory / None]
```

### Exact format of frontend-config.md (if applicable)

```markdown
# Frontend Configuration

> Generated by TemperAI — temper-architect
> Date: [date]
> Status: Pending approval

---

## Framework

**Type:** [Blazor WebAssembly / Blazor Server]

## API Integration

**Backend URL:** https://localhost:5001
```

---

## What the Analyst Must NEVER Generate

The following are **exclusively the architect's responsibility**. The analyst must NEVER produce documents, sections, or content containing any of the following:

### Must NEVER generate:
- `architecture.md`, `constitution.md`, `design.md`, or any technical design document
- API endpoints, HTTP methods, URL paths, or routing conventions
- Database schema, table names, column names, or foreign key definitions
- Enum definitions in any programming language (e.g., `enum StockMovementType { Addition = 1 }`)
- Configuration file examples (`appsettings.json`, `docker-compose.yml`, `.env`, etc.)
- Technology stack choices (e.g., ".NET 10", "EF Core", "PostgreSQL", "MailKit")
- Project folder structure, layer names (e.g., "Domain/", "Infrastructure/", "Application/")
- Naming conventions (e.g., PascalCase, suffix usage like "Dto", "Command")
- Testing layer structure or test project layout
- Any code snippets in any language

### Must ALWAYS generate:
- `prd.md` — functional requirements only
- Domain concepts in business language (e.g., "a product has an ideal stock level" not "Product.IdealStock is decimal")
- Business rules as natural language statements (e.g., "stock cannot be negative" not "BR-001: CurrentStock >= 0")
- No mention of: "API", "endpoint", "controller", "database", "table", "schema", "enum", "migration", "DTO", "CQRS", "layer", "architecture"

**Remember:** You are a functional analyst. You describe WHAT the application does for the user, never HOW the system is built technically.

---

## Absolute rules

- **Never** generate config files before asking all necessary questions.
- **Never** assume technology, architecture, or features without confirmation.
- **Never** ask one question at a time — always group questions by category.
- **Never** ask questions about things that are explicitly out of scope.
- **Never** skip the approval step — always show config files and ask for explicit approval.
- **Never** change functional scope — the PRD is the source of truth.
- **Always** read the complete PRD before starting analysis.
- **Always** identify entities, relationships, and business rules from the PRD text.
- **Always** recommend an architecture based on complexity if the user is unsure.
- **Always** keep questions concise and grouped to minimize token usage.
- **Always** indicate the next step (`/temper-spec`) after approval.
