# NeuralCore — Project Memory Export

> Generated: 2026-04-13 19:16 UTC
> Project: Temper-AI

## Sessions

- **Temper-AI** — Active (2026-04-13)
- **Temper-AI** — Active (2026-04-13)
- **Temper-AI** — Active (2026-04-13)
- **Temper-AI** — Active (2026-04-13)
- **Temper-AI** — Active (2026-04-13)

## Observations

### [Discovery] Session summary: Actualizar skill DDD para enfoque pragmático sin ValueObjects

- **Date:** 2026-04-13 19:16
- **Topic:** session-summary

Goal: Actualizar skill DDD para enfoque pragmático sin ValueObjects
Discoveries: El proyecto usa DDD pragmático donde ValueObjects son overhead innecesario. Los tipos primitivos son preferidos por simplicidad y facilidad de mapeo a DB.
Accomplished: Se actualizó SKILL.md de DDD para reflejar enfoque pragmático: no ValueObjects, tipos primitivos directamente en entidades, validación en métodos factory/update.
Files changed: assets/skills/backend/dotnet/ddd/SKILL.md

---

### [Discovery] Session summary: Corregir permisos de JARVIS para escribir state file

- **Date:** 2026-04-13 19:08
- **Topic:** session-summary

Goal: Corregir permisos de JARVIS para escribir state file
Discoveries: El agente JARVIS tenía permiso `edit: deny` pero sus instrucciones decían que debía escribir `.temper/jarvis-state.json`. Había una contradicción.
Accomplished: Se cambió el permiso de `edit: deny` a `edit: allow` en temper-jarvis.agent.md. Ahora JARVIS puede escribir su state file correctamente.
Files changed: assets/agents/temper-jarvis.agent.md

---

### [Discovery] Session summary: Corregir agentes: Tony Stark referencia y APIBASE tasks

- **Date:** 2026-04-13 18:53
- **Topic:** session-summary

Goal: Corregir agentes: Tony Stark referencia y APIBASE tasks
Discoveries: Jarvis estaba llamando 'Tony' al usuario porque no había aclaración de que 'Tony Stark' es solo referencia de personaje. Las tareas de infraestructura base se generaban al final en lugar de al principio.
Accomplished: Se agregó nota en temper-jarvis para no llamar Tony al usuario. Se creó sección Phase 4.1 en temper-tasks para APIBASE tasks que van primero (T001-T00X) con infraestructura base.
Files changed: assets/agents/temper-jarvis.agent.md, assets/agents/temper-tasks.agent.md

---

### [Discovery] Session summary: Agregar regla de granularidad de tareas CRUD al agente temper-tasks

- **Date:** 2026-04-13 18:32
- **Topic:** session-summary

Goal: Agregar regla de granularidad de tareas CRUD al agente temper-tasks
Discoveries: El agente temper-tasks estaba agrupando múltiples operaciones CRUD en una sola task porque la regla 'Task = Feature' no especificaba que cada operación debe ser una task separada.
Accomplished: Se agregó la sección 'Task Granularity for CRUD Operations' al agente temper-tasks con la regla explícita: One operation = One task.
Files changed: assets/agents/temper-tasks.agent.md

---

### [Discovery] Session summary: Verificar y agregar regla para tipos anidados en DTOs

- **Date:** 2026-04-13 18:16
- **Topic:** session-summary

Goal: Verificar y agregar regla para tipos anidados en DTOs
Discoveries: No existía una regla ni ejemplo sobre tipos anidados en DTO_CONVENTIONS.md. Se agregó la sección 'Nested types in DTOs' con ejemplo de CreateProductRequestDto conteniendo BarcodeInfoDto.
Accomplished: Se agregó la nueva sección al archivo DTO_CONVENTIONS.md con la regla y ejemplo completo de tipos anidados en DTOs.
Files changed: assets/skills/backend/architecture/shared/DTO_CONVENTIONS.md

---

