---
name: mudblazor
description: >
  MudBlazor component library standards for Blazor WebAssembly .NET 10 projects.
  Covers MudBlazor setup, component usage, theming, form integration,
  and coexistence with Tailwind CSS. Use when the frontend project
  uses MudBlazor for UI components.
---

# MudBlazor — TemperAI Standards for Blazor

This skill provides MudBlazor standards for Blazor WebAssembly projects.
Load this skill when the frontend project uses MudBlazor for UI components.

For general Blazor conventions, see `frontend/blazor`.
For styling with Tailwind, see `frontend/tailwind`.

---

## When to use this skill

Load `frontend/mudblazor` when:
- The project uses MudBlazor as the UI component library
- You need to configure MudBlazor theming
- You're building forms, grids, or dialogs with MudBlazor components

**Do NOT load** if the project uses plain Blazor components or Tailwind only.

---

## Project setup

### Install MudBlazor

```bash
dotnet add package MudBlazor
```

### Configure Program.cs

```csharp
// Program.cs
builder.Services.AddMudServices();
```

### Configure _Imports.razor

```razor
@using MudBlazor
```

### Configure App.razor

```razor
<MudThemeProvider />
<MudPopoverProvider />

<CascadingValue Value="@MudAuthenticationState">
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
            <FocusOnNavigate RouteContext="@routeData" RoutePath="@routeData.Uri" />
        </Found>
        <NotFound>
            <PageTitle>Not found</PageTitle>
            <LayoutView Layout="@typeof(MainLayout)">
                <p>Sorry, there's nothing at this address.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingValue>
```

### Configure MainLayout

```razor
@inject MudThemeProvider ThemeProvider

<MudLayout>
    <MudAppBar>
        <MudIconButton Icon="@Icons.Material.Filled.Menu" Edge="EdgeMode.Start" OnClick="@((_) => DrawerToggle())" />
        <MudText Typo="Typo.h6">My Application</MudText>
    </MudAppBar>
    <MudDrawer @bind-Open="@DrawerOpen">
        <NavMenu />
    </MudDrawer>
    <MudMainContent>
        <MudContainer>
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>

@code {
    private bool DrawerOpen = true;

    private void DrawerToggle()
    {
        DrawerOpen = !DrawerOpen;
    }
}
```

---

## Theming

### Custom theme

```csharp
// Theming/CustomTheme.cs
public static class CustomTheme
{
    public static MudTheme Create()
    {
        MudTheme theme = new()
        {
            Palette = new Palette
            {
                Primary = Colors.Indigo.Primary,
                Secondary = Colors.DeepPurple.Secondary,
                AppbarBackground = Colors.Indigo.Primary,
                Background = Colors.Gray.Lighten5,
            },
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = new[] { "Inter", "sans-serif" },
                }
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "8px",
            }
        };

        return theme;
    }
}
```

### Apply custom theme

```csharp
// Program.cs
builder.Services.AddMudServices();
builder.Services.AddSingleton<ITaskScheduler, TaskScheduler>(sp => new TaskScheduler(sp.GetService<IBoxer>(), sp.GetService<IConsumerRegistry>()));

// Configure theme
builder.Services.AddSingleton<IMudThemeProvider>(sp =>
{
    MudThemeProvider provider = new()
    {
        Theme = CustomTheme.Create(),
    };
    return provider;
});
```

---

## Component usage patterns

### MudButton

```razor
<MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="HandleClick">
    Click Me
</MudButton>

<MudButton Variant="Variant.Outlined" Color="Color.Secondary" StartIcon="@Icons.Material.Filled.Add">
    Add Item
</MudButton>

<MudButton Variant="Variant.Text" Color="Color.Error" Disabled="true">
    Disabled
</MudButton>
```

### MudIconButton

```razor
<MudIconButton Icon="@Icons.Material.Filled.Edit" Color="Color.Primary" OnClick="HandleEdit" />
<MudIconButton Icon="@Icons.Material.Filled.Delete" Color="Color.Error" OnClick="HandleDelete" />
```

---

## Forms with FluentValidation

### Form with validation

```razor
@inject IProductService ProductService

<EditForm Model="@model" OnValidSubmit="@HandleSubmitAsync">
    <FluentValidationValidator />

    <MudCard>
        <MudCardContent>
            <MudTextField Label="Name"
                          @bind-Value="model.Name"
                          For="@(() => model.Name)"
                          Immediate="true" />

            <MudTextField Label="Description"
                          @bind-Value="model.Description"
                          For="@(() => model.Description)"
                          Lines="3" />

            <MudNumericField Label="Price"
                            @bind-Value="model.Price"
                            For="@(() => model.Price)"
                            Format="F2" />

            <MudSelect Label="Category"
                      @bind-Value="model.Category"
                      For="@(() => model.Category)">
                <MudSelectItem Value="@("Electronics")">Electronics</MudSelectItem>
                <MudSelectItem Value="@("Clothing")">Clothing</MudSelectItem>
                <MudSelectItem Value="@("Books")">Books</MudSelectItem>
            </MudSelect>
        </MudCardContent>

        <MudCardActions>
            <MudButton ButtonType="ButtonType.Submit"
                       Variant="Variant.Filled"
                       Color="Color.Primary"
                       Disabled="isSubmitting">
                @(isSubmitting ? "Saving..." : "Save")
            </MudButton>
        </MudCardActions>
    </MudCard>
</EditForm>

@code {
    private CreateProductRequestDto model = new();
    private bool isSubmitting;

    private async Task HandleSubmitAsync()
    {
        isSubmitting = true;

        try
        {
            await ProductService.CreateAsync(model);
            Snackbar.Add("Product created successfully", Severity.Success);
            model = new CreateProductRequestDto();
        }
        catch (Exception ex)
        {
            Snackbar.Add("Failed to create product", Severity.Error);
        }
        finally
        {
            isSubmitting = false;
        }
    }
}
```

