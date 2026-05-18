---
name: temper-next
description: >
  Lee .temper/orchestrator-state.md para determinar el estado actual del proyecto.
  Si existe, lo usa como fuente de verdad para avanzar a la siguiente fase.
  Si no existe, detecta la fase leyendo los archivos Docs/ y Plan/ existentes.
  Lanza el agente correspondiente para la siguiente fase del workflow SDD de TemperAI.
  Si el workflow esta completo, informa al usuario sin gastar tokens.
---

# /temper-next

Avanza al siguiente paso del workflow SDD de TemperAI de forma inteligente.

## Que hace

1. Lee `.temper/orchestrator-state.md` (fuente de verdad principal).
2. Si no existe, detecta la fase leyendo los archivos `Docs/` y `Plan/` existentes.
3. Determina cual es el proximo paso.
4. Si el workflow esta completo, informa al usuario sin lanzar agentes.
5. Si hay trabajo pendiente, lanza el agente correspondiente.

## Fuente de verdad: orchestrator-state.md

Si `.temper/orchestrator-state.md` existe, usalo SIEMPRE. No necesitas inferir la fase por los archivos presentes.

| Campo en state.md | Accion |
|---|---|
| `Status: complete` + `Pending phases: none` | Informar al usuario que todo esta completo. No lanzar agentes. |
| `Current phase: analyst` (PRD) | Lanzar `temper-analyst` Phase 1 (PRD) |
| `Current phase: analyst` (Spec) | Lanzar `temper-analyst` Phase 2 (Spec) con skill spec-generator |
| `Current phase: architect` | Lanzar `temper-architect` |
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
| Ningun archivo `Docs/` ni `Plan/` | Fase 0 — Sin iniciar | Fase 1 — Analisis funcional | `temper-analyst` (Phase 1) |
| Solo `Docs/Functional-Analysis/PRD.md` | Fase 1 — PRD completado | Fase 2 — Especificacion (User Stories) | `temper-analyst` (Phase 2) con skill spec-generator |
| `Docs/Functional-Analysis/PRD.md` + `Plan/INDEX.md` + `Plan/User-Stories/` | Fase 2 — Spec completado | Fase 3 — Arquitectura tecnica | `temper-architect` |
| PRD + Plan user stories + config files + `Docs/Application/Domain/domain-model.md` | Fase 3 — Arquitectura completada | Fase 4 — Tareas | `temper-tasks` |
| PRD + config files + `Plan/INDEX.md` with task rows | Fase 4 — Tareas completadas | Fase 5 — Plan | `temper-plan` |
| Todos los anteriores + `Plan/BUILD.md` | Fase 5 — Plan completado | Build Execution | `temper-orchestrator` (ejecuta Group 1) |
| Todos los anteriores + codigo generado | Build completado | Fase 6 — Revision | `temper-review` |
| Todos los archivos + revision aprobada | Revision completada | Fase 7 — Documentacion | `temper-docs` |
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

- Fase 1 → `temper-analyst` Phase 1 (PRD, genera `Docs/Functional-Analysis/PRD.md`)
- Fase 2 → `temper-analyst` Phase 2 (Spec, genera `Plan/User-Stories/`)
- Fase 3 → `temper-architect` (decisiones tecnicas, genera config files + DDD docs)
- Fase 4 → `temper-tasks` (tareas atomicas)
- Fase 5 → `temper-plan` (genera `Plan/BUILD.md`)
- Build → `temper-orchestrator` (lee state.md + `Plan/BUILD.md`, ejecuta UN grupo, actualiza state.md, termina)
- Fase 6 → `temper-review`
- Fase 7 → `temper-docs`

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
- Docs/Functional-Analysis/PRD.md
- Docs/Application/Architecture/backend-config.md
- Docs/Application/Architecture/frontend-config.md (si aplica)
- Docs/Application/Domain/DDD-Vocabulary.md
- Docs/Application/Domain/domain-model.md
- Docs/Application/System/system-architecture.md
- Plan/INDEX.md
- Plan/User-Stories/US-XXX-[slug]/STORY.md
- Plan/BUILD.md
- .temper/orchestrator-state.md
- .temper/budget.md
```
