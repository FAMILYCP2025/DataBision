# Native BI API Contract — Sprint 6A–6L

Backend read-only API over `mart.*`, `stg.*`, and `ctl.*` tables on Supabase PostgreSQL.
All endpoints are under `/api/client/`.

**Auth:** JWT Bearer token (production) or `?companyId` query param (dev only).  
**Versión:** Sprint 6I–6L (2026-06-07)

> **Ver también:** `docs/frontend-native-bi-backend-contract.md` para TypeScript interfaces completos.

---

## Autenticación (Sprint 6E–6I)

`[AllowAnonymous]` permanece en todos los controllers para que ASP.NET no bloquee el pipeline antes de que `CompanyContextResolver` pueda actuar. La seguridad se aplica manualmente dentro de cada action.

**Claim priority (company):** `company_slug` → `company_id` → `companyId`  
**Claim priority (role):** `role` → `user_role` → ClaimTypes.Role URI

| Escenario | Resultado |
|---|---|
| JWT válido + company claim presente | ✅ 200 con datos del tenant |
| JWT válido + sin company claim | ❌ 403 `forbidden_no_company` |
| JWT configurado + sin token | ❌ 401 `unauthorized` |
| JWT NO configurado + `?companyId` | ✅ DEV fallback |
| JWT NO configurado + sin `?companyId` | ❌ 400 `missing_company_id` |

SuperAdmin sin company claim → 403 (cross-company access no habilitado todavía).

---

## Reglas comunes

| Rule | Detail |
|---|---|
| `companyId` | DEV only. En producción viene del JWT. |
| `limit` | Rango 1–100. Fuera de rango → 400 (no se clampea). |
| `offset` | >= 0. Negativo → 400. |
| `days` | Rango 1–365. Fuera de rango → 400. |
| `months` | Rango 1–36. Fuera de rango → 400. |
| `dateFrom` / `dateTo` | Formato `YYYY-MM-DD`. Default: últimos 30 días si omitido. |
| Date validation | `dateFrom` no puede ser > `dateTo` → 400. |
| `sortBy` | Validado contra allowlist por entidad → 400 si inválido. |
| `sortDir` | Solo `asc` o `desc` → 400 si inválido. |
| Success shape | `{ "data": <T>, "traceId": "..." }` |
| Success paged | `{ "data": <T[]>, "meta": { limit, offset, count, hasMore }, "traceId": "..." }` |
| Error shape | `{ "error": "snake_case_code", "message": "...", "traceId": "..." }` |
| Empty data | 200 con `{ "data": [] }` o `{ "data": null }` — nunca 404. |
| Internal errors | PostgreSQL errors capturados y logueados — no expuestos al cliente. |

---

## Dashboard endpoints (`/api/client/dashboard/`)

### GET `/api/client/dashboard/summary`

Returns the single-row KPI summary for the company.

**Query params:**

| Param | Type | Required | Description |
|---|---|---|---|
| `companyId` | string | Yes | |

**Response `data`:**

```json
{
  "companyId": "company-dev-001",
  "grossSalesAmount": 1450000.00,
  "creditMemoAmount": 85000.00,
  "netSalesAmount": 1365000.00,
  "invoiceCount": 72,
  "creditMemoCount": 54,
  "activeCustomers": 20,
  "activeItems": 11,
  "avgTicketAmount": 20138.89,
  "lastInvoiceDate": "2026-05-30",
  "lastCreditMemoDate": "2026-05-28",
  "lastSyncAtUtc": null,
  "transformedAtUtc": "2026-06-06T22:14:00Z"
}
```

Source: `mart.sales_kpi_summary WHERE company_id = @company_id`

---

### GET `/api/client/dashboard/sales-daily`

**Query params:**

| Param | Type | Default | Max |
|---|---|---|---|
| `companyId` | string | — | — |
| `days` | int | 30 | 365 |

**Response `data`:** array of:

```json
{
  "salesDate": "2026-05-30",
  "grossSalesAmount": 48000.00,
  "creditMemoAmount": 5000.00,
  "netSalesAmount": 43000.00,
  "invoiceCount": 3,
  "creditMemoCount": 2,
  "activeCustomers": 3,
  "avgTicketAmount": 16000.00
}
```

