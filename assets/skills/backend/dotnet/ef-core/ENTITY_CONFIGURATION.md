---
name: entity-configuration
description: >
  Fluent API entity configuration for EF Core.
  Never use DataAnnotations. Always use IEntityTypeConfiguration<T>.
---

# Entity Configuration — TemperAI

## Rules

- **Never `DataAnnotations`** on entities or Value Objects
- **One `IEntityTypeConfiguration<T>`** per entity in `Infrastructure/Persistence/Configurations/`
- **Never `nvarchar(max)` or `varchar(max)`** — always specify lengths from `Entity.Rules`
- **`varchar` for ASCII**, `nvarchar` for Unicode
- **Value Objects configured with `OwnsOne`** — no `[ComplexType]` or DataAnnotations
- **Never call `builder.ToTable()`** — EF Core infers from DbSet property name
- **Never call `HasDefaultValueSql()` or `ValueGeneratedOnAdd()`** for primary keys

## Basic entity configuration

```csharp
// Infrastructure/Persistence/Configurations/ProductConfiguration.cs
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .HasMaxLength(Product.Rules.NAME_MAX_LENGTH)
            .HasColumnType("varchar");

        builder.Property(product => product.Description)
            .HasMaxLength(Product.Rules.DESCRIPTION_MAX_LENGTH)
            .HasColumnType("nvarchar");

        builder.Property(product => product.Price)
            .HasColumnType("decimal(18,2)");

        builder.Property(product => product.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnType("varchar");

        builder.Property(product => product.CreatedAt)
            .HasColumnType("datetime2");

        builder.Property(product => product.UpdatedAt)
            .HasColumnType("datetime2");

        builder.HasIndex(product => product.Name)
            .IsUnique();
    }
}
```

## Value Object configuration with OwnsOne

```csharp
// Infrastructure/Persistence/Configurations/OrderConfiguration.cs
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(order => order.Id);

        builder.OwnsOne(order => order.Total, moneyBuilder =>
        {
            moneyBuilder.Property(money => money.Amount)
                .HasColumnName("TotalAmount")
                .HasColumnType("decimal(18,2)");

            moneyBuilder.Property(money => money.Currency)
                .HasColumnName("TotalCurrency")
                .HasMaxLength(3)
                .HasColumnType("varchar");
        });
    }
}
```

## Multiple Value Objects

```csharp
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(order => order.Id);

        // First Value Object
        builder.OwnsOne(order => order.Total, moneyBuilder =>
        {
            moneyBuilder.Property(money => money.Amount)
                .HasColumnName("TotalAmount")
                .HasColumnType("decimal(18,2)");

            moneyBuilder.Property(money => money.Currency)
                .HasColumnName("TotalCurrency")
                .HasMaxLength(3)
                .HasColumnType("varchar");
        });

        // Second Value Object
        builder.OwnsOne(order => order.ShippingAddress, addressBuilder =>
        {
            addressBuilder.Property(address => address.Street)
                .HasColumnName("ShippingStreet")
                .HasMaxLength(200)
                .HasColumnType("nvarchar");

            addressBuilder.Property(address => address.City)
                .HasColumnName("ShippingCity")
                .HasMaxLength(100)
                .HasColumnType("nvarchar");

            addressBuilder.Property(address => address.PostalCode)
                .HasColumnName("ShippingPostalCode")
                .HasMaxLength(20)
                .HasColumnType("varchar");
        });
    }
}
```

## Column type reference

| CLR Type | Typical SQL Type | Notes |
|---|---|---|
| `string` (ASCII) | `varchar(n)` | Names, codes, identifiers |
| `string` (Unicode) | `nvarchar(n)` | Descriptions, addresses |
| `decimal` | `decimal(18,2)` | Money, prices |
| `DateTime` | `datetime2` | Timestamps |
| `Guid` | `uniqueidentifier` | Primary keys |
| `int` | `int` | Counts, quantities |
| `bool` | `bit` | Flags |

## Anti-patterns — NEVER DO THIS

```csharp
// ❌ NEVER use DataAnnotations
public class Product
{
    [Key]
    [Required]
    [MaxLength(100)]
    public Guid Id { get; set; }
}

// ❌ NEVER use nvarchar(max)
builder.Property(product => product.Description)
    .HasColumnType("nvarchar(max)");

// ❌ NEVER call ToTable — EF Core infers from DbSet name
builder.ToTable("Products");

// ❌ NEVER call HasDefaultValueSql for primary keys
builder.Property(product => product.Id)
    .HasDefaultValueSql("NEWID()");

// ✅ CORRECT: Let EF Core handle primary keys
builder.HasKey(product => product.Id);
// That's it — no additional configuration needed for Guid PKs
```