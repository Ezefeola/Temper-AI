---
name: temper-status
description: >
  Lee todos los archivos .temper/ y muestra el estado actual del proyecto:
  fase actual, archivos existentes, progreso de tareas y fases pendientes.
---

# /temper-status

Muestra el estado actual del proyecto en el workflow SDD de TemperAI.

## Que hace

1. Lee todos los archivos en `.temper/`.
2. Determina en que fase esta el proyecto.
3. Muestra el progreso de tareas si existe `tasks.md`.
4. Lista las fases pendientes.

## Instrucciones para el agente

### Paso 1 — Verificar archivos .temper/ existentes

Listar cuales de estos archivos existen:
- `.temper/constitution.md`
- `.temper/spec.md`
- `.temper/design.md`
- `.temper/tasks.md`

### Paso 2 — Determinar fase actual

| Archivos existentes | Fase |
|---|---|
| Ninguno | Fase 0 — Sin iniciar |
| Solo `constitution.md` | Fase 1 — Inicializacion completada |
| `constitution.md` + `spec.md` | Fase 2 — Especificacion completada |
| `constitution.md` + `spec.md` + `design.md` | Fase 3 — Diseno completado |
| `constitution.md` + `spec.md` + `design.md` + `tasks.md` | Fase 4 — Tareas definidas |
| Todos + codigo generado | Fase 5 — Build en progreso o completado |
| Todos + revision aprobada | Fase 6 — Revision completada |
| Todos + documentacion | Fase 7 — Workflow completo |

### Paso 3 — Analizar tareas (si existe tasks.md)

Si `.temper/tasks.md` existe, contar:
- Total de tareas
- Tareas con estado `pending`
- Tareas con estado `in-progress`
- Tareas con estado `done`

### Paso 4 — Mostrar reporte

Mostrar el siguiente formato:

```markdown
## TemperAI — Project Status

### Current phase
**[Phase number and name]**

### Files
| File | Status |
|---|---|
| constitution.md | [Exists / Missing] |
| spec.md | [Exists / Missing] |
| design.md | [Exists / Missing] |
| tasks.md | [Exists / Missing] |

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
- [etc.]

### Next step
Run `/temper-next` to proceed to [next phase name].
```

### Si el workflow esta completo

Si todos los archivos existen y todas las tareas estan `done`:

```markdown
## TemperAI — Project Status

### Status: Complete

All phases of the SDD workflow have been completed.

### Files
| File | Status |
|---|---|
| constitution.md | Exists |
| spec.md | Exists |
| design.md | Exists |
| tasks.md | Exists (all done) |

### Tasks
| Status | Count |
|---|---|
| Done | [total] |
| Pending | 0 |

The project is ready for iterative development.
```
