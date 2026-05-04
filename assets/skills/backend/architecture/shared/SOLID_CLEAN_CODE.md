---
name: solid-clean-code
description: >
  SOLID principles and Clean Code standards for TemperAI projects.
  Covers method design, class boundaries, complexity control, and anti-patterns.
  This skill fills the gaps not covered by dotnet-csharp, USE_CASE_PATTERNS,
  RESULT_PATTERN, or DTO_CONVENTIONS. Load ALWAYS for every backend task.
requires: [dotnet-csharp, backend/architecture/shared]
produces: [method-design, class-boundaries, complexity-control, solid-principles]
---

# SOLID Principles & Clean Code — TemperAI

> This skill covers DESIGN decisions: method cohesion, class boundaries, complexity control.
> It does NOT duplicate rules from dotnet-csharp, RESULT_PATTERN, USE_CASE_PATTERNS, or DTO_CONVENTIONS.

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **NEVER a method that does more than one clearly defined job** — if you can't describe it in one sentence starting with a verb, it does too much. "Create an order from a request, validate it, persist it, and return the result" IS one sentence. "Create an order, send a confirmation email, and generate an invoice" is NOT.
2. **NEVER deep nesting that hides the happy path** — use early returns to keep code flat. If you have to scroll horizontally or count braces to find what the method actually does, flatten it.
3. **NEVER a method where you lose track of the happy path** — if branching is so complex that you can't trace the success path at a glance, extract the confusing part into a well-named method.
4. **NEVER modify an existing use case to add new behavior** — create a new use case class
5. **NEVER a class with 2+ unrelated responsibilities** — split into separate classes
6. **NEVER dead code** — no commented-out code, no unused methods, no unused imports
7. **NEVER boolean parameters** — extract to a separate method with a descriptive name
8. **NEVER code that needs a comment to explain WHAT** — refactor until the code explains itself; comments only for WHY

---

## The Use Case Flow — Readability First

A use case's `ExecuteAsync` has a natural rhythm: **validate → create → persist → return**. This is ONE cohesive job. The steps are sequential, they depend on each other, and they read naturally top-to-bottom.

### When to keep it inline

If the full flow is readable at a glance — you can see the beginning and the end on screen, and each step is straightforward — **keep it inline**. Do NOT fragment it into `ValidateRequest()`, `CreateEntity()`, `PersistEntity()`, `BuildResponse()` private methods. That makes it HARDER to follow because you have to jump between methods to understand a simple flow.

```csharp
// ✅ CORRECT — cohesive flow, reads top-to-bottom
public async Task<Result<CreateProductResponseDto>> ExecuteAsync(
    CreateProductRequestDto request, CancellationToken ct)
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
    await _unitOfWork.SaveChangesAsync(ct);

    return Result<CreateProductResponseDto>.Success(HttpStatusCode.Created)
        .WithPayload(product.ToCreateProductResponseDto());
}
```

Notice: validation IS extracted here because it has its own internal complexity (multiple rules, a loop). But entity creation, persistence, and response mapping are inline because they're each 2-3 lines that add no complexity.

### When to extract from a use case

Extract a private method when:
- A block of logic has **its own internal complexity** that breaks the reading flow (e.g., validation with cross-field rules, multi-step aggregate construction)
- The same logic appears in multiple places (DRY)
- Extracting it gives it a **name that makes the caller more readable** — the name communicates something the raw code doesn't

Do NOT extract when:
- The extracted method is only called once AND its name doesn't add information beyond what the code already says
- Reading it requires jumping to another method to understand what should be obvious inline

### The readability test

Ask yourself: **"Can I read this method once, top-to-bottom, and understand the complete flow?"**

- If yes → it's good, even if it's 40-50 lines
- If no → find the part that's confusing and extract THAT part — not everything

---

## SRP — Single Responsibility

One class = one reason to change. One method = one cohesive job.

### Detect violations

- A method that does "create an order AND send an email AND generate an invoice" is doing three jobs — split into separate use cases
- If a class has 3+ private fields from different domains (e.g., `_orderRepo`, `_emailSender`, `_paymentGateway`), it's doing too much
- If you can describe a class's purpose only with "AND", it violates SRP

### ✅ CORRECT — separate use cases for separate jobs

