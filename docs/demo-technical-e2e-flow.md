# DataBision — Technical E2E Flow (Demo Reference)

End-to-end data flow from SAP Business One to a Power BI dashboard embedded in the portal.

---

## Architecture diagram

```
SAP B1 (KSDEPOR)
  └─ Service Layer OData API (HTTPS :50000)
       │
       │  HTTP GET /b1s/v1/Orders?$filter=...&$top=50
       │
  ┌────▼─────────────────┐
  │  DataBision.Extractor │   .NET 8 console / Windows Service
  │  (OinvExtractorJob,   │
  │   OcrdExtractorJob,   │
  │   OitmExtractorJob,   │
  │   OrinExtractorJob)   │
  └────────┬─────────────┘
           │
           │  HTTP POST /api/ingest/batch
           │  Authorization: Bearer <service-token>
           │
  ┌────────▼──────────────────┐
  │   DataBision.Api           │   .NET 8 Web API (Azure)
  │   IngestController         │
  │   SapRawRepository (EF)    │
  └────────┬──────────────────┘
           │
           │  INSERT ... ON CONFLICT DO UPDATE
           │
  ┌────────▼────────────────────────────────────────┐
  │  Supabase PostgreSQL                             │
  │                                                  │
  │  raw.*  ◄── Extractor writes here               │
  │    raw.o_inv, raw.inv_1                          │
  │    raw.o_rin, raw.rin_1                          │
  │    raw.o_itm, raw.o_slp                          │
  │                                                  │
  │  ctl.*  ◄── Extraction metadata                 │
  │    ctl.extraction_run                            │
  │    ctl.ingest_checkpoint                         │
  │    ctl.source_object_config                      │
  │                                                  │
  │  stg.*  ◄── Transformation layer (STG)          │
  │    stg.sales_invoice, stg.sales_invoice_line     │
  │    stg.credit_memo, stg.credit_memo_line         │
  │    stg.order, stg.item, stg.salesperson          │
  │    (populated by stg.refresh_all)                │
  │                                                  │
  │  mart.*  ◄── Aggregation layer (MART)           │
  │    mart.sales_daily, mart.sales_monthly          │
  │    mart.customer_sales, mart.item_sales          │
  │    mart.salesperson_sales, mart.sales_kpi_summary│
  │    (populated by mart.refresh_all)               │
  └────────┬────────────────────────────────────────┘
           │
           │  DirectQuery / Import
           │
  ┌────────▼──────────┐
  │  Power BI Desktop  │   Published to Power BI workspace
  │  (mart.* tables)   │
  └────────┬──────────┘
           │
           │  Embed token (RLS: username=company.slug)
           │  POST /api/reports/{id}/embed-token
           │
  ┌────────▼─────────────────────────────────────────┐
  │  DataBision Portal  {slug}.databision.app         │
  │  React + powerbi-client-react                     │
  │  JWT auth (RS256), TenantMiddleware               │
  └──────────────────────────────────────────────────┘
```

---

## Step-by-step data flow

### Step 1 — SAP B1 Service Layer authentication

The extractor authenticates to SAP B1 Service Layer using Basic Auth over HTTPS (port 50000).
A `B1SESSION` cookie is maintained for the session duration and refreshed as needed.

```
POST https://sap-host:50000/b1s/v1/Login
{ "CompanyDB": "KSDEPOR", "UserName": "...", "Password": "..." }
← 200 { "SessionId": "..." }  +  Set-Cookie: B1SESSION=...
```

### Step 2 — Incremental extraction from SAP OData

Each extractor job (e.g., `OinvExtractorJob`) queries Service Layer with OData filters:

```
GET /b1s/v1/Invoices?$filter=UpdateDate ge '2026-06-01'&$top=50&$skip=0&$select=DocEntry,...
```

Key behaviors:
- **Checkpoint-based**: last successful watermark stored in `ctl.ingest_checkpoint`
- **Lookback window**: configurable days to re-check recently updated documents
- **Page size**: configurable (default 50) to avoid Service Layer timeouts
- **Retry**: exponential back-off on 5xx / timeout

### Step 3 — Ingest API write

