# NeuralCore — Project Memory Export

> Generated: 2026-06-07 23:26 UTC
> Project: temper-ai

## Sessions

- **Temper-AI** — Active (2026-04-13)
- **Temper-AI** — Active (2026-04-13)
- **Temper-AI** — Active (2026-04-13)
- **Temper-AI** — Active (2026-04-13)
- **Temper-AI** — Active (2026-04-13)
- **Temper-AI** — Active (2026-04-14)
- **temper-ai** — Active (2026-06-07)
- **temper-ai** — Active (2026-06-07)

## Observations

### [Discovery] Session summary: Update documentation to explain the current FRIDAY-centered TemperAI model and exclude JARVIS from supported documentation scope

- **Date:** 2026-06-07 23:26
- **Topic:** session-summary

Goal: Update documentation to explain the current FRIDAY-centered TemperAI model and exclude JARVIS from supported documentation scope
Discoveries: The repository already had a broad README and an outdated human skill catalog that still referenced JARVIS; active FRIDAY workflow and agent-skill relationships are defined across temper-friday.agent.md and specialist agent contracts.
Accomplished: Rewrote README around the supported FRIDAY model, added dedicated docs for workflow, agents, and skills, and replaced the human-readable skills catalog with a FRIDAY-centered version.
Files changed: README.md, docs/friday-workflow.md, docs/agents.md, docs/skills.md, assets/docs/skills-catalog.md

---

### [Decision] Document FRIDAY-centered supported model

- **Date:** 2026-06-07 23:26
- **Topic:** friday-docs

What: Rewrote human-facing documentation to center the supported TemperAI model on FRIDAY and exclude JARVIS from documented scope. Why: The current supported workflow is FRIDAY-based, and older JARVIS references in public docs created confusion about the intended model. Where: README.md, docs/friday-workflow.md, docs/agents.md, docs/skills.md, assets/docs/skills-catalog.md. Learned: Public docs should describe only active agent contracts and skill relationships that are wired into the supported orchestration flow.

---

### [Discovery] Session summary: Actualizar skills DDD para enfoque pragmático sin ValueObjects

- **Date:** 2026-04-14 14:28
- **Topic:** session-summary

Goal: Actualizar skills DDD para enfoque pragmático sin ValueObjects
Discoveries: Se encontraron secciones de ValueObjects en ddd/SKILL.md y en las 4 arquitecturas (clean, hexagonal, onion, vertical-slice). La regla de no usar VOs pertenece solo al skill DDD, no a arquitecturas.
Accomplished: Actualizado ddd/SKILL.md: reemplazada sección de VOs con 'NOT USED', clarificado que DomainEvents son contratos sueltos en Events/ folder por entidad, no van en la entidad. Eliminados ValueObjects/ de folder structures en las 4 arquitecturas. Removida regla de VOs de archivos de arquitectura.
Files changed: assets/skills/backend/dotnet/ddd/SKILL.md, assets/skills/backend/architecture/clean/SKILL.md, assets/skills/backend/architecture/hexagonal/SKILL.md, assets/skills/backend/architecture/onion/SKILL.md, assets/skills/backend/architecture/vertical-slice/SKILL.md

---

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

