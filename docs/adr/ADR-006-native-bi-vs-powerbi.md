# ADR-006 — Por Qué Construir BI Propio en Lugar de Embeber Power BI

**Fecha:** 2026-05-29  
**Estado:** Aceptado  
**Autor:** Chief Architect  

---

## Contexto

Esta decisión es arquitectónicamente la más significativa del roadmap. Construir un motor BI propio es significativamente más complejo que embeber Power BI. Requiere justificación explícita.

---

## El argumento de "usar Power BI"

Power BI ofrece:
- Motor de visualización maduro con 30+ tipos de gráficos
- DAX: lenguaje de medidas poderoso
- Drill-through, bookmarks, slicers
- Mobile responsive nativo
- Millones de usuarios ya saben usarlo

Este es un argumento válido. Power BI es una herramienta excelente.

---

## Por qué DataBision NO puede depender de Power BI como núcleo

### 1. El cliente no es "el analista de Power BI"

El cliente objetivo de DataBision es una PyME SAP B1 con 2–15 usuarios que necesitan ver datos. No son analistas de BI. No tienen licencias Power BI. No quieren aprender DAX. Quieren dashboards específicos de su negocio SAP, actualizados automáticamente, sin fricción.

Power BI es una herramienta para analistas. DataBision es un producto para operaciones de negocio.

### 2. La licencia es una barrera de venta

Para usar Power BI en el portal de DataBision:
- **Opción Embed for Organization:** el usuario del cliente necesita licencia Azure AD + Power BI Pro (USD 10/usuario/mes pagados a Microsoft)
- **Opción Embed for Customers (Embedded):** DataBision necesita Premium capacity (USD 4.995/mes) o Fabric F SKU

Ambas opciones agregan costo o complejidad de venta. El comercial de DataBision tiene que explicar licencias de Microsoft, no el valor del producto.

### 3. Power BI no permite branding 100%

Power BI tiene restricciones de branding. El cliente siempre verá el logo de Microsoft Power BI en algún punto. DataBision quiere que el cliente diga "este es mi sistema", no "este es Power BI con nuestro logo".

### 4. La dependencia de Microsoft es un riesgo de negocio

- Microsoft puede cambiar el modelo de licencias (lo hizo varias veces en los últimos 5 años)
- Microsoft puede competir directamente con DataBision (SAP + Microsoft tienen acuerdos)
- Las APIs de Power BI Embedded cambian sin previo aviso suficiente
- Un outage de Power BI Service es un outage de DataBision

### 5. Los datos operacionales de SAP no son BI tradicional

Los dashboards que necesita el cliente de DataBision son:
- "¿Cuánto vendí este mes?"
- "¿Cuál es mi top 10 de clientes?"
- "¿Qué ítems no se han vendido en 60 días?"
- "¿Cuándo se actualizaron mis datos?"

Esto no requiere DAX, ni drill-through complejo, ni modelos tabulares. Requiere queries bien escritas sobre `fact.ventas` y gráficos claros. ECharts hace esto perfectamente.

---

## La apuesta de construir BI propio

### Costo real

| Componente | Estimación |
|---|---|
| Dashboard de ventas básico (KPI + tendencia + top N) | 1–2 semanas |
| Filtros de fecha cross-chart | 1 semana adicional |
| Tabla analítica paginada con sort | 3–5 días |
| Catálogo de reportes + navegación | 3–5 días |
| Estado del extractor (tabla de checkpoints) | 1–2 días |
| **Total Fase 1 BI nativo** | **~4–6 semanas** |

Este es un costo real, no trivial. Pero es un activo propio que se construye una vez y se monetiza con cada cliente.

### El diferenciador a largo plazo

- DataBision puede construir exactamente los dashboards que los clientes SAP B1 necesitan
- El ciclo de feedback es directo: cliente pide → DataBision añade → en el producto
- No hay intermediario (Microsoft) que filtre qué se puede hacer
- La IP del motor analítico pertenece a DataBision
- A medida que se acumulan clientes, los dashboards genéricos se vuelven activos compartibles entre todos

### Umbral de capacidad de ECharts

ECharts puede hacer todo lo que los clientes de DataBision necesitarán en los próximos 18–24 meses:
- Series de tiempo, barras, pie, treemap, scatter, gauge, heatmap
- Filtros, zoom, tooltips interactivos
- Responsive en mobile
- Export a imagen (PNG/SVG)
- Animaciones de entrada

Lo que ECharts NO hace tan bien (y que no es prioridad en Fase 1–3):
- DAX equivalente para medidas complejas (usar SQL en el backend)
- Drill-through entre reportes (implementable con React Router)
- Bookmarks personalizados por usuario (implementable en Fase 3)

---

## Power BI como Add-on Opcional

Para clientes que ya tienen Power BI Pro y quieren mantenerlo:
1. DataBision puede publicar un workspace de Power BI con los datos de Supabase
2. El portal puede mostrar un iframe de un reporte Power BI como tab adicional
3. Este es un add-on de implementación, no el producto principal

Esto no contradice la decisión de Native BI. Son dos capas complementarias.

---

## Criterio de Re-evaluación

Esta decisión debe re-evaluarse si:
- 3 o más clientes en negociación piden explícitamente Power BI como condición de cierre
- El costo de desarrollo de Native BI supera USD 50.000 sin llegar al producto vendible
- Microsoft ofrece un SKU de Embedded a un precio < USD 200/mes para 10–20 clientes

Hasta que ocurra alguno de los triggers anteriores, la decisión es firme.
