# Sprint 12 — Native BI Sub-Report Catalog

Generated: 2026-06-17

## Overview

Each Native BI process module has been expanded to a minimum of 4 sub-reports (tabs). All sub-reports are visible and navigable in the UI. Sub-reports backed by real MART data are marked ✅. Sub-reports with placeholder screens pending MART data are marked 🟡.

---

## Ventas (Sales) — 8 sub-reports

| # | Tab ID | Label | Status | Data source | Charts |
|---|--------|-------|--------|-------------|--------|
| 1 | `resumen` | Resumen Ejecutivo | ✅ | `mart.sales_*` via `useSalesOverview` + `useSalesMonthly` | KPI cards, NbAreaChart (trend), MiniBarList |
| 2 | `tendencia` | Tendencia | ✅ | `mart.sales_monthly` via `useSalesMonthly` | NbLineChart (gross vs net dual-series), period stat cards |
| 3 | `grupos` | Por Grupo de Producto | ✅ | `GET /api/client/bi/sales/item-groups` | NbPieChart (share), NbBarChart (top groups), detail table |
| 4 | `almacenes` | Por Almacén | ✅ | `GET /api/client/bi/sales/warehouses` | NbBarChart (gross by wh), NbPieChart (share), detail table |
| 5 | `customers` | Clientes | ✅ | `mart.sales_customer_dashboard` via `useSalesCustomers` | NbScatterChart (ticket vs volume), SortableTable |
| 6 | `items` | Productos | ✅ | `mart.sales_item_dashboard` via `useSalesItems` | SortableTable with mini progress bars |
| 7 | `salespersons` | Vendedores | ✅ | `mart.sales_customer_dashboard` via `useSalesSalespersons` | NbBarChart (top 10), SortableTable |
| 8 | `fulfillment` | Fulfillment | ✅ | `mart.fulfillment_dashboard` via `useBiSalesFulfillment` | KPI cards, SortableTable |

**Filters applied to data:** `dateFrom`, `dateTo`, `itemGroupCodes` (item-groups + warehouses endpoints), `salespersonCodes` (customer query)

---

## Inventario — 5 sub-reports

| # | Tab ID | Label | Status | Data source | Charts |
|---|--------|-------|--------|-------------|--------|
| 1 | `resumen` | Resumen | ✅ | `useBiInventoryRotation` + `useBiInventoryWarehouses` | NbPieChart (rotation dist.), NbBarChart (stock by warehouse) |
| 2 | `grupos` | Por Grupo | ✅ | Derived from `allRotation` (client-side groupBy `itemGroupCode`) | NbBarChart (stock by group), NbPieChart (items/group), summary table |
| 3 | `rotation` | Rotación y Cobertura | ✅ | `mart.inventory_rotation` via `useBiInventoryRotation` | NbScatterChart (coverage vs qty sold 90d), SortableTable |
| 4 | `warehouses` | Por Almacén | ✅ | `mart.inventory_warehouse` via `useBiInventoryWarehouses` | SortableTable |
| 5 | `no-movement` | Sin Movimiento | ✅ | Filtered from `allRotation` (rotationStatus === 'NO_MOVEMENT') | SortableTable |

**Filters applied:** `warehouseCodes` (UI-ready, backend wiring Sprint 13), `itemGroupCodes` (UI-ready)

---

## Compras (Purchasing) — 5 sub-reports

| # | Tab ID | Label | Status | Data source | Charts |
|---|--------|-------|--------|-------------|--------|
| 1 | `resumen` | Resumen Ejecutivo | ✅ | `mart.purchasing_executive` via `useBiPurchasingExecutive` | KPI cards, NbAreaChart (OC + recibido trend) |
| 2 | `suppliers` | Proveedores | ✅ | `mart.purchasing_suppliers` via `useBiPurchasingSuppliers` | NbBarChart (top 10 by PO amount), SortableTable |
| 3 | `receiving` | Recepciones | ✅ | `mart.purchasing_receiving` via `useBiPurchasingReceiving` | SortableTable |
| 4 | `grupos` | Por Grupo Proveedor | ✅ | Derived from supplier data (client-side) | NbPieChart (OC share), NbStackedBarChart (OC vs recibido) |
| 5 | `evolution` | Evolución OC | ✅ | `mart.purchasing_executive` (daily) | NbAreaChart |

**Filters applied:** `dateFrom`, `dateTo` (forward-compat on advanced filters), `supplierGroupCodes`, `warehouseCodes` (UI-ready)

---

## Operaciones — 4 sub-reports

