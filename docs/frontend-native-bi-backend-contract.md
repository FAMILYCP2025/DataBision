# Native BI — Frontend Backend Contract

Contrato técnico completo para que el equipo frontend consuma los endpoints Native BI sin necesidad de leer el código C#.

**Versión:** Sprint 6I–6L (2026-06-07)  
**Base URL dev:** `http://localhost:5103`  
**Base URL prod:** `https://api.databision.app`

---

## Autenticación

### Modo DEV (sin JWT configurado)

Pasar `companyId` como query param en todos los requests:

```
?companyId=company-dev-001
```

El servidor detecta que `Jwt:PublicKey` no está configurado y acepta el query param.

### Modo PROD (JWT Bearer)

El token JWT se obtiene via `POST /api/auth/login`. Enviarlo en el header:

```
Authorization: Bearer <token>
```

El server resuelve `company_id` desde el claim `company_slug` del JWT. El query param `?companyId` es **ignorado en producción**.

### Claims JWT relevantes para Native BI

| Claim | Tipo | Descripción |
|---|---|---|
| `company_slug` | string | MART company_id (slug del tenant) — claim principal |
| `company_id` | string | PK integer del tenant (fallback si no hay `company_slug`) |
| `role` | string | `Admin`, `Viewer`, `SuperAdmin` |
| `module_ids` | string[] | IDs de módulos autorizados para el usuario |

### Comportamiento por escenario

| Escenario | Resultado |
|---|---|
| JWT válido + `company_slug` presente | ✅ Resuelve company |
| JWT válido + sin claim de company | ❌ 403 `forbidden_no_company` |
| JWT configurado + request sin token | ❌ 401 `unauthorized` |
| JWT NO configurado + `?companyId` presente | ✅ DEV fallback |
| JWT NO configurado + sin `?companyId` | ❌ 400 `missing_company_id` |

---

## Formatos de respuesta

### Éxito (dato simple)

```json
{
  "data": { ... },
  "traceId": "0HMWPKFQDAV2H:00000003"
}
```

### Éxito (lista paginada)

```json
{
  "data": [ ... ],
  "meta": {
    "limit": 10,
    "offset": 0,
    "count": 10,
    "hasMore": true
  },
  "traceId": "0HMWPKFQDAV2H:00000004"
}
```

### Error

```json
{
  "error": "snake_case_code",
  "message": "Human readable description.",
  "traceId": "0HMWPKFQDAV2H:00000005",
  "details": null
}
```

### Dato vacío o nulo

- Lista vacía → `{ "data": [], "meta": { "limit": 10, "offset": 0, "count": 0, "hasMore": false } }`
- Dato nullable → `{ "data": null }`
- Nunca retorna 404 por datos vacíos

---

## Modelos TypeScript

