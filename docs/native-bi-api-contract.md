# Native BI API Contract — Sprint 6A–6C

Backend read-only API over `mart.*`, `stg.*`, and `ctl.*` tables on Supabase PostgreSQL.
All endpoints are under `/api/client/` and require `companyId` as a query parameter.

> **Auth status:** `[AllowAnonymous]` for MVP. JWT enforcement planned for Sprint 6E.

---

## Common rules

| Rule | Detail |
|---|---|
| `companyId` | Required on all endpoints. Returns 400 if missing or blank. |
| `limit` | Max 100. Values out of range are clamped (not rejected). |
| `days` | Max 365. Values out of range are clamped. |
| `months` | Max 36. Values out of range are clamped. |
| `dateFrom` / `dateTo` | Format: `YYYY-MM-DD`. Default: last 30 days if omitted. |
| Date validation | `dateFrom` cannot be after `dateTo` → 400. |
| Success shape | `{ "data": <T> }` |
| Error shape | `{ "error": "snake_case_code", "message": "Human readable text." }` |
| Empty data | Returns 200 with `{ "data": [] }` or `{ "data": null }` — never 404. |
| Internal errors | Never exposed to client. PostgreSQL errors are caught and logged. |

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

| Code | HTTP | Description |
|---|---|---|
| `missing_company_id` | 400 | `companyId` param is empty or not provided |
| `invalid_days` | 400 | `days` outside 1–365 |
| `invalid_months` | 400 | `months` outside 1–36 |
| `invalid_limit` | 400 | `limit` outside 1–100 (only when value rejected, not when clamped) |
| `invalid_date_from` | 400 | `dateFrom` not a valid `YYYY-MM-DD` |
| `invalid_date_to` | 400 | `dateTo` not a valid `YYYY-MM-DD` |
| `invalid_date_range` | 400 | `dateFrom` > `dateTo` |
