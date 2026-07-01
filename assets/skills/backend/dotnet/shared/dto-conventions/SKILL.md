---
name: dto-conventions
description: >
  Canonical DTO naming, structure, and mapping conventions for backend tasks.
  Load when creating or modifying DTOs.
requires: [backend-dotnet-csharp]
produces: [request-dtos, response-dtos, mapping-conventions]
---

# DTO Conventions — TemperAI

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **ALWAYS use `sealed record`** with explicit properties
2. **ALWAYS use the `Dto` suffix**
3. **NEVER use primary constructors** on DTOs
4. **ALWAYS use `required` for non-nullable DTO properties** — use nullable properties only when null is semantically required

## Naming

| Element | Convention | Example |
|---|---|---|
| Request DTO | `RequestDto` | `CreateProductRequestDto` |
| Response DTO | `ResponseDto` | `CreateProductResponseDto` |

## Mapping

- Use extension methods in `[Entity]MappingExtensions.cs`
- Name methods `To[DtoName]`
- Mappers transform data only; they do not decide HTTP or Result behavior

## Examples

```csharp
// ✅ GOOD — explicit properties, no primary constructor
public sealed record CreateProductRequestDto
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public decimal Price { get; init; }
}

// ❌ BAD — primary constructor
public sealed record CreateProductRequestDto(string Name, string Description, decimal Price);

// ✅ GOOD — explicit properties with required non-nullable members
public sealed record CreateProductResponseDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public decimal Price { get; init; }
    public required string Status { get; init; }
}
```

## Mapping example

```csharp
public static class ProductMappingExtensions
{
    public static CreateProductResponseDto ToCreateProductResponseDto(this Product product)
    {
        return new CreateProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Status = product.Status.ToString()
        };
    }
}
```

## Anti-patterns — never do this

```csharp
// ❌ NEVER map HTTP responses from a mapper
public static IActionResult MapToResponse(this Result<Product> result)
{
    if (result.HttpStatusCode == HttpStatusCode.NotFound)
        return NotFound();

    return Ok();
}

// ❌ NEVER create conditional DTO mapping from Result state
public static object ToResponseDto(this Result<Product> result)
{
    if (!result.IsSuccess)
        return new { error = result.Description };

    if (result.Payload is null)
        return new { error = "Missing payload" };

    return result.Payload.ToProductDto();
}
```

## Nested DTO types

When a nested type is only used by one DTO, keep it in the same file instead of creating a separate one-off file.

```csharp
public sealed record CreateProductRequestDto
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required BarcodeInfoDto Barcode { get; init; }

    public sealed record BarcodeInfoDto
    {
        public required string Code { get; init; }
        public BarcodeType Type { get; init; }
    }
}
```