```typescript
// Respuesta genérica de éxito
interface ApiResponse<T> {
  data: T;
  traceId: string;
}

// Respuesta paginada
interface PagedApiResponse<T> {
  data: T[];
  meta: PagedMeta;
  traceId: string;
}

interface PagedMeta {
  limit: number;
  offset: number;
  count: number;
  hasMore: boolean;
}

// Error estándar
interface ApiErrorResponse {
  error: string;
  message: string;
  traceId: string;
  details?: Record<string, string[]>;
}

// ── Modelos de datos ──────────────────────────────────────────────────────────

interface DashboardSummary {
  companyId: string;
  grossSalesAmount: number;
  creditMemoAmount: number;
  netSalesAmount: number;
  invoiceCount: number;
  creditMemoCount: number;
  activeCustomers: number;
  activeItems: number;
  avgTicketAmount: number;
  lastInvoiceDate: string | null;       // ISO date: "2026-05-30"
  lastCreditMemoDate: string | null;
  lastSyncAtUtc: string | null;         // ISO datetime UTC
  transformedAtUtc: string | null;      // ISO datetime UTC
}

interface SalesDaily {
  salesDate: string;                    // "2026-05-30"
  grossSalesAmount: number;
  creditMemoAmount: number;
  netSalesAmount: number;
  invoiceCount: number;
  creditMemoCount: number;
  activeCustomers: number;
  avgTicketAmount: number;
}

interface SalesMonthly {
  salesMonth: string;                   // "2026-05-01" (primer día del mes)
  grossSalesAmount: number;
  creditMemoAmount: number;
  netSalesAmount: number;
  invoiceCount: number;
  creditMemoCount: number;
  activeCustomers: number;
  avgTicketAmount: number;
}

interface CustomerSales {
  cardCode: string;
  cardName: string;
  salesAmount: number;
  creditMemoAmount: number;
  netSalesAmount: number;
  invoiceCount: number;
  creditMemoCount: number;
  lastInvoiceDate: string | null;
  firstInvoiceDate: string | null;
  avgTicketAmount: number;
}

interface ItemSales {
  itemCode: string;
  itemName: string;
  quantitySold: number;
  grossSalesAmount: number;
  lineCount: number;
  invoiceCount: number;
  lastSaleDate: string | null;
}

interface SalespersonSales {
  salesPersonCode: string;
  salesPersonName: string;
  salesAmount: number;
  creditMemoAmount: number;
  netSalesAmount: number;
  invoiceCount: number;
  creditMemoCount: number;
  activeCustomers: number;
  avgTicketAmount: number;
}

interface SalesOverview {
  grossSalesAmount: number;
  creditMemoAmount: number;
  netSalesAmount: number;
  invoiceCount: number;
  creditMemoCount: number;
  avgTicketAmount: number;
  activeCustomers: number;
  dateFrom: string;
  dateTo: string;
}

interface SyncStatus {
  companyId: string;
  overallStatus: 'ok' | 'warning' | 'error' | 'unknown';
  lastSyncAtUtc: string | null;
  lastTransformAtUtc: string | null;
  objects: SyncObjectStatus[];
  dataFreshness: DataFreshness;
}

interface SyncObjectStatus {
  sapObject: string;                    // "OINV", "INV1", "ORIN", "RIN1", "OCRD", "OITM", "OSLP"
  watermarkDate: string | null;
  lastSuccessfulRunUtc: string | null;
  totalRowsIngested: number;
  status: 'ok' | 'warning' | 'no_data';
}

interface DataFreshness {
  rawLastUpdatedAtUtc: string | null;
  stgLastTransformedAtUtc: string | null;
  martLastTransformedAtUtc: string | null;
}

interface SyncTransformStatus {
  companyId: string;
  martTransformedAtUtc: string | null;
  stgTransformedAtUtc: string | null;
  martTables: MartTableStatus[];
}

interface MartTableStatus {
  tableName: string;
  rowCount: number;
  transformedAtUtc: string | null;
}

interface NativeBiDiagnostics {
  companyId: string;
  status: 'ok' | 'warning' | 'error' | 'unknown';
  checks: DiagnosticCheck[];
  generatedAtUtc: string;
}

interface DiagnosticCheck {
  name: string;                         // "staging_connection" | "mart_data_freshness" | etc.
  status: 'ok' | 'warning' | 'error' | 'unknown';
  detail: string | null;
}

interface NativeBiTableCounts {
  companyId: string;
  tables: TableCount[];
  generatedAtUtc: string;
}

interface TableCount {
  schema: string;                       // "stg" | "mart"
  tableName: string;
  rowCount: number;
  transformedAtUtc: string | null;
}
```

---

## Endpoints

### GET /api/client/dashboard/summary

Retorna KPI summary de la empresa.

**Query params:** solo `companyId` (DEV mode)

**Response:** `ApiResponse<DashboardSummary>`

```json
{
  "data": {
    "companyId": "company-dev-001",
    "grossSalesAmount": 1450000.00,
    "netSalesAmount": 1365000.00,
    "invoiceCount": 72,
    "activeCustomers": 20,
    "transformedAtUtc": "2026-06-07T00:00:00Z"
  },
  "traceId": "..."
}
```

---

### GET /api/client/dashboard/sales-daily

**Query params:**

| Param | Tipo | Default | Rango |
|---|---|---|---|
| `days` | int | 30 | 1–365 |

**Response:** `ApiResponse<SalesDaily[]>`

---

### GET /api/client/dashboard/sales-monthly

**Query params:**

| Param | Tipo | Default | Rango |
|---|---|---|---|
| `months` | int | 12 | 1–36 |

**Response:** `ApiResponse<SalesMonthly[]>`

---

### GET /api/client/dashboard/top-customers

**Query params:**

| Param | Tipo | Default | Válidos |
|---|---|---|---|
| `limit` | int | 10 | 1–100 |
| `offset` | int | 0 | >= 0 |
| `sortBy` | string | — | `netSalesAmount`, `salesAmount`, `invoiceCount`, `lastInvoiceDate`, `cardCode` |
| `sortDir` | string | `desc` | `asc`, `desc` |

**Response:** `PagedApiResponse<CustomerSales>`

---

### GET /api/client/dashboard/top-items

