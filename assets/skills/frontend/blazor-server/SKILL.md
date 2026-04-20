---
name: blazor-server
description: >
  Blazor Server component and application standards for .NET 10 projects.
  Covers component structure, code-behind separation, state management, forms,
  signalr circuits, authentication, scoped state, and server-side rendering.
  Use when the frontend project uses Blazor Server (not WebAssembly).
---

# Blazor Server — TemperAI Standards

This skill provides Blazor Server-specific conventions.
Load this skill when `blazorType` is `server` in `.temper/frontend-config.md`.

For general Blazor conventions shared between Server and WASM, see `frontend/blazor`.

---

## When to use this skill

Load `frontend/blazor-server` when:
- The project uses Blazor Server (not Blazor WebAssembly)
- The `blazorType` in `.temper/frontend-config.md` is set to `server`

**Load `frontend/blazor` instead** when:
- The project uses Blazor WebAssembly
- The `blazorType` in `.temper/frontend-config.md` is set to `wasm`

---

## Key differences from Blazor WASM

| Aspect | Blazor Server | Blazor WASM |
|-------|--------------|------------|
| Execution | Server (SignalR) | Browser (WebAssembly) |
| HTTP | Direct use of services | Typed HttpClient |
| State | Circuit-scoped | Client-side |
| Authentication | Cookies/session | Token in headers |
| Offline support | No | Yes |
| Initial load | Fast (progressive) | Larger download |
| Connection | SignalR circuit | HTTP calls |

---

## Project structure

```
MyProjectServer/                   ← Server app root
├── MyProjectServer.sln
├── src/
│   ├── MyProjectServer/
│   │   ├── Components/
│   │   │   ├── Pages/
│   │   │   │   ├── Products/
│   │   │   │   │   ├── ProductsList.razor
│   │   │   │   │   └── ProductsList.razor.cs
│   │   │   │   ├── _Host.cshtml
│   │   │   │   └── Error.cshtml
│   │   │   └── Shared/
│   │   │       ├── MainLayout.razor
│   │   │       ├── MainLayout.razor.cs
│   │   │       └── NavMenu.razor
│   │   ├── Data/
│   │   │   └── ApplicationDbContext.cs
│   │   └── Program.cs
│   └── MyProjectServer.Client/       ← Optional: shared DTOs
└── tests/
```

### Key differences in structure

- Has `_Host.cshtml` entry point (not `index.html`)
- Has optional `.Client` project for shared DTOs
- No `wwwroot/` folder for static assets
- Has `Data/` for DbContext if using EF Core

---

## Program.cs setup

```csharp
// Program.cs - Blazor Server
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorComponents()
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });

builder.Services.AddMudServices();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// Configure
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddHubCircuitHub<Hub>();

app.Run();
```

---

## Circuit and connection state

### Circuit example

```csharp
// Components/Products/ProductsList.razor.cs
public sealed partial class ProductsList : IDisposable
{
    [Inject]
    private IProductService ProductService { get; set; } = default!;

    [Inject]
    private CircuitState CircuitState { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private List<ProductResponseDto> products = [];
    private bool isLoading;
    private string errorMessage = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        // Circuit lifecycle - runs on server connection
        CircuitState.OnCircuitClosed += HandleCircuitClosed;

        await LoadProductsAsync();
    }

    private async Task HandleCircuitClosed(CircuitClosedEventArgs args)
    {
        // Clean up when circuit closes
        // Unsubscribe from events, dispose services
    }

    private async Task LoadProductsAsync()
    {
        isLoading = true;
        errorMessage = string.Empty;

        try
        {
            // Direct service call - no HTTP client needed
            List<ProductResponseDto> result = await ProductService.GetAllAsync();
            products = result;
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

    public void Dispose()
    {
        CircuitState.OnCircuitClosed -= HandleCircuitClosed;
    }
}
```

### ConnectionState service

```csharp
// Services/ConnectionState.cs
public sealed class ConnectionState
{
    public string ConnectionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public Dictionary<string, object> Items { get; } = new();

    public event Action<CircuitClosedEventArgs>? OnCircuitClosed;
    public event Action? OnStateChanged;

    public void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }

    public void OnCircuitClosed(CircuitClosedEventArgs args)
    {
        OnCircuitClosed?.Invoke(args);
    }
}
```

---

## Authentication with cookies

### Program.cs with authentication

```csharp
// Program.cs
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

builder.Services.AddAuthorization();
```

### Login component

```razor
@page "/login"
@inject IAuthService AuthService
@inject NavigationManager Navigation

<EditForm Model="@model" OnValidSubmit="@HandleSubmitAsync">
    <MudTextField Label="Email" @bind-Value="model.Email" />
    <MudTextField Label="Password" @bind-Value="model.Password" InputType="InputType.Password" />

    <MudButton ButtonType="ButtonType.Submit" Variant="Variant.Filled" Color="Color.Primary">
        Login
    </MudButton>
</EditForm>

@code {
    private LoginRequestDto model = new();

    private async Task HandleSubmitAsync()
    {
        bool success = await AuthService.LoginAsync(model);

        if (success)
        {
            Navigation.NavigateTo("/", forceLoad: true);
        }
    }
}
```

### Authorized view

```razor
@attribute [Authorize]

@inject IAuthService AuthService
@inject AuthenticationState AuthState

<MudText>Welcome, @AuthState.User.Identity.Name</MudText>
```

---

## Dependency injection (Server-specific)

### Scoped vs Singleton

In Blazor Server, be careful with dependency injection:

