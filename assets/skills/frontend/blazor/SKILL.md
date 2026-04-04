---
name: blazor
description: >
  Blazor WebAssembly component and application standards for .NET 10 projects.
  Covers component structure, code-behind separation, state management, forms,
  routing, API consumption, JavaScript interop, performance optimization,
  accessibility, security, and testing. Use when creating or modifying any
  Blazor component, page, or frontend service.
---

# Blazor WebAssembly — TemperAI Standards

TemperAI uses **Blazor WebAssembly** exclusively. The frontend runs entirely in the browser via WebAssembly and communicates with the backend API via HTTP.

## API and Frontend separation — never in the same solution

The API project and the Blazor Frontend project **must always be in separate solutions**. They are independent applications with independent lifecycles, build pipelines, and deployment targets.

```
TodoManagerApi/                          ← Backend API solution
├── TodoManagerApi.sln
├── src/
│   └── TodoManagerApi/
│       ├── Controllers/
│       ├── Middlewares/
│       └── Program.cs
└── tests/

TodoManagerFront/                        ← Blazor WASM frontend solution
├── TodoManagerFront.sln
├── src/
│   └── TodoManagerFront/
│       ├── Components/
│       ├── Services/
│       ├── wwwroot/
│       └── Program.cs
└── tests/
```

### Separation rules

- **Never** put the API and Frontend in the same `.sln` file.
- **Never** put the Frontend inside the API project folder or vice versa.
- **Always** create a separate solution for each — `TodoManagerApi.sln` and `TodoManagerFront.sln`.
- **Always** keep them as sibling directories at the same level — never nested.
- **Always** configure the Frontend's `ApiBaseUrl` in `appsettings.json` or `wwwroot/appsettings.json` to point to the running API.
- The Frontend communicates with the API **only via HTTP** — no shared projects, no project references.

---

## Project structure

```
src/
├── YourProject.Web/                  ← Blazor WASM client
│   ├── Components/
│   │   ├── Pages/
│   │   │   └── Products/
│   │   │       ├── ProductsList.razor
│   │   │       ├── ProductsList.razor.cs
│   │   │       ├── ProductDetail.razor
│   │   │       ├── ProductDetail.razor.cs
│   │   │       ├── ProductCreate.razor
│   │   │       └── ProductCreate.razor.cs
│   │   ├── Shared/
│   │   │   ├── MainLayout.razor
│   │   │   ├── MainLayout.razor.cs
│   │   │   ├── NavMenu.razor
│   │   │   ├── LoadingSpinner.razor
│   │   │   ├── ConfirmDialog.razor
│   │   │   └── DataTable.razor
│   │   └── Components/
│   │       └── Products/
│   │           ├── ProductCard.razor
│   │           ├── ProductForm.razor
│   │           └── ProductStatusBadge.razor
│   ├── Services/
│   │   ├── IProductService.cs
│   │   ├── ProductService.cs
│   │   └── ApiHttpClient.cs
│   ├── wwwroot/
│   │   ├── css/
│   │   ├── js/
│   │   └── images/
│   ├── _Imports.razor
│   ├── App.razor
│   └── Program.cs
│
└── YourProject.Api/                  ← Backend API
    ├── Controllers/
    ├── Middlewares/
    └── Program.cs
```

---

## Component naming

- Always use `PascalCase` — `ProductList.razor`, `OrderDetail.razor`.
- Pages are suffixed by purpose — `List`, `Detail`, `Create`, `Edit`.
- Shared components are descriptive — `LoadingSpinner`, `ConfirmDialog`, `DataTable`.
- Reusable domain components go in `Components/[Domain]/` — `Components/Products/ProductCard.razor`.
- Never prefix with `Blazor`, `Component`, or `Ctrl`.
- Never use abbreviations — `ProductList` not `ProdList`.

---

## Code-behind separation

- Always separate logic into `[ComponentName].razor.cs` when the component exceeds 50 lines.
- The `.razor` file contains only markup and minimal UI state bindings.
- The `.razor.cs` file contains the `partial class` with injection, lifecycle methods, and event handlers.
- Small, purely presentational components (under 20 lines, no logic) may keep code inline.

