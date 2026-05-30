# DataBision — Native BI Architecture

**Versión:** 1.0  
**Fecha:** 2026-05-29  
**Estado:** Diseño funcional y técnico — sin implementación  
**Reemplaza:** `docs/phase-3-bi-architecture.md` (Power BI Embedded)

---

## 1. Decisión Estratégica

**Power BI Embedded → Native BI propio.**

| Criterio | Power BI Embedded | Native BI |
|---|---|---|
| Costo fijo | USD 735–5,889/mes (A1–A4) | USD 0 (stack ya pagado) |
| Control de UX | Limitado (iframe de MS) | Total |
| Customización por tenant | Solo temas básicos | Completo (white-label real) |
| Velocidad de iteración | Requiere .pbix + Service Principal | Deploy directo desde código |
| Dependencia externa | Alta (PBI outages, RLS bugs) | Ninguna |
| Onboarding de cliente | Complejo (workspace, SP, RLS DAX) | Agregar `company_id` en BD |
| Módulos custom por cliente | Costoso de mantener | Feature flags simples |
| Licensing | $10/user/mes (PPU) | Incluido en plan SaaS |

**Conclusión:** Native BI elimina el mayor blocker técnico (Service Principal), el mayor blocker de costo (A1+) y entrega white-label real. El riesgo es mayor superficie de desarrollo — mitigado por ECharts (charts completos) + shadcn/ui (componentes listos) + el data pipeline Supabase que ya existe.

---

## 2. Arquitectura de Alto Nivel

```
SAP Business One (HANA / SQL Server)
         │
         │  Extractor .NET Worker → Ingest API
         ▼
Supabase PostgreSQL
 ┌─────────────────────────────────────────┐
 │  raw.*   réplica idempotente SAP        │
 │  stg.*   datos limpios y tipados        │
 │  dim.*   dimensiones (clientes, items)  │
 │  fact.*  hechos calculados              │
 │  ctl.*   control ETL y checkpoints      │
 │  audit.* log de cambios                 │
 └─────────────────────────────────────────┘
         │
         │  Queries analíticas (Dapper + SQL parametrizado)
         ▼
DataBision Analytics API (.NET 8)
  /api/analytics/kpis
  /api/analytics/sales/**
  /api/analytics/customers/**
  /api/analytics/products/**
  /api/analytics/inventory/**
  /api/sync/status
         │
         │  JSON → TanStack Query (caché cliente)
         ▼
Portal React (TypeScript + Vite)
  shadcn/ui + ECharts + Tailwind
  {slug}.databision.app
         │
         ▼
Usuario Final del cliente
```

---

## 3. Stack Técnico por Capa

### 3.1. Backend (.NET 8)

| Componente | Tecnología | Justificación |
|---|---|---|
| Queries analíticas | Dapper + SQL parametrizado | Rendimiento sobre EF Core para agregaciones |
| Caché de respuestas | IMemoryCache → Redis | TTL por módulo según frecuencia de actualización |
| Auth | JWT RS256 existente | Sin cambio; `company_id` ya en el claim |
| Tenant isolation | Filtro `company_id` explícito en cada query | Regla #1 del CLAUDE.md |
| Audit | `AuditService` existente (`VIEW_REPORT` → `VIEW_ANALYTICS`) | Sin cambio de patrón |

### 3.2. Frontend (React + TypeScript)

| Componente | Tecnología |
|---|---|
| Componentes UI | shadcn/ui |
| Gráficos | Apache ECharts (via `echarts-for-react`) |
| Data fetching | TanStack Query v5 |
| Estado global | Zustand (auth + tenant, ya existentes) |
| Fechas | `date-fns` |
| Estilos | Tailwind CSS + CSS vars de branding |
| Routing | React Router v6 |

### 3.3. Base de Datos (Supabase PostgreSQL)

Mismo esquema definido en `docs/supabase-postgres-mvp-architecture.md`. Las tablas `fact.*` y `dim.*` son la fuente de verdad para todas las visualizaciones.

---

## 4. Estructura de Capas (.NET)

```
DataBision.Application/
  Interfaces/Analytics/
    IKpiService.cs
    ISalesAnalyticsService.cs
    ICustomerAnalyticsService.cs
    IProductAnalyticsService.cs
    IInventoryAnalyticsService.cs
    ISyncStatusService.cs
  DTOs/Analytics/
    KpiSummaryDto.cs
    SalesByPeriodDto.cs
    SalesByDimensionDto.cs
    ArAgingDto.cs
    TopCustomerDto.cs
    TopProductDto.cs
    InventorySnapshotDto.cs
    SyncStatusDto.cs
  Services/Analytics/
    KpiService.cs
    SalesAnalyticsService.cs
    CustomerAnalyticsService.cs
    ProductAnalyticsService.cs
    InventoryAnalyticsService.cs
    SyncStatusService.cs

DataBision.Infrastructure/
  Repositories/Analytics/
    KpiRepository.cs
    SalesAnalyticsRepository.cs
    CustomerAnalyticsRepository.cs
    ProductAnalyticsRepository.cs
    InventoryAnalyticsRepository.cs
    SyncStatusRepository.cs

DataBision.Api/
  Controllers/Analytics/
    KpiController.cs
    SalesController.cs
    CustomersController.cs
    ProductsController.cs
    InventoryController.cs
    SyncController.cs
```

