---
name: tailwind
description: >
  Tailwind CSS standards for Blazor WebAssembly .NET 10 projects.
  Covers Tailwind setup, usage patterns, responsive design, dark mode,
  and coexistence with Blazor CSS isolation. Use when the frontend project
  uses Tailwind CSS for styling.
---

# Tailwind CSS — TemperAI Standards for Blazor

This skill provides Tailwind CSS standards for Blazor WebAssembly projects.
Load this skill when the frontend project uses Tailwind for styling.

For general CSS isolation and component styles, see `frontend/blazor`.

---

## When to use this skill

Load `frontend/tailwind` when:
- The project uses Tailwind CSS as the primary styling solution
- You need to configure Tailwind for a new Blazor project
- You're creating reusable Tailwind component patterns

**Do NOT load** if the project uses plain CSS or component-scoped CSS only.

---

## Project setup

### Install dependencies

```bash
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init
```

### Configure tailwind.config.js

```javascript
/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,razor,cs}",
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}
```

### Configure Blazor wwwroot/css/app.css

```css
@tailwind base;
@tailwind components;
@tailwind utilities;

/* Custom base styles */
@layer base {
  html {
    @apply scroll-smooth;
  }

  body {
    @apply bg-gray-50 text-gray-900 antialiased;
  }
}

/* Custom component styles */
@layer components {
  .btn-primary {
    @apply px-4 py-2 bg-indigo-600 text-white rounded-md 
           hover:bg-indigo-700 focus:outline-none focus:ring-2 
           focus:ring-indigo-500 focus:ring-offset-2 
           transition-colors duration-200;
  }

  .btn-secondary {
    @apply px-4 py-2 bg-gray-200 text-gray-900 rounded-md 
           hover:bg-gray-300 focus:outline-none focus:ring-2 
           focus:ring-gray-500 focus:ring-offset-2 
           transition-colors duration-200;
  }

  .input-field {
    @apply w-full px-3 py-2 border border-gray-300 rounded-md 
           focus:outline-none focus:ring-2 focus:ring-indigo-500 
           focus:border-indigo-500 placeholder-gray-400;
  }

  .card {
    @apply bg-white rounded-lg shadow-sm border border-gray-200 p-4;
  }
}

/* Custom utility overrides */
@layer utilities {
  .text-balance {
    text-wrap: balance;
  }
}
```

---

## Usage patterns in Blazor

### Direct classes (recommended for simple cases)

Use direct Tailwind classes for layout and spacing:

```razor
<div class="flex items-center justify-between p-4">
    <h1 class="text-2xl font-semibold text-gray-900">Products</h1>
    <button class="btn-primary" @onclick="NavigateToCreate">
        Add Product
    </button>
</div>
```

### @apply directive (for reusable patterns)

Define reusable styles in `app.css` with `@layer components`:

```css
@layer components {
  .product-card {
    @apply bg-white rounded-lg shadow-sm border border-gray-200 p-4 
           hover:shadow-md transition-shadow duration-200;
  }
}
```

Then use the class name:

```razor
<div class="product-card">
    <h3 class="text-lg font-medium">@Product.Name</h3>
    <p class="text-gray-500">@Product.Price</p>
</div>
```

### Conditional classes

Use string interpolation for conditional classes:

```razor
<div class="@(isActive ? 'bg-green-100' : 'bg-gray-100')">
    Status: @(isActive ? "Active" : "Inactive")
</div>
```

For multiple conditions:

```razor
<div class="@($"{(isActive ? 'bg-green-100' : 'bg-gray-100')} {(isHighlighted ? 'ring-2 ring-indigo-500' : '')}")">
    Content
</div>
```

---

## Responsive design

### Mobile-first approach

Always design mobile-first and layer responsive classes:

```razor
<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
    @foreach (var product in Products)
    {
        <ProductCard Product="@product" />
    }
</div>
```

### Responsive breakpoints

| Breakpoint | Min width | Use case |
|-----------|-----------|---------|
| `sm` | 640px | Small tablets |
| `md` | 768px | Tablets / small laptops |
| `lg` | 1024px | Laptops |
| `xl` | 1280px | Desktops |
| `2xl` | 1536px | Large screens |

### Common responsive patterns

```razor
<!-- Hide on mobile, show on larger -->
<div class="hidden md:block">
    Desktop navigation
</div>

<!-- Always visible, change layout on larger -->
<div class="flex flex-col md:flex-row gap-4">
    Sidebar content
</div>

<!-- Text size that scales -->
<p class="text-sm md:text-base lg:text-lg">
    Responsive text
</p>
```

---

## Layout patterns

### Flexbox

```razor
<!-- Center content -->
<div class="flex items-center justify-center min-h-screen">
    Content
</div>

<!-- Space between -->
<div class="flex items-center justify-between">
    <span>Left</span>
    <span>Right</span>
</div>

<!-- Gap spacing -->
<div class="flex gap-2">
    <button>1</button>
    <button>2</button>
</div>
```

### Grid

```razor
<!-- Simple grid -->
<div class="grid grid-cols-3 gap-4">
    <div>1</div>
    <div>2</div>
    <div>3</div>
</div>

<!-- Auto-fit grid -->
<div class="grid grid-cols-auto-fit minmax(200px, 1fr) gap-4">
    <!-- Cards that fill available space -->
</div>
```

### Spacing scale

Use consistent spacing from Tailwind's scale:

```razor
<!-- Margins: m-{0,1,2,4,6,8,12,16,24,32,48,64} -->
<div class="m-4">
    Content with standard margin
</div>

<!-- Padding: p-{size} -->
<div class="p-4">
    Content with standard padding
</div>

<!-- Gap: gap-{size} -->
<div class="flex gap-4">
    Items with consistent gap
</div>
```

