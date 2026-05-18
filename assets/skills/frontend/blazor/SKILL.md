---
name: blazor
description: >
  Blazor WebAssembly standards for .NET 10 frontend work. Use ONLY for Blazor
  WebAssembly components, pages, routing, forms, client-side state, API clients,
  JavaScript interop, accessibility, and frontend services. Do not use for
  Blazor Server or Angular work.
---

# Blazor WebAssembly / .NET 10

## Scope

Use this skill for Blazor WebAssembly projects targeting .NET 10. It covers browser-hosted Blazor UI and HTTP-based communication with backend APIs.

Do not load this skill for Blazor Server. Use `blazor-server` instead.
Do not load this skill for Angular.

## Project Shape

- Keep API and frontend applications separate unless the existing project intentionally combines them.
- Follow the existing solution and folder layout before creating new folders.
- Common folders: `Components`, `Components/Pages`, `Layouts`, `Services`, `Models`, `wwwroot`, and `wwwroot/css`.
- Register client services in `Program.cs` with scoped lifetimes unless the project uses a different established pattern.
- Configure API base URLs through configuration, not hard-coded component values.

## Components

- Use `PascalCase` component names such as `ProductList.razor` and `OrderDetail.razor`.
- Keep `.razor` files focused on markup and binding.
- Use `[Component].razor.cs` partial classes when logic, injected dependencies, or lifecycle behavior would make markup noisy.
- Use `[Parameter]`, `[CascadingParameter]`, and `EventCallback` for component boundaries.
- Do not mutate parameters directly inside child components.
- Dispose timers, subscriptions, and JS references with `IDisposable` or `IAsyncDisposable`.

## C# Rules

- Follow `dotnet-csharp` for C# syntax and .NET 10 conventions.
- Use explicit constructors in services when dependencies are required.
- Use braces for methods and control flow.
- Use `async Task`; never use `async void` except required event signatures.
- Never block async work with `.Result`, `.Wait()`, or synchronous HTTP calls.
- Use nullable reference types correctly and initialize injected component properties with `= default!`.

## Dependency Injection

- In components, use `[Inject]` properties for framework services and application services.
- In services, use constructor injection.
- Do not instantiate `HttpClient` manually.
- Do not inject typed HTTP clients directly into pages when a domain-specific frontend service is appropriate.

## API Consumption

- Use typed frontend services such as `IProductService` and `ProductService`.
- Keep request and response DTOs aligned with the API contract used by the task.
- Add `CancellationToken` parameters to async service methods.
- Handle HTTP failures with user-safe messages and typed outcomes where the project already has a result pattern.
- Keep authorization headers, base addresses, and serialization settings centralized.

## Forms

- Use `EditForm` and the validation approach already used by the project.
- Prefer explicit model classes for forms instead of binding directly to API response objects.
- Disable submit actions while requests are in flight.
- Show validation errors next to the relevant field.
- Show success or error feedback after submission.

## Routing and Navigation

- Use `@page` routes for pages.
- Use typed route constraints where appropriate, such as `{id:guid}`.
- Use `NavigationManager` for programmatic navigation.
- Handle missing, invalid, or unauthorized route data gracefully.

## UI State

- Represent loading, empty, error, and ready states explicitly.
- Keep local state in the component when it is not shared.
- Use scoped state services only for cross-component state.
- Do not introduce Fluxor or another state library unless already present or explicitly requested.

## JavaScript Interop

- Use JS interop only when Blazor cannot provide the behavior directly.
- Isolate interop behind small services or modules.
- Dispose `IJSObjectReference` instances.
- Avoid DOM mutation that conflicts with Blazor rendering.

## Accessibility and Performance

- Use semantic HTML and accessible labels.
- Ensure keyboard access for interactive controls.
- Use `Virtualize` or project UI-grid virtualization for large lists.
- Avoid unnecessary re-rendering and long-running work in lifecycle methods.
- Prefer CSS isolation or the project styling system for component-specific styles.