**Regla de capas:** Los repositorios de Analytics solo hacen `SELECT`. Nunca escriben ni modifican datos del cliente. No cruzan el límite `company_id`.

---

## 5. Estructura Frontend

```
src/apps/portal/
  pages/
    home/
      DashboardHome.tsx        ← página raíz del portal
    sales/
      SalesOverview.tsx
      SalesByPeriod.tsx
      SalesBySalesperson.tsx
      SalesByCustomer.tsx
    customers/
      CustomersOverview.tsx
      ArAging.tsx
      CustomerDetail.tsx
    products/
      ProductsOverview.tsx
      ProductMargins.tsx
      SlowMoving.tsx
    inventory/
      InventoryOverview.tsx
      InventoryByWarehouse.tsx
      StockMovements.tsx
    sync/
      SyncStatus.tsx

  components/
    charts/
      RevenueLineChart.tsx     ← ECharts line/bar
      ArAgingBarChart.tsx      ← ECharts stacked bar
      PieDonutChart.tsx        ← ECharts pie
      HeatmapChart.tsx         ← ECharts heatmap
      ScatterChart.tsx         ← ECharts scatter
    kpi/
      KpiCard.tsx              ← número grande + delta + sparkline
      KpiGrid.tsx              ← grid de 4/6 KpiCards
    filters/
      DateRangePicker.tsx      ← presets: MTD, QTD, YTD, 12M, custom
      MultiSelectFilter.tsx    ← vendedor, cliente, categoría, almacén
      FilterBar.tsx            ← composición de filtros activos
    layout/
      AnalyticsLayout.tsx      ← sidebar + header + content
      Sidebar.tsx
      BreadcrumbNav.tsx
      DataFreshnessTag.tsx     ← "Datos al [timestamp]"

  hooks/
    useKpis.ts
    useSalesAnalytics.ts
    useCustomerAnalytics.ts
    useProductAnalytics.ts
    useInventoryAnalytics.ts
    useSyncStatus.ts
    useAnalyticsFilters.ts     ← estado de filtros globales por módulo
```

---

## 6. Modelo de Permisos

Extiende el sistema existente (`UserPermission` + `Module`). Cada módulo de BI es un `Module` con su propio `module_key`.

### 6.1. Módulos BI

| module_key | Nombre visible | Descripción |
|---|---|---|
| `bi:home` | Dashboard | KPIs resumen de la empresa |
| `bi:sales` | Ventas | Análisis de facturas y pedidos |
| `bi:customers` | Clientes | Cartera, aging AR, top clientes |
| `bi:products` | Productos | Rendimiento, márgenes, rotación |
| `bi:inventory` | Inventario | Stock, movimientos, alertas |
| `bi:sync` | Sincronización | Estado de la carga de datos SAP |

### 6.2. Niveles de acceso

| Nivel | Valor | Comportamiento |
|---|---|---|
| `none` | 0 | No aparece en sidebar. Endpoint devuelve 403. |
| `read` | 1 | Visualización completa. Sin export. |
| `export` | 2 | Visualización + exportar CSV/Excel. |
| `admin` | 3 | Todo + configurar alertas y umbrales. |

### 6.3. Roles por defecto al crear empresa

| Rol | Módulos con acceso |
|---|---|
| `CompanyAdmin` | Todos con nivel `export` |
| `Manager` | `bi:home`, `bi:sales`, `bi:customers`, `bi:products`, `bi:inventory` con `read` |
| `Viewer` | `bi:home` con `read` + módulos asignados manualmente |

### 6.4. Flujo de validación

```
[Request /api/analytics/sales/**]
       │
       ▼
[ValidateTenantClaim] → 401 si JWT inválido
       │
       ▼
[RequireModulePermission("bi:sales", PermissionLevel.Read)]
  → consulta UserPermission (user_id, module_key)
  → 403 si nivel < Read
       │
       ▼
[Handler usa company_id del JWT para filtrar queries]
```

El atributo `[RequireModulePermission]` ya existe como patrón en el proyecto (`ValidateTenantClaimAttribute`). Se extiende con la lógica de nivel de acceso.

---

## 7. API de Analytics — Contrato

### 7.1. Convenciones generales

