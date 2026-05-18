---
name: api-docs-swagger
description: >
  API documentation provider skill for Swagger.
  Load only when backend config selects Swagger and the task touches API documentation wiring.
requires: [dotnet-api]
produces: [swagger-api-docs]
---

# API Docs — Swagger

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **ONLY use this skill when `Docs/Application/Architecture/backend-config.md` selects `Swagger`**
2. **ALWAYS expose docs only in Development unless a future skill says otherwise**
3. **NEVER wire Scalar together with Swagger in the same host**

## Package and wiring

```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.+" />
```

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

- OpenAPI JSON: `/swagger/v1/swagger.json`
- Swagger UI: `/swagger`
