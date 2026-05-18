---
name: temper-status
description: >
  Lee .temper/orchestrator-state.md como fuente de verdad principal.
  Si no existe, lee los archivos Docs/ y Plan/ y muestra el estado actual del proyecto:
  fase actual, archivos existentes, progreso de tareas y fases pendientes.
---

# /temper-status

Muestra el estado actual del proyecto en el workflow SDD de TemperAI.

## Que hace

1. Lee `.temper/orchestrator-state.md` (fuente de verdad principal).
2. Si no existe, detecta el estado leyendo los archivos `Docs/` y `Plan/`.
3. Muestra el progreso de tareas si existe `Plan/INDEX.md`.
4. Lista las fases pendientes.

## Instrucciones para el agente

### Paso 1 — Intentar leer orchestrator-state.md

Si `.temper/orchestrator-state.md` existe, extraer:
- `Status` (in-progress / complete / blocked)
- `Current phase`
- `Build group` (si aplica)
- `Last completed task`
- `Last build status`
- `Next action`
- `Pending phases`

### Paso 2 — Fallback: verificar archivos Docs/ y Plan/ existentes

Si `.temper/orchestrator-state.md` NO existe, listar:
- `Docs/Functional-Analysis/PRD.md`
- `Docs/Application/Architecture/backend-config.md`
- `Docs/Application/Architecture/frontend-config.md` (si aplica)
- `Docs/Application/Domain/DDD-Vocabulary.md`
- `Docs/Application/Domain/domain-model.md`
- `Docs/Application/System/system-architecture.md`
- `Plan/INDEX.md`
- `Plan/User-Stories/`
- `Plan/BUILD.md`

### Paso 3 — Determinar fase actual (fallback)

| Archivos existentes | Fase |
|---|---|
| Ninguno | Fase 0 — Sin iniciar |
| Solo `Docs/Functional-Analysis/PRD.md` | Fase 1 — Analisis funcional completado |
| PRD + `Plan/User-Stories/` | Fase 2 — Especificacion completada |
| PRD + Plan + `Docs/Application/Architecture/backend-config.md` | Fase 3 — Arquitectura tecnica completada |
| PRD + Plan + config files + `Docs/Application/Domain/domain-model.md` | Fase 4 — Diseno completado |
| PRD + config files + `Plan/INDEX.md` with task rows | Fase 5 — Tareas definidas |
| Todos los anteriores + `Plan/BUILD.md` | Fase 6 — Plan completado |
| Todos + codigo generado | Build Execution — En progreso o completado |
| Todos + revision aprobada | Fase 7 — Revision completada |
| Todos + documentacion | Fase 8 — Workflow completo |

### Paso 4 — Analizar tareas (si existe Plan/INDEX.md)

Si `Plan/INDEX.md` existe, contar:
- Total de tareas
- Tareas con estado `pending`
- Tareas con estado `in-progress`
- Tareas con estado `done`

### Paso 5 — Mostrar reporte

**Si orchestrator-state.md existe:**

```markdown
## TemperAI — Project Status

### Current state (from orchestrator-state.md)
**Status:** [in-progress / complete / blocked]
**Current phase:** [phase name]
**Build group:** [N of M] (if applicable)
**Last completed:** [task or phase]
**Last build:** [ok / failed / not-applicable]
**Next action:** [description]

### Pending phases
- [Phase X]
- [Phase Y]
```

**Si no existe state.md (fallback):**

```markdown
## TemperAI — Project Status

### Current phase
**[Phase number and name]**

### Files
| File | Status |
|---|---|
| Docs/Functional-Analysis/PRD.md | [Exists / Missing] |
| Docs/Application/Architecture/backend-config.md | [Exists / Missing] |
| Docs/Application/Architecture/frontend-config.md | [Exists / Missing / N/A] |
| Plan/User-Stories/ | [Exists / Missing] |
| Docs/Application/Domain/domain-model.md | [Exists / Missing] |
| Plan/INDEX.md | [Exists / Missing] |
| Plan/BUILD.md | [Exists / Missing] |

### Tasks
| Status | Count |
|---|---|
| Done | [count] |
| In Progress | [count] |
| Pending | [count] |
| **Total** | **[total]** |

### Remaining phases
- [Phase X — Name]
- [Phase Y — Name]

### Next step
Run `/temper-next` in a new session to proceed to [next phase name].
```

### Si el workflow esta completo

```markdown
## TemperAI — Project Status

### Status: Complete ✅

All phases of the SDD workflow have been completed.

### Files
| File | Status |
|---|---|
| Docs/Functional-Analysis/PRD.md | Exists |
| Docs/Application/Architecture/backend-config.md | Exists |
| Docs/Application/Architecture/frontend-config.md | Exists (or N/A) |
| Plan/User-Stories/ | Exists |
| Docs/Application/ | Exists |
| Plan/INDEX.md | Exists (all done) |
| Plan/BUILD.md | Exists |
| orchestrator-state.md | Exists (status: complete) |

### Tasks
| Status | Count |
|---|---|
| Done | [total] |
| Pending | 0 |

The project is ready for iterative development.
```
