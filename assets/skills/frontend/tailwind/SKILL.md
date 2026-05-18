---
name: tailwind
description: >
  Tailwind CSS standards for Blazor / .NET 10 frontend styling. Use ONLY when a
  Blazor project already uses Tailwind CSS or the task explicitly asks for
  Tailwind setup, utility styling, responsive layout, dark mode, design tokens,
  or Tailwind component patterns. Do not use for Angular SCSS work.
---

# Tailwind CSS For Blazor

## Scope

Use this optional skill with `blazor` or `blazor-server` when Tailwind is relevant. Do not load it for Angular SCSS tasks.

## Setup

- Configure Tailwind content scanning for `.razor`, `.cshtml`, `.html`, and related source files used by the project.
- Keep generated CSS output in the project's existing stylesheet pipeline.
- Do not introduce Tailwind if the project uses plain CSS, SCSS, or a component library exclusively unless the task asks for it.

## Usage

- Prefer utility classes for layout, spacing, typography, and responsive behavior.
- Extract repeated patterns into `@layer components` only when duplication becomes real.
- Keep component-specific behavior in Blazor code and component-specific appearance in CSS isolation when that is the existing convention.
- Avoid large unreadable class strings; split markup or extract reusable classes when it improves clarity.

## Responsive Design

- Design mobile-first and add breakpoint variants for larger screens.
- Ensure navigation, tables, cards, forms, and dialogs work on narrow screens.
- Use consistent spacing and sizing scales.

## State Styling

- Include visible focus states for interactive elements.
- Style loading, empty, error, disabled, selected, and active states explicitly.
- Use `aria-*` attributes only when semantic HTML does not already communicate state.

## Dark Mode and Tokens

- Follow the project's dark-mode strategy if one exists.
- Prefer CSS variables or Tailwind theme extension for reusable colors, typography, and spacing.
- Do not hard-code arbitrary values when the design system already provides a token.

## Coexistence With MudBlazor

- Load `mudblazor` only when MudBlazor is relevant.
- Use Tailwind for layout and custom sections; use MudBlazor theme APIs for MudBlazor component styling.
- Avoid fighting component-library internals with brittle utility selectors.