- **Auth:** Bearer JWT en header. `company_id` se extrae del claim — no en el body.
- **Filtros:** Query params: `?from=2026-01-01&to=2026-05-29&salesperson_ids=1,2&warehouse_ids=3`
- **Respuesta exitosa:** `{ "data": T, "meta": { "generated_at": "ISO8601", "data_as_of": "ISO8601" } }`
- **Error:** `{ "error": "snake_case_code", "message": "Human readable" }`
- **Caché server-side:** `Cache-Control: private, max-age=300` (5 min por defecto; varía por módulo)

### 7.2. Endpoints por módulo

#### KPIs (Dashboard Home)
```
GET /api/analytics/kpis?from=&to=
→ KpiSummaryDto {
    revenue_mtd, revenue_ytd, revenue_vs_prev_period_pct,
    gross_margin_pct, gross_margin_vs_prev_pct,
    active_customers, new_customers_period,
    ar_outstanding, ar_overdue_pct,
    inventory_value, low_stock_items_count,
    open_orders_count, open_orders_value
  }
```

#### Ventas
```
GET /api/analytics/sales/by-period?from=&to=&granularity=month|week|day
→ SalesByPeriodDto[] { period, revenue, cost, gross_margin, units }

GET /api/analytics/sales/by-salesperson?from=&to=
→ SalesByDimensionDto[] { id, name, revenue, share_pct, orders_count }

GET /api/analytics/sales/by-customer?from=&to=&limit=20
→ SalesByDimensionDto[] { id, name, revenue, share_pct, orders_count }

GET /api/analytics/sales/by-product-category?from=&to=
→ SalesByDimensionDto[] { id, name, revenue, share_pct, units }

GET /api/analytics/sales/trend?from=&to=&compare_prev=true
→ { current: SalesByPeriodDto[], previous: SalesByPeriodDto[] }
```

#### Clientes
```
GET /api/analytics/customers/ar-aging?snapshot_date=
→ ArAgingDto {
    total_outstanding,
    bucket_current, bucket_1_30, bucket_31_60, bucket_61_90, bucket_90_plus,
    items: ArAgingItemDto[] { customer_id, name, ...buckets, days_overdue_avg }
  }

GET /api/analytics/customers/top?from=&to=&limit=20
→ TopCustomerDto[] { id, name, revenue, margin_pct, orders_count, last_order_date }

GET /api/analytics/customers/dso?from=&to=
→ { dso_days: number, trend: { period, dso_days }[] }
```

#### Productos
```
GET /api/analytics/products/top?from=&to=&limit=20&metric=revenue|units|margin
→ TopProductDto[] { id, code, name, revenue, units, margin_pct, category }

GET /api/analytics/products/margins?from=&to=&category_id=
→ ProductMarginDto[] { id, code, name, revenue, cost, margin, margin_pct }

GET /api/analytics/products/slow-moving?days_without_movement=90
→ SlowMovingProductDto[] { id, code, name, last_movement_date, stock_qty, stock_value }
```

#### Inventario
```
GET /api/analytics/inventory/snapshot?date=
→ InventorySnapshotDto {
    total_value, total_items,
    by_warehouse: WarehouseStockDto[] { warehouse_id, name, value, qty_items },
    by_category: CategoryStockDto[] { category, value, qty_items }
  }

GET /api/analytics/inventory/movements?from=&to=&warehouse_id=&item_code=
→ StockMovementDto[] { date, item_code, item_name, warehouse, type, qty, reference }

GET /api/analytics/inventory/low-stock?threshold=min_stock
→ LowStockDto[] { item_code, name, warehouse, current_qty, min_qty, shortage_qty }
```

#### Sincronización
```
GET /api/sync/status
→ SyncStatusDto {
    company_id,
    entities: SyncEntityStatusDto[] {
      entity_name,        -- "OINV", "OCRD", "OITM", etc.
      last_sync_at,
      records_last_run,
      status,             -- "ok" | "warning" | "error" | "never"
      error_message       -- null si ok
    },
    overall_status,
    data_freshness_hours  -- horas desde el último sync exitoso
  }
```

---

## 8. KPIs por Módulo — Definición Completa

### 8.1. Dashboard Home

| KPI | Fuente | Fórmula |
|---|---|---|
| Ingresos MTD | `fact_Sales` | `SUM(net_amount) WHERE period = current_month` |
| Ingresos YTD | `fact_Sales` | `SUM(net_amount) WHERE year = current_year` |
| Variación vs período anterior | `fact_Sales` | `(mtd_actual - mtd_prev) / mtd_prev * 100` |
| Margen bruto % | `fact_Sales` | `(SUM(net_amount) - SUM(cost_amount)) / SUM(net_amount) * 100` |
| Clientes activos | `dim_Customer` + `fact_Sales` | `COUNT DISTINCT customer_id con compra en período` |
| AR Total | `fact_ARAging` | `SUM(open_amount)` |
| AR vencido | `fact_ARAging` | `SUM(bucket_1_30 + bucket_31_60 + ... + bucket_90_plus)` |
| Valor inventario | `fact_Inventory` | `SUM(stock_value) último snapshot` |
| Items bajo mínimo | `fact_Inventory` | `COUNT WHERE current_qty < min_qty` |
| Pedidos abiertos | `fact_Sales` (ORDR) | `COUNT WHERE doc_status = 'O'` |

