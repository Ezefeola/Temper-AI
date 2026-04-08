---
name: temper-init
description: >
  Inicia el workflow SDD de TemperAI con la fase de descubrimiento.
  Ahora este comando lanza temper-discover que hace las preguntas de clarification.
---

# /temper-init

Inicia el workflow SDD de TemperAI desde cero.

## Que hace

1. El orchestrator lanza `temper-discover` para analizar lo que el usuario quiere construir.
2. `temper-discover` hace preguntas iterativas hasta que todo está claro.
3. Una vez claro, el orchestrator lanza `temper-constitution` para generar `.temper/constitution.md`.
4. El usuario aprueba la constitución.
5. Después se puede continuar con `/temper-spec`.

## Flujo

```
temper-discover → preguntas → temper-constitution → constitution.md
```

## Instrucciones para el orchestrator

1. Lanzar `temper-discover` con la descripción del proyecto del usuario.
2. Esperar a que discover haga todas las preguntas yclarezca todo.
3. Lanzar `temper-constitution` con la info aclarada.
4. Mostrar la constitución y pedir aprobación.
5. Indicar el próximo paso: `/temper-spec`.