---
name: angular-material
description: >
  Angular Material standards. Use ONLY when an Angular project already uses
  Angular Material or the task explicitly asks for Material components,
  theming, dialogs, tables, forms, overlays, navigation, or accessibility.
  Do not use for Blazor or Angular projects without Material.
---

# Angular Material

## Scope

Use this optional skill with `angular` when Angular Material is relevant.

## Setup

- Do not introduce Angular Material unless the task explicitly requires it.
- Import only the Material modules or standalone component imports needed by the feature.
- Keep global theme setup centralized.
- Follow the project's density, typography, color, and dark-mode strategy.

## Components

- Use Material components for forms, dialogs, menus, tables, tabs, navigation, cards, snack bars, and overlays when they match the task.
- Keep business logic out of dialog and table markup; delegate to services or component methods.
- Use `MatDialog` for focused modal tasks and return typed results.
- Use `MatSnackBar` for transient feedback and inline messages for persistent errors.

## Forms

- Use `mat-form-field` with accessible labels and validation messages.
- Connect Material inputs to reactive or template-driven forms according to the project pattern.
- Use `mat-error` for validation feedback.
- Disable submit controls during pending requests.

## Tables and Lists

- Use Material table, paginator, and sort when the project uses them and the data set requires it.
- Prefer server-side paging/filtering for large data.
- Provide empty and loading states.
- Keep row actions keyboard accessible.

## Theming

- Use Angular Material theming APIs and design tokens rather than one-off CSS overrides.
- Keep component overrides minimal and scoped.
- Avoid deep selectors unless there is no supported theming alternative.

## Accessibility

- Preserve Material-provided roles, focus traps, keyboard navigation, and aria behavior.
- Add meaningful labels to icon buttons.
- Ensure dialogs have titles and clear close/cancel behavior.
- Do not use disabled elements when an explanatory validation or permission message is needed.