---

## Component styling examples

### Button variants

```css
@layer components {
  .btn {
    @apply px-4 py-2 rounded-md font-medium transition-colors duration-200 
           focus:outline-none focus:ring-2 focus:ring-offset-2;
  }

  .btn-primary {
    @apply btn bg-indigo-600 text-white hover:bg-indigo-700 
           focus:ring-indigo-500;
  }

  .btn-secondary {
    @apply btn bg-gray-200 text-gray-900 hover:bg-gray-300 
           focus:ring-gray-500;
  }

  .btn-danger {
    @apply btn bg-red-600 text-white hover:bg-red-700 
           focus:ring-red-500;
  }

  .btn-ghost {
    @apply btn bg-transparent text-gray-600 hover:bg-gray-100 
           focus:ring-gray-500;
  }
}
```

### Input fields

```css
@layer components {
  .input-label {
    @apply block text-sm font-medium text-gray-700 mb-1;
  }

  .input-field {
    @apply block w-full rounded-md border-gray-300 shadow-sm 
           focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm;
  }

  .input-error {
    @apply block w-full rounded-md border-red-300 shadow-sm 
           focus:border-red-500 focus:ring-red-500 sm:text-sm;
  }
}
```

### Card components

```css
@layer components {
  .card {
    @apply bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden;
  }

  .card-header {
    @apply px-4 py-3 border-b border-gray-200 bg-gray-50;
  }

  .card-body {
    @apply px-4 py-4;
  }

  .card-footer {
    @apply px-4 py-3 border-t border-gray-200 bg-gray-50;
  }
}
```

### Table

```css
@layer components {
  .table-container {
    @apply overflow-x-auto rounded-lg border border-gray-200;
  }

  .table {
    @apply min-w-full divide-y divide-gray-200;
  }

  .table-header {
    @apply bg-gray-50;
  }

  .table-header-cell {
    @apply px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider;
  }

  .table-body {
    @apply bg-white divide-y divide-gray-200;
  }

  .table-cell {
    @apply px-6 py-4 whitespace-nowrap text-sm text-gray-900;
  }
}
```

---

## Utility patterns

### Loading state

```razor
@if (isLoading)
{
    <div class="flex items-center justify-center p-8">
        <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
    </div>
}
```

### Empty state

```razor
@if (Items.Count == 0)
{
    <div class="flex flex-col items-center justify-center p-8 text-center">
        <svg class="h-12 w-12 text-gray-400 mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
        </svg>
        <p class="text-gray-500">No items found</p>
    </div>
}
```

### Error state

```razor
@if (!string.IsNullOrEmpty(errorMessage))
{
    <div class="rounded-md bg-red-50 p-4 mb-4">
        <div class="flex">
            <div class="flex-shrink-0">
                <svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
                </svg>
            </div>
            <div class="ml-3">
                <p class="text-sm text-red-700">@errorMessage</p>
            </div>
        </div>
    </div>
}
```

---

## Coexistence with Blazor CSS isolation

### When to use which

| Style type | Use solution |
|-----------|--------------|
| Layout (flex, grid, spacing) | Tailwind classes |
| Reusable component patterns | Tailwind `@layer components` + class |
| Component-specific overrides | Blazor CSS isolation (`.razor.css`) |
| Theme variables | CSS custom properties |

### Example: combining approaches

```razor
<!-- ProductCard.razor -->
<div class="product-card">  <!-- Tailwind class from app.css -->
    <h3 class="text-lg font-medium">@Product.Name</h3>
    <p class="text-gray-500">@Product.Price</p>
</div>
```

```css
/* ProductCard.razor.css - only for component-specific animations */
::deep .product-card {
    animation: fade-in 0.3s ease-out;
}

@keyframes fade-in {
    from {
        opacity: 0;
        transform: translateY(8px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}
```

### Priority: Blazor isolation wins for conflicts

When the same property is defined in both Tailwind and Blazor CSS isolation,
the Blazor scoped CSS takes precedence for that component.

---

## Dark mode

### Enable dark mode in tailwind.config.js

```javascript
module.exports = {
  darkMode: 'class',  // or 'media' for system preference
  // ...
}
```

### Dark mode classes

```razor
<div class="bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100">
    Content that adapts to dark mode
</div>
```

### Toggle dark mode (JavaScript)

```javascript
// Toggle dark mode
function toggleDarkMode() {
    document.documentElement.classList.toggle('dark');
}
```

---

## Performance tips

### Purge unused styles

Tailwind purges unused styles in production by default when configured correctly.
Ensure all component files are in the `content` array:

```javascript
content: [
    "./src/**/*.{html,razor,cs}",
],
```

### Use arbitrary values sparingly

```razor
<!-- Avoid -->
<div class="w-[123px]">

<!-- Prefer -->
<div class="w-28">  <!-- 7 * 4 = 28 * 4px = 112px -->
```

### Prefer utility composition over custom classes

```razor
<!-- Good -->
<div class="flex items-center justify-between">

<!-- Less optimal -->
<div class="special-layout">
```

---

## Absolute rules

- Never use `!important` in Tailwind classes — modify the config instead
- Never duplicate Tailwind utilities in Blazor CSS isolation — conflicts cause debugging issues
- Always use the `content` array in tailwind.config.js — production builds break without it
- Always test responsive layouts manually — don't assume mobile-first works
- Always keep custom component styles in `@layer components` — easier to maintain
- Never hardcode colors — use Tailwind's color palette for consistency
- Never mix inline styles with Tailwind — choose one approach per element