### 8.2. Ventas

| KPI | Fórmula |
|---|---|
| Revenue por período | `SUM(net_amount)` agrupado por `date_trunc('month'/'week'/'day', doc_date)` |
| Costo de ventas | `SUM(cost_amount)` |
| Margen bruto | `revenue - costo` |
| Margen % | `(revenue - costo) / revenue * 100` |
| Ticket promedio | `SUM(net_amount) / COUNT(DISTINCT doc_entry)` |
| Unidades vendidas | `SUM(quantity)` |
| Tasa YoY | `(ytd_actual - ytd_prev) / ytd_prev * 100` |
| Top vendedores | `SUM(net_amount) GROUP BY salesperson_id ORDER BY 1 DESC` |
| Concentración top-5 clientes | `SUM top-5 / SUM total * 100` |

### 8.3. Clientes

| KPI | Fórmula |
|---|---|
| Antigüedad promedio de cartera (DSO) | `SUM(open_amount * days_overdue) / SUM(open_amount)` |
| AR Aging bucket 0 | `SUM(open_amount) WHERE days_overdue = 0` |
| AR Aging bucket 1-30 | `SUM(open_amount) WHERE days_overdue BETWEEN 1 AND 30` |
| AR Aging bucket 31-60 | `...BETWEEN 31 AND 60` |
| AR Aging bucket 61-90 | `...BETWEEN 61 AND 90` |
| AR Aging bucket 90+ | `... > 90` |
| Clientes nuevos | `COUNT WHERE first_invoice_date IN período` |
| Clientes recurrentes | `COUNT con facturas en período AND first_invoice_date < período` |
| Revenue por cliente (Pareto) | `SUM(net_amount) GROUP BY customer ORDER BY 1 DESC LIMIT 20` |

### 8.4. Productos

| KPI | Fórmula |
|---|---|
| Revenue por producto | `SUM(net_amount) GROUP BY item_code` |
| Margen por producto | `SUM(net_amount - cost_amount) / SUM(net_amount)` |
| Unidades vendidas | `SUM(quantity) GROUP BY item_code` |
| Rotación de inventario | `SUM(cost_sold) / AVG(inventory_value)` |
| Productos sin movimiento (N días) | `item_code WHERE MAX(last_movement_date) < TODAY - N` |
| Índice de concentración | `revenue top-10 / revenue total` |

### 8.5. Inventario

| KPI | Fórmula |
|---|---|
| Valor total inventario | `SUM(stock_qty * avg_cost) por snapshot más reciente` |
| Ítems bajo mínimo | `COUNT WHERE stock_qty < min_qty` |
| Cobertura de días | `stock_qty / AVG(daily_sales_qty últimos 30 días)` |
| Entradas del período | `SUM(qty) WHERE movement_type = 'IN' AND date IN período` |
| Salidas del período | `SUM(qty) WHERE movement_type = 'OUT' AND date IN período` |
| Stock por almacén % | `value_per_warehouse / total_value * 100` |

### 8.6. Sincronización (operativo, no analítico)

| KPI | Fuente |
|---|---|
| Última sincronización por entidad | `ctl.ingest_checkpoint.last_run_utc` |
| Registros procesados | `ctl.run_log.rows_processed` |
| Estado de la ejecución | `ctl.run_log.status` |
| Antigüedad de datos (horas) | `NOW() - MIN(last_run_utc)` |
| Errores en últimas N ejecuciones | `COUNT WHERE status = 'failed' AND started_at_utc > NOW() - INTERVAL '7 days'` |

---

## 9. Módulos — Diseño Funcional

### 9.1. Dashboard Home

**Propósito:** Vista ejecutiva. Lectura en 30 segundos del estado del negocio.

**Layout:**
```
┌──────────────────────────────────────────────────────────────┐
│  HEADER: Empresa / Período  [Date Range Picker]  [Refresh]   │
├────────────────────────────────────────────────────────────  │
│  KPI GRID (2 filas × 4 columnas — KpiCard)                   │
│  [Ingresos MTD] [Ingresos YTD] [Margen%]   [AR Total]        │
│  [Clientes act] [Items bajo min][Ped. Abier][Fresc. datos]   │
├──────────────────────────┬───────────────────────────────────│
│  Revenue últimos 12 meses│  Ventas por categoría (donut)     │
│  (line chart ECharts)    │                                   │
├──────────────────────────┴───────────────────────────────────│
│  Top 5 Vendedores (bar)  │  AR Aging resumen (stacked bar)   │
└──────────────────────────────────────────────────────────────┘
```