```csharp
// Components/Pages/Products/ProductsList.razor.cs
public sealed partial class ProductsList : IDisposable
{
    [Inject]
    private IProductService ProductService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private List<ProductResponseDto> products = [];
    private bool isLoading;
    private string errorMessage = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        isLoading = true;
        errorMessage = string.Empty;

        try
        {
            products = await ProductService.GetAllAsync();
        }
        catch (Exception ex)
        {
            errorMessage = "Failed to load products. Please try again.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void NavigateToCreate()
    {
        Navigation.NavigateTo("/products/create");
    }

    private void NavigateToDetail(Guid id)
    {
        Navigation.NavigateTo($"/products/{id}");
    }

    public void Dispose()
    {
        // Unsubscribe from events, dispose timers, cancel pending operations
    }
}
```

---

## Component lifecycle

### OnInitializedAsync
Called once when the component is first instantiated. Use for initial data loading.

### OnParametersSetAsync
Called when parameters change. Use to react to parameter changes.

### OnAfterRenderAsync
Called after rendering. Use for JS interop or DOM manipulation. Always check `firstRender`.

### Lifecycle rules

- Never call `StateHasChanged()` inside `OnInitializedAsync` or `OnParametersSetAsync` — Blazor renders automatically.
- Only call `StateHasChanged()` when external events update state (timer callbacks, JS interop callbacks).
- Always check `firstRender` in `OnAfterRenderAsync` before running one-time initialization.
- Never perform blocking operations in lifecycle methods — always use async.
- Always implement `IDisposable` or `IAsyncDisposable` when the component subscribes to events, uses timers, or holds JS references.
- Always unsubscribe from events in `Dispose()` to prevent memory leaks.

---

## Dependency injection

- Always use `[Inject]` — never constructor injection in components.
- Always inject typed services — never inject `HttpClient` directly in pages.
- Always use `= default!` to suppress nullable warnings on injected properties.

```csharp
public sealed partial class ProductsList
{
    [Inject]
    private IProductService ProductService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;
}
```

### Service registration

```csharp
// Program.cs
builder.Services.AddHttpClient<ApiHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
```

### Service rules

- Services are always `sealed class`.
- Services always implement an interface.
- Services never depend on Blazor-specific types (`NavigationManager`, `IJSRuntime`) — keep them pure for testability.
- Services always accept `CancellationToken` on async methods.

---

## State management

### Level 1 — Component state (simple)

Use component fields for state that only matters within a single component.

```csharp
private List<ProductResponseDto> products = [];
private bool isLoading;
private string searchTerm = string.Empty;
```

### Level 2 — Parent-child communication (moderate)

Use parameters and `EventCallback` for communication between parent and child components.

```csharp
// Child component
[Parameter]
public ProductResponseDto Product { get; set; } = default!;

[Parameter]
public EventCallback<Guid> OnDelete { get; set; }

private async Task HandleDeleteAsync()
{
    await OnDelete.InvokeAsync(Product.Id);
}
```

### Level 3 — Cascading parameters (shared context)

Use cascading parameters for values that many components in a subtree need (theme, culture, current user).

```csharp
// Parent (layout or page)
<CascadingValue Value="@currentUser">
    <ProductsList />
</CascadingValue>

// Child component
[CascadingParameter]
private UserResponseDto? CurrentUser { get; set; }
```

### Level 4 — Services (application-wide state)

Use scoped services for state shared across unrelated components.

```csharp
// Services/SessionState.cs
public sealed class SessionState : IDisposable
{
    public UserResponseDto? CurrentUser { get; set; }
    public string SelectedCulture { get; set; } = "en-US";

    public event Action? OnChange;

    public void SetUser(UserResponseDto user)
    {
        CurrentUser = user;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }

    public void Dispose()
    {
        OnChange = null;
    }
}
```

### Level 5 — Fluxor (complex state)

Use Fluxor (Redux pattern) only when:
- Multiple unrelated components need to read and write the same state.
- State changes need to be tracked, replayed, or debugged.
- The application has complex state transitions.

Do not use Fluxor for simple CRUD applications.

---

## Communication between components

| Pattern | Use case |
|---|---|
| `[Parameter]` | Parent to child data flow |
| `EventCallback` | Child to parent events |
| Cascading parameters | Deep tree shared values |
| Scoped service with events | Sibling to sibling |
| LocalStorage/SessionStorage | Persistent client state |

### Parameter rules

- Always provide a default value — `= default!` for reference types, `= 0` for numbers.
- Never mutate parameters inside a child component — parameters are owned by the parent.
- Use `EventCallback` instead of `Action` or `Func` — `EventCallback` properly triggers re-rendering.

