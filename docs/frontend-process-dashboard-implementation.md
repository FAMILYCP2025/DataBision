# Frontend Process Dashboard Implementation

Sprint 8K — June 2026

## Routes Created

| Path | Component | Description |
|---|---|---|
| `/client/bi/purchasing` | `PurchasingDashboardPage` | KPIs + Proveedores/Recepciones tabs |
| `/client/bi/inventory` | `InventoryDashboardPage` | KPIs + Rotación/Almacenes tabs |
| `/client/bi/finance` | `FinanceDashboardPage` | KPIs + AR/AP aging tabs |
| `/client/bi/operations` | `OperationsDashboardPage` | Pipeline health + Alertas/Calidad tabs |

The Fulfillment tab was added to the existing `/client/bi/sales` route (`NativeBiSalesPage`).

## Endpoints Consumed

All under `/api/client/bi/`:

| Endpoint | Hook | Page |
|---|---|---|
| `GET /sales/fulfillment?days=N` | `useBiSalesFulfillment` | NativeBiSalesPage (Fulfillment tab) |
| `GET /purchasing/executive?days=N` | `useBiPurchasingExecutive` | PurchasingDashboardPage |
| `GET /purchasing/suppliers?limit&offset&sortBy&sortDir` | `useBiPurchasingSuppliers` | PurchasingDashboardPage |
| `GET /purchasing/receiving?limit&offset&sortBy&sortDir` | `useBiPurchasingReceiving` | PurchasingDashboardPage |
| `GET /inventory/rotation?limit&offset&sortBy&sortDir` | `useBiInventoryRotation` | InventoryDashboardPage |
| `GET /inventory/stock?days=N` | `useBiInventoryStock` | InventoryDashboardPage (future) |
| `GET /inventory/warehouses` | `useBiInventoryWarehouses` | InventoryDashboardPage |
| `GET /finance/executive?days=N` | `useBiFinanceExecutive` | FinanceDashboardPage |
| `GET /finance/ar-aging?limit&offset&sortBy&sortDir` | `useBiFinanceArAging` | FinanceDashboardPage |
| `GET /finance/ap-aging?limit&offset&sortBy&sortDir` | `useBiFinanceApAging` | FinanceDashboardPage |
| `GET /operations/pipeline-health` | `useBiOperationsPipelineHealth` | OperationsDashboardPage |
| `GET /operations/alerts?limit&offset` | `useBiOperationsAlerts` | OperationsDashboardPage |
| `GET /operations/data-quality?limit&offset` | `useBiOperationsDataQuality` | OperationsDashboardPage |

All hooks live in `src/client/hooks/useProcessBi.ts`. staleTime = 5 min (2 min for operations health). Operations health auto-refreshes every 5 min.

## New Files

| File | Purpose |
|---|---|
| `src/client/types/processBi.ts` | TypeScript interfaces for all 15 process DTOs |
| `src/client/api/processBiApi.ts` | Axios API functions (follows `nativeBiApi.ts` pattern) |
| `src/client/hooks/useProcessBi.ts` | 15 TanStack Query hooks |
| `src/client/pages/PurchasingDashboardPage.tsx` | Purchasing dashboard |
| `src/client/pages/InventoryDashboardPage.tsx` | Inventory dashboard |
| `src/client/pages/FinanceDashboardPage.tsx` | Finance dashboard |
| `src/client/pages/OperationsDashboardPage.tsx` | Operations dashboard |

## Modified Files

| File | Change |
|---|---|
| `src/client/ClientApp.tsx` | Added 4 new routes |
| `src/client/components/ClientSidebar.tsx` | Added 4 nav items in Análisis section |
| `src/client/pages/NativeBiSalesPage.tsx` | Added Fulfillment tab |

## How to Test Locally

1. Start the API: `dotnet run --project src/DataBision.Api`
2. Start the frontend: `npm run dev` (from `databision-frontend/`)
3. Browse to `http://localhost:5173/client/login?tenant=ksdepor`
4. Log in as a CompanyUser or CompanyAdmin
5. Sidebar "Análisis" section shows: Dashboard, Ventas, Compras, Inventario, Finanzas, Operaciones

## What Data Shows for KSDEPOR

- **Ventas → Fulfillment**: last 30 days of sales order vs delivery fill-rate by period
- **Compras**: executive time series (days=30) + paged supplier and receiving tables
- **Inventario**: paged rotation table with FAST/NORMAL/SLOW/NO_MOVEMENT badges + warehouse list
- **Finanzas**: AR overdue KPIs + paged AR aging by customer (AP aging returns empty — in preparation)
- **Operaciones**: pipeline health score, extractor/transform status, active alerts, DQ issues

## Known Limitations / Pending

- AP aging endpoint may return empty — mart data pending for KSDEPOR AP
- Inventory stock endpoint exists but is not wired to a UI table yet (KPIs only use rotation counts)
- Operations data quality severity colors use same palette as alerts — future refinement if new severities added
- `INEFFECTIVE_DYNAMIC_IMPORT` bundler warning for `useClientAuthStore` is pre-existing and harmless; the dynamic import pattern is required to break circular deps between the auth store and Axios interceptors
