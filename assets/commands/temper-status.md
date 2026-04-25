---
name: temper-status
description: >
  Lee .temper/orchestrator-state.md como fuente de verdad principal.
  Si no existe, lee los archivos .temper/ y muestra el estado actual del proyecto:
  fase actual, archivos existentes, progreso de tareas y fases pendientes.
---

# /temper-status

Muestra el estado actual del proyecto en el workflow SDD de TemperAI.

## Que hace

1. Lee `.temper/orchestrator-state.md` (fuente de verdad principal).
2. Si no existe, detecta el estado leyendo los archivos `.temper/`.
3. Muestra el progreso de tareas si existe `tasks/INDEX.md`.
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

### Paso 2 — Fallback: verificar archivos .temper/ existentes

Si `.temper/orchestrator-state.md` NO existe, listar:
- `.temper/prd.md`
- `.temper/backend-config.md`
- `.temper/frontend-config.md` (si aplica)
- `.temper/specs/INDEX.md`
- `.temper/design.md`
- `.temper/tasks/INDEX.md`
- `.temper/build-plan.md`

### Paso 3 — Determinar fase actual (fallback)

| Archivos existentes | Fase |
|---|---|
| Ninguno | Fase 0 — Sin iniciar |
| Solo `prd.md` | Fase 1 — Analisis funcional completado |
| `prd.md` + `backend-config.md` | Fase 2 — Arquitectura tecnica completada |
| `prd.md` + config files + `specs/INDEX.md` | Fase 3 — Especificacion completada |
| `prd.md` + config files + `specs/` + `design.md` | Fase 4 — Diseno completado |
| `prd.md` + config files + `specs/` + `design.md` + `tasks/INDEX.md` | Fase 5 — Tareas definidas |
| Todos los anteriores + `build-plan.md` | Fase 6 — Plan completado |
| Todos + codigo generado | Build Execution — En progreso o completado |
| Todos + revision aprobada | Fase 7 — Revision completada |
| Todos + documentacion | Fase 8 — Workflow completo |

### Paso 4 — Analizar tareas (si existe tasks/INDEX.md)

Si `.temper/tasks/INDEX.md` existe, contar:
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
| prd.md | [Exists / Missing] |
| backend-config.md | [Exists / Missing] |
| frontend-config.md | [Exists / Missing / N/A] |
| specs/INDEX.md | [Exists / Missing] |
| design.md | [Exists / Missing] |
| tasks/INDEX.md | [Exists / Missing] |
| build-plan.md | [Exists / Missing] |

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
| prd.md | Exists |
| backend-config.md | Exists |
| frontend-config.md | Exists (or N/A) |
| specs/ | Exists |
| design.md | Exists |
| tasks/ | Exists (all done) |
| build-plan.md | Exists |
| orchestrator-state.md | Exists (status: complete) |

### Tasks
| Status | Count |
|---|---|
| Done | [total] |
| Pending | 0 |

The project is ready for iterative development.
```