**Query params:** mismo patrón — `sortBy` válidos: `grossSalesAmount`, `quantitySold`, `invoiceCount`, `itemCode`

**Response:** `PagedApiResponse<ItemSales>`

---

### GET /api/client/dashboard/salespersons

**Query params:** mismo patrón — `sortBy` válidos: `netSalesAmount`, `salesAmount`, `invoiceCount`, `salesPersonCode`

**Response:** `PagedApiResponse<SalespersonSales>`

---

### GET /api/client/sales/overview

**Query params:**

| Param | Tipo | Default |
|---|---|---|
| `dateFrom` | string (YYYY-MM-DD) | hoy - 30 días |
| `dateTo` | string (YYYY-MM-DD) | hoy |

**Response:** `ApiResponse<SalesOverview>`

---

### GET /api/client/sales/daily

Mismos query params que `/sales/overview`.

**Response:** `ApiResponse<SalesDaily[]>`

---

### GET /api/client/sales/monthly

Mismos query params que `/sales/overview`.

**Response:** `ApiResponse<SalesMonthly[]>`

---

### GET /api/client/sales/customers

**Query params:** mismos que `/dashboard/top-customers` con `limit` default = 50.

**Response:** `PagedApiResponse<CustomerSales>`

---

### GET /api/client/sales/items

**Query params:** mismos que `/dashboard/top-items` con `limit` default = 50.

**Response:** `PagedApiResponse<ItemSales>`

---

### GET /api/client/sales/salespersons

**Query params:** mismos que `/dashboard/salespersons` con `limit` default = 50.

**Response:** `PagedApiResponse<SalespersonSales>`

---

### GET /api/client/sync/status

**Response:** `ApiResponse<SyncStatus>`

---

### GET /api/client/sync/objects

**Response:** `ApiResponse<SyncObjectStatus[]>`

---

### GET /api/client/sync/transform-status

**Response:** `ApiResponse<SyncTransformStatus>`

---

### GET /api/client/diagnostics/native-bi

Endpoint de salud del pipeline. Para panel de admin/ops, no para usuarios finales.

**Response:** `ApiResponse<NativeBiDiagnostics>`

Checks incluidos:
- `staging_connection` — Supabase accesible
- `mart_data_freshness` — OK < 24h, warning 24–48h, error > 48h
- `mart_tables_populated` — todas las tablas mart.* con filas
- `checkpoints_exist` — extractor ha corrido al menos una vez
- `last_extraction_run` — última corrida del extractor

---

### GET /api/client/diagnostics/native-bi/tables

**Response:** `ApiResponse<NativeBiTableCounts>`

Incluye row counts de `stg.sales_invoice`, `stg.credit_memo` y 6 tablas `mart.*`.

---

## Códigos de error

| Código | HTTP | Cuándo |
|---|---|---|
| `unauthorized` | 401 | JWT configurado pero request sin token |
| `forbidden_no_company` | 403 | Token válido pero sin claim de company |
| `missing_company_id` | 400 | DEV mode sin `?companyId` |
| `invalid_days` | 400 | `days` fuera de 1–365 |
| `invalid_months` | 400 | `months` fuera de 1–36 |
| `invalid_limit` | 400 | `limit` fuera de 1–100 |
| `invalid_offset` | 400 | `offset` < 0 |
| `invalid_sort_by` | 400 | `sortBy` no está en el allowlist |
| `invalid_sort_dir` | 400 | `sortDir` no es `asc` ni `desc` |
| `invalid_date_from` | 400 | `dateFrom` no es fecha válida |
| `invalid_date_to` | 400 | `dateTo` no es fecha válida |
| `invalid_date_range` | 400 | `dateFrom` > `dateTo` |

Todos los errores 4xx tienen body `ApiErrorResponse` con `error`, `message` y `traceId`.

---

## Notas importantes para el frontend

1. **No hardcodear `?companyId`** en prod — viene del JWT.
2. **`data` puede ser `null`** si no hay datos MART — tratar como estado vacío, no como error.
3. **`meta.hasMore = true`** → hay más páginas disponibles; incrementar `offset` por `limit`.
4. **`transformedAtUtc`** es la fuente de verdad para saber si los datos son frescos.
5. **Diagnostics** no deben mostrarse a usuarios finales — solo a admins/ops.
6. **`traceId`** útil para correlacionar errores con logs del servidor.
7. Todos los decimales son `number` (float64) — formatear con `Intl.NumberFormat` para moneda.
8. Todas las fechas son UTC — convertir a local del usuario antes de mostrar.
