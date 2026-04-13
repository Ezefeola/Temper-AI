---
name: result-pattern
description: >
  Result<T> pattern implementation with HttpStatusCode.
  This is the ONLY Result pattern allowed in TemperAI projects.
---

# Result Pattern — TemperAI

## Result<T> class

**CRITICAL: This is the ONLY Result pattern allowed. NEVER create variations, alternatives, or simplified versions.**

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

## Usage rules — NEVER broken

### 1. HttpStatusCode is MANDATORY

Every `Success()` and `Failure()` call MUST include an HttpStatusCode parameter.

- ✅ `Result<UserDto>.Success(HttpStatusCode.Created)`
- ✅ `Result<UserDto>.Failure(HttpStatusCode.NotFound)`
- ❌ `Result<UserDto>.Success()` — NEVER omit HttpStatusCode
- ❌ `Result<UserDto>.Success(201)` — NEVER use numeric codes

### 2. Common HttpStatusCode values

| HttpStatusCode | When to use |
|---|---|
| `HttpStatusCode.Created` | New resource created |
| `HttpStatusCode.OK` | Successful query/update |
| `HttpStatusCode.BadRequest` | Validation errors |
| `HttpStatusCode.NotFound` | Resource not found |
| `HttpStatusCode.Conflict` | Business rule violation |
| `HttpStatusCode.InternalServerError` | Unexpected error |

### 3. Flow: Use case returns Result, Controller calls ToActionResult()

The use case returns a Result with HttpStatusCode. The controller calls `result.ToActionResult()`. NOTHING ELSE.

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

- Always return `result.ToActionResult()` — never build responses manually
- **NEVER check `result.IsSuccess` to decide status codes** — ResultExtensions handles this
- **NEVER create custom error mapping** — Result pattern with HttpStatusCode is the single source of truth
- **NEVER use switch/if on HttpStatusCode in controllers**

```csharp
// ✅ CORRECT — minimal controller
[HttpPost]
public async Task<IActionResult> Create(
    [FromBody] CreateProductRequestDto request,
    [FromServices] ICreateProduct useCase,
    CancellationToken ct)
{
    Result<CreateProductResponseDto> result = await useCase.ExecuteAsync(request, ct);
    return result.ToActionResult();
}

// ❌ NEVER DO THIS — manual status code checking
[HttpPost]
public async Task<IActionResult> Create(...)
{
    var result = await useCase.ExecuteAsync(request, ct);
    
    if (result.HttpStatusCode == HttpStatusCode.NotFound)
        return NotFound(result.Description);
    
    if (!result.IsSuccess)
        return BadRequest(result.Errors);
    
    return Ok(result.Payload);
}
```