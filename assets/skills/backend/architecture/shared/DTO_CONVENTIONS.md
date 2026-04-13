---
name: dto-conventions
description: >
  DTO naming and structure conventions for TemperAI projects.
  Load when creating or modifying DTOs.
---

# DTO Conventions — TemperAI

## Structure rules

- Always `sealed record` with explicit properties
- Never primary constructors
- Always `Dto` suffix
- String properties default to `string.Empty`

## Naming conventions

| Element | Convention | Example |
|---|---|---|
| Request DTOs | Suffix `RequestDto` | `CreateProductRequestDto` |
| Response DTOs | Suffix `ResponseDto` | `CreateProductResponseDto` |

## Examples

```csharp
// ✅ GOOD — explicit properties, no primary constructor
public sealed record CreateProductRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
}

// ❌ BAD — primary constructor (NEVER DO THIS)
public sealed record CreateProductRequestDto(string Name, string Description, decimal Price);

// ✅ GOOD — explicit properties with defaults
public sealed record CreateProductResponseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Status { get; init; } = string.Empty;
}

// ❌ BAD — primary constructor with no defaults (NEVER DO THIS)
public sealed record CreateProductResponseDto(Guid Id, string Name, decimal Price, string Status);
```

## Mapping conventions

- Extension methods in `[Entity]MappingExtensions.cs`
- Method name: `To[DtoName]` — exact match with DTO name
- Located at the use case or feature level
- **Mappers ONLY transform data** — they NEVER check status codes, NEVER decide HTTP responses, NEVER contain conditional logic based on HttpStatusCode or Result state

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

## Anti-patterns — NEVER DO THIS

```csharp
// ❌ NEVER check status codes in mappers
public static IActionResult MapToResponse(this Result<Product> result)
{
    if (result.HttpStatusCode == HttpStatusCode.NotFound)
        return NotFound();
    // ...
}

// ❌ NEVER create conditional mapping based on Result state
public static object ToResponseDto(this Result<Product> result)
{
    if (!result.IsSuccess)
        return new { error = result.Description };
    return result.Payload.ToDto();
}

// ✅ CORRECT: Simple data transformation only
public static ProductDto ToProductDto(this Product product)
{
    return new ProductDto
    {
        Id = product.Id,
        Name = product.Name
    };
}
```