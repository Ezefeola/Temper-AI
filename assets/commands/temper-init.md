---
name: temper-init
description: >
  Inicia el workflow SDD de TemperAI con la fase de analisis funcional.
  Lanza temper-analyst que hace preguntas de clarification funcional.
---

# /temper-init

Inicia el workflow SDD de TemperAI desde cero.

## Que hace

1. El orchestrator lanza `temper-analyst` para analizar lo que el usuario quiere construir.
2. `temper-analyst` hace preguntas funcionales iterativas hasta que todo esta claro (nunca tecnologia ni arquitectura).
3. `temper-analyst` genera `.temper/prd.md` con el alcance funcional.
4. El usuario aprueba el PRD.
5. El orchestrator lanza `temper-analyst` Phase 2 (Spec) con skill spec-generator para generar user stories.
6. El usuario aprueba los specs.
7. El orchestrator lanza `temper-architect` para definir stack tecnico y arquitectura.
8. `temper-architect` genera `.temper/backend-config.md` y `.temper/frontend-config.md`.
9. Despues se puede continuar con `/temper-tasks`.

## Flujo

```
temper-analyst Phase 1 → preguntas funcionales → prd.md →
temper-analyst Phase 2 → specs (user stories) →
temper-architect → preguntas tecnicas → backend-config.md + frontend-config.md
```

## Instrucciones para el orchestrator

1. Lanzar `temper-analyst` Phase 1 (PRD) con la descripcion del proyecto del usuario.
2. Esperar a que analyst haga todas las preguntas funcionales y genere el PRD.
3. Mostrar el PRD y pedir aprobacion.
4. Lanzar `temper-analyst` Phase 2 (Spec) con skill spec-generator para generar user stories.
5. Mostrar los specs y pedir aprobacion.
6. Lanzar `temper-architect` con los specs aprobados.
7. Esperar a que architect haga las preguntas tecnicas y genere los config files.
8. Mostrar los config files y pedir aprobacion.
9. Indicar el proximo paso: `/temper-tasks`.
