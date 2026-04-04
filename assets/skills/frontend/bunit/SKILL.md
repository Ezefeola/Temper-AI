---
name: bunit
description: >
  bUnit testing standards for Blazor components. Covers component rendering
  tests, event handling tests, parameter binding tests, and state verification.
  Use when creating or modifying Blazor component tests.
---

# bUnit — TemperAI Standards

## Test class structure

- Always inherit from `TestContext`.
- Always use `sealed class` for test classes.
- Always name the test class after the component with `Tests` suffix.

```csharp
public sealed class ProductListTests : TestContext
{
    [Fact]
    public void Render_WithProducts_DisplaysProductNames()
    {
        List<ProductResponseDto> products = new()
        {
            new() { Id = Guid.NewGuid(), Name = "Product A", Price = 10m, Status = "Active" },
            new() { Id = Guid.NewGuid(), Name = "Product B", Price = 20m, Status = "Active" }
        };

        ProductList cut = RenderComponent<ProductList>(parameters => parameters
            .Add(p => p.Products, products));

        cut.Markup.Contains("Product A");
        cut.Markup.Contains("Product B");
    }

    [Fact]
    public void Render_WithNoProducts_DisplaysEmptyMessage()
    {
        ProductList cut = RenderComponent<ProductList>(parameters => parameters
            .Add(p => p.Products, new List<ProductResponseDto>()));

        cut.Markup.Contains("No products found");
    }
}
```

## What to test

- **Render test** — component renders without errors.
- **Data binding test** — parameters and state bind correctly.
- **Event handler test** — clicking buttons or submitting forms triggers expected behavior.
- **Loading state test** — shows loading indicator while data is fetching.
- **Error state test** — shows error message when API call fails.

## Rules

- Never test implementation details — test what the user sees.
- Never mock component rendering — use bUnit's `RenderComponent<T>`.
- Always test both positive and negative paths.
- Always use `sealed class` for test classes.
- Always follow the xUnit naming convention: `Component_Scenario_Result`.
- **Never use `using static`** — always use explicit `using` directives with the namespace, then reference types by their name. Static usings hide the type origin and make code harder to read and navigate.
