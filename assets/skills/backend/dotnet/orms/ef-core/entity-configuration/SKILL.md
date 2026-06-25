---
name: entity-configuration
description: >
  Canonical EF Core Fluent API entity configuration rules.
  Load when creating or modifying IEntityTypeConfiguration classes.
requires: [backend-dotnet-csharp, dotnet-ddd]
produces: [entity-configurations, relationship-mappings]
---

# Entity Configuration — TemperAI

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **NEVER use DataAnnotations** on entities
2. **NEVER use `nvarchar(max)` or `varchar(max)`** — always use explicit lengths from `Entity.Rules`
3. **NEVER call `builder.ToTable()`** unless a future skill explicitly requires it
4. **NEVER use `HasDefaultValueSql()` or `ValueGeneratedOnAdd()` for primary keys**
5. **NEVER use `OwnsOne`** in this taxonomy

## Load when

- Creating `IEntityTypeConfiguration<T>` classes
- Mapping relationships, indexes, precision, or column lengths
- Registering configurations in `DbContext`

## Basic entity configuration

```csharp
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

## Aggregate with child entities

```csharp
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

        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

## Column type reference

| CLR Type | SQL Type | Notes |
|---|---|---|
| `string` ASCII | `varchar(n)` | Codes, names, identifiers |
| `string` Unicode | `nvarchar(n)` | Descriptions and free text |
| `decimal` | `decimal(18,2)` | Monetary values |
| `DateTime` | `datetime2` | Always UTC |
| `Guid` | `uniqueidentifier` | PKs and FKs |
| `int` | `int` | Counts and quantities |
| `bool` | `bit` | Flags |
| `enum` | `varchar(n)` | Use `.HasConversion<string>()` |

## Anti-patterns — never do this

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

// ❌ NEVER use ToTable here
builder.ToTable("Products");

// ❌ NEVER use default SQL generation for PKs here
builder.Property(product => product.Id)
    .HasDefaultValueSql("NEWID()");
```
