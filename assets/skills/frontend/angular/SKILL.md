---
name: angular
description: >
  Angular frontend standards. Use ONLY for Angular components, templates,
  services, routing, forms, HTTP clients, signals or RxJS state, guards,
  interceptors, accessibility, and Angular tests. Do not use for Blazor work.
---

# Angular

## Scope

Use this skill for Angular application work. Do not load Blazor or .NET frontend skills for Angular-only tasks.

## Project Conventions

- Follow the existing Angular version, standalone-vs-NgModule style, folder layout, and naming conventions.
- Prefer standalone components when the project already uses standalone APIs.
- Keep feature code grouped by domain or route when that is the project pattern.
- Do not add new state, UI, or styling libraries unless the task explicitly requires them.

## Components

- Use focused components with clear input and output boundaries.
- Keep templates declarative and move non-trivial behavior to component methods or services.
- Use `ChangeDetectionStrategy.OnPush` when consistent with the project.
- Prefer `input()`, `output()`, and signals when the project already uses modern Angular signal APIs.
- Use `@if`, `@for`, and `@switch` when the project uses Angular control-flow syntax; otherwise match existing template style.

## Services and HTTP

- Use injectable services for API calls and shared frontend behavior.
- Keep API routes, DTO mapping, and error handling out of components where practical.
- Use `HttpClient` through services and interceptors, not directly in templates.
- Centralize auth headers, base URLs, and cross-cutting HTTP behavior in existing interceptors or API clients.
- Handle loading, empty, error, and success states explicitly in the UI.

## State

- Keep state local when only one component needs it.
- Use services, signals, RxJS subjects, or the project's existing store for shared state.
- Do not introduce NgRx, Akita, or another store unless already present or explicitly requested.
- Clean up subscriptions with `async` pipe, `takeUntilDestroyed`, or the project's existing pattern.

## Forms

- Match the project's existing reactive or template-driven form style.
- Prefer reactive forms for complex validation and dynamic forms.
- Keep validation rules explicit and user-facing messages close to fields.
- Disable submit actions while requests are in progress.
- Prevent duplicate submissions.

## Routing

- Keep route configuration close to the feature when the project is feature-routed.
- Use guards and resolvers only when they simplify user flow or authorization behavior.
- Preserve lazy loading boundaries.
- Handle missing IDs, failed loads, and unauthorized states gracefully.

## Templates and Accessibility

- Use semantic HTML before ARIA.
- Associate labels with controls.
- Preserve keyboard navigation and focus states.
- Avoid inaccessible click-only elements; use buttons and links for actions and navigation.
- Use `track` expressions with repeated lists where the project uses modern control flow.

## Testing

- Follow existing test tools and style.
- Test user-observable component behavior, service API behavior, routing decisions, and form validation.
- Avoid brittle tests tied to private implementation details.
