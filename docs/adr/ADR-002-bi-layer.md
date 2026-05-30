# ADR-002 — Capa BI: Power BI → DataBision Native BI (React + ECharts)

**Fecha:** 2026-05-29  
**Estado:** Aceptado  
**Autor:** Chief Architect  

---

## Contexto

El diseño original de DataBision usaba Power BI como motor de visualización. La estrategia evolucionó por fases:

1. **Diseño original:** Power BI Embedded con Premium capacity
2. **Revisión de costos:** Power BI Pro + Import Mode + workspace por cliente
3. **Decisión actual:** DataBision Native BI con React + ECharts como núcleo del producto

El cambio no es incremental. Es una redefinición del producto.

---

## Problema con Power BI como núcleo

### Costo
- Power BI Embedded Premium: USD 4.995/mes (inviable para MVP)
- Power BI Pro por usuario del cliente: USD 10/usuario/mes (dependencia externa)
- Licencia Pro de DataBision para crear/publicar reportes: USD 10/mes (aceptable)

### Dependencia y control
- DataBision no controla el ciclo de release de Power BI
- Microsoft puede cambiar precios, APIs, o deprecar features
- El cliente necesita cuenta Microsoft para acceder a los reportes
- El branding queda limitado a lo que Power BI permite customizar

### Producto
- El cliente ve "Power BI" — no ve "DataBision"
- Difícil diferenciarse de un consultor SAP que también usa Power BI
- El valor de DataBision está en la extracción + pipeline, no en el visualizador de Microsoft
- No hay control sobre la experiencia: refresh delays, errores de Power BI, outages de PBI Service

### Restricciones técnicas
- Import Mode: máximo 8 refreshes/día con Pro
- Dataset en memoria de Power BI: límite 1 GB
- RLS en Power BI: un bug en DAX = data breach entre tenants
- Embed tokens no invalidables una vez emitidos

---

## Opciones Evaluadas

### Opción A — Power BI Embedded (diseño original)
- Requiere Premium capacity desde el día 1
- Inviable por costo para USD 350/mes

### Opción B — Power BI Pro + Workspace por cliente
- Viable en costo pero no cubre el branding
- Cliente debe tener licencia Pro propia o DataBision la provee
- Dificulta la diferenciación del producto
- Documenta el "qué pasa si el cliente ya usa Power BI": add-on válido

### Opción C — DataBision Native BI (decisión tomada)
- React + ECharts: sin costo de licencias, control total
- El portal ES el producto, no un contenedor de Power BI
- Branding 100% del cliente sin restricciones
- Ciclo de release controlado por DataBision
- Datos servidos por DataBision API propio → no depende de credenciales Microsoft

### Opción D — Metabase / Superset (open source BI)
- Soluciones self-hosted complejas de operar
- Branding limitado
- El cliente accede a Metabase/Superset, no a DataBision
- Descartado: agrega complejidad operacional sin agregar diferenciación

---

## Decisión

**Opción C — DataBision Native BI con React + ECharts.**

ECharts es la librería de visualización más madura del ecosistema React/TypeScript:
- Apache License 2.0 (sin restricciones comerciales)
- 30+ tipos de gráficos incluyendo complejos (Sankey, Sunburst, geo, 3D)
- WebGL rendering para datasets grandes
- Documentación excelente, comunidad activa
- `echarts-for-react` como wrapper oficial

---

## Consecuencias

### Lo que se construye en Fase 1

**Visualizaciones MVP (Fase 1 — 8 semanas):**

| Componente | Tipo ECharts | Datos SAP |
|---|---|---|
| KPI Card Ventas Mes | Texto + variación % | `fact.ventas` + período anterior |
| Tendencia mensual | `line` (12 meses) | `fact.ventas` agrupado por mes |
| Top N Clientes | `bar` horizontal | `fact.ventas` agrupado por `CardCode` |
| Top N Items | `bar` horizontal | `fact.ventas` agrupado por `ItemCode` |
| Distribución Vendedores | `bar` o `pie` | `fact.ventas` agrupado por `SlpCode` |
| Tabla de Facturas | `table` paginada | `raw.sap_oinv` con filtros |
| Estado del Extractor | `table` de checkpoints | `ctl.ingest_checkpoint` |

**Visualizaciones Fase 2:**
- Notas de crédito vs facturas
- Estado de cartera (aging)
- Heatmap de actividad (días/semanas)
- Gauge de cumplimiento de meta mensual

### Power BI como add-on

Power BI no se elimina del roadmap. Se reposiciona como:
- **Add-on para clientes que ya tienen Power BI Pro** (empresa del cliente tiene licencias)
- **Compatibilidad:** DataBision puede seguir sirviendo un endpoint de embed como opción
- **Documentado en:** `powerbi-pro-import-mode-strategy.md` (renombrar a `powerbi-addon-strategy.md`)

### API de datos

El DataBision API necesita endpoints analíticos que antes eran resueltos por Power BI Service:

```
GET /api/analytics/sales/kpis?period=current_month
GET /api/analytics/sales/trend?months=12
GET /api/analytics/customers/ranking?top=10&period=ytd
GET /api/analytics/items/ranking?top=10&period=current_month
GET /api/analytics/salespeople/summary?period=current_month
GET /api/analytics/extractor/status
```

Estos endpoints consultan `fact.*` y `stg.*` en Supabase con `company_id` del JWT.

### Riesgo: alcance de features BI

**Riesgo real:** construir un motor BI nativo es significativamente más complejo que embeber Power BI. Un dashboard de ventas básico toma 1–2 semanas. Filtros cruzados entre charts toman 2–4 semanas adicionales.

**Mitigación:**
- Fase 1 apunta a 3–4 tipos de gráficos y 1–2 dashboards
- No intentar replicar Power BI; construir dashboards operacionales específicos
- El catálogo de visualizaciones crece con cada cliente nuevo
- Criterio de "vendible": cliente puede tomar decisiones de negocio con los gráficos entregados

---

## Documentos Afectados

- `phase-3-bi-architecture.md` — SUPERSEDED como núcleo. Aplicable solo como referencia si se ofrece Power BI add-on en el futuro.
- `powerbi-pro-import-mode-strategy.md` — Reposicionar como estrategia de add-on, no producto principal.
- `commercial-mvp-strategy.md` — Actualizar descripción del producto para reflejar BI nativo.
