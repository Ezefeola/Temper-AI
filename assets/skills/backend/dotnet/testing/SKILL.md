---
name: dotnet-testing
description: >
  Testing standards for .NET 10 projects using xUnit, Moq, and bUnit.
  Covers unit tests for domain and application layers, integration tests
  for APIs, and component tests for Blazor. Use when creating or modifying
  any test files.
---

# Testing — TemperAI Standards

## Test project structure

```
tests/
├── ProjectName.Domain.UnitTests/
│   ├── Entities/
│   │   └── ProductTests.cs
│   └── ValueObjects/
│       └── MoneyTests.cs
├── ProjectName.Application.UnitTests/
│   └── UseCases/
│       └── Products/
│           ├── CreateProductTests.cs
│           └── UpdateProductTests.cs
└── ProjectName.Api.IntegrationTests/
    └── ProductsApiTests.cs
```

## Test naming convention

**Always** use this format: `MethodBeingTested_Scenario_ExpectedResult`

Examples:
- `Create_ValidNameAndPrice_ReturnsSuccess`
- `Create_EmptyName_ReturnsError`
- `UpdateName_SameName_ReturnsNotUpdated`
- `UpdateName_NewNameExceedsMaxLength_ReturnsError`

## Entity tests

For each entity, test:

- Factory method happy path — valid inputs return `(empty errors, entity)`.
- Factory method validation failures — each validation rule has a test.
- Update method happy path — valid update returns `(empty errors, true)`.
- Update method no-change — same value returns `(empty errors, false)`.
- Update method validation failures — each validation rule has a test.

```csharp
public sealed class ProductTests
{
    [Fact]
    public void Create_ValidNameAndPrice_ReturnsSuccess()
    {
        (List<string> errors, Product? product) = Product.Create("Test Product", "A test product", 19.99m);

        Assert.Empty(errors);
        Assert.NotNull(product);
        Assert.Equal("Test Product", product.Name);
        Assert.Equal(19.99m, product.Price);
        Assert.Equal(ProductStatus.Active, product.Status);
    }

    [Fact]
    public void Create_EmptyName_ReturnsError()
    {
        (List<string> errors, Product? product) = Product.Create("", "A test product", 19.99m);

        Assert.NotEmpty(errors);
        Assert.Null(product);
        Assert.Contains(errors, e => e.Contains("name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UpdateName_ValidNewName_ReturnsUpdated()
    {
        (_, Product? product) = Product.Create("Original", "Description", 10m);

        (List<string> errors, bool updated) = product!.UpdateName("Updated Name");

        Assert.Empty(errors);
        Assert.True(updated);
        Assert.Equal("Updated Name", product.Name);
    }

    [Fact]
    public void UpdateName_SameName_ReturnsNotUpdated()
    {
        (_, Product? product) = Product.Create("Original", "Description", 10m);

        (List<string> errors, bool updated) = product!.UpdateName("Original");

        Assert.Empty(errors);
        Assert.False(updated);
    }
}
```

## Use case tests

For each use case, test:

- Happy path — valid input returns success with expected response.
- Validation failures — invalid input returns failure with appropriate errors.
- Business rule violations — e.g., duplicate entity, not found, conflict.
- Edge cases from spec.md — every edge case listed in the spec must have a test.

Use mocks for repositories and external services.

```csharp
public sealed class CreateProductTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly CreateProduct _createProduct;

    public CreateProductTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _createProduct = new CreateProduct(_unitOfWorkMock.Object, _eventPublisherMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_ReturnsCreated()
    {
        CreateProductRequestDto requestDto = new()
        {
            Name = "Test Product",
            Description = "A test product",
            Price = 19.99m
        };

        _unitOfWorkMock
            .Setup(u => u.ProductRepository.ExistsByNameAsync("Test Product", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaveResult { IsSuccess = true, RowsAffected = 1 });

        Result<CreateProductResponseDto> result = await _createProduct.ExecuteAsync(requestDto);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Created, result.HttpStatusCode);
        Assert.NotNull(result.Payload);
        Assert.Equal("Test Product", result.Payload.Name);

        _eventPublisherMock.Verify(
            e => e.PublishAsync(It.Is<ProductCreatedEvent>(ev => ev.ProductName == "Test Product"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateName_ReturnsConflict()
    {
        CreateProductRequestDto requestDto = new()
        {
            Name = "Existing Product",
            Description = "A test product",
            Price = 19.99m
        };

        _unitOfWorkMock
            .Setup(u => u.ProductRepository.ExistsByNameAsync("Existing Product", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<CreateProductResponseDto> result = await _createProduct.ExecuteAsync(requestDto);

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Conflict, result.HttpStatusCode);
    }
}
```

## Mocking conventions

- Always name mock variables with `Mock` suffix — `_unitOfWorkMock`, `_eventPublisherMock`.
- Always pass `It.IsAny<CancellationToken>()` for cancellation token parameters.
- Always verify that domain events are published when expected.
- Always set up the complete chain of dependencies.

## Test class structure

- Always use `sealed class` for test classes.
- Always name the test class after the class being tested with `Tests` suffix — `ProductTests`, `CreateProductTests`.
- Always use the `[Fact]` attribute for single-case tests.
- Always use the `[Theory]` attribute with `[InlineData]` for parameterized tests.
- Always arrange tests in Given/When/Then order within the method body.

## Integration tests

For API integration tests, use `WebApplicationFactory<T>`:

```csharp
public sealed class ProductsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProductsApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProduct_ValidRequest_ReturnsCreated()
    {
        CreateProductRequestDto request = new()
        {
            Name = "Test Product",
            Description = "Description",
            Price = 19.99m
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CreateProductResponseDto product = await response.Content.ReadFromJsonAsync<CreateProductResponseDto>();
        Assert.NotNull(product);
        Assert.Equal("Test Product", product.Name);
    }
}
```

## Rules

For general C# conventions (syntax, usings, naming, async, DTOs), see `dotnet-csharp`.

- Never skip testing error paths — every validation must have a test.
- Never write tests that depend on non-deterministic behavior.
- Always test edge cases defined in the spec document.
- Always follow the naming convention: `Method_Scenario_Result`.
