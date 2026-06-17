# Native BI — Mapa de Endpoints → MART → Frontend

Trazabilidad completa: endpoint API ↔ tabla MART/OPS ↔ hook React ↔ página/componente.

---

## Módulo: Dashboard

| Endpoint | Tabla MART | Hook React | Página / Componente |
|---|---|---|---|
| `GET /api/nativebi/dashboard/summary` | `mart.dashboard_summary` | `useDashboardSummary()` | `NativeBiDashboardPage` → KpiCard ×6 |
| `GET /api/nativebi/dashboard/sales-daily?days=N` | `mart.sales_daily` | `useDashboardSalesDaily()` | `NativeBiDashboardPage` → SalesBarChart |
| `GET /api/nativebi/dashboard/sales-monthly?months=N` | `mart.sales_monthly` | `useDashboardSalesMonthly()` | `NativeBiDashboardPage` |
| `GET /api/nativebi/dashboard/top-customers?limit=N` | `mart.customers_dashboard` | `useDashboardTopCustomers()` | `NativeBiDashboardPage` → TopCustomersTable |
| `GET /api/nativebi/sync/status` | `mart.*` + `cfg.sync_state` | `useSyncStatus()` | `SyncStatusWidget` |

---

## Módulo: Ventas

| Endpoint | Tabla MART | Hook React | Página / Componente |
|---|---|---|---|
| `GET /api/process/sales/overview?dateFrom=&dateTo=` | `mart.sales_overview` | `useSalesOverview()` | `NativeBiSalesPage` → 8 KPI cards |
| `GET /api/process/sales/customers?limit=&offset=` | `mart.customers_dashboard` | `useSalesCustomers()` | `NativeBiSalesPage` → Clientes tab |
| `GET /api/process/sales/items?limit=&offset=` | `mart.sales_item_dashboard` | `useSalesItems()` | `NativeBiSalesPage` → Productos tab |
| `GET /api/process/sales/salespersons?limit=&offset=` | `mart.salesperson_dashboard` | `useSalesSalespersons()` | `NativeBiSalesPage` → Vendedores tab |
| `GET /api/process/sales/fulfillment?limit=&offset=` | `mart.sales_fulfillment` | `useBiSalesFulfillment()` | `NativeBiSalesPage` → Fulfillment tab |

---

## Módulo: Compras

| Endpoint | Tabla MART | Hook React | Página / Componente |
|---|---|---|---|
| `GET /api/process/purchasing/executive?limit=&offset=` | `mart.purchasing_executive` | `useBiPurchasingExecutive()` | `PurchasingDashboardPage` → 8 KPIs + Evolución |
| `GET /api/process/purchasing/suppliers?limit=&offset=` | `mart.purchasing_supplier` | `useBiPurchasingSuppliers()` | `PurchasingDashboardPage` → Proveedores tab |
| `GET /api/process/purchasing/receiving?limit=&offset=` | `mart.purchasing_receiving` | `useBiPurchasingReceiving()` | `PurchasingDashboardPage` → Recepciones tab |

---

## Módulo: Inventario

| Endpoint | Tabla MART | Hook React | Página / Componente |
|---|---|---|---|
| `GET /api/process/inventory/rotation?limit=&offset=&sortBy=` | `mart.inventory_rotation` | `useBiInventoryRotation()` | `InventoryDashboardPage` → Resumen + Rotación + Sin movimiento |
| `GET /api/process/inventory/warehouses` | `mart.inventory_warehouse` | `useBiInventoryWarehouses()` | `InventoryDashboardPage` → Almacenes tab |

---

## Módulo: Finanzas

| Endpoint | Tabla MART | Hook React | Página / Componente |
|---|---|---|---|
| `GET /api/process/finance/executive?periods=N` | `mart.finance_executive` | `useBiFinanceExecutive()` | `FinanceDashboardPage` → 4 KPIs + Resumen tabla |
| `GET /api/process/finance/ar-aging?limit=&offset=` | `mart.ar_aging` | `useBiFinanceArAging()` | `FinanceDashboardPage` → AR tab + Risk tab |
| `GET /api/process/finance/ap-aging?limit=&offset=` | `mart.ap_aging` | `useBiFinanceApAging()` | `FinanceDashboardPage` → AP tab |

---

## Módulo: Operaciones

| Endpoint | Tabla OPS | Hook React | Página / Componente |
|---|---|---|---|
| `GET /api/process/operations/pipeline-health` | `ops.pipeline_health` | `useBiOperationsPipelineHealth()` | `OperationsDashboardPage` → Pipeline + Runs tab |
| `GET /api/process/operations/alerts?limit=&offset=` | `ops.alerts` | `useBiOperationsAlerts()` | `OperationsDashboardPage` → Alertas tab |
| `GET /api/process/operations/data-quality?limit=&offset=` | `ops.data_quality` | `useBiOperationsDataQuality()` | `OperationsDashboardPage` → Calidad tab |

---

## Módulo: Diagnósticos

| Endpoint | Fuente | Hook React | Página / Componente |
|---|---|---|---|
| `GET /api/nativebi/diagnostics` | `cfg.*` + checks activos | `useNativeBiDiagnostics()` | `NativeBiDiagnosticsPage` → Sistema tab |
| `GET /api/nativebi/diagnostics/table-counts` | `information_schema` | `useNativeBiTableCounts()` | `NativeBiDiagnosticsPage` → Tablas tab |
| `GET /api/nativebi/sync/objects` | `cfg.sync_state` | `useSyncObjects()` | `NativeBiDiagnosticsPage` → Extracción tab |
| `GET /api/nativebi/sync/transform-status` | `mart.*` | `useSyncTransformStatus()` | `NativeBiDiagnosticsPage` → Consistencia tab |

---

## Resolución de tenant → analytics_company_id

```
JWT claim company_slug (e.g. "ksdepor")
    ↓
AnalyticsCompanyResolver.ResolveAsync(companyIdentifier)
    ↓ [Sprint 9: DB lookup primero]
Company.AnalyticsCompanyId en Azure SQL
    ↓ [fallback Dev]
NativeBi:CompanySlugMap en appsettings.json
    ↓
analytics_company_id (e.g. "company-dev-001")
    ↓
Todas las queries Supabase usan este ID
```

---

## Seguridad y multi-tenant

- Todos los endpoints `/api/nativebi/*` y `/api/process/*` requieren JWT válido.
- El `company_slug` del JWT se mapea a `analytics_company_id` antes de cualquier query a Supabase.
- Ningún endpoint acepta `analytics_company_id` del cliente — siempre proviene del JWT + resolver.
- Los datos en Supabase están particionados por `analytics_company_id` — filtro explícito en cada query.