**Comportamiento:**
- Filtro de fecha afecta solo KPIs y gráficos de período. Los snapshots (inventario, AR) usan fecha del filtro o último disponible.
- Cada KpiCard muestra: valor principal, delta vs período anterior (↑ verde / ↓ rojo / neutro), sparkline de 6 períodos.
- `DataFreshnessTag` en el header muestra "Datos al [timestamp último sync]".

---

### 9.2. Ventas

**Sub-páginas:**
1. **Overview** — resumen ejecutivo con KPIs + chart de tendencia
2. **Por Período** — análisis temporal detallado
3. **Por Vendedor** — ranking y drill-down
4. **Por Cliente** — top clientes, concentración, Pareto

**Filtros disponibles:** Fecha, Vendedor (multi-select), Categoría de producto, Cliente.

**Overview:**
```
┌─────────────────────────────────────────────────────────────┐
│  [Revenue MTD] [Margen%] [Ticket Prom] [Unidades] [YoY%]   │
├────────────────────────────────────────────────────────────  │
│  Revenue vs Costo vs Margen por mes (bar+line combinado)    │
├──────────────────────────┬──────────────────────────────────│
│  Top 10 Clientes (bar H) │  Revenue por categoría (treemap) │
└──────────────────────────────────────────────────────────────┘
```

**Por Período:**
```
Selector granularidad: [Día] [Semana] [Mes] [Trimestre]
Toggle comparar vs período anterior: [ON/OFF]

Chart principal: barras para Revenue + línea para Margen%
Tabla detalle: período | revenue | costo | margen | margen% | variación%
```

**Por Vendedor:**
```
Ranking tabla: foto/iniciales | nombre | revenue | margen | orders | ticket prom
Chart: barras horizontales de revenue por vendedor
Selector: click en vendedor → drill-down a sus clientes
```

**Por Cliente:**
```
Análisis Pareto: curva acumulada de revenue (20% clientes = 80% revenue)
Top 20 tabla: cliente | revenue | margen% | orders | último pedido
Mapa de calor: cliente × mes → intensidad de compra
```

---

### 9.3. Clientes

**Sub-páginas:**
1. **Overview** — resumen cartera
2. **AR Aging** — cartera vencida detallada
3. **Top Clientes** — análisis de los mejores

**AR Aging:**
```
┌────────────────────────────────────────────────────────────┐
│  [Total AR] [Corriente] [1-30d] [31-60d] [61-90d] [90d+]  │
│  (KpiCards con monto + % del total)                        │
├────────────────────────────────────────────────────────────│
│  Stacked bar horizontal: todos los clientes por bucket     │
│  (cada barra = 1 cliente, ordenado por total descending)   │
├────────────────────────────────────────────────────────────│
│  Tabla detallada con filtros de bucket y búsqueda          │
│  Columnas: cliente | corriente | 1-30 | 31-60 | 61-90 | 90+│
│           | total | días prom | acción (ver detalle)       │
└────────────────────────────────────────────────────────────┘
```

**Comportamiento crítico:** Los datos de AR Aging provienen de `fact_ARAging` que es un snapshot. El encabezado muestra explícitamente la fecha del snapshot. No se mezclan con filtros de período.

**Top Clientes:**
```
Filtro período: fecha de las facturas
Métrica: [Revenue] [Margen] [Volumen unidades]

Tabla: ranking | cliente | revenue | margen% | orders | días entre compras | DSO
Chart donut: top 5 vs resto

Seleccionar cliente → modal con:
  - Línea de tiempo de compras (12 meses)
  - Productos más comprados
  - Saldo AR actual
```

---

### 9.4. Productos

**Sub-páginas:**
1. **Overview** — ranking general
2. **Márgenes** — análisis de rentabilidad
3. **Rotación Lenta** — alertas de stock sin movimiento

**Overview:**
```
Filtros: período, categoría, vendedor

KPIs: [Revenue total] [Margen prom%] [Ítems activos] [Ítems sin venta]

Chart principal: barras horizontales top-20 por revenue
Toggle métrica: [Revenue] [Unidades] [Margen absoluto] [Margen%]

Tabla: código | descripción | categoría | revenue | unidades | margen | margen%
```

**Márgenes:**
```
Scatter chart: eje X = revenue, eje Y = margen%, tamaño burbuja = unidades
Cuadrantes:
  - Alto revenue + alto margen → "Estrellas"
  - Alto revenue + bajo margen → "Vacas de caja"  
  - Bajo revenue + alto margen → "Oportunidades"
  - Bajo revenue + bajo margen → "Revisar"

Click en punto → detalle del producto
Tabla exportable con clasificación de cuadrante
```

