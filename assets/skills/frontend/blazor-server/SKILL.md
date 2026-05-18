---
name: blazor-server
description: >
  Blazor Server and interactive server rendering standards for .NET 10 frontend
  work. Use ONLY for Blazor Server components, server-side circuits, rendering,
  authentication UI, scoped state, forms, SignalR-aware behavior, and server-side
  Razor component applications. Do not use for Blazor WebAssembly or Angular work.
---

# Blazor Server / .NET 10

## Scope

Use this skill for Blazor Server or .NET 10 interactive server-rendered Razor component applications.

Do not load `blazor` with this skill unless the task explicitly spans both WebAssembly and Server projects.
Do not load this skill for Angular.

## Server-Specific Principles

- UI runs on the server and interacts with the browser over a SignalR circuit.
- Treat scoped services as circuit-scoped, not request-scoped browser state.
- Avoid storing large mutable user state in components or singleton services.
- Design for reconnects, prerendering, cancellation, and circuit disposal.

## Components

- Use the same component naming and markup/code-behind discipline as Blazor WebAssembly.
- Keep server calls out of markup and in component methods or injected services.
- Avoid long-running work during rendering and lifecycle methods.
- Dispose event subscriptions, timers, streams, and JS references.

## Dependency Injection

- Use `[Inject]` in components and constructor injection in services.
- Use scoped services for per-user UI state.
- Avoid singleton services that hold user-specific data.
- Do not use `HttpClient` for same-process application services unless the architecture explicitly requires API calls.
- Use `IDbContextFactory<TContext>` or short-lived data access patterns when components need data access through application services.

## Forms and Validation

- Use `EditForm` and the validation approach already established by the project.
- Disable submit controls while server work is running.
- Keep validation messages close to fields.
- Avoid duplicate submissions caused by slow circuits or reconnects.

## Authentication and Authorization UI

- Use the project authentication mechanism, typically cookie-based auth for server apps.
- Use `AuthorizeView`, route authorization, and policy checks consistently with the existing project.
- Never expose privileged UI actions without server-side authorization backing them.

## Circuit and Connection Behavior

- Expect disconnects and reconnects.
- Cancel in-flight work when components are disposed.
- Do not assume component state survives reconnection unless explicitly persisted.
- Keep transferred render payloads small.

## Rendering and Performance

- Use pagination, filtering, virtualization, or server-side grid loading for large data sets.
- Avoid frequent `StateHasChanged` calls; Blazor renders automatically for normal event flow.
- Use streaming or progressive rendering only when the existing project supports it.
- Be careful with prerendering: browser-only APIs and JS interop are not available until interactive rendering.

## Accessibility

- Use semantic HTML or accessible component-library equivalents.
- Preserve focus behavior across validation errors, dialogs, and navigation.
- Make connection/error states understandable to screen readers when they affect user interaction.