Extracted rows are batched and sent to `POST /api/ingest/batch`:

```json
{
  "sapObject": "OINV",
  "companyId": "KSDEPOR",
  "rows": [ { "DocEntry": 1001, "CardCode": "C001", ... }, ... ]
}
```

`SapRawRepository` writes to `raw.o_inv` using `INSERT ... ON CONFLICT (company_id, doc_entry) DO UPDATE`.
Header and line rows are written separately but in the same extraction run.

### Step 4 — STG transformation

After extraction completes, `stg.refresh_all(company_id)` is called via `TransformationRunner`:

```
DataBision.Extractor --transform --company KSDEPOR
```

Each `stg.refresh_*` function:
1. Reads from `raw.*`
2. Excludes cancelled documents (`COALESCE(cancelled,'N') != 'Y'`)
3. Writes to `stg.*` with `INSERT ... ON CONFLICT DO UPDATE`

Output: 7 clean, deduplicated staging tables.

### Step 5 — MART aggregation

After STG completes, `mart.refresh_all(company_id)` is called:

```
DataBision.Extractor --transform-mart --company KSDEPOR
```

Each `mart.refresh_*` function aggregates `stg.*` into reporting-ready tables:
- `mart.sales_daily` / `mart.sales_monthly`: `FULL OUTER JOIN` of invoice and credit memo aggregations
- `mart.customer_sales` / `mart.item_sales` / `mart.salesperson_sales`: per-entity totals
- `mart.sales_kpi_summary`: single-row `CROSS JOIN` of aggregate subqueries

### Step 6 — Power BI dataset refresh

Power BI connects to `mart.*` via a Supabase PostgreSQL connection.
After MART refresh, Power BI dataset is refreshed (manually or scheduled).
RLS is enforced: `username = company.slug`, `role = "CompanyRole"`.

### Step 7 — Embed token generation

Company user logs in → frontend calls `POST /api/reports/{id}/embed-token`:

```
Authorization: Bearer <jwt>
← { "embedToken": "...", "embedUrl": "...", "expiry": "..." }
```

Embed token is scoped to the requesting company via `TenantMiddleware` + JWT `company_id` claim.
Token auto-refreshed 5 min before expiry.

### Step 8 — Dashboard rendered in portal

`powerbi-client-react` renders the report inside the portal at `{slug}.databision.app`.
Branding (`--brand-primary`, sidebar color) is applied from `GET /api/tenant/config`.

---

## Timing reference (KSDEPOR demo)

| Step | Typical duration |
|---|---|
| SAP extraction (incremental, 1 day lookback) | 30–90 s |
| Ingest API batch write | 5–15 s |
| STG refresh (7 tables) | 1–3 s |
| MART refresh (6 tables) | 0.5–2 s |
| Power BI dataset refresh | 30–120 s |
| Embed token generation | < 1 s |

---

## CLI commands for live demo

```bash
# 1. Incremental extraction
DataBision.Extractor --run-once --company KSDEPOR

# 2. STG + MART refresh
DataBision.Extractor --transform --include-mart --company KSDEPOR

# 3. Validate data
-- Run docs/sql/kpi-validation-queries.sql in Supabase
-- Run docs/sql/mart-validation-queries.sql for row counts + top-10

# 4. Trigger Power BI dataset refresh (manual or via Power BI REST API)

# 5. Open portal
# https://ksdepor.databision.app  (or ?tenant=ksdepor locally)
```

---

## Key design decisions

| Decision | Rationale |
|---|---|
| RAW layer is append-via-upsert | SAP documents can be updated retroactively; upsert ensures idempotency |
| STG excludes cancelled docs | Cancelled docs should not appear in reports; cleaner than filtering in Power BI |
| MART uses FULL OUTER JOIN | Days with credit memos but no invoices (and vice versa) must appear in daily/monthly tables |
| No tenant_id in STG/MART | Single-company demo; raw tables carry `company_id` as the isolation key |
| Service Layer page size = 50 | Empirically avoids SAP Service Layer timeout at 100+ rows per page |
| Embed token with RLS | Prevents cross-tenant data leakage at the Power BI level |
