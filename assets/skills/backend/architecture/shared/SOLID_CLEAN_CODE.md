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

> This skill covers DESIGN decisions: method size, class boundaries, complexity control.
> It does NOT duplicate rules from dotnet-csharp, RESULT_PATTERN, USE_CASE_PATTERNS, or DTO_CONVENTIONS.

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **NEVER methods longer than ~30 lines** — extract private methods with descriptive names
2. **NEVER more than 3 levels of nesting** — use early returns to flatten
3. **NEVER methods with more than ~7 branches** (if/switch/loops combined) — extract or simplify
4. **NEVER modify an existing use case to add new behavior** — create a new use case class
5. **NEVER a class with 2+ unrelated responsibilities** — split into separate classes
6. **NEVER dead code** — no commented-out code, no unused methods, no unused imports
7. **NEVER boolean parameters** — extract to a separate method with a descriptive name
8. **NEVER code that needs a comment to explain WHAT** — refactor until the code explains itself; comments only for WHY

---

## SRP — Single Responsibility

One class = one reason to change. One method = one job.

### Detect violations

- A method that does "validate AND persist AND notify" is doing three jobs — extract
- If a class has 3+ private fields from different domains (e.g., `_productRepo`, `_emailSender`, `_paymentGateway`), it's doing too much
- If you can describe a class's purpose only with "AND", it violates SRP

### ✅ CORRECT — extracted private methods with descriptive names

```csharp
public async Task<Result<CreateOrderResponseDto>> ExecuteAsync(
    CreateOrderRequestDto request, CancellationToken ct)
{
    Result<Order> orderResult = CreateOrderFromRequest(request);
    if (!orderResult.IsSuccess)
        return Result<CreateOrderResponseDto>.Failure(orderResult.HttpStatusCode)
            .WithErrors(orderResult.Errors);

    await _orderRepository.AddAsync(orderResult.Payload!, ct);
    await _unitOfWork.SaveChangesAsync(ct);

    return Result<CreateOrderResponseDto>.Success(HttpStatusCode.Created)
        .WithPayload(orderResult.Payload.ToCreateOrderResponseDto());
}
```

### ❌ WRONG — one fat method doing everything

```csharp
public async Task<Result<CreateOrderResponseDto>> ExecuteAsync(
    CreateOrderRequestDto request, CancellationToken ct)
{
    List<string> errors = [];
    if (string.IsNullOrWhiteSpace(request.CustomerName))
        errors.Add("Customer name is required");
    if (request.Items.Count == 0)
        errors.Add("Order must have at least one item");
    foreach (var item in request.Items)
    {
        if (item.Quantity <= 0)
            errors.Add($"Item {item.ProductId} has invalid quantity");
        if (item.UnitPrice <= 0)
            errors.Add($"Item {item.ProductId} has invalid price");
    }
    if (errors.Count > 0)
        return Result<CreateOrderResponseDto>.Failure(HttpStatusCode.BadRequest)
            .WithErrors(errors);

    Order order = new()
    {
        Id = Guid.NewGuid(),
        CustomerName = request.CustomerName,
        Items = request.Items.Select(i => new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList(),
        CreatedAt = DateTime.UtcNow
    };

    await _orderRepository.AddAsync(order, ct);
    await _unitOfWork.SaveChangesAsync(ct);

    // ... more logic
    return Result<CreateOrderResponseDto>.Success(HttpStatusCode.Created)
        .WithPayload(order.ToCreateOrderResponseDto());
}
```

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

### Method size — ~30 lines max

If a method exceeds ~30 lines, extract private methods with descriptive names.

```csharp
// ❌ WRONG — 50-line ExecuteAsync
public async Task<Result<CreateProductResponseDto>> ExecuteAsync(...)
{
    // 10 lines of validation
    // 10 lines of entity creation
    // 10 lines of persistence
    // 10 lines of notification
    // 10 lines of response mapping
}

// ✅ CORRECT — composed of small, named steps
public async Task<Result<CreateProductResponseDto>> ExecuteAsync(
    CreateProductRequestDto request, CancellationToken ct)
{
    Product? product = CreateAndValidateProduct(request);
    if (product is null)
        return Result<CreateProductResponseDto>.Failure(HttpStatusCode.BadRequest)
            .WithErrors(_validationErrors);

    await PersistProductAsync(product, ct);

    return Result<CreateProductResponseDto>.Success(HttpStatusCode.Created)
        .WithPayload(product.ToCreateProductResponseDto());
}
```

### Nesting — 3 levels max, use early return

```csharp
// ❌ WRONG — 4 levels of nesting
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
                    // logic here
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

    // flat logic here
}
```

### Complexity — ~7 branches max per method

If a method has more than ~7 branches (if/switch/loops combined), extract methods or simplify.

```csharp
// ❌ WRONG — 12 branches in one method
if (status == Status.Active) { ... }
else if (status == Status.Pending) { ... }
else if (status == Status.Suspended) { ... }
else if (status == Status.Cancelled) { ... }
// ... 8 more branches

// ✅ CORRECT — extract each branch into its own method
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
