---
name: entity-configuration
description: >
  Fluent API entity configuration for EF Core.
  Load as part of dotnet-ef-core — never in isolation.
  Never use DataAnnotations. Always use IEntityTypeConfiguration<T>.
  OwnsOne is NOT used in this project — Value Objects are not used.
requires: [dotnet-ef-core]
produces: [entity-configuration-classes, relationship-mappings, column-types]
---

# Entity Configuration — TemperAI

## 🚨 NON-NEGOTIABLE RULES

1. **NEVER `DataAnnotations`** on entities — Fluent API only
2. **NEVER `nvarchar(max)` or `varchar(max)`** — always specify lengths from `Entity.Rules`
3. **NEVER `builder.ToTable()`** — EF Core infers from DbSet property name
4. **NEVER `HasDefaultValueSql()` or `ValueGeneratedOnAdd()`** for primary keys
5. **NEVER `OwnsOne`** — Value Objects are not used in this project. See `dotnet-ddd`.

> ⚠️ `OwnsOne` exists in EF Core but does NOT apply here.
> This project uses primitives directly in entities — never Value Objects.
> If you think you need `OwnsOne`, you are creating a Value Object. Stop and read `dotnet-ddd` first.

---

## Basic entity configuration

```csharp
// Infrastructure/Persistence/Configurations/ProductConfiguration.cs
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .HasMaxLength(Product.Rules.NAME_MAX_LENGTH)  // ← always from Entity.Rules
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

---

## Aggregate with child entity configuration

When configuring an aggregate root with child entities, use `HasMany` / `WithOne`.

```csharp
// Infrastructure/Persistence/Configurations/OrderConfiguration.cs
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(order => order.Id);

        builder.Property(order => order.CustomerId)
            .HasColumnType("uniqueidentifier");

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnType("varchar");

        builder.Property(order => order.CreatedAt)
            .HasColumnType("datetime2");

        builder.Property(order => order.UpdatedAt)
            .HasColumnType("datetime2");

        // Configure private collection navigation — EF Core accesses via shadow property
        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// Infrastructure/Persistence/Configurations/OrderItemConfiguration.cs
public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(item => item.Id);

        builder.Property(item => item.OrderId)
            .HasColumnType("uniqueidentifier");

        builder.Property(item => item.ProductId)
            .HasColumnType("uniqueidentifier");

        builder.Property(item => item.Quantity)
            .HasColumnType("int");

        builder.Property(item => item.UnitPrice)
            .HasColumnType("decimal(18,2)");
    }
}
```

---

## Column type reference

| CLR Type | SQL Type | Notes |
|---|---|---|
| `string` (ASCII) | `varchar(n)` | Names, codes, enums as string, identifiers |
| `string` (Unicode) | `nvarchar(n)` | Descriptions, addresses, free text |
| `decimal` | `decimal(18,2)` | Money, prices, quantities with decimals |
| `DateTime` | `datetime2` | All timestamps — always UTC |
| `Guid` | `uniqueidentifier` | Primary keys, foreign keys |
| `int` | `int` | Counts, quantities without decimals |
| `bool` | `bit` | Flags |
| `enum` | `varchar(n)` | Always `.HasConversion<string>()` |

---

## Anti-patterns — NEVER DO THIS

```csharp
// ❌ NEVER DataAnnotations
public class Product
{
    [Key]
    [Required]
    [MaxLength(100)]
    public Guid Id { get; set; }
}

// ❌ NEVER nvarchar(max)
builder.Property(product => product.Description)
    .HasColumnType("nvarchar(max)");

// ❌ NEVER ToTable
builder.ToTable("Products");

// ❌ NEVER HasDefaultValueSql for PKs
builder.Property(product => product.Id)
    .HasDefaultValueSql("NEWID()");

// ❌ NEVER OwnsOne — Value Objects are not used
builder.OwnsOne(order => order.Total, money => { ... });

// ✅ CORRECT — HasKey only, EF handles the rest
builder.HasKey(product => product.Id);
```