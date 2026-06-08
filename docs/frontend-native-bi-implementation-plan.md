# Native BI Frontend — Implementation Plan

Guía de implementación para el equipo frontend (Sprint 7A–7E).  
El backend está completo y estable (Sprint 6A–6L). Este doc describe el orden recomendado para construir el frontend.

**Contrato API:** `docs/frontend-native-bi-backend-contract.md`  
**Ejemplos curl/PowerShell:** `docs/native-bi-endpoint-samples.md`  
**E2E runbook:** `docs/native-bi-e2e-runbook.md`

---

## Principios de implementación

1. **Un módulo a la vez** — dashboard primero, luego sales, luego sync/diag.
2. **API client primero** — construir el cliente antes de los componentes.
3. **Estados loading/error/empty siempre** — nunca dejar un componente sin estado de carga.
4. **Sin hardcoding de company** — siempre del JWT o del contexto de tenant.
5. **Números con `Intl.NumberFormat`** — no formatear decimales manualmente.

---

## Sprint 7A — API Client + Auth Token Handling

### Objetivo

Crear el cliente HTTP para Native BI y asegurar que el JWT se envía correctamente.

### Tareas

#### 1. API client base

Crear `src/apps/portal/api/nativeBiClient.ts`:

```typescript
import axios from 'axios';

const nativeBi = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5103',
  headers: { 'Content-Type': 'application/json' },
});

// Interceptor: adjuntar JWT en cada request
nativeBi.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Interceptor: auto-refresh en 401
nativeBi.interceptors.response.use(
  (res) => res,
  async (error) => {
    if (error.response?.status === 401 && !error.config._retry) {
      error.config._retry = true;
      await useAuthStore.getState().refresh();
      return nativeBi(error.config);
    }
    return Promise.reject(error);
  }
);
```

#### 2. Hooks de TanStack Query

Crear `src/apps/portal/api/dashboard.ts`:

```typescript
export const useDashboardSummary = () =>
  useQuery({
    queryKey: ['dashboard', 'summary'],
    queryFn: async () => {
      const { data } = await nativeBi.get<ApiResponse<DashboardSummary>>('/api/client/dashboard/summary');
      return data.data;
    },
    staleTime: 5 * 60 * 1000,
  });

export const useSalesDaily = (days = 30) =>
  useQuery({
    queryKey: ['dashboard', 'sales-daily', days],
    queryFn: async () => {
      const { data } = await nativeBi.get<ApiResponse<SalesDaily[]>>(`/api/client/dashboard/sales-daily?days=${days}`);
      return data.data;
    },
    staleTime: 5 * 60 * 1000,
  });

export const useTopCustomers = (params: PaginationParams) =>
  useQuery({
    queryKey: ['dashboard', 'top-customers', params],
    queryFn: async () => {
      const qs = new URLSearchParams({ limit: String(params.limit), offset: String(params.offset) });
      if (params.sortBy) qs.set('sortBy', params.sortBy);
      if (params.sortDir) qs.set('sortDir', params.sortDir);
      const { data } = await nativeBi.get<PagedApiResponse<CustomerSales>>(`/api/client/dashboard/top-customers?${qs}`);
      return data;
    },
  });
```

#### 3. Error boundary

Crear `src/apps/portal/components/ApiErrorBoundary.tsx` — captura errores de red y muestra mensaje con traceId.

### Criterios de aceptación 7A

- [ ] `nativeBiClient.ts` compilado sin errores TypeScript
- [ ] JWT adjuntado a todos los requests
- [ ] Auto-refresh en 401 funciona
- [ ] Hooks tipados para todos los endpoints dashboard + sales
- [ ] `useQuery` con `staleTime` definido (no refetch en cada navegación)

---

## Sprint 7B — Dashboard Executive Layout

### Objetivo

Dashboard principal con KPI cards y gráfico de ventas.

### Componentes sugeridos

```
src/apps/portal/pages/DashboardPage.tsx
src/apps/portal/components/KpiCard.tsx
src/apps/portal/components/SalesBarChart.tsx      ← ECharts bar, ventas diarias
src/apps/portal/components/TopCustomersTable.tsx
```

### KPI cards (datos de /dashboard/summary)

| Card | Campo | Formato |
|---|---|---|
| Ventas netas | `netSalesAmount` | `$1.365.000` |
| N° facturas | `invoiceCount` | `72` |
| Clientes activos | `activeCustomers` | `20` |
| Ticket promedio | `avgTicketAmount` | `$18.958` |

### SalesBarChart

- Datos: `useSalesDaily(30)`
- Eje X: fecha, Eje Y: `netSalesAmount`
- ECharts `bar` con `tooltip`
- Sin animación al hover (datos financieros, no entretenimiento)

### TopCustomersTable

- Datos: `useTopCustomers({ limit: 10, offset: 0, sortBy: 'netSalesAmount', sortDir: 'desc' })`
- Columnas: Cliente, Ventas netas, N° facturas, Última factura
- Paginación con `meta.hasMore`

### Estados obligatorios

Cada componente debe tener 3 estados:

```typescript
if (isLoading) return <SkeletonRows count={5} />;
if (isError) return <ErrorCard message={error.message} traceId={...} onRetry={refetch} />;
if (!data || data.length === 0) return <EmptyState message="Sin datos disponibles" />;
```