---

## Forms and validation

### EditForm with FluentValidation (preferred)

```csharp
// Components/Pages/Products/ProductCreate.razor.cs
public sealed partial class ProductCreate
{
    [Inject]
    private IProductService ProductService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private CreateProductRequestDto model = new();
    private bool isSubmitting;
    private string errorMessage = string.Empty;
    private string successMessage = string.Empty;

    private async Task HandleSubmitAsync()
    {
        isSubmitting = true;
        errorMessage = string.Empty;

        try
        {
            await ProductService.CreateAsync(model);
            successMessage = "Product created successfully.";
            model = new CreateProductRequestDto();
        }
        catch (Exception ex)
        {
            errorMessage = "Failed to create product. Please try again.";
        }
        finally
        {
            isSubmitting = false;
        }
    }
}
```

```razor
@page "/products/create"

<EditForm Model="@model" OnValidSubmit="@HandleSubmitAsync">
    <FluentValidationValidator />

    <div class="form-group">
        <label for="name">Name</label>
        <InputText id="name" @bind-Value="model.Name" class="form-control" />
        <ValidationMessage For="@(() => model.Name)" />
    </div>

    <div class="form-group">
        <label for="price">Price</label>
        <InputNumber id="price" @bind-Value="model.Price" class="form-control" />
        <ValidationMessage For="@(() => model.Price)" />
    </div>

    <button type="submit" disabled="@isSubmitting">
        @(isSubmitting ? "Creating..." : "Create Product")
    </button>
</EditForm>

@if (!string.IsNullOrWhiteSpace(successMessage))
{
    <div class="alert alert-success">@successMessage</div>
}

@if (!string.IsNullOrWhiteSpace(errorMessage))
{
    <div class="alert alert-danger">@errorMessage</div>
}
```

### Form rules

- Always disable the submit button while processing.
- Always show loading state during submission.
- Always display validation errors next to the relevant field.
- Always show a success or error message after submission.
- Always reset the form model after successful submission.
- Always use `OnValidSubmit` — never `OnSubmit` (bypasses validation).
- Always use FluentValidation — never DataAnnotations on DTOs.

---

## Routing

- Always use `@page` directive with the exact route from the design document.
- Always use route parameters with the correct type — `@page "/products/{id:guid}"`.
- Always handle missing or invalid route parameters gracefully.
- Always use `NavigationManager.NavigateTo()` for programmatic navigation — never `<a href>` for internal routes.

```razor
@page "/products"
@page "/products/{id:guid}"

@code {
    [Parameter]
    public Guid? Id { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (Id.HasValue)
        {
            product = await ProductService.GetByIdAsync(Id.Value);
        }
    }
}
```

---

## Consuming the API — typed HttpClient

Never use raw `HttpClient` in components. Always create a typed service with proper error handling.

```csharp
// Services/ApiHttpClient.cs
public sealed class ApiHttpClient
{
    private readonly HttpClient _httpClient;

    public ApiHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProductResponseDto>> GetProductsAsync(
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.GetAsync("api/products", cancellationToken);
        response.EnsureSuccessStatusCode();

        List<ProductResponseDto> products = await response.Content.ReadFromJsonAsync<List<ProductResponseDto>>(
            cancellationToken: cancellationToken);

        return products ?? [];
    }

    public async Task<ProductResponseDto> GetProductByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"api/products/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ProductNotFoundException(id);
        }

        response.EnsureSuccessStatusCode();

        ProductResponseDto product = await response.Content.ReadFromJsonAsync<ProductResponseDto>(
            cancellationToken: cancellationToken);

        return product ?? throw new InvalidOperationException("Unexpected null response");
    }

    public async Task<ProductResponseDto> CreateProductAsync(
        CreateProductRequestDto request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/products", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        ProductResponseDto product = await response.Content.ReadFromJsonAsync<ProductResponseDto>(
            cancellationToken: cancellationToken);

        return product ?? throw new InvalidOperationException("Unexpected null response");
    }
}
```

### API service rules

- Services always implement an interface.
- Services always accept `CancellationToken`.
- Services never catch and swallow exceptions — let them bubble up to the component.
- Services never reference Blazor types — keep them testable with plain unit tests.
- Services always return typed results — never `HttpResponseMessage` or `JsonElement`.
- Always throw custom exceptions for known error cases (NotFound, Conflict) so components can handle them gracefully.

---

## Performance optimization

