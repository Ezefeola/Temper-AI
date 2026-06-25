---
name: solid-clean-code
description: >
  Canonical SOLID and Clean Code rules for backend tasks.
  Load on every backend task.
requires: [backend-dotnet-csharp]
produces: [method-design, class-boundaries, complexity-control]
---

# SOLID Principles & Clean Code — TemperAI

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **NEVER write a method with multiple unrelated responsibilities**
2. **NEVER hide the happy path behind deep nesting** — use early returns
3. **NEVER add new behavior by mutating an existing use case** — create a new use case class
4. **NEVER keep dead code, commented code, or unused imports**
5. **NEVER use boolean parameters** when separate methods would express intent better
6. **NEVER require comments to explain WHAT the code does** — comments are for WHY only

## Practical guidance

- Keep straightforward use case flows inline when they remain readable
- Extract private methods only when they improve clarity, not mechanically
- Split classes when responsibilities diverge
- Prefer descriptive names over explanatory comments

## The use case flow — readability first

A typical use case flow such as `validate -> create -> persist -> return` is one cohesive job. Keep that flow inline when each step is straightforward.

```csharp
// ✅ CORRECT — cohesive flow, readable top-to-bottom
public async Task<Result<CreateProductResponseDto>> ExecuteAsync(
    CreateProductRequestDto request,
    CancellationToken ct)
{
    List<string> errors = Validate(request);
    if (errors.Count > 0)
        return Result<CreateProductResponseDto>.Failure(HttpStatusCode.BadRequest)
            .WithErrors(errors);

    Product product = new()
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        Price = request.Price,
        CreatedAt = DateTime.UtcNow
    };

    await _productRepository.AddAsync(product, ct);
    await _unitOfWork.CompleteAsync(ct);

    return Result<CreateProductResponseDto>.Success(HttpStatusCode.Created)
        .WithPayload(product.ToCreateProductResponseDto());
}
```

Extract only when:

- A block has its own internal complexity
- The same logic appears in multiple places
- The extracted name makes the caller more readable

## SOLID application rules

### SRP — single responsibility

- One class, one reason to change
- One method, one cohesive job
- If you describe a class with "and", it is probably doing too much

```csharp
// ✅ CORRECT — separate jobs
public sealed class CreateOrder : ICreateOrder { }
public sealed class SendOrderConfirmation : ISendOrderConfirmation { }

// ❌ WRONG — one use case doing unrelated work
public sealed class CreateOrder : ICreateOrder
{
    public async Task<Result<CreateOrderResponseDto>> ExecuteAsync(...)
    {
        await _orderRepository.AddAsync(order, ct);
        await _emailSender.SendAsync(order.CustomerEmail, "Order confirmed", ct);
        await _invoiceGenerator.GenerateAsync(order.Id, ct);
        return Result<CreateOrderResponseDto>.Success(HttpStatusCode.Created);
    }
}
```

### OCP — open for extension, closed for modification

- New behavior means new file or new use case
- New query need means new repository method
- New response shape means new DTO

```csharp
// ❌ WRONG — modify existing use case for unrelated scenario
if (request.IsBulkImport)
{
    // bulk logic here
}

// ✅ CORRECT
public sealed class CreateProduct : ICreateProduct { }
public sealed class BulkImportProducts : IBulkImportProducts { }
```

### LSP and ISP

- Never throw `NotImplementedException` from repository overrides just because a base contract does not fit
- If the base abstraction does not fit, define a narrower interface
- Split fat interfaces so callers depend only on what they use

### DIP

- Use cases depend on interfaces, not infrastructure classes
- Introduce a new abstraction when a use case needs an external capability that should vary by implementation

## Clean code rules

### Keep code flat with early returns

```csharp
// ❌ WRONG — deep nesting hides the happy path
if (request != null)
{
    if (request.Items.Count > 0)
    {
        foreach (OrderItemDto item in request.Items)
        {
            if (item.Quantity > 0)
            {
                // logic buried here
            }
        }
    }
}

// ✅ CORRECT — flat flow
if (request is null)
    return Result<OrderDto>.Failure(HttpStatusCode.BadRequest);

if (request.Items.Count == 0)
    return Result<OrderDto>.Failure(HttpStatusCode.BadRequest)
        .WithErrors(["Order must have items"]);
```

### Boolean parameters hide intent

```csharp
// ❌ WRONG
Task<Result<List<ProductDto>>> GetProductsAsync(bool includeInactive, CancellationToken ct);

// ✅ CORRECT
Task<Result<List<ProductDto>>> GetActiveProductsAsync(CancellationToken ct);
Task<Result<List<ProductDto>>> GetAllProductsAsync(CancellationToken ct);
```

### Comments only for why

```csharp
// ✅ CORRECT — comment explains why, not what
// External pricing API returns -1 when rate limit is hit — treat as unavailable.
if (response.Price == -1)
    return Result<ProductDto>.Failure(HttpStatusCode.ServiceUnavailable);
```

### Avoid primitive obsession

If a method takes many related primitive arguments, group them into a request DTO.

## Anti-patterns — never do this

```csharp
// ❌ God class
public sealed class OrderManager
{
    public Task<Order> CreateOrder() => throw new NotImplementedException();
    public Task SendConfirmationEmail() => throw new NotImplementedException();
    public Task ChargeCreditCard() => throw new NotImplementedException();
}

// ❌ Swiss army knife method
public Task<Result<ProductDto>> UpsertProductAsync(
    CreateProductRequestDto? createRequest,
    UpdateProductRequestDto? updateRequest,
    bool isCreate,
    bool skipValidation,
    CancellationToken ct) => throw new NotImplementedException();
```