```csharp
// Each use case does ONE job
public sealed class CreateOrder : ICreateOrder { ... }
public sealed class SendOrderConfirmation : ISendOrderConfirmation { ... }
public sealed class GenerateInvoice : IGenerateInvoice { ... }
```

### ❌ WRONG — one use case doing unrelated things

```csharp
public sealed class CreateOrder : ICreateOrder
{
    public async Task<Result<CreateOrderResponseDto>> ExecuteAsync(
        CreateOrderRequestDto request, CancellationToken ct)
    {
        // Create the order — this is the use case's job
        Order order = new() { /* ... */ };
        await _orderRepository.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Send confirmation email — NOT this use case's job
        await _emailSender.SendAsync(order.CustomerEmail, "Order confirmed", ct);

        // Generate invoice PDF — NOT this use case's job
        byte[] pdf = await _invoiceGenerator.GenerateAsync(order.Id, ct);
        await _storage.SaveAsync($"invoices/{order.Id}.pdf", pdf, ct);

        return Result<CreateOrderResponseDto>.Success(HttpStatusCode.Created)
            .WithPayload(order.ToCreateOrderResponseDto());
    }
}
```

The CreateOrder use case should create an order. Email and invoice are separate concerns — trigger them via domain events or orchestrate from a higher level.

---

## OCP — Open/Closed

New behavior = new file. Never modify an existing use case's `ExecuteAsync` to handle a new scenario.

### Rules

- New use case for new behavior — never add `if/else` branches for a new case in an existing use case
- New repository method for new query need — don't add optional parameters to existing methods
- New DTO for new shape — don't overload an existing DTO with extra fields "just in case"

### ❌ WRONG — modifying existing use case for new behavior

```csharp
// Original CreateProduct use case modified to also handle "bulk import"
public async Task<Result<CreateProductResponseDto>> ExecuteAsync(
    CreateProductRequestDto request, CancellationToken ct)
{
    if (request.IsBulkImport)  // ❌ Added branch for new behavior
    {
        // 20 lines of bulk import logic...
    }
    else
    {
        // original single-create logic
    }
}
```

### ✅ CORRECT — new use case for new behavior

```csharp
// CreateProduct stays untouched — single responsibility
public sealed class CreateProduct : ICreateProduct { ... }

// New behavior gets its own use case
public sealed class BulkImportProducts : IBulkImportProducts { ... }
```

---

## LSP — Liskov Substitution

When overriding in `GenericRepository<T>` or extending `Entity<TId>`, the derived type must be usable anywhere the base is expected.

### Rules

- Never make an override stricter than the base contract (e.g., base accepts null, override throws)
- Never throw `NotImplementedException` in overrides — if a method doesn't apply, the interface is too wide (ISP violation)
- Never change the semantics of a base method — if base `GetByIdAsync` returns null when not found, override must do the same

### ❌ WRONG — override violates base contract

```csharp
public class SpecialOrderRepository : GenericRepository<SpecialOrder>
{
    public override Task<SpecialOrder?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        throw new NotImplementedException("Use GetByReferenceCode instead");
        // ❌ Breaks LSP — callers expect this to work
    }
}
```

### ✅ CORRECT — if the interface doesn't fit, segregate it

```csharp
// If SpecialOrder doesn't need GetById, it shouldn't implement a generic repo
// Create a specific interface instead
public interface ISpecialOrderRepository
{
    Task<SpecialOrder?> GetByReferenceCodeAsync(string code, CancellationToken ct);
    Task AddAsync(SpecialOrder order, CancellationToken ct);
}
```

---

## ISP — Interface Segregation

If an interface has 4+ methods serving different callers, split it.

### Rules

- Use case interfaces are already single-method (enforced by USE_CASE_PATTERNS) — this applies to repository and service interfaces
- No client should be forced to depend on methods it doesn't use
- Split by caller role: read vs write, admin vs user, internal vs external

### ❌ WRONG — fat interface serving different callers

```csharp
public interface IProductService
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Product>> GetAllAsync(CancellationToken ct);
    Task<Product> CreateAsync(string name, decimal price, CancellationToken ct);
    Task<Product> UpdatePriceAsync(Guid id, decimal newPrice, CancellationToken ct);
    Task DeactivateAsync(Guid id, CancellationToken ct);
    Task<byte[]> GenerateReportAsync(Guid id, CancellationToken ct);
}
```

### ✅ CORRECT — split by role

