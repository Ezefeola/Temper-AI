---
name: dotnet-ddd
description: >
  Domain-Driven Design standards for .NET 10 projects. Covers entity design,
  value objects, domain events, aggregates, and domain rules. Use when
  creating or modifying domain layer components.
---

# DDD — TemperAI Standards

## Entities — sealed class with private constructor

```csharp
public sealed class Product : Entity<Guid>
{
    public class Rules
    {
        public const int NAME_MAX_LENGTH = 100;
        public const int DESCRIPTION_MAX_LENGTH = 500;
        public const decimal MIN_PRICE = 0;
    }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public ProductStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Product() { }

    public static (List<string> Errors, Product? Product) Create(
        string name,
        string description,
        decimal price)
    {
        List<string> productErrors = [];

        if (string.IsNullOrWhiteSpace(name))
        {
            productErrors.Add("Name is required");
        }
        else if (name.Length > Rules.NAME_MAX_LENGTH)
        {
            productErrors.Add($"Name cannot exceed {Rules.NAME_MAX_LENGTH} characters");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            productErrors.Add("Description is required");
        }
        else if (description.Length > Rules.DESCRIPTION_MAX_LENGTH)
        {
            productErrors.Add($"Description cannot exceed {Rules.DESCRIPTION_MAX_LENGTH} characters");
        }

        if (price <= Rules.MIN_PRICE)
        {
            productErrors.Add("Price must be greater than zero");
        }

        if (productErrors.Count > 0)
        {
            return (productErrors, null);
        }

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

    public (List<string> Errors, bool Updated) UpdateName(string newName)
    {
        List<string> nameErrors = [];

        if (string.IsNullOrWhiteSpace(newName))
        {
            nameErrors.Add("Name is required");
        }
        else if (newName.Length > Rules.NAME_MAX_LENGTH)
        {
            nameErrors.Add($"Name cannot exceed {Rules.NAME_MAX_LENGTH} characters");
        }

        if (nameErrors.Count > 0)
        {
            return (nameErrors, false);
        }

        if (Name == newName)
        {
            return ([], false);
        }

        Name = newName;
        UpdatedAt = DateTime.UtcNow;
        return ([], true);
    }
}
```

### Entity rules

- Always `sealed class` with `private` constructor.
- Always a nested `Rules` class with constraint constants.
- Factory method always returns `(List<string> Errors, Entity? Entity)`.
- Update methods always return `(List<string> Errors, bool Updated)`.
- Update methods always validate invariants, check if value changed, and set `UpdatedAt`.
- Never `throw` for business validations.
- `Entity<TId>` base is clean — no event logic or automatic auditing.

### Base Entity

```csharp
public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;
}
```

## Value Objects — NOT USED

**ValueObjects are intentionally NOT used in this project.**

We follow a pragmatic DDD approach where:
- Primitive types are used directly in entities
- `string`, `decimal`, `int`, `Guid`, `DateTime`, etc. are the preferred types
- Validation is performed in factory and update methods
- No complex mapping or `OwnsOne` configurations needed

**Example — using primitives instead of ValueObject:**

```csharp
// ✅ CORRECT — primitives in entity
public sealed class Product : Entity<Guid>
{
    public string Currency { get; private set; } = string.Empty;  // primitive
    public decimal Amount { get; private set; }                   // primitive
    
    // Validation in factory method
    public static (List<string> Errors, Product? Product) Create(
        string name, decimal amount, string currency)
    {
        // validate currency and amount here
    }
}
```

```csharp
// ❌ WRONG — ValueObject (not used in this project)
public sealed record Money { ... }  // DO NOT CREATE
```

## Domain Events — contract only, published to message broker

Domain events are **contracts only** — `sealed record` with data, no behavior.

**Key distinction**: Domain events do NOT live in a list on the entity. They are created on-demand in use cases and published to a message broker (RabbitMQ, Azure Service Bus, etc.) when the business scenario requires async communication.

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

### Domain Event rules

- Domain events are **contracts only** — `sealed record` with data, no behavior.
- Domain events live in `Domain/Entities/{EntityName}/Events/` folder — **NOT** on the entity itself.
- **Never** create a domain event list on the entity (no `List<IDomainEvent>` property, no `RaiseDomainEvent` method).
- Create the event in the UseCase **only when publishing is required** (e.g., after `CompleteAsync` when you need to notify other systems).
- Publish via `IEventPublisher.PublishAsync()` — the event is sent to a message broker for async processing by other services.
- `Entity<TId>` base is clean — no event logic whatsoever.

**When to publish a domain event**: Only when other services/systems need to react to something that happened (e.g., "send welcome email", "notify inventory service", "update search index"). If no external reaction is needed, don't create the event.

**When NOT to publish a domain event**: When the operation is self-contained and no other system needs to know about it.

### Domain Event organization

Domain events are organized by entity in their respective Events folder:

```
Domain/
├── Entities/
│   └── Products/
│       ├── Product.cs
│       └── Events/
│           └── ProductCreatedEvent.cs
├── Common/
│   └── Primitives/
│       ├── Entity.cs
│       └── IDomainEvent.cs
└── Errors/
```

**Note:** No `ValueObjects/` folder — primitives are used directly in entities.

## Enums

```csharp
public enum ProductStatus
{
    Active = 0,
    Inactive = 1,
    Discontinued = 2
}
```

### Enum rules

- Always start at 0 — EF Core maps 0 to the first value by default.
- Always use explicit values — never rely on implicit ordering.
- Store as string in the database using `.HasConversion<string>()` in configuration.

## Domain organization

```
Domain/
├── Entities/
│   └── Products/
│       ├── Product.cs
│       ├── Enums/
│       └── Events/
│           └── ProductCreatedEvent.cs
├── Common/
│   └── Primitives/
│       ├── Entity.cs
│       └── IDomainEvent.cs
└── Errors/
```

**Note:** No `ValueObjects/` folder — primitives are used directly.

## Rules

- `Domain` has zero external dependencies — pure C# only.
- Never reference EF Core, HTTP, or any infrastructure concern in the domain.
- Never use `throw` for business validations — use the Result tuple pattern.
- Never put domain events on entities — publish them explicitly in use cases.
- Never use DataAnnotations on entities or Value Objects.
- **Always use primitive types** — never create ValueObjects. Validate in factory/update methods instead.
- **Never put domain events on entities** — publish them explicitly in use cases when needed.
- **Always place the nested `Rules` class at the TOP of the entity** — before properties, constructor, and methods. Constraints should be visible first for readability.

```csharp
// GOOD — Rules class first
public sealed class Product : Entity<Guid>
{
    public class Rules
    {
        public const int NAME_MAX_LENGTH = 100;
    }

    public string Name { get; private set; } = string.Empty;
    // ...
}

// BAD — Rules class at the bottom
public sealed class Product : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    // ... methods ...

    public class Rules  // WRONG — should be at the top
    {
        public const int NAME_MAX_LENGTH = 100;
    }
}
```
