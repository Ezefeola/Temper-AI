---
name: dotnet-ddd
description: >
  Domain-Driven Design standards for .NET 10 projects. Covers entity design,
  aggregates with child entities, domain events, and domain rules.
  Load when creating or modifying any Domain layer component — entities, enums, events, aggregates.
  DO NOT load for infrastructure, repository, or controller tasks.
  Value Objects are intentionally NOT used — primitives are used directly in entities.
requires: [dotnet-csharp]
produces: [entities, aggregates, domain-events, enums, domain-rules]
---

# DDD — TemperAI Standards

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **NEVER Value Objects** — primitives are used directly in entities. Never create `sealed record` value types.
2. **NEVER throw** for business validations — always return `(List<string> Errors, T? Entity)`
3. **NEVER domain events on entities** — publish explicitly in use cases when needed
4. **NEVER external dependencies in Domain** — pure C# only, zero EF Core or HTTP references
5. **ALWAYS Rules class at the TOP** of the entity — before properties, constructor, and methods
6. **NEVER `public List<T>`** for child collections — always use `private readonly List<T>` backing field + `IReadOnlyList<T>` public property
7. **NEVER calculate or derive domain values in use cases or services** — business rules and calculations belong to the entity

---

## When NOT to apply this skill

- You are configuring EF Core mappings — load `dotnet-ef-core` instead
- You are writing repositories or use cases without modifying domain entities
- You are working on the API or infrastructure layer

---

## Entities — sealed class with private constructor

```csharp
public sealed class Product : Entity<Guid>
{
    // 1. Rules class — ALWAYS FIRST
    public class Rules
    {
        public const int NAME_MAX_LENGTH = 100;
        public const int DESCRIPTION_MAX_LENGTH = 500;
        public const decimal MIN_PRICE = 0;
    }

    // 2. Properties
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public ProductStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // 3. Private constructor — EF Core needs this
    private Product() { }

    // 4. Factory method — always returns (errors, entity?)
    public static (List<string> Errors, Product? Product) Create(
        string name,
        string description,
        decimal price)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Name is required");
        else if (name.Length > Rules.NAME_MAX_LENGTH)
            errors.Add($"Name cannot exceed {Rules.NAME_MAX_LENGTH} characters");

        if (string.IsNullOrWhiteSpace(description))
            errors.Add("Description is required");
        else if (description.Length > Rules.DESCRIPTION_MAX_LENGTH)
            errors.Add($"Description cannot exceed {Rules.DESCRIPTION_MAX_LENGTH} characters");

        if (price <= Rules.MIN_PRICE)
            errors.Add("Price must be greater than zero");

        if (errors.Count > 0)
            return (errors, null);

        Product product = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        return ([], product);
    }

    // 5. Update methods — always return (errors, updated)
    public (List<string> Errors, bool Updated) UpdateName(string newName)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(newName))
            errors.Add("Name is required");
        else if (newName.Length > Rules.NAME_MAX_LENGTH)
            errors.Add($"Name cannot exceed {Rules.NAME_MAX_LENGTH} characters");

        if (errors.Count > 0)
            return (errors, false);

        if (Name == newName)
            return ([], false);

        Name = newName;
        UpdatedAt = DateTime.UtcNow;
        return ([], true);
    }
}
```

### Entity rules

- Always `sealed class` with `private` constructor
- Always a nested `Rules` class at the TOP — before properties and methods
- Factory method always returns `(List<string> Errors, Entity? Entity)`
- Update methods always return `(List<string> Errors, bool Updated)`
- Update methods always validate, check if value changed, and set `UpdatedAt`
- Never `throw` for business validations
- `Entity<TId>` base is clean — no event logic, no auditing

### Base Entity

```csharp
public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;
}
```

---

## Aggregates with child entities

An aggregate root controls all mutations to its children. Child collections are exposed as `IReadOnlyList<T>` — all writes go exclusively through the aggregate root's methods.