### Virtualization for large lists

Always use `<Virtualize>` for lists with more than 50 items.

```razor
<Virtualize Items="@products" Context="product">
    <ProductCard Product="@product" />
</Virtualize>
```

### Prevent unnecessary renders

- Use `ShouldRender()` to conditionally skip re-rendering when state changes don't affect the UI.
- Use `@key` on list items to help Blazor track element identity — especially important for reorderable lists.

```razor
@foreach (var product in products)
{
    <ProductCard @key="product.Id" Product="@product" />
}
```

### Lazy load assemblies

For large WASM applications, lazy load feature assemblies to reduce initial download size.

```csharp
// Program.cs
builder.Services.AddLazyAssemblyLoader();
```

### Debounce search inputs

Never fire API calls on every keystroke. Use debounce for search inputs.

```csharp
private System.Timers.Timer? _debounceTimer;
private string _searchTerm = string.Empty;

private void OnSearchInput(ChangeEventArgs args)
{
    _searchTerm = args.Value?.ToString() ?? string.Empty;

    _debounceTimer?.Stop();
    _debounceTimer?.Dispose();

    _debounceTimer = new System.Timers.Timer(300);
    _debounceTimer.Elapsed += async (_, _) =>
    {
        _debounceTimer.Stop();
        await InvokeAsync(async () =>
        {
            await SearchProductsAsync(_searchTerm);
            StateHasChanged();
        });
    };
    _debounceTimer.AutoReset = false;
    _debounceTimer.Start();
}
```

### WASM-specific optimizations

- Enable AOT compilation for production builds — significantly improves runtime performance.
- Use `PublishTrimmed=true` with `TrimMode=link` — reduces download size by 60-80%.
- Enable Brotli compression on the server for `.dll` and `.wasm` files.
- Preload critical assemblies with `<link rel="preload">` in `index.html`.

---

## Error boundaries

Always wrap page content in error boundaries to catch rendering exceptions.

```razor
// Shared/ErrorBoundary.razor
<ErrorBoundary>
    <ChildContent>@ChildContent</ChildContent>
    <ErrorContent>
        <div class="alert alert-danger">
            <p>Something went wrong while rendering this component.</p>
            <button @onclick="Recover">Try again</button>
        </div>
    </ErrorContent>
</ErrorBoundary>

@code {
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private ErrorBoundary? _errorBoundary;

    private void Recover()
    {
        _errorBoundary?.Recover();
    }
}
```

---

## Accessibility (a11y)

- Always use semantic HTML — `<button>` for actions, `<a>` for navigation, `<label>` for inputs.
- Always associate `<label>` with inputs using `for` attribute matching the input `id`.
- Always provide `alt` text on images — use empty `alt=""` for decorative images.
- Always use `aria-*` attributes for dynamic content — `aria-live="polite"` for status messages, `aria-busy="true"` during loading.
- Always ensure keyboard navigation works — test with Tab key.
- Always maintain sufficient color contrast — minimum 4.5:1 for normal text.
- Never use `onclick` on non-interactive elements — use `<button>` or `<a>`.

```razor
<!-- GOOD -->
<button @onclick="HandleDelete" aria-label="Delete product">
    <i class="icon-trash" aria-hidden="true"></i>
</button>

<!-- BAD -->
<div @onclick="HandleDelete">Delete</div>
```

---

## CSS isolation

Always use CSS isolation for component-specific styles. Create `[ComponentName].razor.css` alongside the component.

```css
/* Components/Products/ProductCard.razor.css */
.product-card {
    border: 1px solid #e0e0e0;
    border-radius: 8px;
    padding: 16px;
    transition: box-shadow 0.2s ease;
}

.product-card:hover {
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}
```

### CSS isolation rules

- Never use global CSS for component-specific styles.
- Use `::deep` selector only when styling child components you don't control.
- Keep global CSS in `wwwroot/css/app.css` minimal — only resets, typography, and utility classes.
- Always use CSS variables for theme colors — `var(--primary-color)`.

---

## JavaScript interop

Use JS interop only when Blazor cannot do it natively (charts, file uploads, clipboard, browser APIs).

### JS interop rules

- Always wrap JS calls in `try/catch` — JS errors should not crash the Blazor app.
- Always check `firstRender` before initializing JS libraries.
- Always dispose of JS references in `DisposeAsync()` when the component is removed.
- Keep JS interop code in a dedicated service — never call `JSRuntime` directly from pages.
- Always use `IJSObjectReference` with module imports — never call global JS functions.
- Always mark JS-exposed methods with `[JSInvokable]` and use unique identifiers.

