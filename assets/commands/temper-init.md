---
name: temper-init
description: >
  Inicia el workflow SDD de TemperAI. Verifica si existe PRD.md y arranca
  la Fase 1 de inicializacion con el agente temper-init.
---

# /temper-init

Inicia el workflow SDD de TemperAI desde cero.

## Que hace

1. Verifica si existe `PRD.md` en el directorio actual.
2. **Si existe:** lo lee completo, analiza ambiguedades y hace preguntas de aclaracion.
3. **Si no existe:** construye el PRD colaborativamente haciendo preguntas agrupadas por categoria (vision, funcionalidades, tecnico, contexto).
4. Hace TODAS las preguntas necesarias antes de escribir cualquier archivo.
5. Genera `.temper/constitution.md` con stack, arquitectura elegida y estandares.
6. Muestra el resumen y pide aprobacion explicita del usuario.
7. Al aprobarse, indica que puede ejecutar `/temper-spec` para continuar.

## Instrucciones para el agente

- Carga la skill `prd-analyzer`.
- Segui el workflow del agente `temper-init` definido en `assets/agents/temper-init.agent.md`.
- Nunca asumas tecnologia, arquitectura, o funcionalidades sin confirmacion explicita.
- Nunca escribas archivos sin mostrar el contenido primero y pedir aprobacion.
- Siempre agrupa preguntas por categoria para minimizar token usage.
- Al finalizar, indica el proximo paso: `/temper-spec`.