Source: `mart.sales_daily` ordered by `sales_date DESC`

---

### GET `/api/client/dashboard/sales-monthly`

| Param | Default | Max |
|---|---|---|
| `months` | 12 | 36 |

**Response `data`:** array of `SalesMonthlyDto` — same fields as daily but `salesMonth` (first day of month).

Source: `mart.sales_monthly` ordered by `sales_month DESC`

---

### GET `/api/client/dashboard/top-customers`

| Param | Default | Max |
|---|---|---|
| `limit` | 10 | 100 |

**Response `data`:** array of:

```json
{
  "cardCode": "C001",
  "cardName": "Cliente SA",
  "salesAmount": 500000.00,
  "creditMemoAmount": 20000.00,
  "netSalesAmount": 480000.00,
  "invoiceCount": 12,
  "creditMemoCount": 2,
  "lastInvoiceDate": "2026-05-30",
  "firstInvoiceDate": "2026-01-05",
  "avgTicketAmount": 41666.67
}
```

Source: `mart.customer_sales ORDER BY net_sales_amount DESC`

---

### GET `/api/client/dashboard/top-items`

| Param | Default | Max |
|---|---|---|
| `limit` | 10 | 100 |

```json
{
  "itemCode": "ITEM-001",
  "itemName": "Producto A",
  "quantitySold": 245.00,
  "grossSalesAmount": 98000.00,
  "lineCount": 18,
  "invoiceCount": 12,
  "lastSaleDate": "2026-05-28"
}
```

Source: `mart.item_sales ORDER BY gross_sales_amount DESC`

---

### GET `/api/client/dashboard/salespersons`

| Param | Default | Max |
|---|---|---|
| `limit` | 20 | 100 |

```json
{
  "salesPersonCode": "3",
  "salesPersonName": "Juan Pérez",
  "salesAmount": 600000.00,
  "creditMemoAmount": 30000.00,
  "netSalesAmount": 570000.00,
  "invoiceCount": 35,
  "creditMemoCount": 8,
  "activeCustomers": 12,
  "avgTicketAmount": 17142.86
}
```

Source: `mart.salesperson_sales ORDER BY net_sales_amount DESC`

---

## Sales endpoints (`/api/client/sales/`)

### GET `/api/client/sales/overview`

Aggregates `mart.sales_daily` for the given date range.

| Param | Default | Notes |
|---|---|---|
| `companyId` | required | |
| `dateFrom` | today - 30 days | `YYYY-MM-DD` |
| `dateTo` | today | `YYYY-MM-DD` |

```json
{
  "grossSalesAmount": 850000.00,
  "creditMemoAmount": 45000.00,
  "netSalesAmount": 805000.00,
  "invoiceCount": 42,
  "creditMemoCount": 18,
  "avgTicketAmount": 20238.10,
  "activeCustomers": 15,
  "dateFrom": "2026-05-01",
  "dateTo": "2026-05-31"
}
```

Source: `SUM/MAX` over `mart.sales_daily WHERE sales_date BETWEEN @from AND @to`

---

### GET `/api/client/sales/daily`

Same shape as `/dashboard/sales-daily` but with explicit date range instead of `days`.

| Param | Default |
|---|---|
| `dateFrom` | today - 30 days |
| `dateTo` | today |

---

### GET `/api/client/sales/monthly`

Same shape as `/dashboard/sales-monthly` but with date range. Filters on `sales_month` using `DATE_TRUNC('month', ...)`.

---

### GET `/api/client/sales/customers`

Same as `/dashboard/top-customers` with default `limit=50`.

---

### GET `/api/client/sales/items`

Same as `/dashboard/top-items` with default `limit=50`.

---

### GET `/api/client/sales/salespersons`

Same as `/dashboard/salespersons` with default `limit=50`.

---

## Sync endpoints (`/api/client/sync/`)

### GET `/api/client/sync/status`

Overall sync and freshness status for the company.