```csharp
public interface IProductReader
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Product>> GetAllAsync(CancellationToken ct);
}

public interface IProductWriter
{
    Task<Product> CreateAsync(string name, decimal price, CancellationToken ct);
    Task<Product> UpdatePriceAsync(Guid id, decimal newPrice, CancellationToken ct);
    Task DeactivateAsync(Guid id, CancellationToken ct);
}

// Report generation is a separate concern entirely
public interface IProductReportGenerator
{
    Task<byte[]> GenerateReportAsync(Guid id, CancellationToken ct);
}
```

---

## DIP — Dependency Inversion

Most DIP coverage already exists in USE_CASE_PATTERNS and architecture skills (use cases depend on interfaces, not implementations).

### When to introduce a NEW abstraction

- A use case needs something that has no interface yet — create the interface in the use case's layer, implement it in infrastructure
- Never depend on a concrete class from a different layer

> For existing patterns (IUnitOfWork, IRepository, etc.), see USE_CASE_PATTERNS and the architecture-specific skill.

---

## Clean Code Rules

### Method cohesion — readable at a glance

A method should do ONE cohesive thing that you can understand at a glance. If it does, length doesn't matter. If you have to scroll and lose track of what's happening, THAT's when you extract.

**Guidelines:**

- A method should be readable at a glance — you see the beginning and the end on screen
- If a method does ONE cohesive thing (like a use case's ExecuteAsync that validates, creates, persists, and returns), that's FINE even if it's 40-50 lines. The flow reads top-to-bottom.
- Extract private methods when:
  - A block of logic has its own internal complexity that breaks the reading flow
  - The same logic appears in multiple places (DRY)
  - Extracting it gives it a name that makes the caller MORE readable
- Do NOT extract when:
  - The extracted method is only called once
  - Its name doesn't add information beyond what the code already says
  - Reading it requires jumping to another method to understand what should be obvious inline

```csharp
// ❌ WRONG — over-extracted, you have to jump to 4 private methods to understand a simple flow
public async Task<Result<CreateProductResponseDto>> ExecuteAsync(
    CreateProductRequestDto request, CancellationToken ct)
{
    var errors = ValidateRequest(request);
    if (errors.Count > 0)
        return Result<CreateProductResponseDto>.Failure(HttpStatusCode.BadRequest)
            .WithErrors(errors);

    Product product = CreateProductEntity(request);
    await PersistProductAsync(product, ct);
    return BuildResponse(product);
}
// Each private method is 3-5 lines that would be clearer inline

// ✅ CORRECT — inline flow that reads top-to-bottom
public async Task<Result<CreateProductResponseDto>> ExecuteAsync(
    CreateProductRequestDto request, CancellationToken ct)
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
    await _unitOfWork.SaveChangesAsync(ct);

    return Result<CreateProductResponseDto>.Success(HttpStatusCode.Created)
        .WithPayload(product.ToCreateProductResponseDto());
}
```

Note: `Validate` IS extracted above because it contains multiple validation rules with their own logic. The entity creation, persistence, and response mapping stay inline because they're each 2-3 obvious lines.

### Nesting — keep code flat with early returns

Deep nesting hides the happy path and forces readers to track brace levels. Use early returns to flatten.

```csharp
// ❌ WRONG — deep nesting hides the actual logic
public async Task<Result<OrderDto>> ExecuteAsync(...)
{
    if (request != null)
    {
        if (request.Items.Count > 0)
        {
            foreach (var item in request.Items)
            {
                if (item.Quantity > 0)
                {
                    // logic buried 4 levels deep
                }
            }
        }
    }
}

// ✅ CORRECT — early returns flatten nesting
public async Task<Result<OrderDto>> ExecuteAsync(...)
{
    if (request is null)
        return Result<OrderDto>.Failure(HttpStatusCode.BadRequest);

    if (request.Items.Count == 0)
        return Result<OrderDto>.Failure(HttpStatusCode.BadRequest)
            .WithErrors(["Order must have items"]);

    foreach (OrderItemDto item in request.Items)
    {
        if (item.Quantity <= 0)
            return Result<OrderDto>.Failure(HttpStatusCode.BadRequest)
                .WithErrors(["Quantity must be positive"]);
    }

    // flat, readable logic here
}
```

### Complexity — can you trace the happy path?

If a method has so many branches that you lose track of the happy path, extract the confusing part.

**Ask yourself:** "Can I trace the success path through this method without re-reading?"

- If yes → the complexity is manageable
- If no → find the block of branching logic that's confusing, extract it into a well-named method

```csharp
// ❌ WRONG — can't trace the happy path
if (status == Status.Active) { /* 10 lines */ }
else if (status == Status.Pending) { /* 15 lines */ }
else if (status == Status.Suspended) { /* 8 lines */ }
else if (status == Status.Cancelled) { /* 12 lines */ }
// ... 8 more branches

// ✅ CORRECT — each branch extracted into its own well-named method
private Result<OrderDto> HandleActiveStatus(Order order) { ... }
private Result<OrderDto> HandlePendingStatus(Order order) { ... }
private Result<OrderDto> HandleSuspendedStatus(Order order) { ... }
```

### Boolean parameters — use separate methods

```csharp
// ❌ WRONG — boolean parameter hides intent
public async Task<Result<List<ProductDto>>> GetProductsAsync(
    bool includeInactive, CancellationToken ct)

// Callers are unclear:
await repo.GetProductsAsync(true, ct);  // true means what?

// ✅ CORRECT — separate methods with descriptive names
public async Task<Result<List<ProductDto>>> GetActiveProductsAsync(CancellationToken ct) { ... }
public async Task<Result<List<ProductDto>>> GetAllProductsAsync(CancellationToken ct) { ... }
```

### Names that reveal intention

```csharp
// ❌ WRONG — name describes HOW, not WHAT
private async Task<Result<Order>> ProcessOrderAndValidateStock(Order order, CancellationToken ct)

// ✅ CORRECT — name reveals intent
private async Task<Result<Order>> FulfillOrderAsync(Order order, CancellationToken ct)
```

### No dead code

```csharp
// ❌ WRONG — commented-out code, unused method
// public async Task<Result<ProductDto>> OldCreateMethod(...)
// {
//     ...
// }

private async Task<decimal> CalculateLegacyDiscount() { ... }  // never called

// ✅ CORRECT — delete it. Git remembers everything.
```

### Comments only for WHY

```csharp
// ❌ WRONG — comment explains WHAT the code does
// Check if the product is active
if (product.Status == ProductStatus.Active) { ... }

// ❌ WRONG — comment that should be a method name
// Validate that the price is not negative
if (request.Price < 0) { ... }

// ✅ CORRECT — comment explains WHY, code explains WHAT
// External pricing API returns -1 when rate limit is hit — treat as unavailable
if (response.Price == -1)
    return Result<ProductDto>.Failure(HttpStatusCode.ServiceUnavailable);
```

### Avoid primitive obsession in parameters

When a method takes 5+ primitive parameters, group related parameters into a DTO instead.

```csharp
// ❌ WRONG — too many primitives
public async Task<Result<OrderDto>> CreateOrderAsync(
    string customerName, string customerEmail, string shippingStreet,
    string shippingCity, string shippingZip, List<OrderItemDto> items,
    CancellationToken ct)

// ✅ CORRECT — related parameters grouped into DTOs
public async Task<Result<OrderDto>> CreateOrderAsync(
    CreateOrderRequestDto request, CancellationToken ct)
```

---

## Anti-patterns — NEVER DO THIS

```csharp
// ❌ God class — doing everything
public sealed class OrderManager
{
    public async Task<Order> CreateOrder() { ... }
    public async Task SendConfirmationEmail() { ... }
    public async Task ChargeCreditCard() { ... }
    public async Task UpdateInventory() { ... }
    public async Task<byte[]> GenerateInvoice() { ... }
}
// Split into: CreateOrder, SendOrderConfirmation, ProcessPayment, UpdateInventory, GenerateInvoice

// ❌ Swiss army knife — one method with a flag for every scenario
public async Task<Result<ProductDto>> UpsertProductAsync(
    CreateProductRequestDto? createRequest,
    UpdateProductRequestDto? updateRequest,
    bool isCreate,
    bool skipValidation,
    CancellationToken ct)

// ❌ Train wreck — chained calls that hide intermediate steps
var result = (await (await _repo.GetQueryable().Where(x => x.Active).ToListAsync())
    .Select(p => p.ToDto()).ToList());
// Extract intermediate variables with explicit types
```