```csharp
public sealed class Order : Entity<Guid>
{
    public class Rules
    {
        public const int MAX_ITEMS = 50;
    }

    // Backing field — private, mutable, only the aggregate root touches this
    private readonly List<OrderItem> _items = [];

    // Public surface — read-only, prevents any external mutation
    public IReadOnlyList<OrderItem> Items => _items;

    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Order() { }

    public static (List<string> Errors, Order? Order) Create(Guid customerId)
    {
        List<string> errors = [];

        if (customerId == Guid.Empty)
            errors.Add("CustomerId is required");

        if (errors.Count > 0)
            return (errors, null);

        Order order = new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        return ([], order);
    }

    // Aggregate root validates its own invariants, then delegates child creation to the child itself
    public (List<string> Errors, bool Added) AddItem(Guid productId, int quantity, decimal unitPrice)
    {
        List<string> errors = [];

        // Aggregate-level invariants only — not field-level validation
        if (Status != OrderStatus.Pending)
            errors.Add("Items can only be added to pending orders");

        if (_items.Count >= Rules.MAX_ITEMS)
            errors.Add($"Order cannot exceed {Rules.MAX_ITEMS} items");

        // Cross-child invariant — duplicate product check belongs here, not in the use case
        if (_items.Any(i => i.ProductId == productId))
            errors.Add("Product already exists in this order");

        if (errors.Count > 0)
            return (errors, false);

        // Delegate field validation to the child's own factory method
        var (itemErrors, item) = OrderItem.Create(Id, productId, quantity, unitPrice);

        if (itemErrors.Count > 0)
            return (itemErrors, false);

        _items.Add(item!);
        UpdatedAt = DateTime.UtcNow;
        return ([], true);
    }

    // RemoveItem — always through the aggregate root, never via Items directly
    public (List<string> Errors, bool Removed) RemoveItem(Guid productId)
    {
        if (Status != OrderStatus.Pending)
            return (["Items can only be removed from pending orders"], false);

        OrderItem? item = _items.FirstOrDefault(i => i.ProductId == productId);

        if (item is null)
            return (["Item not found in order"], false);

        _items.Remove(item);
        UpdatedAt = DateTime.UtcNow;
        return ([], true);
    }

    public (List<string> Errors, bool Confirmed) Confirm()
    {
        if (Status != OrderStatus.Pending)
            return (["Order is not in Pending status"], false);

        if (_items.Count == 0)
            return (["Cannot confirm an order with no items"], false);

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
        return ([], true);
    }
}

// Child entity — internal Create factory method, validates its own business rules
public sealed class OrderItem : Entity<Guid>
{
    public class Rules
    {
        public const int MAX_QUANTITY = 100;
    }

    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    // Private constructor — EF Core needs this
    private OrderItem() { }

    // internal — only the aggregate root can call this
    internal static (List<string> Errors, OrderItem? Item) Create(
        Guid orderId,
        Guid productId,
        int quantity,
        decimal unitPrice)
    {
        List<string> errors = [];

        if (orderId == Guid.Empty)
            errors.Add("OrderId is required");

        if (productId == Guid.Empty)
            errors.Add("ProductId is required");

        if (quantity <= 0 || quantity > Rules.MAX_QUANTITY)
            errors.Add($"Quantity must be between 1 and {Rules.MAX_QUANTITY}");

        if (unitPrice <= 0)
            errors.Add("Unit price must be greater than zero");

        if (errors.Count > 0)
            return (errors, null);

        return ([], new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice
        });
    }
}
```

### Aggregate rules

**Structure**
- Child collections use a `private readonly List<T> _items = []` backing field — the aggregate root mutates this directly
- Child collections expose `IReadOnlyList<T>` publicly — external code can read but never write
- **NEVER** use `public List<T>` — `private set` only protects the reference, not the contents of the list
- All child mutations go through aggregate root methods — never modify children directly from outside
- Child entities live in the same folder as their aggregate root
- EF Core maps the backing field automatically by convention when the field name is `_<propertyName>` (e.g. `_items` for `Items`)

**Why `public List<T>` is wrong even with `private set`:**

```csharp
// ❌ WRONG — private set only protects the reference, not the list contents
public List<OrderItem> Items { get; private set; } = [];

// This compiles and runs — bypasses ALL aggregate invariants silently:
order.Items.Add(someItem);   // no status check, no max items check, no UpdatedAt
order.Items.Clear();         // destroys aggregate state with zero validation
order.Items.RemoveAt(0);    // no invariant enforced whatsoever

// ✅ CORRECT — IReadOnlyList prevents all external mutation at compile time
private readonly List<OrderItem> _items = [];
public IReadOnlyList<OrderItem> Items => _items;

order.Items.Add(someItem);   // ❌ compile error — IReadOnlyList has no Add
order.Items.Clear();         // ❌ compile error
order.Items.RemoveAt(0);    // ❌ compile error
```

