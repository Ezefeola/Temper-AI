---
name: scss
description: >
  SCSS standards for Angular frontend styling. Use ONLY when creating or modifying
  Angular SCSS styles, component styles, layouts, design tokens, themes,
  responsive behavior, or style architecture. Do not use for Blazor Tailwind work.
---

# SCSS For Angular

## Scope

Use this optional skill with `angular` when SCSS files or Angular style architecture are relevant.

## Structure

- Follow the project's existing global style and component style organization.
- Keep component-specific styles in the component `.scss` file.
- Keep global resets, tokens, typography, and layout primitives in global style files.
- Avoid broad global selectors that leak into unrelated features.

## Design Tokens

- Prefer CSS custom properties or existing SCSS variables/maps for colors, spacing, typography, shadows, and radii.
- Keep tokens centralized and reusable.
- Do not hard-code repeated values in component styles.
- Preserve dark-mode and theme conventions already present.

## Component Styling

- Use class names that describe intent, not implementation details.
- Keep selectors shallow and readable.
- Avoid `::ng-deep` unless there is no supported component-library theming API.
- Avoid `!important` except as a last resort for external library overrides.

## Layout and Responsiveness

- Build mobile-first styles.
- Use flexbox, grid, and container patterns consistently with the project.
- Ensure forms, tables, cards, navigation, and dialogs remain usable on narrow screens.
- Prefer reusable layout classes or mixins only when duplication justifies them.

## Accessibility States

- Preserve visible focus states.
- Style hover, active, selected, disabled, invalid, loading, empty, and error states intentionally.
- Do not rely on color alone to communicate status.

## Angular Material Coexistence

- Load `angular-material` only when Material components or themes are relevant.
- Prefer Material theming APIs over brittle selectors for Material internals.
- Scope custom Material overrides narrowly.