```json
{
  "companyId": "company-dev-001",
  "overallStatus": "ok",
  "lastSyncAtUtc": "2026-06-06T22:10:00Z",
  "lastTransformAtUtc": "2026-06-06T22:14:00Z",
  "objects": [
    {
      "sapObject": "INV1",
      "watermarkDate": "2026-06-05",
      "lastSuccessfulRunUtc": "2026-06-06T22:10:00Z",
      "totalRowsIngested": 8430,
      "status": "ok"
    }
  ],
  "dataFreshness": {
    "rawLastUpdatedAtUtc": "2026-06-06T22:10:00Z",
    "stgLastTransformedAtUtc": "2026-06-06T22:14:00Z",
    "martLastTransformedAtUtc": "2026-06-06T22:14:00Z"
  }
}
```

**`overallStatus` logic:**

| Condition | Status |
|---|---|
| `mart.transformed_at_utc` < 24h old | `"ok"` |
| `mart.transformed_at_utc` 24–48h old | `"warning"` |
| `mart.transformed_at_utc` > 48h old | `"error"` |
| No MART data | `"unknown"` |

Sources: `ctl.ingest_checkpoint`, `ctl.extraction_run`, `mart.sales_kpi_summary`, `stg.sales_invoice`

---

### GET `/api/client/sync/objects`

Per-SAP-object checkpoint status. Returns entries for:
`OINV`, `INV1`, `ORIN`, `RIN1`, `OCRD`, `OITM`, `OSLP`

If an object has no checkpoint, returns `{ "status": "no_data" }`.

---

### GET `/api/client/sync/transform-status`

MART and STG transform freshness + row counts per table.

```json
{
  "companyId": "company-dev-001",
  "martTransformedAtUtc": "2026-06-06T22:14:00Z",
  "stgTransformedAtUtc": "2026-06-06T22:14:00Z",
  "martTables": [
    { "tableName": "sales_daily",      "rowCount": 36, "transformedAtUtc": "2026-06-06T22:14:00Z" },
    { "tableName": "sales_monthly",    "rowCount": 6,  "transformedAtUtc": "2026-06-06T22:14:00Z" },
    { "tableName": "customer_sales",   "rowCount": 20, "transformedAtUtc": "2026-06-06T22:14:00Z" },
    { "tableName": "item_sales",       "rowCount": 11, "transformedAtUtc": "2026-06-06T22:14:00Z" },
    { "tableName": "salesperson_sales","rowCount": 3,  "transformedAtUtc": "2026-06-06T22:14:00Z" },
    { "tableName": "sales_kpi_summary","rowCount": 1,  "transformedAtUtc": "2026-06-06T22:14:00Z" }
  ]
}
```

---

## Error codes reference

Todos los errores retornan body `ApiErrorResponse` con `error`, `message` y `traceId`.

| Code | HTTP | Description |
|---|---|---|
| `unauthorized` | 401 | JWT configurado pero request sin token |
| `forbidden_no_company` | 403 | Token válido pero sin claim de company |
| `missing_company_id` | 400 | DEV mode — `?companyId` param faltante |
| `invalid_days` | 400 | `days` fuera de 1–365 |
| `invalid_months` | 400 | `months` fuera de 1–36 |
| `invalid_limit` | 400 | `limit` fuera de 1–100 |
| `invalid_offset` | 400 | `offset` < 0 |
| `invalid_sort_by` | 400 | `sortBy` no está en el allowlist del endpoint |
| `invalid_sort_dir` | 400 | `sortDir` no es `asc` ni `desc` |
| `invalid_date_from` | 400 | `dateFrom` no es fecha válida YYYY-MM-DD |
| `invalid_date_to` | 400 | `dateTo` no es fecha válida YYYY-MM-DD |
| `invalid_date_range` | 400 | `dateFrom` > `dateTo` |

## Diagnostics endpoints (Sprint 6H)

Agregados en Sprint 6H. Ver descripción completa en `docs/frontend-native-bi-backend-contract.md`.

```
GET /api/client/diagnostics/native-bi
GET /api/client/diagnostics/native-bi/tables
```

Checks de salud incluidos: `staging_connection`, `mart_data_freshness`, `mart_tables_populated`, `checkpoints_exist`, `last_extraction_run`.
