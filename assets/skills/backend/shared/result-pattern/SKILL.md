---
name: result-pattern
description: >
  Canonical Result<T> pattern for backend tasks.
  Load on every backend task.
requires: [dotnet-csharp]
produces: [result-pattern, action-result-mapping]
---

# Result Pattern — TemperAI

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **ALWAYS use `Result<TResponse>`** for backend success and failure flows
2. **ALWAYS pass `HttpStatusCode`** to `Success()` and `Failure()`
3. **NEVER use numeric status codes** in Result creation
4. **NEVER throw for expected business outcomes** — return failure results instead
5. **ALWAYS let controllers return `result.ToActionResult()`** — never remap Result manually

## Canonical shape

```csharp
public sealed class Result<TResponse>
{
    public bool IsSuccess { get; private set; }
    public HttpStatusCode HttpStatusCode { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public List<string> Errors { get; private set; } = [];
    public TResponse? Payload { get; private set; }

    private Result(bool isSuccess, HttpStatusCode httpStatusCode)
    {
        IsSuccess = isSuccess;
        HttpStatusCode = httpStatusCode;
    }

    public Result<TResponse> WithDescription(string description)
    {
        Description = description;
        return this;
    }

    public Result<TResponse> WithErrors(List<string> errors)
    {
        Errors = errors;
        return this;
    }

    public Result<TResponse> WithPayload(TResponse payload)
    {
        Payload = payload;
        return this;
    }

    public static Result<TResponse> Success(HttpStatusCode httpStatusCode)
    {
        return new(true, httpStatusCode);
    }

    public static Result<TResponse> Failure(HttpStatusCode httpStatusCode)
    {
        return new(false, httpStatusCode);
    }
}
```

## Usage rules — never broken

### 1. HttpStatusCode is mandatory

Every `Success()` and `Failure()` call must include an `HttpStatusCode`.

- ✅ `Result<UserDto>.Success(HttpStatusCode.Created)`
- ✅ `Result<UserDto>.Failure(HttpStatusCode.NotFound)`
- ❌ `Result<UserDto>.Success()`
- ❌ `Result<UserDto>.Success(201)`

### 2. Flow ownership

- Use case returns `Result<TResponse>` with the final `HttpStatusCode`
- Controller returns `result.ToActionResult()`
- Mappers only transform data; they never decide HTTP behavior

## Common status codes

- `HttpStatusCode.Created` for resource creation
- `HttpStatusCode.OK` for successful reads or updates
- `HttpStatusCode.BadRequest` for validation errors
- `HttpStatusCode.NotFound` for missing resources
- `HttpStatusCode.Conflict` for business rule conflicts
- `HttpStatusCode.InternalServerError` for unexpected persistence or infrastructure failures

## ResultExtensions.ToActionResult()

```csharp
public static class ResultExtensions
{
    public static IActionResult ToActionResult<TResponse>(this Result<TResponse> result)
    {
        if (result.IsSuccess)
        {
            return new ObjectResult(result.Payload)
            {
                StatusCode = (int)result.HttpStatusCode
            };
        }

        ProblemDetails problemDetails = new()
        {
            Status = (int)result.HttpStatusCode,
            Title = "One or more errors occurred.",
            Detail = result.Description
        };

        problemDetails.Extensions["errors"] = result.Errors;

        return new ObjectResult(problemDetails)
        {
            StatusCode = (int)result.HttpStatusCode
        };
    }
}
```

## Controller conventions

- Always return `result.ToActionResult()`
- Never check `result.IsSuccess` in controllers to choose a status code
- Never create custom switch/if trees on `HttpStatusCode` in controllers
- Never duplicate `ProblemDetails` mapping in each endpoint

```csharp
// ✅ CORRECT — controller delegates response mapping
[HttpPost]
public async Task<IActionResult> Create(
    [FromBody] CreateProductRequestDto request,
    [FromServices] ICreateProduct useCase,
    CancellationToken ct)
{
    Result<CreateProductResponseDto> result = await useCase.ExecuteAsync(request, ct);
    return result.ToActionResult();
}

// ❌ WRONG — controller rebuilds status logic manually
[HttpPost]
public async Task<IActionResult> Create(...)
{
    Result<CreateProductResponseDto> result = await useCase.ExecuteAsync(request, ct);

    if (result.HttpStatusCode == HttpStatusCode.NotFound)
        return NotFound(result.Description);

    if (!result.IsSuccess)
        return BadRequest(result.Errors);

    return Ok(result.Payload);
}
```
