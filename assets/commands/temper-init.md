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
5. El orchestrator lanza `temper-architect` para definir stack tecnico y arquitectura.
6. `temper-architect` genera `.temper/backend-config.md` y `.temper/frontend-config.md`.
7. Despues se puede continuar con `/temper-spec`.

## Flujo

```
temper-analyst → preguntas funcionales → prd.md → temper-architect → preguntas tecnicas → backend-config.md + frontend-config.md
```

## Instrucciones para el orchestrator

1. Lanzar `temper-analyst` con la descripcion del proyecto del usuario.
2. Esperar a que analyst haga todas las preguntas funcionales y genere el PRD.
3. Mostrar el PRD y pedir aprobacion.
4. Lanzar `temper-architect` con el PRD aprobado.
5. Esperar a que architect haga las preguntas tecnicas y genere los config files.
6. Mostrar los config files y pedir aprobacion.
7. Indicar el proximo paso: `/temper-spec`.
