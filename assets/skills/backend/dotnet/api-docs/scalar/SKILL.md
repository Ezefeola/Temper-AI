---
name: api-docs-scalar
description: >
  API documentation provider skill for Scalar.
  Load only when backend config selects Scalar and the task touches API documentation wiring.
requires: [dotnet-api]
produces: [scalar-api-docs]
---

# API Docs — Scalar

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **ONLY use this skill when `Docs/Application/Architecture/backend-config.md` selects `Scalar`**
2. **ALWAYS expose docs only in Development unless a future skill says otherwise**
3. **NEVER wire Swagger UI together with Scalar in the same host**

## Package and wiring

```xml
<PackageReference Include="Scalar.AspNetCore" Version="2.+" />
```

```csharp
builder.Services.AddOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("YourProject API");
    });
}
```

- OpenAPI JSON: `/openapi/v1.json`
- Scalar UI: `/scalar/v1`
