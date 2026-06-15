# DataBision — Plan de Integración Frontend: Dashboards por Proceso (Sprint 8H)

**Fecha:** 2026-06-10  
**Nota:** Sprint 8H no modifica el frontend. Este documento es el plan para la siguiente iteración frontend.

---

## Estado Actual

Los endpoints API están listos. El portal frontend (`databision-frontend/`) tiene:
- `src/apps/portal/` — app del tenant, ya con rutas para Dashboard y Sales
- Zustand para auth/tenant, TanStack Query para fetching
- Tema dinámico con `var(--brand-primary)` y clases Tailwind `brand-*`

Lo que **no existe aún** en el frontend:
- Vistas para Finanzas, Inventario, Compras, Operaciones
- Navegación por proceso (`/api/client/processes`)
- Componentes de tablas paginadas reutilizables para los 15 endpoints

---

## Arquitectura Propuesta

### Estructura de archivos target

```
databision-frontend/src/apps/portal/
  pages/
    processes/
      ProcessHub.tsx          ← lista de procesos habilitados
    sales/
      SalesCustomers.tsx      ← GET /bi/sales/customers-dashboard
      SalesItems.tsx          ← GET /bi/sales/items-dashboard
      SalesFulfillment.tsx    ← GET /bi/sales/fulfillment
    finance/
      FinanceExecutive.tsx    ← GET /bi/finance/executive
      FinanceArAging.tsx      ← GET /bi/finance/ar-aging
      FinanceApAging.tsx      ← GET /bi/finance/ap-aging
    inventory/
      InventoryRotation.tsx   ← GET /bi/inventory/rotation
      InventoryStock.tsx      ← GET /bi/inventory/stock
      InventoryWarehouses.tsx ← GET /bi/inventory/warehouses
    purchasing/
      PurchasingExecutive.tsx
      PurchasingSuppliers.tsx
      PurchasingReceiving.tsx
    operations/
      OperationsPipelineHealth.tsx
      OperationsAlerts.tsx
      OperationsDataQuality.tsx
  hooks/
    useProcesses.ts           ← TanStack Query para /api/client/processes
    useBiSales.ts
    useBiFinance.ts
    useBiInventory.ts
    useBiPurchasing.ts
    useBiOperations.ts
  components/
    PaginatedTable.tsx        ← tabla genérica paginada reutilizable
    KpiCard.tsx               ← tarjeta de número KPI
    StatusBadge.tsx           ← badge de estado (FAST/SLOW/SUCCESS/etc.)
    EmptyState.tsx            ← estado vacío consistente
```

---

## Hooks TanStack Query

Patrón para todos los hooks paginados:

```typescript
// hooks/useBiSales.ts
export function useSalesCustomers(opts: { limit: number; offset: number; sortBy?: string }) {
  return useQuery({
    queryKey: ['bi', 'sales', 'customers', opts],
    queryFn: () => api.get<PagedResponse<SalesCustomerDashboardDto>>(
      '/api/client/bi/sales/customers-dashboard',
      { params: opts }
    ).then(r => r.data),
    staleTime: 5 * 60 * 1000, // 5 min
  });
}
```

Hooks de operaciones (no paginados por defecto):
```typescript
export function usePipelineHealth() {
  return useQuery({
    queryKey: ['bi', 'operations', 'pipeline-health'],
    queryFn: () => api.get<SingleResponse<OperationHealthDto | null>>(
      '/api/client/bi/operations/pipeline-health'
    ).then(r => r.data.data),
    staleTime: 2 * 60 * 1000, // 2 min
  });
}
```

---

## Componente PaginatedTable

Componente central reutilizable:

```typescript
interface PaginatedTableProps<T> {
  data: T[];
  columns: Column<T>[];
  meta: PagedMeta;
  onPageChange: (offset: number) => void;
  isLoading?: boolean;
  emptyMessage?: string;
}
```

- Filas de 44px (design system)
- `shadow-sm`, radius 8px para el contenedor
- Usa `brand-primary` para el botón de paginación activo
- Números en `tabular-nums`

---

## Rutas del Portal

```typescript
// En portal router (React Router v6)
<Route path="/processes" element={<ProcessHub />} />
<Route path="/processes/sales/customers" element={<SalesCustomers />} />
<Route path="/processes/sales/items" element={<SalesItems />} />
<Route path="/processes/finance/ar-aging" element={<FinanceArAging />} />
// ... etc
```

Sidebar: los procesos se cargan dinámicamente desde `GET /api/client/processes`. Solo aparecen los habilitados para el tenant.

---

## Tipos TypeScript

```typescript
// types/api.ts
export interface PagedMeta {
  limit: number;
  offset: number;
  count: number;
  hasMore: boolean;
}
export interface PagedResponse<T> { data: T[]; meta: PagedMeta; }
export interface SingleResponse<T> { data: T; }

// types/bi.ts
export interface SalesCustomerDashboardDto {
  cardCode: string;
  cardName: string;
  cardType: string;
  salespersonName: string;
  grossSales: number;
  creditMemos: number;
  netSales: number;
  invoiceCount: number;
  avgTicket: number;
  lastInvoiceDate: string | null; // ISO date string
  isActive: boolean;
}
// ... etc, un DTO por endpoint
```

---

## Comportamiento con datos vacíos

Los endpoints retornan arrays vacíos si las tablas MART no tienen datos aún (Purchasing, algunos Inventory).

Cada vista debe mostrar `EmptyState` con mensaje explicativo, no un error.

```typescript
if (!data || data.length === 0) {
  return <EmptyState message="No hay datos disponibles. La sincronización con SAP se ejecutará próximamente." />;
}
```

---

## Orden de implementación sugerido

1. **Tipos TypeScript** (`types/bi.ts`, `types/api.ts`) — sin dependencias
2. **Hooks TanStack Query** — requieren tipos
3. **PaginatedTable + EmptyState** — componentes base
4. **ProcessHub** — entrada al módulo
5. **Sales (3 vistas)** — datos reales disponibles desde Sprint 8F
6. **Finance AR Aging** — datos reales disponibles (OINV extraídas)
7. **Inventory Rotation** — datos reales disponibles (OITM + OINV1)
8. **Operations** — datos reales disponibles (ops.*)
9. **Purchasing + Finance AP + Inventory Stock/Warehouses** — pending Sprint 8F-ext (OPOR/OPDN/OITW/OWTR)

---

## Deuda técnica / Consideraciones

- **Purchasing y Finance AP:** retornan vacío hasta Sprint 8F-ext. Las vistas deben implementarse pero mostrar EmptyState con mensaje de "próximamente".
- **Inventory Stock:** depende de OITW. `onHandQty` y `coverageDays` serán null hasta extracción.
- **Date handling:** los campos `*Date` vienen como string ISO (`"2026-05-28"`). Usar `date-fns` para formateo local (ya en el proyecto).
- **Refresh automático:** `/api/client/bi/operations/pipeline-health` se recomienda refrescar cada 2 min. Los otros endpoints: 5 min de staleTime es suficiente.
- **Sort en servidor:** los endpoints de clientes e items soportan `sortBy`/`sortDir`. Implementar sort por columna clickeable en la tabla.
