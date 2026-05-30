# ADR-005 — Revisión de Precios: USD 300/500/1000+ → USD 350/600/1000+

**Fecha:** 2026-05-29  
**Estado:** Aceptado  
**Autor:** Chief Architect  

---

## Contexto

El plan de precios original en `commercial-mvp-strategy.md` definía:
- Starter: USD 300/mes
- Business: USD 500/mes
- Advanced: USD 1.000+/mes

Con el nuevo stack tecnológico (Supabase + React/ECharts) y la decisión de construir BI nativo, los costos de infraestructura bajaron pero el costo de desarrollo del producto propio es mayor. El ajuste de precios refleja el mayor valor entregado (producto propio, no un contenedor de Power BI).

---

## Decisión

| Plan | Precio anterior | Precio nuevo | Diferencia |
|---|---|---|---|
| Starter | USD 300/mes | USD 350/mes | +USD 50 |
| Business | USD 500/mes | USD 600/mes | +USD 100 |
| Advanced | USD 1.000+/mes | USD 1.000+/mes | Sin cambio |

---

## Justificación

### Starter (+USD 50)

- DataBision entrega un portal BI propio con branding del cliente
- No un iframe de Power BI (que requiere licencia Pro del cliente)
- El cliente no necesita pagar Power BI Pro (USD 10/usuario/mes adicionales)
- Valor neto para el cliente: neutral o positivo

**Costo infraestructura estimado Starter:** ~USD 65/mes  
**Margen Starter:** USD 285/mes (81%)

### Business (+USD 100)

- 7 objetos SAP completos
- Extracción cada hora
- Portal completo con historial de actualizaciones
- Hasta 15 usuarios sin costo adicional de licencias externas

**Costo infraestructura estimado Business:** ~USD 90/mes  
**Margen Business:** USD 510/mes (85%)

---

## Impacto en Propuesta de Valor

La propuesta de venta debe comunicar:

> "USD 350/mes incluye un portal de inteligencia operacional propio de su empresa, con datos actualizados automáticamente desde SAP B1, sin que sus usuarios necesiten licencias adicionales de ningún otro software."

vs. el modelo anterior:

> "USD 300/mes + sus usuarios necesitan Power BI Pro (USD 10/usuario/mes adicionales)"

---

## Documentos Afectados

- `commercial-mvp-strategy.md` — Tabla de precios y márgenes a actualizar