**Child entities — factory methods and accessibility**
- Child entities have an `internal static Create(...)` factory method — never `public`
- The `internal` factory method validates the child's own business rules (field-level: nulls, ranges, formats)
- Properties use `private set` — never `internal set`
- Constructor is `private` — same as any entity, EF Core requires it
- This guarantees that nothing outside the aggregate can instantiate or mutate a child

**Responsibility split between root and child**
- The aggregate root validates **aggregate-level invariants** — state checks, collection limits, cross-child rules
- The child validates **its own field-level rules** — required fields, value ranges, format constraints
- The aggregate root never duplicates field validation from the child — it calls `Child.Create(...)` and propagates errors
- This keeps each class responsible for exactly what belongs to it (SRP)

**Aggregate size — keep it small**
- An aggregate must be loadable in full from the database without explosive JOINs
- Practical rule: if a child collection can exceed ~50 records in production, reconsider whether it is truly a child or should be its own aggregate referenced by ID
- An aggregate requiring 4+ child collections is a signal it is modeled too broadly — split it
- Prefer small, cohesive aggregates over large aggregates that group everything related

**References between aggregates — by ID only**
- An aggregate never holds an EF navigation property pointing to another aggregate
- References are primitive IDs only (`Guid CustomerId`, never `Customer Customer`)
- If a use case needs data from another aggregate, load it separately in the use case
- This prevents accidental eager loading, hidden lazy loading, and coupling between boundaries

```csharp
// ✅ CORRECT — reference another aggregate by ID only
public sealed class Order : Entity<Guid>
{
    public Guid CustomerId { get; private set; }  // ID only
}

// ❌ WRONG — navigation property to another aggregate
public sealed class Order : Entity<Guid>
{
    public Customer Customer { get; private set; }  // NEVER — hidden loading, tight coupling
}
```

**Invariants**
- Aggregate-level rules (order status, max items, cross-child constraints) live in the aggregate root
- Field-level rules (valid values, required fields, ranges) live in the child's `internal Create` factory method
- If a child accumulates complex business logic beyond validating its own fields, it is a signal it should become its own aggregate
- Never duplicate validations across root and child — each layer owns its slice of responsibility

---

## Business rules belong to entities

Business rules, calculations, and domain decisions live in the entity or aggregate root — never in use cases, services, or controllers.

A use case orchestrates: it loads the entity, calls its methods, and saves the result.
The entity decides: it holds the logic, enforces invariants, and computes derived values.

### What belongs in the entity

| Type | Example | Lives in |
|---|---|---|
| State transition rules | "Cannot confirm if no items" | Aggregate root method |
| Calculations from own data | Total price of an order | Entity method |
| Domain queries | "Does this order have a specific product?" | Entity method |
| Conditional domain logic | "Can this order be cancelled?" | Entity method |
| Cross-child invariants | "No duplicate products in an order" | Aggregate root method |

### Domain methods — calculations and queries

Domain methods that compute or query values from the entity's own data have no side effects and return a value directly — no error tuple needed.

```csharp
public sealed class Order : Entity<Guid>
{
    private readonly List<OrderItem> _items = [];
    public IReadOnlyList<OrderItem> Items => _items;

    public OrderStatus Status { get; private set; }

    // Calculation — derived from own data, no side effects
    public decimal CalculateTotal() =>
        _items.Sum(i => i.Quantity * i.UnitPrice);

    // Domain query — answers a business question about own state
    public bool CanBeConfirmed() =>
        Status == OrderStatus.Pending && _items.Count > 0;

    // Domain query — checks own collection
    public bool HasProduct(Guid productId) =>
        _items.Any(i => i.ProductId == productId);

    // Calculation with business logic
    public int TotalItemCount() =>
        _items.Sum(i => i.Quantity);
}
```

### Business rule method naming

Domain methods that answer a question are named as questions or statements of fact:

```
CanBeConfirmed()          ✅ — answers "can it?"
HasProduct(id)            ✅ — answers "does it have?"
IsEligibleForDiscount()   ✅ — answers "is it eligible?"
CalculateTotal()          ✅ — computes a value
TotalItemCount()          ✅ — computes a derived count
```

### What must NOT leak into use cases or services