**Rotación Lenta:**
```
Filtro: días sin movimiento (30 / 60 / 90 / 180 — selector)

Tabla: código | nombre | último movimiento | días sin mov | stock qty | stock value | almacén
KPI: valor total inmovilizado

Alerta visual si stock_value > umbral configurable por CompanyAdmin
```

---

### 9.5. Inventario

**Sub-páginas:**
1. **Overview** — snapshot actual
2. **Por Almacén** — distribución geográfica
3. **Movimientos** — entradas y salidas

**Overview:**
```
Selector fecha snapshot: [Hoy] [Ayer] [Custom]

KPIs: [Valor total] [Ítems totales] [Ítems bajo mínimo] [Cobertura prom días]

Donut: valor por categoría de producto
Treemap: ítems por valor (tamaño = valor, color = cobertura días)

Tabla top-20 ítems por valor: código | nombre | almacén | qty | valor | cobertura
```

**Por Almacén:**
```
KPI fila por almacén: [nombre] [valor] [% del total] [ítems]

Chart barras apiladas: valor por categoría dentro de cada almacén
Tabla: almacén | categoría | ítems | qty total | valor total | bajo mínimo count
```

**Movimientos:**
```
Filtros: período, almacén, tipo (entrada/salida/transferencia), ítem

Chart: área apilada de entradas (verde) vs salidas (rojo) por período
Saldo neto como línea overlay

Tabla paginada: fecha | ítem | almacén | tipo | qty | referencia (DocEntry)
Exportable a CSV
```

---

### 9.6. Sincronización

**Propósito:** Estado operativo de la carga de datos. Visible para todos los roles (es crítico saber si los datos están actualizados).

```
┌─────────────────────────────────────────────────────────────┐
│  Estado global: ● OK / ● ADVERTENCIA / ● ERROR              │
│  "Última sincronización: hace 2 horas (06:30 AM)"           │
├─────────────────────────────────────────────────────────────│
│  Tabla por entidad SAP:                                      │
│  Entidad | Estado | Última sync | Registros | Error         │
│  OINV    | ✓ OK   | 06:28 AM   | 145 upd   | —             │
│  INV1    | ✓ OK   | 06:28 AM   | 312 upd   | —             │
│  OCRD    | ✓ OK   | 06:29 AM   | 3 upd     | —             │
│  OITM    | ✓ OK   | 06:29 AM   | 0 upd     | —             │
│  OSLP    | ✓ OK   | 06:30 AM   | 0 upd     | —             │
│  ORIN    | ⚠ WARN | 08:00 AM   | —         | Timeout (3s)  │
├─────────────────────────────────────────────────────────────│
│  Historial últimas 7 ejecuciones (tabla compacta)           │
└─────────────────────────────────────────────────────────────┘
```

**Reglas de estado:**
- `OK`: `last_sync_at > NOW() - INTERVAL '26 hours'` (margen de 2h sobre frecuencia diaria)
- `WARNING`: `last_sync_at` entre 26h y 48h, o error en la última ejecución con éxito en la anterior
- `ERROR`: `last_sync_at > 48h` o `N` fallos consecutivos (N configurable, default 3)
- `NEVER`: nunca se ha sincronizado

---

## 10. Navegación

### 10.1. Sidebar (portal cliente)

```
┌──────────────────┐
│  [Logo tenant]   │
│  [Empresa]       │
├──────────────────┤
│  ◉ Dashboard     │  /
├──────────────────┤
│  ▸ Ventas        │  /sales
│    · Overview    │  /sales/overview
│    · Por Período │  /sales/by-period
│    · Por Vendedor│  /sales/by-salesperson
│    · Por Cliente │  /sales/by-customer
├──────────────────┤
│  ▸ Clientes      │  /customers
│    · Overview    │  /customers/overview
│    · AR Aging    │  /customers/ar-aging
│    · Top Clientes│  /customers/top
├──────────────────┤
│  ▸ Productos     │  /products
│    · Overview    │  /products/overview
│    · Márgenes    │  /products/margins
│    · Rot. Lenta  │  /products/slow-moving
├──────────────────┤
│  ▸ Inventario    │  /inventory
│    · Overview    │  /inventory/overview
│    · Por Almacén │  /inventory/by-warehouse
│    · Movimientos │  /inventory/movements
├──────────────────┤
│  ◌ Sincronización│  /sync
├──────────────────┤
│  [Avatar usuario]│
│  [Cerrar sesión] │
└──────────────────┘
```

**Reglas:**
- Módulos sin permiso del usuario no aparecen en el sidebar.
- El nodo padre (ej. "Ventas") colapsa sus sub-páginas; estado persiste en `localStorage`.
- La ruta activa se resalta con `#1E293B` (Active item del design system).
- En móvil: sidebar se convierte en drawer con hamburger button.

### 10.2. Breadcrumb

```
Dashboard > Ventas > Por Vendedor
```

- Cada segmento es clickeable.
- Visible en todas las sub-páginas.

