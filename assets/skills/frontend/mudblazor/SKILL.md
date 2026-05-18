---
name: mudblazor
description: >
  MudBlazor standards for Blazor / .NET 10 UI work. Use ONLY when a Blazor
  project already uses MudBlazor or the task explicitly asks for MudBlazor
  components, theming, dialogs, tables, forms, layout, snackbar, or overlays.
  Do not use for Angular or plain Blazor work without MudBlazor.
---

# MudBlazor

## Scope

Use this optional skill with `blazor` or `blazor-server` when MudBlazor is relevant. Do not load it for Angular tasks.

## Setup

- Add MudBlazor packages only when the task requires introducing MudBlazor.
- Register MudBlazor services in `Program.cs` with the project's existing setup style.
- Add required providers such as theme, popover, dialog, and snackbar providers in the app shell or layout once.
- Keep MudBlazor imports centralized in `_Imports.razor` when the project follows that convention.

## Component Usage

- Prefer MudBlazor components for forms, tables, dialogs, alerts, snackbars, layout, and navigation when the project already uses MudBlazor.
- Keep domain behavior outside MudBlazor markup; call component methods or injected services.
- Use strongly typed table/grid columns and server-data callbacks for large data.
- Use dialogs for focused decisions, not as generic page replacements.
- Use snackbars for transient feedback and inline alerts for persistent errors.

## Forms

- Bind MudBlazor inputs to explicit form models.
- Connect validation with the project's existing validation approach.
- Use `For` expressions where supported so validation messages and accessibility metadata map to fields.
- Disable submit buttons while requests are in progress.

## Theming

- Keep theme definitions centralized.
- Use design tokens, palette entries, typography, spacing, and radius consistently.
- Do not hard-code one-off colors into components when the theme can express the intent.
- Respect dark mode if the project supports it.

## Accessibility

- Provide labels for all inputs and icon-only buttons.
- Ensure dialogs have meaningful titles and focus behavior.
- Use accessible severity/status components for feedback.
- Do not remove keyboard navigation behavior provided by MudBlazor.

## Coexistence With Tailwind

- Load `tailwind` only when Tailwind is used in the project or requested by the task.
- Avoid mixing Tailwind utility classes into MudBlazor components when MudBlazor theme tokens solve the same problem.
- Use Tailwind primarily for page layout or custom non-MudBlazor sections when both systems exist.
