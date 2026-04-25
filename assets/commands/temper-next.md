---
name: temper-next
description: >
  Lee .temper/orchestrator-state.md para determinar el estado actual del proyecto.
  Si existe, lo usa como fuente de verdad para avanzar a la siguiente fase.
  Si no existe, detecta la fase leyendo los archivos .temper/ existentes.
  Lanza el agente correspondiente para la siguiente fase del workflow SDD de TemperAI.
  Si el workflow esta completo, informa al usuario sin gastar tokens.
---

# /temper-next

Avanza al siguiente paso del workflow SDD de TemperAI de forma inteligente.

## Que hace

1. Lee `.temper/orchestrator-state.md` (fuente de verdad principal).
2. Si no existe, detecta la fase leyendo los archivos `.temper/` existentes.
3. Determina cual es el proximo paso.
4. Si el workflow esta completo, informa al usuario sin lanzar agentes.
5. Si hay trabajo pendiente, lanza el agente correspondiente.

## Fuente de verdad: orchestrator-state.md

Si `.temper/orchestrator-state.md` existe, usalo SIEMPRE. No necesitas inferir la fase por los archivos presentes.

| Campo en state.md | Accion |
|---|---|
| `Status: complete` + `Pending phases: none` | Informar al usuario que todo esta completo. No lanzar agentes. |
| `Current phase: analyst` | Lanzar `temper-analyst` |
| `Current phase: architect` | Lanzar `temper-architect` |
| `Current phase: spec` | Lanzar `temper-spec` |
| `Current phase: design` | Lanzar `temper-design` |
| `Current phase: tasks` | Lanzar `temper-tasks` |
| `Current phase: plan` | Lanzar `temper-plan` |
| `Current phase: build` + `Build group: N of M` | Lanzar `temper-orchestrator` para ejecutar Group N |
| `Current phase: review` | Lanzar `temper-review` |
| `Current phase: docs` | Lanzar `temper-docs` |
| `Status: blocked` | Informar al usuario del bloqueo y recomendar accion |

## Fallback: deteccion por archivos (si no existe state.md)

Si `.temper/orchestrator-state.md` NO existe, detecta la fase por los archivos presentes:

| Archivos existentes | Fase detectada | Proximo paso | Agente a lanzar |
|---|---|---|---|
| Ningun archivo `.temper/` | Fase 0 — Sin iniciar | Fase 1 — Analisis funcional | `temper-analyst` |
| Solo `prd.md` | Fase 1 — Completada | Fase 2 — Arquitectura tecnica | `temper-architect` |
| `prd.md` + `backend-config.md` | Fase 2 — Completada | Fase 3 — Especificacion | `temper-spec` |
| `prd.md` + config files + `specs/INDEX.md` | Fase 3 — Completada | Fase 4 — Diseno | `temper-design` |
| `prd.md` + config files + `specs/` + `design.md` | Fase 4 — Completada | Fase 5 — Tareas | `temper-tasks` |
| `prd.md` + config files + `specs/` + `design.md` + `tasks/INDEX.md` | Fase 5 — Completada | Fase 6 — Plan | `temper-plan` |
| Todos los anteriores + `build-plan.md` | Fase 6 — Plan completado | Build Execution | `temper-orchestrator` (ejecuta Group 1) |
| Todos los anteriores + codigo generado | Build completado | Fase 7 — Revision | `temper-review` |
| Todos los archivos + revision aprobada | Revision completada | Fase 8 — Documentacion | `temper-docs` |
| Todos los archivos + documentacion | Workflow completo | — | Informar al usuario |

## Instrucciones para el agente

1. **Primero:** Intentar leer `.temper/orchestrator-state.md`.
2. **Si existe:** Usar los campos `Status`, `Current phase`, `Build group`, y `Pending phases` para determinar la accion.
3. **Si no existe:** Usar la tabla de fallback para detectar la fase por archivos presentes.
4. **Si el workflow esta completo** (todas las fases hechas), informar al usuario:
   > "✅ El workflow SDD esta completo. El proyecto tiene PRD, configuracion tecnica, especificacion, diseno, tareas implementadas, plan de build, revision de calidad y documentacion. No hay fases pendientes."
5. **Si hay trabajo pendiente:** Lanzar el agente correspondiente.
6. **Siempre:** Recordar al usuario que cada fase se ejecuta en una sesion nueva para mantener el contexto limpio.

## Agentes del workflow

- Fase 1 → `temper-analyst` (analisis funcional, genera prd.md)
- Fase 2 → `temper-architect` (decisiones tecnicas, genera config files)
- Fase 3 → `temper-spec` (user stories)
- Fase 4 → `temper-design` (arquitectura)
- Fase 5 → `temper-tasks` (tareas atomicas)
- Fase 6 → `temper-plan` (genera build-plan.md)
- Build → `temper-orchestrator` (lee state.md + build-plan.md, ejecuta UN grupo, actualiza state.md, termina)
- Fase 7 → `temper-review`
- Fase 8 → `temper-docs`

## Mensaje al usuario

Antes de lanzar el agente, mostrar:

```
Fase actual: [nombre de la fase]
Proximo paso: [nombre del proximo paso]
Agente: [nombre del agente]

⚡ Nota: Cada fase se ejecuta en una sesion nueva para mantener el contexto limpio.
Iniciando [agente]...
```

Si el workflow esta completo:

```
✅ Workflow completo

Todas las fases del SDD workflow han sido ejecutadas exitosamente.
El proyecto esta listo para desarrollo iterativo.

Archivos generados:
- .temper/prd.md
- .temper/backend-config.md
- .temper/frontend-config.md (si aplica)
- .temper/specs/INDEX.md
- .temper/specs/US-XXX-*.md
- .temper/design.md
- .temper/tasks/INDEX.md
- .temper/tasks/US-XXX/T###-*.md
- .temper/build-plan.md
- .temper/orchestrator-state.md
- .temper/budget.md
```