### 10.3. Header de página

```
[Título módulo]          [DataFreshnessTag]    [Exportar CSV] [Filtros]
Ventas — Por Vendedor    Datos al 29/05 06:30  ↓ CSV          ⚙ Filtros
```

- `DataFreshnessTag`: color verde si `< 24h`, amarillo si `24-48h`, rojo si `> 48h`.
- `Exportar CSV`: solo visible si el usuario tiene `PermissionLevel.Export` en el módulo.
- `Filtros`: collapsable — muestra chips de filtros activos debajo del header.

---

## 11. Filtros Globales

### 11.1. Date Range Picker

**Presets:**
| Label | Rango |
|---|---|
| MTD | Inicio del mes → hoy |
| QTD | Inicio del trimestre → hoy |
| YTD | 1 enero → hoy |
| Últimos 30 días | hoy-30 → hoy |
| Últimos 3 meses | hoy-90 → hoy |
| Últimos 12 meses | hoy-365 → hoy |
| Custom | Date pickers libres |

**Comportamiento:**
- El rango seleccionado persiste por módulo en `sessionStorage` (no global — Ventas y Clientes tienen contextos diferentes).
- Los endpoints reciben `?from=YYYY-MM-DD&to=YYYY-MM-DD`.
- Snapshots de inventario y AR no usan este filtro para el stock actual; sí para movimientos.

### 11.2. Filtros contextuales por módulo

| Módulo | Filtros disponibles |
|---|---|
| Ventas | Vendedor (multi), Categoría (multi), Cliente (multi) |
| Clientes | Bucket AR (multi), Grupo de cliente |
| Productos | Categoría (multi), Proveedor (multi) |
| Inventario | Almacén (multi), Categoría (multi), Tipo movimiento |

**Todos los filtros son additive (AND).**

---

## 12. Caché y Rendimiento

### 12.1. TTL de caché por módulo

| Módulo | TTL server | Justificación |
|---|---|---|
| KPIs Dashboard | 5 min | Datos operativos, cambio frecuente |
| Ventas | 15 min | Historico; rara vez cambia intradía |
| Clientes AR Aging | 30 min | Snapshot diario; sin valor refrescar más |
| Productos | 15 min | Similar a ventas |
| Inventario snapshot | 30 min | Snapshot; movimientos del día son la fuente |
| Inventario movimientos | 10 min | Puede recibir movimientos intradía |
| Sync Status | 1 min | Operativo; crítico que sea actual |

### 12.2. Caché key

```
analytics:{company_id}:{endpoint}:{params_hash}
```

`params_hash` = SHA1 de `from+to+filter_ids` serializado. Garantiza que dos usuarios con mismos filtros comparten caché.

### 12.3. Invalidación

- Cada ejecución exitosa del Extractor llama a `POST /api/sync/invalidate-cache` con `company_id`.
- El backend invalida todas las keys de esa empresa.
- El frontend invalida TanStack Query con `queryClient.invalidateQueries({ queryKey: ['analytics', company_id] })` al recibir el evento (polling de `/api/sync/status` cada 60s en background).

---

## 13. Exportación

### 13.1. Alcance MVP

- CSV para todas las tablas de datos en módulos con permiso `export`.
- El endpoint de export es el mismo de analytics con `?format=csv`.
- El backend genera el CSV en streaming (sin buffer en memoria para tablas grandes).
- Límite: 50.000 filas por export (configurable en `appsettings.json`).

### 13.2. Fases futuras

- Excel (`.xlsx` con estilos) — requiere `ClosedXML` o `EPPlus`.
- PDF de página completa — requiere headless Chrome o `wkhtmltopdf`.
- Programar envío por email — requiere `CronJob` + SMTP.

---

## 14. Audit de Analytics

Extiende el `AuditService` existente con nuevos eventos:

| Evento | Cuándo |
|---|---|
| `ANALYTICS_VIEW` | Usuario abre cualquier página de analytics |
| `ANALYTICS_FILTER_APPLIED` | Usuario aplica filtros (debounced, 1 evento por sesión de filtrado) |
| `ANALYTICS_EXPORT` | Usuario descarga CSV |
| `ANALYTICS_ACCESS_DENIED` | 403 en endpoint analytics |

Payload mínimo: `user_id`, `company_id`, `module_key`, `timestamp`, `filters_applied`.

---

## 15. Consideraciones de Seguridad

1. **Tenant isolation total:** El `company_id` solo se lee del JWT — nunca del request body ni de query params. Cada query SQL incluye `WHERE company_id = @companyId` explícito. Sin excepciones.

2. **No SQL interpolation:** Todos los parámetros de analytics via Dapper `@param` o EF Core. Los `item_code`, `salesperson_id` y similares recibidos en query params son validados contra un allow-list o convertidos a tipos fuertes (int/GUID) antes de usarse.

