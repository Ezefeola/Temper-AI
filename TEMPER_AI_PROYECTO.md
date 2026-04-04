# TemperAI — Documentación Completa del Proyecto

> **Para cualquier IA que lea esto:** Este documento describe un proyecto en desarrollo activo. Leelo completo antes de responder cualquier pregunta o continuar el trabajo. Contiene decisiones de arquitectura, convenciones de código, y el estado actual del proyecto. Respetá TODAS las decisiones tomadas — no las cuestionés a menos que el usuario lo pida explícitamente.

---

## Tabla de contenidos

1. [Qué es TemperAI](#1-qué-es-temperai)
2. [Stack tecnológico](#2-stack-tecnológico)
3. [Arquitectura del sistema](#3-arquitectura-del-sistema)
4. [Estructura del repositorio](#4-estructura-del-repositorio)
5. [El configurador — TemperAI.Cli](#5-el-configurador--temperaicli)
6. [Los Skills — estándares de código](#6-los-skills--estándares-de-código)
7. [El sistema multi-agente SDD](#7-el-sistema-multi-agente-sdd)
8. [Engram — memoria persistente](#8-engram--memoria-persistente)
9. [Convenciones de código C#](#9-convenciones-de-código-c)
10. [Estado actual y próximos pasos](#10-estado-actual-y-próximos-pasos)

---

## 1. Qué es TemperAI

TemperAI es un **configurador de ecosistema AI** para desarrolladores .NET. Inspirado en [gentle-ai](https://github.com/Gentleman-Programming/gentle-ai) de Gentleman Programming, pero construido completamente en C# y .NET 10, orientado al stack .NET + Blazor.

**Lo que hace:**
- Instala Skills (archivos `.md` con estándares de código) en el directorio de tu agente AI (GitHub Copilot CLI, Claude Code, OpenCode)
- Instala agentes especializados que siguen un workflow SDD (Spec-Driven Development)
- Provee un sistema de memoria persistente (llamado **NeuralCore** — nuestro equivalente a Engram) implementado con .NET + EF Core + SQLite + MCP

**Lo que NO es:**
- No es un agente de IA en sí mismo
- No reemplaza a GitHub Copilot, Claude Code, ni OpenCode
- No tiene su propio LLM — usa el agente que ya tenés instalado

**Nombre del producto:** TemperAI
- "Templar" = proceso de darle dureza, forma y resistencia al metal con precisión
- Metáfora: tomar un PRD y forjar código robusto, bien estructurado, con los estándares más altos

---

## 2. Stack tecnológico

### Configurador (TemperAI)
- **Lenguaje:** C# 13 / .NET 10
- **CLI framework:** Spectre.Console + Spectre.Console.Cli
- **Distribución:** `dotnet publish` con `PublishSingleFile=true` + `SelfContained=true` → `.exe` único sin dependencias
- **Assets embebidos:** `EmbeddedResource` en `.csproj` — todos los Skills y agentes van dentro del binario

### Skills y Agentes
- **Formato:** Markdown puro (`.md`) con YAML frontmatter
- **Compatibilidad:** GitHub Copilot CLI, Claude Code, OpenCode, Cursor (cualquier agente que soporte el estándar Agent Skills)

### NeuralCore (memoria persistente — por construir)
- **Lenguaje:** C# / .NET 10
- **Base de datos:** SQLite via EF Core
- **Protocolo:** MCP (Model Context Protocol) — expuesto como MCP server
- **Inspirado en:** [Engram](https://github.com/Gentleman-Programming/engram) de Gentleman Programming

### Proyectos generados por TemperAI
- **Backend:** .NET 10, C#, EF Core, Clean Architecture / Hexagonal / Vertical Slice / Onion
- **Frontend:** Blazor (Server o WebAssembly)
- **Testing:** xUnit, bUnit
- **DevOps:** Docker, GitHub Actions

---

## 3. Arquitectura del sistema

### El configurador (lo que ya existe)

```
temper-ai.exe install
    ↓
Muestra menú interactivo (Spectre.Console)
    ↓
Usuario elige agente (Copilot CLI / Claude Code / OpenCode)
    ↓
Copia Skills de assets embebidos → ~/.copilot/skills/
Copia Agentes de assets embebidos → ~/.copilot/agents/
    ↓
El agente AI del usuario ahora tiene los Skills y agentes de TemperAI
```

### El sistema multi-agente SDD (en construcción)

```
Usuario escribe PRD.md
    ↓
/temper-init → Lee PRD, hace preguntas, genera .temper/constitution.md
    ↓ (aprobación del usuario)
/temper-spec → Genera .temper/spec.md (user stories, criterios)
    ↓ (aprobación del usuario)
/temper-design → Genera .temper/design.md (arquitectura, entidades, endpoints)
    ↓ (aprobación del usuario)
/temper-tasks → Genera .temper/tasks.md (tareas atómicas con estado)
    ↓ (aprobación del usuario)
/temper-build → Subagentes especializados leen tasks.md y construyen
    ├── backend  → usa Skills dotnet-api + arquitectura elegida
    ├── frontend → usa Skills blazor
    ├── tester   → usa Skills testing
    └── devops   → usa Skills docker/ci
    ↓
/temper-review → Valida código contra spec.md
    ↓ (aprobación del usuario)
/temper-docs → Genera README, API docs, decisiones de arquitectura
```

**Anti-lobotomía:** El contexto NO vive en la conversación. Cada agente lee archivos `.temper/` al arrancar, hace su trabajo, y escribe el resultado en disco. Si el agente se compacta (lobotomía), simplemente se reinicia y lee los archivos del disco — sin perder nada.

**Minimización de context window:** Cada subagente carga SOLO las Skills de su dominio. El agente de backend .NET no sabe que existe Blazor. El de frontend no sabe que existe EF Core.

---

## 4. Estructura del repositorio

```
temper-ai/
│
├── TemperAI.sln
│
├── src/
│   ├── TemperAI.Cli/               ← Punto de entrada, comandos CLI
│   │   ├── TemperAI.Cli.csproj
│   │   ├── Program.cs
│   │   └── Commands/
│   │       └── InstallCommand.cs
│   │
│   ├── TemperAI.Core/              ← Modelos, assets embebidos, configuración
│   │   ├── TemperAI.Core.csproj
│   │   ├── Models/
│   │   │   ├── AgentTarget.cs
│   │   │   ├── InstallResult.cs
│   │   │   └── SaveResult.cs
│   │   ├── Assets/
│   │   │   └── EmbeddedAssets.cs
│   │   └── Configuration/
│   │       └── AgentTargets.cs
│   │
│   └── TemperAI.Installer/         ← Lógica de instalación de Skills
│       ├── TemperAI.Installer.csproj
│       └── InstallerService.cs
│
└── assets/                         ← Se embeben dentro del binario en build time
    ├── skills/
    │   ├── README.md
    │   ├── dotnet-api/
    │   │   └── SKILL.md            ← Estándares .NET 10 universales
    │   ├── blazor/
    │   │   └── SKILL.md            ← placeholder (por escribir)
    │   ├── prd-analyzer/
    │   │   └── SKILL.md            ← placeholder (por escribir)
    │   └── architecture/
    │       ├── clean/
    │       │   └── SKILL.md        ← COMPLETO ✅
    │       ├── hexagonal/
    │       │   └── SKILL.md        ← placeholder (por escribir)
    │       ├── vertical-slice/
    │       │   └── SKILL.md        ← placeholder (por escribir)
    │       └── onion/
    │           └── SKILL.md        ← placeholder (por escribir)
    ├── agents/
    │   └── README.md               ← Agentes SDD por escribir
    └── config/
        └── README.md
```

---

## 5. El configurador — TemperAI.Cli

### Cómo compilar y publicar

```powershell
# Desarrollo
dotnet build
dotnet run --project src\TemperAI.Cli -- install --dry-run

# Publicar ejecutable único standalone
dotnet publish src\TemperAI.Cli -c Release -o publish
.\publish\temper-ai.exe install --dry-run
```

### Comandos disponibles

```powershell
temper-ai install                    # Menú interactivo
temper-ai install --dry-run          # Simula sin escribir
temper-ai install --agent copilot    # Instala para Copilot CLI
temper-ai install --agent claude     # Instala para Claude Code
temper-ai install --agent all        # Instala para todos
```

### Agentes soportados

| ID | Nombre | Skills Path | Agents Path | Windows |
|---|---|---|---|---|
| `copilot` | GitHub Copilot CLI | `~/.copilot/skills/` | `~/.copilot/agents/` | ✅ |
| `claude` | Claude Code | `~/.claude/skills/` | `~/.claude/agents/` | ✅ |
| `opencode` | OpenCode | `~/.config/opencode/skills/` | `~/.config/opencode/agent/` | ❌ |

### Cómo agregar un nuevo agente

En `src/TemperAI.Core/Configuration/AgentTargets.cs` agregás un nuevo objeto en la lista `All()`.

### Cómo funciona el embed de assets

Los archivos en `/assets/` se configuran en `TemperAI.Core.csproj` como `EmbeddedResource`. En build time quedan dentro del binario. `EmbeddedAssets.cs` los lee en runtime con `Assembly.GetManifestResourceStream()`.

Formato del nombre del recurso: `assets/skills/dotnet-api/SKILL.md`

---

## 6. Los Skills — estándares de código

Los Skills son archivos `.md` con YAML frontmatter que el agente AI lee cuando es relevante para la tarea. El `description` en el frontmatter es lo que el agente usa para decidir si carga ese Skill.

### Formato de un Skill

```markdown
---
name: nombre-del-skill
description: >
  Cuándo usar este skill — el agente lo lee para decidir si es relevante.
  Sé específico sobre el contexto de uso.
---

# Contenido del Skill
... instrucciones para el agente ...
```

### Skills existentes

#### `architecture/clean/SKILL.md` ✅ COMPLETO

Clean Architecture + DDD con todas las decisiones tomadas:

**Estructura de capas:**
- `Api` → `Application` → `Domain` ← `Infrastructure`
- `Domain` sin dependencias externas (cero NuGet)
- `Application` solo depende de `Domain`

**Entidades:**
- `sealed class` con constructor `private`
- Factory method estático que devuelve `(List<string> Errors, Entity? Entity)`
- Nunca `throw` para validaciones de negocio
- Clase `Rules` anidada con constantes de límites
- `UpdatedAt` seteado explícitamente en cada método de actualización
- Métodos de actualización validan invariants Y verifican si el valor realmente cambió

**Ejemplo de factory method:**
```csharp
public sealed class Product : Entity<Guid>
{
    public class Rules
    {
        public const int NAME_MAX_LENGTH = 100;
        public const int DESCRIPTION_MAX_LENGTH = 500;
        public const decimal MIN_PRICE = 0;
    }

    public string Name { get; private set; } = string.Empty;
    public DateTime? UpdatedAt { get; private set; }

    private Product() { }

    public static (List<string> Errors, Product? Product) Create(
        string name, string description, decimal price)
    {
        List<string> productErrors = [];

        if (string.IsNullOrWhiteSpace(name))
        {
            productErrors.Add("El nombre es requerido");
        }
        else if (name.Length > Rules.NAME_MAX_LENGTH)
        {
            productErrors.Add($"El nombre no puede superar {Rules.NAME_MAX_LENGTH} caracteres");
        }

        if (productErrors.Count > 0)
        {
            return (productErrors, null);
        }

        Product product = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
        };

        return ([], product);
    }

    public (List<string> Errors, bool Updated) UpdateName(string newName)
    {
        List<string> nameErrors = [];

        if (string.IsNullOrWhiteSpace(newName))
        {
            nameErrors.Add("El nombre es requerido");
        }

        if (nameErrors.Count > 0)
        {
            return (nameErrors, false);
        }

        if (Name == newName)
        {
            return ([], false);
        }

        Name = newName;
        UpdatedAt = DateTime.UtcNow;
        return ([], true);
    }
}
```

**Domain Events:**
- Son solo contratos — `sealed record` con datos, sin comportamiento
- NO se registran en la entidad ni se despachan en SaveChanges
- Se publican EXPLÍCITAMENTE en el UseCase después del `CompleteAsync`
- `Entity<TId>` base es limpia — sin lista de eventos ni `RaiseDomainEvent`

```csharp
// Solo el contrato — nada más
public sealed record ProductCreatedEvent : IDomainEvent
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;

    public ProductCreatedEvent(Guid productId, string productName)
    {
        ProductId = productId;
        ProductName = productName;
    }
}
```

**Result pattern:**
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

    public Result<TResponse> WithDescription(string description) { Description = description; return this; }
    public Result<TResponse> WithErrors(List<string> errors) { Errors = errors; return this; }
    public Result<TResponse> WithPayload(TResponse payload) { Payload = payload; return this; }

    public static Result<TResponse> Success(HttpStatusCode httpStatusCode) => new(true, httpStatusCode);
    public static Result<TResponse> Failure(HttpStatusCode httpStatusCode) => new(false, httpStatusCode);
}
```

**Casos de uso:**
- Nombre sin sufijo `UseCase` — `CreateProduct`, `UpdateProduct` (NO `CreateProductUseCase`)
- Interfaz en la misma carpeta — `ICreateProduct`, `IUpdateProduct`
- `sealed class`
- SIN CQRS — casos de uso simples con interfaz propia
- Inyección por constructor explícito (NUNCA primary constructor)

```csharp
public sealed class CreateProduct : ICreateProduct
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public CreateProduct(IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<CreateProductResponseDto>> ExecuteAsync(
        CreateProductRequestDto createProductRequestDto,
        CancellationToken cancellationToken = default)
    {
        var (productErrors, product) = Product.Create(
            createProductRequestDto.Name,
            createProductRequestDto.Description,
            createProductRequestDto.Price);

        if (product is null)
        {
            return Result<CreateProductResponseDto>
                .Failure(HttpStatusCode.BadRequest)
                .WithErrors(productErrors);
        }

        await _unitOfWork.ProductRepository.AddAsync(product, cancellationToken);
        SaveResult saveResult = await _unitOfWork.CompleteAsync(cancellationToken);

        if (!saveResult.IsSuccess)
        {
            return Result<CreateProductResponseDto>
                .Failure(HttpStatusCode.InternalServerError)
                .WithDescription(saveResult.ErrorMessage);
        }

        // Publicación explícita del evento
        ProductCreatedEvent productCreatedEvent = new(product.Id, product.Name);
        await _eventPublisher.PublishAsync(productCreatedEvent, cancellationToken);

        return Result<CreateProductResponseDto>
            .Success(HttpStatusCode.Created)
            .WithPayload(product.ToCreateProductResponseDto());
    }
}
```

**DTOs:**
- Siempre `sealed record`
- Propiedades explícitas (NUNCA primary constructor)
- Sufijo `Dto` — `CreateProductRequestDto`, `CreateProductResponseDto`

```csharp
public sealed record CreateProductRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
}
```

**Mapeos:**
- Extension methods en `ProductMappingExtensions.cs`
- Nombre con prefijo `To` + nombre exacto del DTO
- En carpeta `UseCases/Products/` a nivel general (no dentro de cada caso de uso)

```csharp
public static class ProductMappingExtensions
{
    public static CreateProductResponseDto ToCreateProductResponseDto(this Product product)
    {
        return new CreateProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Status = product.Status.ToString()
        };
    }
}
```

**UnitOfWork:**
- Es el punto de entrada ÚNICO a todos los repositorios
- Los repositorios se acceden via `unitOfWork.ProductRepository`
- Maneja `BeginTransactionAsync`, `CommitTransactionAsync`, `RollbackTransactionAsync`, `CompleteAsync`
- `CompleteAsync` devuelve `SaveResult` — maneja el `DbUpdateException` internamente
- Sin lazy loading — includes explícitos siempre

**Repositorios:**
- `GetByIdAsync` → con tracking (para modificaciones)
- `GetByIdAsNoTrackingAsync` → sin tracking (para lecturas)
- NUNCA usar `.Update()` de EF Core — EF detecta cambios via change tracker
- `AsNoTracking()` solo en métodos que lo expresan en el nombre

**EF Core:**
- NUNCA `DataAnnotations` en entidades ni Value Objects
- Un archivo `IEntityTypeConfiguration<T>` por entidad en `Infrastructure/Persistence/Configurations/`
- NUNCA `nvarchar(max)` ni `varchar(max)` — longitud siempre desde `Entity.Rules`
- `varchar` para ASCII, `nvarchar` para Unicode
- Value Objects configurados con `OwnsOne` en la configuración de la entidad

**Controllers:**
- Sin constructor general — dependencias con `[FromServices]` por endpoint
- Siempre `[FromBody]`, `[FromRoute]`, `[FromQuery]` explícitos
- Usan `result.ToActionResult()` — nunca arman la respuesta a mano
- Errores siempre como `ProblemDetails` con campo `errors`

```csharp
[HttpPost]
public async Task<IActionResult> Create(
    [FromBody] CreateProductRequestDto createProductRequestDto,
    [FromServices] ICreateProduct createProduct,
    CancellationToken cancellationToken)
{
    Result<CreateProductResponseDto> createProductResult =
        await createProduct.ExecuteAsync(createProductRequestDto, cancellationToken);

    return createProductResult.ToActionResult();
}
```

**DI:**
- Métodos privados por responsabilidad
- `AddInfrastructure` → `AddDatabase`, `AddRepositories`, `AddUnitOfWork`
- `AddApplication` → `AddUseCases` → `AddProductUseCases`, `AddOrderUseCases`, etc.

**Organización de Domain por entidad:**
```
Domain/
├── Entities/
│   └── Product/
│       ├── Product.cs
│       ├── ValueObjects/
│       ├── Enums/
│       └── Events/
├── Common/
│   ├── ValueObjects/       ← VOs compartidos entre entidades
│   └── Primitives/
│       ├── Entity.cs       ← base limpia, sin eventos
│       └── IDomainEvent.cs
└── Errors/
```

#### Skills pendientes de escribir

| Skill | Estado |
|---|---|
| `architecture/hexagonal/SKILL.md` | ⏳ Pendiente |
| `architecture/vertical-slice/SKILL.md` | ⏳ Pendiente |
| `architecture/onion/SKILL.md` | ⏳ Pendiente |
| `skills/blazor/SKILL.md` | ⏳ Pendiente |
| `skills/prd-analyzer/SKILL.md` | ⏳ Pendiente |
| `skills/dotnet-api/SKILL.md` | ⏳ Pendiente (hay un draft inicial) |

---

## 7. El sistema multi-agente SDD

### Agentes planificados

| Archivo | Fase | Descripción |
|---|---|---|
| `orchestrator.agent.md` | — | Coordina todas las fases |
| `temper-init.agent.md` | Fase 1 | Lee PRD, hace preguntas, genera `constitution.md` |
| `temper-spec.agent.md` | Fase 2 | User stories, criterios de aceptación, `spec.md` |
| `temper-design.agent.md` | Fase 3 | Arquitectura, entidades, endpoints, `design.md` |
| `temper-tasks.agent.md` | Fase 4 | Rompe design en tareas atómicas, `tasks.md` |
| `temper-backend.agent.md` | Fase 5a | Genera código .NET 10 según tasks.md |
| `temper-frontend.agent.md` | Fase 5b | Genera código Blazor |
| `temper-tester.agent.md` | Fase 5c | Genera tests xUnit/bUnit |
| `temper-devops.agent.md` | Fase 5d | Docker, GitHub Actions |
| `temper-review.agent.md` | Fase 6 | Valida código contra spec.md |
| `temper-docs.agent.md` | Fase 7 | README, API docs, decisiones |

### Comandos slash planificados

| Comando | Acción |
|---|---|
| `/temper-init` | Arranca el workflow SDD |
| `/temper-next` | Avanza a la siguiente fase |
| `/temper-status` | Muestra en qué fase estás |

### Estructura `.temper/` en cada proyecto

```
tu-proyecto/
└── .temper/
    ├── constitution.md   ← stack, arquitectura, estándares del proyecto
    ├── spec.md           ← user stories, criterios de aceptación
    ├── design.md         ← entidades, endpoints, estructura de carpetas
    ├── tasks.md          ← tareas atómicas con estado (pending/done)
    └── decisions.md      ← log de decisiones importantes tomadas
```

### Formato de agente (`.agent.md`)

Para GitHub Copilot CLI, los agentes van en `~/.copilot/agents/` con extensión `.agent.md`:

```markdown
---
name: temper-init
description: >
  Agente de inicialización de TemperAI. Usar cuando el usuario
  ejecuta /temper-init o quiere iniciar un nuevo proyecto desde un PRD.
mode: agent
allowed-tools: read_file, write_file, ask_followup_question
---

# temper-init — Agente de Inicialización

## Tu rol
Sos el primer agente del workflow SDD de TemperAI...
```

### Estado actual de los agentes

**Ningún agente ha sido escrito todavía.** El próximo paso es escribir `temper-init.agent.md`.

---

## 8. Engram — memoria persistente (NeuralCore)

### Qué es

Engram es el sistema de memoria persistente de Gentleman Programming. Lo vamos a reimplementar en .NET con el nombre **NeuralCore**.

### Cómo funciona Engram (referencia)

1. El agente completa trabajo significativo (bugfix, decisión de arquitectura, etc.)
2. El agente llama `mem_save` con un resumen estructurado
3. Se persiste en SQLite con FTS5 para búsqueda full-text
4. En la próxima sesión, el agente llama `mem_search` para recuperar contexto relevante
5. Después de cualquier compactación/lobotomía, el agente llama `mem_context` para recuperar el estado

### Formato de memoria (What/Why/Where/Learned)

```
title: "Elegimos Clean Architecture para el módulo de pagos"
type: decision | bugfix | architecture | discovery | pattern | config | preference
content:
  What: Qué se hizo en una oración
  Why: Qué lo motivó
  Where: Archivos o paths afectados
  Learned: Gotchas, edge cases, cosas que sorprendieron (omitir si no hay)
```

### Schema de base de datos

```sql
-- Sesiones de trabajo
CREATE TABLE sessions (
    id TEXT PRIMARY KEY,
    project TEXT,
    directory TEXT,
    started_at TEXT,
    ended_at TEXT,
    summary TEXT,
    status TEXT
);

-- Las memorias reales
CREATE TABLE observations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT REFERENCES sessions(id),
    type TEXT,
    title TEXT,
    content TEXT,
    project TEXT,
    scope TEXT,
    topic_key TEXT,
    revision_count INTEGER,
    created_at TEXT,
    updated_at TEXT,
    deleted_at TEXT
);

-- Prompts del usuario
CREATE TABLE user_prompts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT REFERENCES sessions(id),
    content TEXT,
    project TEXT,
    created_at TEXT
);
```

### NeuralCore — plan de implementación

**Proyecto:** `TemperAI.NeuralCore` (por crear)
- EF Core + SQLite
- MCP server expuesto via stdio
- HTTP API en puerto configurable
- Herramientas MCP: `mem_save`, `mem_search`, `mem_context`, `mem_session_summary`

**NO está construido todavía.** Se construye después de completar los agentes SDD.

---

## 9. Convenciones de código C#

Estas convenciones aplican a TODO el código del proyecto, incluyendo TemperAI y los proyectos que genera.

### Reglas absolutas — NUNCA se rompen

```
✗ NUNCA primary constructors — siempre constructor explícito
✗ NUNCA return expression => en métodos — siempre llaves {}
✗ NUNCA DataAnnotations en entidades
✗ NUNCA nvarchar(max) ni varchar(max)
✗ NUNCA .Update() de EF Core
✗ NUNCA async void — siempre async Task
✗ NUNCA .Result ni .Wait() — causa deadlocks
✗ NUNCA lazy loading — includes explícitos
✗ NUNCA throw para validaciones de negocio
✗ NUNCA saltarse warnings con ! (null-forgiving operator) sin justificación

✅ SIEMPRE nombres de variables igual al tipo — SaveResult saveResult, Product product
✅ SIEMPRE CancellationToken en métodos async públicos
✅ SIEMPRE sealed en clases que no se heredan
✅ SIEMPRE llaves {} inclusive en if de una sola línea
✅ SIEMPRE sufijo Dto en DTOs
✅ SIEMPRE prefijo To en extension methods de mapeo
✅ SIEMPRE GetByIdAsync (con tracking) vs GetByIdAsNoTrackingAsync (sin tracking)
✅ SIEMPRE varchar para ASCII, nvarchar para Unicode
✅ SIEMPRE longitud de columnas desde Entity.Rules
✅ SIEMPRE un IEntityTypeConfiguration<T> por entidad
```

### Nomenclatura

| Elemento | Convención | Ejemplo |
|---|---|---|
| Casos de uso | Sin sufijo, PascalCase | `CreateProduct`, `UpdateProduct` |
| Interfaces de UC | Prefijo `I` | `ICreateProduct`, `IUpdateProduct` |
| DTOs de entrada | Sufijo `RequestDto` | `CreateProductRequestDto` |
| DTOs de salida | Sufijo `ResponseDto` | `CreateProductResponseDto` |
| Extension mapeos | Prefijo `To` + nombre DTO | `ToCreateProductResponseDto()` |
| Repositorios | `I` + nombre + `Repository` | `IProductRepository` |
| Eventos de dominio | Sufijo `Event` | `ProductCreatedEvent` |
| Configs EF | Nombre entidad + `Configuration` | `ProductConfiguration` |

### Estructura de proyectos generados

Para Clean Architecture:
```
src/
├── ProjectName.Api/
│   ├── Controllers/
│   ├── Middlewares/
│   ├── Extensions/
│   │   └── ResultExtensions.cs
│   └── Program.cs
├── ProjectName.Application/
│   ├── Contracts/
│   │   └── Services/
│   │       └── IEventPublisher.cs
│   ├── UseCases/
│   │   └── Products/
│   │       ├── ProductMappingExtensions.cs
│   │       ├── CreateProduct/
│   │       │   ├── ICreateProduct.cs
│   │       │   ├── CreateProduct.cs
│   │       │   ├── CreateProductRequestDto.cs
│   │       │   └── CreateProductResponseDto.cs
│   │       └── UpdateProduct/
│   │           ├── IUpdateProduct.cs
│   │           ├── UpdateProduct.cs
│   │           ├── UpdateProductRequestDto.cs
│   │           └── UpdateProductResponseDto.cs
│   ├── Common/
│   │   └── Result.cs
│   └── DependencyInjection.cs
├── ProjectName.Domain/
│   ├── Entities/
│   │   └── Product/
│   │       ├── Product.cs
│   │       ├── ValueObjects/
│   │       ├── Enums/
│   │       └── Events/
│   ├── Common/
│   │   └── Primitives/
│   │       ├── Entity.cs
│   │       └── IDomainEvent.cs
│   └── Errors/
└── ProjectName.Infrastructure/
    ├── Persistence/
    │   ├── Configurations/
    │   ├── Migrations/
    │   ├── Repositories/
    │   ├── UnitOfWork.cs
    │   └── AppDbContext.cs
    ├── Services/
    └── DependencyInjection.cs
```

---

## 10. Estado actual y próximos pasos

### ✅ Completado

- [x] Estructura del proyecto C# con Spectre.Console
- [x] `TemperAI.Core` — modelos, assets embebidos, configuración de targets
- [x] `TemperAI.Installer` — lógica de instalación de Skills
- [x] `TemperAI.Cli` — comando `install` con menú interactivo y dry-run
- [x] Skill `architecture/clean/SKILL.md` — Clean Architecture + DDD completo
- [x] Ejecutable standalone funcional (`temper-ai.exe`)
- [x] Estructura de assets embebidos funcionando

### ⏳ Próximo paso inmediato

**Escribir los agentes SDD** — empezando por `temper-init.agent.md`:

El agente `temper-init` debe:
1. Verificar si existe un `PRD.md` en el directorio actual
2. Si no existe, hacer preguntas para construirlo colaborativamente
3. Hacer TODAS las preguntas necesarias antes de generar cualquier archivo
4. Generar `.temper/constitution.md` con stack, arquitectura elegida, y estándares
5. Mostrar el resumen y pedir aprobación antes de continuar
6. Al aprobarse, indicar que se puede ejecutar `/temper-spec`

Formato del archivo de agente para Copilot CLI: `~/.copilot/agents/temper-init.agent.md`

### 📋 Backlog ordenado

1. [ ] Escribir `temper-init.agent.md` — Fase 1 del SDD
2. [ ] Escribir `temper-spec.agent.md` — Fase 2
3. [ ] Escribir `temper-design.agent.md` — Fase 3
4. [ ] Escribir `temper-tasks.agent.md` — Fase 4
5. [ ] Escribir `temper-backend.agent.md` — Fase 5a (carga Skills .NET)
6. [ ] Escribir `temper-frontend.agent.md` — Fase 5b (carga Skills Blazor)
7. [ ] Escribir `temper-tester.agent.md` — Fase 5c
8. [ ] Escribir `temper-devops.agent.md` — Fase 5d
9. [ ] Escribir `temper-review.agent.md` — Fase 6
10. [ ] Escribir `temper-docs.agent.md` — Fase 7
11. [ ] Escribir Skill `architecture/hexagonal/SKILL.md`
12. [ ] Escribir Skill `architecture/vertical-slice/SKILL.md`
13. [ ] Escribir Skill `architecture/onion/SKILL.md`
14. [ ] Escribir Skill `skills/blazor/SKILL.md`
15. [ ] Escribir Skill `skills/dotnet-api/SKILL.md` (hay un draft, necesita revisión)
16. [ ] Escribir Skill `skills/prd-analyzer/SKILL.md`
17. [ ] Construir `TemperAI.NeuralCore` — memoria persistente MCP en .NET
18. [ ] Agregar comando `temper-ai update` — actualiza Skills desde versión nueva
19. [ ] Agregar comando `temper-ai status` — muestra qué está instalado

---

## Prompt de continuación para otras IAs

Si estás leyendo esto para continuar el trabajo, usá este prompt como contexto inicial:

```
Estoy trabajando en TemperAI, un configurador de ecosistema AI para desarrolladores .NET.
Lee el archivo TEMPER_AI_PROYECTO.md que te voy a pasar — contiene toda la documentación
del proyecto, las decisiones de arquitectura tomadas, las convenciones de código, y el
estado actual.

IMPORTANTE:
- Todo el código es C# / .NET 10
- Usamos Spectre.Console para la TUI del CLI
- Las convenciones de código están en la sección 9 — respétalas siempre
- El próximo paso está en la sección 10
- No uses primary constructors en nada
- No uses return expression => en métodos
- Los casos de uso no tienen sufijo UseCase
- Los DTOs siempre son sealed record con propiedades explícitas y sufijo Dto

El próximo paso es: [DESCRIBÍ QUÉ QUERÉS HACER]
```

---

*Documento generado el 03/04/2026. Actualizar este archivo cuando se completen items del backlog.*
