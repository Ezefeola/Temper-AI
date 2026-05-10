jarvis-prompt-excellence
========================

PROPÓSITO:
Dotar a JARVIS de principios de elite para construir prompts
de clase mundial. Cada prompt debe ser:
- Claro (sin ambigüedad)
- Conciso (mínimo necesario)
- Completo (sin gaps que generen suposiciones)
- Contextualizado (en lenguaje del dominio, nunca técnico)

PRINCIPIOS DE EXCELENCIA
========================

1. CLARIDAD SOBRE ELEGCANCIA
   → Un prompt mediocre dice "hazlo bien"
   → Un prompt excelente dice PRECISAMENTE qué success y qué failure
   → Nunca asumas que el agent "sabe lo que quiere decir"

2. EL MÍNIMO NECESARIO
   → No es "todo lo que sabes", es "exactamente lo que necesita"
   → Contexto faltante = suposiciones = errores
   → Contexto excedente = ruido = distracción

3. LENGUAJE DE DOMINIO, NUNCA DE IMPLEMENTACIÓN
   → "El pedido tiene estado: Pendiente, Confirmado, Cancelado"
   → NUNCA: "Crear OrderStatus enum en Domain/Enums/"
   → El agent traduce dominio → implementación

4. SIN AMBIGÜEDAD
   → Si puede interpretarse de dos formas, AGREGAR CONTEXTO
   → Si no estás seguro de qué necesita, PREGUNTA primero
   → "No sé" es acceptable. "Supongo" no lo es.

REGLAS DE ORO
=============

Para IMPLEMENTACIÓN (backend/frontend/tester/devops):

  CON TASK FILE:
    ✓ Formato: "Implement task T###: [title]"
    ✗ Sin contexto adicional

  REQUEST DIRECTO (sin task file):
    ✓ "Implementación: [descripción clara en lenguaje de dominio]"
    ✓ Context: [bounded context si el usuario lo specificó]
    ✗ Sin inferir arquitectura, stack, o estructura de archivos

Para ANÁLISIS (analyst):

  CON TASK FILE:
    ✓ PRD existente + gap report previo

  REQUEST DIRECTO:
    ✓ "Análisis: [request del usuario en sus propias palabras]"
    ✓ Context: [dominio/bounded context conocido]
    ✗ NO inferir gaps — si falta algo, preguntar

Para ARQUITECTURA (architect):

  CON TASK FILE:
    ✓ PRD + specs existentes

  REQUEST DIRECTO:
    ✓ "Arquitectura: [request de decisión técnica]"
    ✓ Context: [dominio o problema conocido]
    ✗ NO pre-definir soluciones técnicas

CHECKLIST PRE-DELEGACIÓN
=========================

Antes de enviar CUALQUIER prompt, verificar:

□ ¿El request del usuario es claro y sin ambigüedad?
□ Si no lo es, ¿ya pregunté las aclaraciones necesarias?
□ ¿El prompt dice qué SUCCESS y qué FAIL?
□ ¿El agent tiene todo lo que necesita para empezar?
□ ¿Hay algo que podría malinterpretarse?
□ ¿Estoy incluyendo implementación (cómo) cuando solo debería dar dominio (qué)?
□ ¿El lenguaje es del negocio, no técnico?

AUDITORÍA FINAL
===============

Pregunta de calidad:

"Si este prompt cae en manos del mejor agent del mundo,
¿tiene todo lo que necesita para entregar lo que pido,
o tiene que hacer suposiciones?"

→ Si tiene que suponer → FAIL, volver a escribir
→ Si no tiene que suponer → PASS, delegar

FALLOS COMUNES A EVITAR
========================

1. Prompt vago: "Implementa el módulo de usuarios"
   → El agent no sabe qué necesita. PREGUNTAR primero.

2. Sobre-explicación para tasks:
   "Implementa T001. El producto tiene nombre, descripción,
   precio, categoría..."
   → El agent leerá el task file. No repetir.

3. Inyección de implementación:
   "Agregar Product al inventory, crear Product entity en
   Domain/, usar EF Core..."
   → Esto es HOW, no WHAT. PROHIBIDO.

4. Contexto faltante para análisis:
   "Analizá los requisitos"
   → Necesita el request del usuario, no solo el título.

5. Suponer bounded context:
   "Endpoint para listar productos"
   → ¿A qué dominio pertenece? PREGUNTAR si no está claro.