### Form validation rules

```csharp
// FluentValidation/ProductValidator.cs
using FluentValidation;

public sealed class ProductValidator : AbstractValidator<CreateProductRequestDto>
{
    public ProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(100)
            .WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than 0")
            .LessThan(100000)
            .WithMessage("Price must be less than 100,000");
    }
}
```

---

## DataGrid patterns

### MudDataGrid

```razor
<MudDataGrid Items="@products"
            Sortable="true"
            Filterable="true"
            QuickFilter="@QuickFilter"
            Bordered="true"
            Dense="true">
    <ToolBarContent>
        <MudText Typo="Typo.h6">Products</MudText>
        <MudSpacer />
        <MudTextField @bind-Value="searchString"
                     Placeholder="Search..."
                     Adornment="Adornment.Start"
                     Immediate="true"
                     AdornmentIcon="@Icons.Material.Filled.Search"
                     IconSize="Size.Medium" />
    </ToolBarContent>

    <Columns>
        <TemplateColumn Title="Actions" Sortable="false">
            <CellTemplate>
                <MudIconButton Icon="@Icons.Material.Filled.Edit"
                             Color="Color.Primary"
                             OnClick="@(() => NavigateToEdit(context.Item))" />
                <MudIconButton Icon="@Icons.Material.Filled.Delete"
                             Color="Color.Error"
                             OnClick="@(() => HandleDelete(context.Item))" />
            </CellTemplate>
        </TemplateColumn>

        <TemplateColumn Title="Name" SortBy="x => x.Name">
            <CellTemplate>
                <MudHighlighter Text="@context.Item.Name"
                                HighlightedText="@searchString" />
            </CellTemplate>
        </TemplateColumn>

        <PropertyColumn Title="Price" Property="x => x.Price" Format="C" />
        <PropertyColumn Title="Category" Property="x => x.Category" />
        <TemplateColumn Title="Status">
            <CellTemplate>
                <MudChip Color="@GetStatusColor(context.Item.Status)"
                         Size="Size.Small">
                    @context.Item.Status
                </MudChip>
            </CellTemplate>
        </TemplateColumn>
    </Columns>

    <NoRecordsContent>
        <MudText>No products found</MudText>
    </NoRecordsContent>

    <LoadingContent>
        <MudText>Loading products...</MudText>
    </LoadingContent>
</MudDataGrid>

@code {
    private List<ProductResponseDto> products = [];
    private string searchString = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        products = await ProductService.GetAllAsync();
    }

    private Func<ProductResponseDto, bool> QuickFilter => product =>
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;

        return product.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
               product.Category.Contains(searchString, StringComparison.OrdinalIgnoreCase);
    };

    private Color GetStatusColor(string status)
    {
        return status switch
        {
            "Active" => Color.Success,
            "Inactive" => Color.Default,
            "Pending" => Color.Warning,
            _ => Color.Default,
        };
    }
}
```

---

## Dialog patterns

### Simple dialog

```razor
@code {
    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    [Parameter]
    public string ItemName { get; set; } = string.Empty;

    private void Cancel()
    {
        MudDialog?.Cancel();
    }

    private void Confirm()
    {
        MudDialog?.Close(DialogResult.Ok(true));
    }
}

<MudDialog>
    <DialogContent>
        <MudText>Confirm deletion of @ItemName?</MudText>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Error" OnClick="Confirm">Delete</MudButton>
    </DialogActions>
</MudDialog>
```

### Open dialog from component

```csharp
[Inject]
private IDialogService? DialogService { get; set; } = default!;

private async Task HandleDelete(ProductResponseDto product)
{
    DialogParameters parameters = new()
    {
        { "ItemName", product.Name },
    };

    IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(
        "Delete Product",
        parameters);

    DialogResult result = await dialog.Result;

    if (result.Canceled is false)
    {
        await ProductService.DeleteAsync(product.Id);
        Snackbar.Add("Product deleted", Severity.Success);
    }
}
```

---

## Snackbar (notifications)

### Configure Program.cs

```csharp
// Program.cs
builder.Services.AddMudServices();

// No additional configuration needed - Snackbar is included in AddMudServices()
```

### Use in component

