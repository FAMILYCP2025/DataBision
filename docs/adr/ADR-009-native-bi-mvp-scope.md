# ADR-009 — Native BI MVP Scope: Qué Se Construye y Cuándo

**Fecha:** 2026-05-30  
**Estado:** Aceptado  
**Autor:** Chief Architect  

---

## Contexto

DataBision construye su propio motor de visualización (Native BI con ECharts) en lugar de embeber Power BI. Esta decisión (ADR-002, ADR-006) implica que el equipo debe construir dashboards, gráficos y el pipeline de datos que los alimenta. Existe riesgo de scope creep: intentar replicar todas las capacidades de Power BI en el MVP.

Este ADR define qué se construye en cada fase y qué queda explícitamente fuera.

---

## Decisión

### Scope Fase 1 — MVP (Sprint 3, semanas 1–8)

**Tipos de gráficos:** máximo 4 tipos en el MVP.

| Tipo | Componente | Uso principal |
|---|---|---|
| Barras verticales / horizontales | `BarChart` | Rankings, comparativas por período |
| Línea temporal con área | `RevenueLineChart` | Tendencia de ventas mensual |
| KPI Card con sparkline | `KpiCard` | Métricas principales del dashboard |
| Tabla analítica paginada | `DataTable` | Detalle de facturas, clientes |

**Módulos:**
- Dashboard Home (4 KPIs + gráfico mensual + top 5 clientes)
- Módulo Ventas (barras mensuales + tabla de facturas)
- Módulo Clientes (tabla con últimas facturas, click en cliente)
- Sync Center básico (estado de última sincronización por tabla)

**Fuente de datos en Sprint 3:** queries directas a `raw.*` en Supabase. No requiere `fact.*` ni `StagingTransformWorker` operativo.

**NO incluir en MVP:**
- Editor de reportes drag-and-drop
- Builder de gráficos por el usuario
- Drill-through entre módulos (click en cliente desde Ventas → AR Aging)
- Comparativa interanual en todos los gráficos
- Gráficos complejos: Sankey, Treemap, Sunburst, scatter complejo

---

### Scope Fase 1.5 (semanas 8–16)

- Recomendaciones (insights + atribución + acción)
- Alertas de negocio
- Sección Insights en portal

### Scope Fase 2 (semanas 9–20)

- Módulos adicionales: AR Aging, Productos (márgenes, slow-movers)
- `StagingTransformWorker` operativo → migrar queries de `raw.*` a `fact.*`
- DonutChart y ScatterChart
- Export CSV básico
- Filtros contextuales por módulo

### Scope Fase 3 (meses 6–12)

- Inventario completo
- Operational Live Layer (/live/*)
- HeatmapChart para patrones temporales
- Export Excel y PDF

---

## Criterios para Agregar un Gráfico Nuevo

Un tipo de gráfico nuevo se añade cuando:
1. **Al menos 2 clientes activos lo solicitan** (no uno solo)
2. **ECharts lo soporta nativamente** (no requiere librería adicional)
3. **Existe un caso de uso específico de SAP B1 que lo justifica**

Si un cliente pide un tipo de gráfico no disponible, la respuesta es: "Está en nuestro roadmap. La data ya está ahí; el gráfico llega en la próxima actualización."

---

## Por Qué Este Scope

### Por qué tan reducido en MVP

1. **El valor del MVP no es el gráfico.** Es que los datos SAP del cliente estén en DataBision, actualizados, accesibles. El primer cliente valida el pipeline, no la riqueza visual.
2. **Cada tipo de gráfico nuevo = ~3-5 días de trabajo** (componente + tests + integración con filtros + loading/error states + responsive). En un equipo pequeño, esto es coste real.
3. **La DataTable es lo que más usan los usuarios SAP.** Los usuarios de SAP B1 están acostumbrados a tablas. Los gráficos son el bonus, no el core.

### Por qué ECharts y no otras librerías

- Apache License 2.0: sin costo, sin restricciones comerciales
- 30+ tipos de gráficos incluidos nativamente
- Canvas-based: performance con 10k+ puntos de datos
- `echarts-for-react`: wrapper oficial mantenido
- Dark mode nativo con tema configurable

---

## Documentos relacionados

- [native-bi-architecture.md](../native-bi-architecture.md) — Diseño completo del sistema BI
- [ADR-002](ADR-002-bi-layer.md) — Power BI → Native BI
- [ADR-006](ADR-006-native-bi-vs-powerbi.md) — Por qué construir BI propio
