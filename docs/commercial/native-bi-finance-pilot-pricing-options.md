# DataBision Native BI Finance — Pricing Options

**Sprint:** 21F  
**Fecha:** 2026-06-20  
**Uso:** Referencia interna de precios y estructura comercial

---

## Pricing Piloto

| Opción | Nombre | Precio (PEN) | Precio (USD ~) | Duración |
|---|---|---|---|---|
| A | Diagnóstico SAP B1 | S/ 1,500 + IGV | ~USD 400 | 5 días |
| B | Piloto 2 semanas | S/ 4,500 + IGV | ~USD 1,200 | 10 días |
| C | Implementación producción | Desde S/ 12,000 + IGV | ~USD 3,200+ | 4-8 semanas |

*IGV Perú: 18%. Ajustar para Colombia (IVA 19%), Chile (IVA 19%), etc.*

---

## Pricing Suscripción Anual (post-piloto)

A definir en Sprint 22, pero estructura referencial:

| Plan | Para quién | Precio estimado mensual |
|---|---|---|
| Starter | 1 empresa, 1 módulo (finanzas) | S/ 800/mes |
| Professional | 1 empresa, 3 módulos | S/ 1,800/mes |
| Enterprise | Multi-empresa o implementación a medida | Cotización |

*El piloto exitoso puede descontarse del primer mes de suscripción.*

---

## Estructura de costos internos (referencia)

| Actividad | Tiempo estimado | Recurso |
|---|---|---|
| D0: Setup + validación técnica | 3h | Consultor técnico |
| D1: Extracción + pipeline | 2h | Consultor técnico |
| D2: Validación con contador | 3h (incluye cliente) | Consultor técnico |
| D3: Ajustes + re-validación | 2h | Consultor técnico |
| D4: Capacitación + entrega | 2h | Consultor técnico |
| D5: Soporte post-entrega | 1h | Consultor técnico |
| **Total por piloto B** | **~13h** | — |

A S/ 300/h para consultor técnico DataBision:
- Costo interno: S/ 3,900
- Precio Opción B: S/ 4,500 → margen ~15%
- Precio Opción A: S/ 1,500 → se cubre con 5h de trabajo

---

## Reglas de descuento

- Opción A → Opción B: descuento S/ 500 (descontar el diagnóstico del piloto)
- Opción B → suscripción anual: primer mes gratis
- Referido: 10% descuento para el cliente referido + 10% para el referidor
- Early adopter (primeros 3 clientes): 20% descuento en Opción B

---

## Objections frecuentes y respuestas

| Objeción | Respuesta |
|---|---|
| "Es muy caro" | "¿Cuánto tiempo tarda hoy generar el Estado de Resultados mensual? Si son 8 horas de contador a S/ 80/h, DataBision se paga en 7 meses." |
| "¿Y si SAP no conecta?" | "Por eso ofrecemos Opción A: validamos primero. Si no conecta, no hay piloto completo y se reembolsa." |
| "No tenemos IT interno" | "DataBision se instala en nuestros servidores. El cliente solo necesita darnos las credenciales SAP." |
| "¿Cuánto tarda actualizarse?" | "Hoy: manual (el consultor ejecuta). Con scheduler: automático diario. Tiempo de ejecución: 5-30 minutos según el tamaño del libro diario." |
| "¿Qué pasa si me voy de DataBision?" | "Los datos son del cliente. Se exportan en CSV antes de salir. No hay lock-in." |