```csharp
[Inject]
private ISnackbar Snackbar { get; set; } = default!;

// Show success
Snackbar.Add("Product saved successfully", Severity.Success);

// Show error
Snackbar.Add("Failed to save product", Severity.Error);

// Show warning
Snackbar.Add("Warning message", Severity.Warning);

// Show info
Snackbar.Add("Info message", Severity.Info);
```

### Configuration

```csharp
// Program.cs - customize snackbar
builder.Services.AddMudServices();
builder.Services.AddTransient<ISnackbar, SnackbarService>(sp => new SnackbarService(
    sp.GetService<IMudPopoverService>(),
    sp.GetService<IBoxer>(),
    sp.GetService<NavigationManager>())
{
    PositionClass = Defaults.Classes.Position.TopEnd,
    ShowTransitionDuration = 200,
    HideTransitionDuration = 200,
});
```

---

## Card patterns

```razor
<MudCard>
    <MudCardMedia Image="/images/card-image.jpg" Height="200" />
    <MudCardContent>
        <MudText Typo="Typo.h5">Product Name</MudText>
        <MudText Typo="Typo.body2">Product description goes here.</MudText>
        <MudText Typo="Typo.caption">Added on: Jan 1, 2024</MudText>
    </MudCardContent>
    <MudCardActions>
        <MudButton Variant="Variant.Text" Color="Color.Primary">Share</MudButton>
        <MudButton Variant="Variant.Text" Color="Color.Primary">Learn More</MudButton>
    </MudCardActions>
</MudCard>
```

---

## Table patterns ( MudTable for simpler cases)

```razor
<MudTable Items="@products" Hover="true" Bordered="true">
    <HeaderContent>
        <MudTh>Name</MudTh>
        <MudTh>Price</MudTh>
        <MudTh>Actions</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd>@context.Name</MudTd>
        <MudTd>@context.Price.ToString("C")</MudTd>
        <MudTd>
            <MudIconButton Icon="@Icons.Material.Filled.Edit"
                           Color="Color.Primary"
                           OnClick="@(() => NavigateToEdit(context))" />
            <MudIconButton Icon="@Icons.Material.Filled.Delete"
                           Color="Color.Error"
                           OnClick="@(() => HandleDelete(context))" />
        </MudTd>
    </RowTemplate>
    <NoRecordsContent>
        <MudText>No products found</MudText>
    </NoRecordsContent>
</MudTable>
```

---

## Tabs

```razor
<MudTabs>
    <MudTabPanel Text="Products" Icon="@Icons.Material.Filled.List">
        <ProductsList />
    </MudTabPanel>
    <MudTabPanel Text="Categories" Icon="@Icons.Material.Filled.Category">
        <CategoriesList />
    </MudTabPanel>
    <MudTabPanel Text="Settings" Icon="@Icons.Material.Filled.Settings">
        <Settings />
    </MudTabPanel>
</MudTabs>
```

---

## Loading and states

### Loading button

```razor
<MudButton Variant="Variant.Filled"
          Color="Color.Primary"
          Disabled="isLoading"
          OnClick="HandleSubmit">
    @if (isLoading)
    {
        <MudProgressCircular Class="mr-3" Size="Size.Small" Indeterminate="true" />
        <MudText>Loading...</MudText>
    }
    else
    {
        <MudText>Submit</MudText>
    }
</MudButton>
```

### Progress overlay

```razor
@if (isLoading)
{
    <MudOverlay Visible="true" DarkBackground="true" zIndex="9999">
        <MudProgressCircular Indeterminate="true" Color="Color.Primary" Size="Size.Large" />
    </MudOverlay>
}
```

---

## Coexistence with Tailwind CSS

### When to use which

| UI element | Use solution |
|-----------|--------------|
| Layout containers | MudBlazor components (`MudContainer`, `MudGrid`) |
| Interactive components | MudBlazor (`MudButton`, `MudTextField`, `MudDataGrid`) |
| Custom styling | Tailwind utilities for spacing, colors, typography |
| Complex animations | Blazor CSS isolation or custom CSS |

### Example: combining

```razor
<MudContainer MaxWidth="MaxWidth.Large" Class="my-4">  <!-- MudBlazor + Tailwind -->
    <MudGrid>
        <MudItem xs="12" sm="6">
            <MudCard Class="shadow-lg">  <!-- MudBlazor card + Tailwind shadow -->
                <MudCardContent>
                    <MudText Typo="Typo.h5" Class="text-indigo-600">  <!-- Tailwind text -->
                        @Product.Name
                    </MudText>
                </MudCardContent>
            </MudCard>
        </MudItem>
    </MudGrid>
</MudContainer>
```

---

## Absolute rules

- Never use DataAnnotations for validation — always use FluentValidation with MudBlazor forms
- Never disable MudBlazor services in Program.cs — breaks all MudBlazor components
- Never mix @bind with MudTextField without specifying For — loses validation
- Always use MudDialogService for dialogs — ensures consistent UX
- Always inject ISnackbar for notifications — consistent with MudBlazor design
- Never use plain HTML tables when MudTable or MudDataGrid is available — accessibility
- Always configure custom theme in Program.cs before using in components
- Always handle loading and empty states in MudDataGrid and MudTable