| # | Tab ID | Label | Status | Data source | Charts |
|---|--------|-------|--------|-------------|--------|
| 1 | `Estado` / `pipeline` | Estado del Sistema | ✅ | `ops.*` schema via diagnostics hooks | NbBarChart (health scores), KPI metrics |
| 2 | `Alertas` | Alertas | ✅ | `ops.sync_alerts` via diagnostics hook | NbPieChart (severity donut), alert table |
| 3 | `Calidad de datos` | Calidad de Datos | ✅ | `ops.data_quality_issues` | NbBarChart (by issue type + by SAP object), NbPieChart (severity) |
| 4 | `Historial runs` | Historial / Pipeline | ✅ | `ops.pipeline_runs` | NbBarChart (KPIs from last run), run detail table |

---

## Finanzas — 9 sub-reports (5 live, 4 pending MART)

| # | Tab ID | Label | Status | Data source | Charts |
|---|--------|-------|--------|-------------|--------|
| 1 | `resumen` | Resumen Financiero | ✅ | `mart.finance_executive` via `useBiFinanceExecutive` | KPI cards, NbAreaChart (CxC vs CxP trend) |
| 2 | `ar` | Cuentas por Cobrar | ✅ | `mart.ar_aging` via `useBiFinanceArAging` | NbStackedBarChart (aging buckets top 10), SortableTable |
| 3 | `ap` | Cuentas por Pagar | ✅ | `mart.ap_aging` via `useBiFinanceApAging` | NbStackedBarChart (aging buckets top 10), SortableTable |
| 4 | `risk` | Riesgo +90d | ✅ | Filtered from AR aging | SortableTable with risk badges |
| 5 | `tendencia` | Tendencia Financiera | ✅ | `mart.finance_executive` (daily) | NbLineChart (net position CxC−CxP), period cards |
| 6 | `resultados` | Estado de Resultados | 🟡 | Pending `mart.journal_entries` + `mart.gl_accounts` | Placeholder screen |
| 7 | `balance` | Balance General | 🟡 | Pending `mart.gl_accounts` (BS classification) | Placeholder screen |
| 8 | `ebitda` | EBITDA / Rentabilidad | 🟡 | Pending `mart.gl_accounts` + `mart.cost_centers` | Placeholder screen |
| 9 | `cuentas` | Plan de Cuentas | 🟡 | Pending `mart.gl_accounts` + `mart.account_balances` | Placeholder screen |

---

## Chart type coverage

| Chart type | Used in |
|---|---------|
| `NbBarChart` | Sales (Vendedores, Grupos, Almacenes), Inventory (Grupos, Almacenes), Purchasing (Proveedores), Operations (Estado, Calidad) |
| `NbLineChart` | Finance (Tendencia net position) |
| `NbAreaChart` | Sales (Resumen trend, Tendencia gross), Purchasing (Resumen trend, Evolución) |
| `NbPieChart` | Sales (Grupos, Almacenes), Inventory (Resumen, Grupos), Purchasing (Grupos), Operations (Alertas severity) |
| `NbStackedBarChart` | Finance (AR Aging, AP Aging), Purchasing (Grupos OC vs recibido) |
| `NbScatterChart` | Sales (Clientes ticket vs volume), Inventory (Rotación coverage vs qty) |

---

## Filter status

| Filter key | Applies to SQL | Pages wired |
|---|---|---|
| `dateFrom` / `dateTo` | ✅ Yes | Sales (all), item-groups, warehouses |
| `salespersonCodes` | ✅ Yes | Sales customers query |
| `itemGroupCodes` | ✅ Yes | Item-group summary, warehouse summary, sales items |
| `customerGroupCodes` | 🟡 UI only (Sprint 13) | Sales |
| `supplierGroupCodes` | 🟡 UI only (Sprint 13) | Purchasing |
| `warehouseCodes` | 🟡 UI only (Sprint 13) | Inventory, Purchasing |
| `year` / `month` | 🟡 UI only (Sprint 13) | Finance |

---

## Sprint 13 backlog

1. Wire `customerGroupCodes`, `supplierGroupCodes`, `warehouseCodes` filters to SQL (ProcessDashboardRepository)
2. Implement `mart.gl_accounts` ETL pipeline in DataBision.Extractor (OACT table)
3. Implement `mart.journal_entries` ETL (OJDT + JDT1 join)
4. Build Finance accounting endpoints: `/api/client/bi/finance/income-statement`, `/balance`, `/ebitda`, `/chart-of-accounts`
5. Enable Finance accounting tabs (Estado de Resultados, Balance, EBITDA, Plan de Cuentas) with real data
6. Add `mart.account_balances` aggregation view for period-over-period balance
7. Inventory: wire warehouse/item-group filters to `GetInventoryRotationAsync`