3. **Rate limiting:** `/api/analytics/**` → 120 req/min/user (más generoso que embed-config dado que no hay costo externo). `/api/sync/status` → 60 req/min/user.

4. **Audit de accesos:** Cada visualización queda en `AuditLog`. Los exports quedan con los filtros exactos aplicados.

5. **Export sin PII innecesaria:** Los exports de AR Aging no incluyen emails ni teléfonos de contacto de clientes — solo datos comerciales (montos, fechas, códigos).

---

## 16. Roadmap de Implementación

### Fase 1 — MVP (semanas 1–4)
**Objetivo:** Dashboard Home + Ventas funcionales con datos reales.

- [ ] Tablas `fact.*`, `dim.*` en Supabase (schema en `supabase-postgres-mvp-architecture.md`)
- [ ] `KpiRepository` + `SalesAnalyticsRepository` con Dapper (queries contra `raw.*` en Sprint 3)
- [ ] `KpiController` + `SalesController`
- [ ] Frontend: `DashboardHome.tsx` + `SalesOverview.tsx` + `KpiCard.tsx` + `RevenueLineChart.tsx`
- [ ] Sidebar con navegación básica
- [ ] DateRangePicker con presets
- [ ] Caché IMemoryCache (TTL por módulo)
- [ ] Módulos registrados en tabla `Module`; permisos asignados en seed

### Fase 1.5 — Recommendations y Alertas de Negocio (semanas 8–16, post primer cliente estable)
**Objetivo:** Transformar DataBision de reportería a inteligencia operacional.

- [ ] `RecommendationWorker` con 5 reglas de alto valor (clientes inactivos, cartera vencida, stock crítico, caída de ventas con atribución, vendedores sin actividad)
- [ ] Sección "Insights" en portal con formato insight + atribución + acción sugerida
- [ ] `AlertingWorker` con alertas de negocio configurables (umbral de métricas)
- [ ] Centro de notificaciones in-portal (badge + lista)
- [ ] Email de alerta via Resend.com con cooldown configurable
- [ ] Business Actions: marcar recomendación gestionada, descartar alerta

### Fase 2 — Clientes y Productos (semanas 9–20)
**Objetivo:** Cartera AR + análisis de productos + portal completo.

- [ ] `fact.ar_aging` population logic en ETL
- [ ] `CustomerAnalyticsRepository` + `ProductAnalyticsRepository`
- [ ] `ArAging.tsx` + `TopCustomers.tsx`
- [ ] `ProductsOverview.tsx` + `ProductMargins.tsx` + scatter chart
- [ ] Filtros contextuales por módulo
- [ ] Export CSV básico
- [ ] Gestión de usuarios por CompanyAdmin
- [ ] SuperAdmin: gestión de tenants, estado global

### Fase 3 — Inventario y Sync (meses 6–12)
**Objetivo:** Inventario operativo + visibilidad de estado ETL + Operational Live Layer.

- [ ] `fact.inventory` + `ctl.run_log` poblados por `StagingTransformWorker`
- [ ] `InventoryAnalyticsRepository` + `SyncStatusRepository`
- [ ] `InventoryOverview.tsx` + `SyncCenter.tsx`
- [ ] `DataFreshnessTag` en todos los módulos
- [ ] Invalidación de caché post-sync
- [ ] Operational Live Layer: 3 vistas predefinidas para Plan Business
- [ ] Tablas SAP adicionales: ORDR, OPCH, OITW

### Fase 4 — Pulido y Enterprise (año 2+)
**Objetivo:** UX completa, white-label avanzado, funcionalidades enterprise.

- [ ] Alertas configurables vía webhook (Slack, Teams)
- [ ] Export Excel con estilos
- [ ] Favoritos y vista reciente por usuario
- [ ] Comparación interanual en todos los gráficos
- [ ] Responsive / mobile-first (ya parcial en Fase 1)
- [ ] Power BI Embedded como add-on para clientes Enterprise (ver ADR-011)
- [ ] Benchmarking cross-tenant (anonimizado, opt-in)

---

## 17. Lo Que NO Cubre Este Diseño

- **Builder de reportes custom:** El usuario final no puede crear sus propios gráficos. Los módulos son fijos. El builder se diseña en una fase posterior.
- **Alertas en tiempo real (WebSocket/SSE):** La actualización es pull (polling). Push se evalúa en Fase 4.
- **Drill-through entre módulos:** Cada módulo es independiente. Navegación cruzada (ej. click en cliente desde Ventas → va a AR Aging de ese cliente) se diseña en Fase 4.
- **Datos históricos pre-DataBision:** El sistema solo muestra datos desde la primera sincronización. Migración histórica es un proyecto separado por cliente.
- **Multi-empresa (holdings):** Un JWT tiene un solo `company_id`. Vistas consolidadas de grupo requieren diseño de auth separado.