```csharp
// Program.cs
// ✅ CORRECT: Scoped for circuit-scoped state
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ConnectionState>();

// ⚠️ CAUTION: Singleton shares state across all users
builder.Services.AddSingleton<GlobalState>();

// ✅ CORRECT: Scoped DbContext per request
builder.Services.AddDbContext<ApplicationDbContext>(...);
```

### Never inject HttpClient

```csharp
// ❌ WRONG - Blazor Server doesn't need HttpClient
[Inject]
private HttpClient Http { get; set; } = default!;

// ✅ CORRECT - Inject services directly
[Inject]
private IProductService ProductService { get; set; } = default!;
```

---

## Form handling (Server-specific)

```razor
@page "/products/create"
@inject IProductService ProductService
@inject NavigationManager Navigation

<EditForm Model="@model" OnValidSubmit="@HandleSubmitAsync">
    <FluentValidationValidator />

    <MudTextField Label="Name" @bind-Value="model.Name" For="@(() => model.Name)" />
    <MudTextField Label="Description" @bind-Value="model.Description" For="@(() => model.Description)" Lines="3" />
    <MudNumericField Label="Price" @bind-Value="model.Price" For="@(() => model.Price)" />

    <MudButton ButtonType="ButtonType.Submit" Variant="Variant.Filled" Color="Color.Primary">
        Create
    </MudButton>
</EditForm>

@code {
    private CreateProductRequestDto model = new();
    private bool isSubmitting;
    private string? successMessage;
    private string? errorMessage;

    private async Task HandleSubmitAsync()
    {
        isSubmitting = true;
        errorMessage = null;
        successMessage = null;

        try
        {
            // Direct call - no HTTP needed in Blazor Server
            ProductResponseDto created = await ProductService.CreateAsync(model);
            successMessage = "Product created successfully!";

            // Navigate after success
            await Task.Delay(1000); // Let user see success message
            Navigation.NavigateTo("/products");
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

---

## Error handling (Server-specific)

### Detailed errors

```csharp
// Program.cs - Enable detailed errors in development
builder.Services.AddRazorComponents()
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });
```

### Error boundary

```razor
@* Components/Shared/ErrorBoundary.razor *@
@attribute [CascadingCircuitParameter]

<ErrorBoundary>
    <ChildContent>
        @ChildContent
    </ChildContent>
    <ErrorContent>
        <div class="p-4">
            <MudAlert Severity="Severity.Error" Variant="Variant.Filled">
                <MudText>An error occurred in this circuit.</MudText>
                <MudText Class="mt-2">@Error?.Message</MudText>
            </MudAlert>

            <MudButton Variant="Variant.Outlined" Color="Color.Primary" OnClick="Recover" Class="mt-3">
                Try Again
            </MudButton>
        </div>
    </ErrorContent>
</ErrorBoundary>

@code {
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [CascadingParameter]
    private CircuitCircuitState? Error { get; set; }

    [Inject]
    private CircuitHander CircuitHandler { get; set; } = default!;

    private void Recover()
    {
        CircuitHandler.RestoreCircuit(Context);
    }
}
```

---

## State persistence (Server-specific)

### Persisting state with circuits

```csharp
// Services/CircuitStateService.cs
public sealed class CircuitStateService
{
    private readonly ConcurrentDictionary<string, object> _circuitData = new();

    public void SetData<T>(string circuitId, string key, T value)
    {
        _circuitData[$"{circuitId}:{key}"] = value!;
    }

    public T? GetData<T>(string circuitId, string key)
    {
        if (_circuitData.TryGetValue($"{circuitId}:{key}", out object? value))
        {
            return (T)value;
        }

        return default;
    }

    public void RemoveCircuitData(string circuitId)
    {
        IEnumerable<string> keys = _circuitData.Keys
            .Where(k => k.StartsWith(circuitId));

        foreach (string key in keys)
        {
            _circuitData.TryRemove(key, out _);
        }
    }
}
```

---

## Performance tips (Server-specific)

### Minimize circuit data transfer

```razor
@* ❌ BAD - Large data transferred on every render *@
@foreach (var item in HeavyCollection)
{
    <HeavyComponent Data="@item" />
}

@* ✅ GOOD - Use pagination or virtualization *@
<MudVirtualize Items="@PagedItems" Context="item">
    <LightComponent Data="@item" />
@* Use MudDataGrid with server-side pagination *@
<MudDataGrid ServerData="ReloadServerData" ...>
```

### Prerendering

Use prerendering for perceived performance:

```csharp
// Program.cs
builder.Services.AddRazorComponents()
    .AddInteractiveServerRenderMode()
    .AddPrerenderedRemoteValidation();
```

---

## SignalR configuration

### Configure SignalR

```csharp
// Program.cs
builder.Services.AddSignalR()
    .AddHubOptions<Hub>(options =>
    {
        options.MaximumReceiveMessageSize = 65536; // 64KB
        options.StreamBufferCapacity = 10;
    });
```

### Hub for custom logic

```csharp
// Hubs/ApplicationHub.cs
public sealed class ApplicationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        // Track connection
        var state = Context.Get<ConnectionState>();
        state.ConnectionId = Context.ConnectionId;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Clean up circuit data
        var state = Context.Get<ConnectionState>();
        // ...

        await base.OnDisconnectedAsync(exception);
    }
}
```

---

## Absolute rules

- **Never** inject `HttpClient` in Blazor Server — inject services directly
- **Always** implement `IDisposable` to clean up circuit resources
- **Never** use Singleton for user-scoped state — use Scoped or Circuit-scoped
- **Always** handle circuit disconnection gracefully
- **Always** use `[Authorize]`attribute for protected pages
- **Always** use cookies for authentication (not tokens)
- **Always** configure `DetailedErrors` only in development
- **Never** transfer large datasets — use pagination
- **Always** use `OnValidSubmit` for forms