### Criterios de aceptación 7B

- [ ] Dashboard carga sin errores con datos de Supabase
- [ ] KPI cards muestran valores correctos
- [ ] Gráfico muestra últimos 30 días de ventas
- [ ] Tabla top-10 clientes con paginación
- [ ] Skeleton mientras carga (no spinner genérico)
- [ ] Estado vacío con mensaje apropiado

---

## Sprint 7C — Sales Module UI

### Objetivo

Módulo de ventas con selector de rango de fechas y tablas paginadas.

### Componentes sugeridos

```
src/apps/portal/pages/SalesPage.tsx
src/apps/portal/components/DateRangePicker.tsx
src/apps/portal/components/SalesOverviewCards.tsx
src/apps/portal/components/SortableTable.tsx       ← tabla genérica con sort + pagination
```

### DateRangePicker

- Dos campos de fecha `<input type="date" />`
- Validación: `dateFrom` no puede ser > `dateTo`
- Al cambiar fechas → invalidar queries de sales

### SalesOverviewCards

- Datos: `useSalesOverview({ dateFrom, dateTo })`
- Muestra `netSalesAmount`, `invoiceCount`, `avgTicketAmount`

### SortableTable

Componente reutilizable para customers, items, salespersons:
- Cabeceras clickeables para ordenar (toggle asc/desc)
- Paginación controlada: botones Anterior/Siguiente basados en `meta.hasMore`
- `offset` aumenta en `limit` al presionar Siguiente

```typescript
interface SortableTableProps<T> {
  data: T[];
  columns: ColumnDef<T>[];
  meta: PagedMeta;
  onPageChange: (offset: number) => void;
  onSortChange: (sortBy: string, sortDir: 'asc' | 'desc') => void;
  isLoading: boolean;
}
```

### Criterios de aceptación 7C

- [ ] Selector de fechas filtra correctamente
- [ ] Tablas de clientes/items/vendedores con sort + paginación
- [ ] Loading state en cambio de filtro (no parpadeo de contenido)
- [ ] `SortableTable` reutilizable (usado también en 7B si aplica)

---

## Sprint 7D — Sync / Diagnostics UI

### Objetivo

Widget de estado de sincronización visible para admins del tenant. Diagnostics solo para ops.

### Componentes sugeridos

```
src/apps/portal/components/SyncStatusWidget.tsx     ← visible en sidebar o dashboard
src/apps/portal/pages/AdminDiagnosticsPage.tsx      ← solo admins (role === 'Admin')
```

### SyncStatusWidget

- Datos: `useSyncStatus()` con `refetchInterval: 60_000`
- Badge de estado: `ok` → verde, `warning` → amarillo, `error` → rojo
- Tooltip: "Última actualización: hace 23 minutos"
- Click → navegar a diagnostics si el usuario es Admin

### AdminDiagnosticsPage

- Solo visible si `role === 'Admin'` (from JWT claim)
- Muestra `NativeBiDiagnostics.checks` como lista con íconos
- Muestra `NativeBiTableCounts.tables` como tabla de row counts
- Auto-refresh cada 5 min

### Criterios de aceptación 7D

- [ ] SyncStatusWidget muestra estado correcto
- [ ] Auto-refresh cada 60s sin bloquear UI
- [ ] DiagnosticsPage oculta para `role !== 'Admin'`
- [ ] Estados de error mostrando `traceId` para soporte

---

## Sprint 7E — Tenant Branding / Navigation

### Objetivo

Aplicar branding por tenant en toda la UI Native BI.

### Implementación

El branding ya está en `ThemeProvider` desde el portal base. Para Native BI:

1. Verificar que todos los colores usan `var(--brand-primary)` o clases `brand-*` de Tailwind.
2. Verificar que el sidebar muestra el logo del tenant desde `localStorage`.
3. Navegación: agregar ítems al sidebar:
   - Dashboard (ícono: `LayoutDashboard`)
   - Ventas (ícono: `TrendingUp`)
   - Sync Center (ícono: `RefreshCw`)
   - Diagnósticos — solo Admin (ícono: `Activity`)

### Criterios de aceptación 7E

- [ ] Sin hardcoded colors en ningún componente Native BI
- [ ] Logo del tenant visible en sidebar
- [ ] Ítems de navegación correctos según `role`
- [ ] `npm run lint` sin errores
- [ ] `npm run build` sin errores TypeScript

---

## Qué NO tocar del backend

Durante los sprints 7A–7E, el equipo frontend no debe:

- Modificar controllers (`/api/client/*`)
- Modificar `CompanyContextResolver`
- Modificar DTOs de Application
- Modificar Program.cs
- Agregar migraciones
- Cambiar la forma de los responses (ya definida en `ApiResponse<T>`)

Si un endpoint retorna datos incorrectos o falta un campo, abrir issue para sesión backend separada.

---

## Herramientas recomendadas

| Función | Librería | Ya instalada |
|---|---|---|
| HTTP client | Axios | Verificar |
| Data fetching | TanStack Query | Verificar |
| State management | Zustand | Verificar |
| Gráficos | ECharts + echarts-for-react | Instalar si falta |
| Fechas | `Intl.DateTimeFormat` nativo | — |
| Moneda | `Intl.NumberFormat` nativo | — |
| Tipos | TypeScript strict | Habilitado |