```csharp
// Services/ChartService.cs
public sealed class ChartService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    public ChartService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        if (_module is null)
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./js/chart.js");
        }

        return _module;
    }

    public async Task InitializeAsync(string elementId, object data)
    {
        IJSObjectReference module = await GetModuleAsync();
        await module.InvokeVoidAsync("initializeChart", elementId, data);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }
}
```

---

## Security

- Never trust client-side data — always validate on the server.
- Never store sensitive data in localStorage — use sessionStorage or secure cookies.
- Always sanitize user input before rendering — Blazor automatically HTML-encodes `@variable` output.
- Never use `@((MarkupString)html)` with user-provided content — XSS vulnerability.
- Always include authentication tokens in API requests via a delegating handler.
- Always validate route parameters — don't assume `Guid` parameters are valid.

```csharp
// Services/AuthMessageHandler.cs
public sealed class AuthMessageHandler : DelegatingHandler
{
    private readonly IAuthService _authService;

    public AuthMessageHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string token = await _authService.GetTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
```

---

## Testing

### Component tests with bUnit

```csharp
public sealed class ProductsListTests : TestContext
{
    [Fact]
    public void Render_WithProducts_DisplaysProductNames()
    {
        List<ProductResponseDto> products = new()
        {
            new() { Id = Guid.NewGuid(), Name = "Product A", Price = 10m, Status = "Active" },
            new() { Id = Guid.NewGuid(), Name = "Product B", Price = 20m, Status = "Active" }
        };

        ProductsList cut = RenderComponent<ProductsList>(parameters => parameters
            .Add(p => p.Products, products));

        cut.Markup.Contains("Product A");
        cut.Markup.Contains("Product B");
    }

    [Fact]
    public void Render_WithNoProducts_DisplaysEmptyMessage()
    {
        ProductsList cut = RenderComponent<ProductsList>(parameters => parameters
            .Add(p => p.Products, new List<ProductResponseDto>()));

        cut.Markup.Contains("No products found");
    }
}
```

### Service tests

```csharp
public sealed class ProductServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsProducts()
    {
        MockHttpMessageHandler mockHttp = new();
        mockHttp.When("*/api/products")
            .Respond("application/json", "[{\"id\":\"00000000-0000-0000-0000-000000000001\",\"name\":\"Test\"}]");

        HttpClient httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost:5001/");

        ApiHttpClient apiClient = new(httpClient);
        ProductService service = new(apiClient);

        List<ProductResponseDto> products = await service.GetAllAsync();

        Assert.Single(products);
        Assert.Equal("Test", products[0].Name);
    }
}
```

---

## Absolute rules

- Never put business logic in components — components only orchestrate UI and call services.
- Never use `async void` — always `async Task`.
- Never use `.Result` or `.Wait()` — causes deadlocks.
- Never use primary constructors — always explicit constructors.
- Never use return expression `=>` on methods — always braces `{}`.
- Never use `using static` — always use explicit `using` directives with the namespace, then reference types by their name.
- Never use named usings — rename the entity or use fully qualified namespace instead.
- Never use `var` — always declare the explicit type.
- Never inject `HttpClient` directly in components — always use a typed service.
- Never instantiate `HttpClient` manually — always inject it through DI.
- Never use `OnSubmit` on EditForm — always use `OnValidSubmit`.
- Never use `@((MarkupString)html)` with user-provided content — XSS vulnerability.
- Never trust client-side data — always validate on the server.
- Never store sensitive data in localStorage.
- Always use `[Inject]` attribute for dependency injection — never constructor injection in components.
- Always handle loading, empty, and error states in every component that fetches data.
- Always use `CancellationToken` on async service methods.
- Always separate logic into code-behind when the component exceeds 50 lines.
- Always use `sealed` on service classes and component code-behind classes that are not inherited.
- Always use `@key` on list items in `@foreach` loops.
- Always use `<Virtualize>` for lists with more than 50 items.
- Always use CSS isolation for component-specific styles.
- Always wrap page content in error boundaries.
- Always use semantic HTML and proper ARIA attributes.
- Always debounce search inputs before firing API calls.
- Always place components in the correct folder: `Pages/` for routable pages, `Shared/` for layout elements, `Components/` for reusable domain components.
