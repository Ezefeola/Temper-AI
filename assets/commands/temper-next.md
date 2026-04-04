---
name: temper-next
description: >
  Lee .temper/tasks.md y detecta en que fase esta el proyecto.
  Lanza automaticamente el agente correspondiente para la siguiente fase
  del workflow SDD de TemperAI.
---

# /temper-next

Avanza al siguiente paso del workflow SDD de TemperAI de forma inteligente.

## Que hace

1. Lee los archivos `.temper/` existentes para detectar en que fase esta el proyecto.
2. Determina cual es el proximo paso basado en los archivos que existen y los que faltan.
3. Lanza el agente correspondiente para esa fase.

## Logica de deteccion de fase

| Archivos existentes | Fase actual | Proximo paso | Agente a lanzar |
|---|---|---|---|
| Ningun archivo `.temper/` | Fase 0 — Sin iniciar | Fase 1 — Inicializacion | `temper-init` |
| Solo `constitution.md` | Fase 1 — Completada | Fase 2 — Especificacion | `temper-spec` |
| `constitution.md` + `spec.md` | Fase 2 — Completada | Fase 3 — Diseno | `temper-design` |
| `constitution.md` + `spec.md` + `design.md` | Fase 3 — Completada | Fase 4 — Tareas | `temper-tasks` |
| `constitution.md` + `spec.md` + `design.md` + `tasks.md` | Fase 4 — Completada | Fase 5 — Build | `temper-build` |
| Todos los anteriores + codigo generado | Build completado | Fase 6 — Revision | `temper-review` |
| Todos los anteriores + revision aprobada | Revision completada | Fase 7 — Documentacion | `temper-docs` |
| Todos los archivos + documentacion | Workflow completo | — | Informar al usuario que el workflow esta completo |

## Instrucciones para el agente

1. Listar los archivos en `.temper/`:
   ```
   - constitution.md
   - spec.md
   - design.md
   - tasks.md
   ```
2. Comparar con la tabla de arriba para determinar la fase actual y el proximo paso.
3. Si hay tareas en `tasks.md` con estado `pending` o `in-progress`, informar al usuario y preguntar si quiere continuar con el build.
4. Si el workflow esta completo (todas las fases hechas), informar al usuario:
   > "El workflow SDD esta completo. El proyecto tiene constitucion, especificacion, diseno, tareas implementadas, revision de calidad y documentacion. No hay fases pendientes."
5. Si hay fases pendientes, lanzar el agente correspondiente:
   - Fase 1 → `temper-init`
   - Fase 2 → `temper-spec`
   - Fase 3 → `temper-design`
   - Fase 4 → `temper-tasks`
   - Fase 5 → `temper-build` (orquesta backend, frontend, tester, devops)
   - Fase 6 → `temper-review`
   - Fase 7 → `temper-docs`

## Mensaje al usuario

Antes de lanzar el agente, mostrar:

```
Fase actual: [nombre de la fase]
Proximo paso: [nombre del proximo paso]
Agente: [nombre del agente]

Iniciando [agente]...
```