```csharp
// ❌ WRONG — domain logic leaking into the use case
public async Task ExecuteAsync(ConfirmOrderCommand command)
{
    Order order = await _repository.GetByIdAsync(command.OrderId);

    // These are domain rules — they do NOT belong here:
    if (order.Status != OrderStatus.Pending)
        return (["Order is not pending"], false);

    if (order.Items.Count == 0)
        return (["Order has no items"], false);

    var total = order.Items.Sum(i => i.Quantity * i.UnitPrice); // domain calculation leaked out

    order.Confirm();
    await _repository.SaveAsync(order);
}

// ✅ CORRECT — use case only orchestrates, entity decides
public async Task ExecuteAsync(ConfirmOrderCommand command)
{
    Order order = await _repository.GetByIdAsync(command.OrderId);

    var (errors, confirmed) = order.Confirm(); // all rules live inside Confirm()

    if (errors.Count > 0)
        return;

    await _repository.SaveAsync(order);
}
```

### Absolute rules for business rules

- **NEVER** calculate or derive domain values in a use case — move them to the entity
- **NEVER** check domain conditions (status, count, eligibility) in a use case before calling the entity — let the entity enforce its own invariants
- **NEVER** iterate over child collections from outside the aggregate to make a decision — expose a domain method instead
- **ALWAYS** ask: "does this logic depend only on the entity's own data?" — if yes, it belongs in the entity
- **Domain methods with no side effects return a value directly** — no error tuple needed, no `Updated` bool

---

## Value Objects — NOT USED in this project

**Primitives are used directly in entities.**

```csharp
// ✅ CORRECT — primitives
public sealed class Product : Entity<Guid>
{
    public string Currency { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
}

// ❌ WRONG — Value Object
public sealed record Money { ... }  // DO NOT CREATE
```

> ⚠️ NOTE FOR EF CORE: Since Value Objects are not used, `OwnsOne` configuration is also never used.
> The `OwnsOne` API exists in EF Core but does not apply to this project.
> See `dotnet-ef-core/ENTITY_CONFIGURATION.md` for correct entity configuration.

---

## Domain Events

Domain events are **contracts only** — `sealed record` with data, no behavior.
They do NOT live on entities. They are created and published in use cases.

```csharp
public interface IDomainEvent { }

public sealed record ProductCreatedEvent : IDomainEvent
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal Price { get; init; }

    public ProductCreatedEvent(Guid productId, string productName, decimal price)
    {
        ProductId = productId;
        ProductName = productName;
        Price = price;
    }
}
```

**When to publish:** Only when another system needs to react (send email, notify inventory, update search index).
**When NOT to publish:** When the operation is self-contained with no external reactions needed.

```csharp
// ✅ CORRECT — event created and published in use case, not on entity
public async Task<Result<...>> ExecuteAsync(...)
{
    // ... create and save entity ...

    ProductCreatedEvent domainEvent = new(product.Id, product.Name, product.Price);
    await _eventPublisher.PublishAsync(domainEvent, cancellationToken);

    return Result<...>.Success(...);
}

// ❌ WRONG — event list on entity
public sealed class Product : Entity<Guid>
{
    private readonly List<IDomainEvent> _domainEvents = [];  // NEVER
    public void RaiseDomainEvent(IDomainEvent e) => _domainEvents.Add(e);  // NEVER
}
```

---

## Enums

```csharp
public enum ProductStatus
{
    Active = 1,
    Inactive = 2,
    Discontinued = 3
}
```

**Rules:**
- Always start at 1
- Always use explicit values — never rely on implicit ordering
- Store as string in database — configure with `.HasConversion<string>()` in entity configuration

---

## Domain folder structure

```
Domain/
├── Entities/
│   ├── Products/
│   │   ├── Product.cs
│   │   ├── Enums/
│   │   │   └── ProductStatus.cs
│   │   └── Events/
│   │       └── ProductCreatedEvent.cs
│   └── Orders/
│       ├── Order.cs
│       ├── OrderItem.cs          ← child entity, same folder as aggregate root
│       ├── Enums/
│       │   └── OrderStatus.cs
│       └── Events/
│           └── OrderConfirmedEvent.cs
└── Common/
    └── Primitives/
        ├── Entity.cs
        └── IDomainEvent.cs
```

**Note:** No `ValueObjects/` folder — primitives are used directly in entities.