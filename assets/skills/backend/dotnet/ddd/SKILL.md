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

## Value Objects — sealed record with factory method

```csharp
public sealed record Money
{
    public decimal Amount { get; init; } = 0;
    public string Currency { get; init; } = string.Empty;

    private Money() { }

    public static (List<string> Errors, Money? Money) Create(decimal amount, string currency)
    {
        List<string> moneyErrors = [];

        if (amount < 0)
        {
            moneyErrors.Add("Amount cannot be negative");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            moneyErrors.Add("Currency is required");
        }

        if (moneyErrors.Count > 0)
        {
            return (moneyErrors, null);
        }

        Money money = new()
        {
            Amount = amount,
            Currency = currency
        };

        return ([], money);
    }
}
```

### Value Object rules

- Always `sealed record` with explicit properties — no `[ComplexType]` or DataAnnotations.
- Always a factory method returning `(List<string> Errors, ValueObject? ValueObject)`.
- Always configured with `OwnsOne` in the entity's `IEntityTypeConfiguration`.
- Never expose public setters — properties are `init` only.

## Domain Events — contract only

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

- Domain events are only contracts — `sealed record` with data, no behavior.
- Never register events on the entity or dispatch them in SaveChanges.
- Always publish explicitly in the UseCase after `CompleteAsync`.
- `Entity<TId>` base is clean — no event list or `RaiseDomainEvent` method.

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
│   └── Product/
│       ├── Product.cs
│       ├── ValueObjects/
│       ├── Enums/
│       └── Events/
├── Common/
│   ├── ValueObjects/       ← Shared VOs between entities
│   └── Primitives/
│       ├── Entity.cs
│       └── IDomainEvent.cs
└── Errors/
```

## Rules

- `Domain` has zero external dependencies — pure C# only.
- Never reference EF Core, HTTP, or any infrastructure concern in the domain.
- Never use `throw` for business validations — use the Result tuple pattern.
- Never put domain events on entities — publish them explicitly in use cases.
- Never use DataAnnotations on entities or Value Objects.
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
