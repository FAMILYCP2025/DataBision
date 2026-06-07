# Transform Runner — Usage Guide

Runs STG and/or MART SQL transformation functions against the staging PostgreSQL database.
No SAP connection required — operates entirely on already-ingested raw data.

## Modes

### STG only (default transform)

Calls `stg.refresh_all(company_id)` — refreshes the 7 STG tables from `raw.*`.

```
DataBision.Extractor --transform
DataBision.Extractor --transform --company KSDEPOR
```

### STG + MART (full pipeline)

Calls `stg.refresh_all` then `mart.refresh_all` sequentially.
MART step runs only if STG succeeds.

```
DataBision.Extractor --transform --include-mart
DataBision.Extractor --transform --include-mart --company KSDEPOR
```

### MART only

Calls `mart.refresh_all(company_id)` — refreshes the 6 MART tables from `stg.*`.
Use when STG is already up to date.

```
DataBision.Extractor --transform-mart
DataBision.Extractor --transform-mart --company KSDEPOR
```

## Company resolution

| Source | Priority |
|---|---|
| `--company <id>` flag | 1 (highest) |
| `Extractor:CompanyId` in `appsettings.json` | 2 (fallback) |

If neither is set, the command exits with code 2.

## Exit codes

| Code | Meaning |
|---|---|
| 0 | All transformations completed successfully |
| 2 | Configuration error (missing company, invalid connection string) |
| 5 | Runtime exception during transformation |

## Configuration

`appsettings.json` — `Staging` section:

```json
{
  "Staging": {
    "ConnectionString": "Host=...;Database=postgres;Username=...;Password=..."
  }
}
```

The runner uses a direct Npgsql connection — not EF Core. The connection is opened per call and closed immediately after.

## What each layer refreshes

### STG (`stg.refresh_all`)

| Function | Target table | Source |
|---|---|---|
| `stg.refresh_sales_invoice` | `stg.sales_invoice` | `raw.o_inv` |
| `stg.refresh_sales_invoice_line` | `stg.sales_invoice_line` | `raw.inv_1` |
| `stg.refresh_credit_memo` | `stg.credit_memo` | `raw.o_rin` |
| `stg.refresh_credit_memo_line` | `stg.credit_memo_line` | `raw.rin_1` |
| `stg.refresh_order` | `stg.order` | `raw.o_rdr` |
| `stg.refresh_item` | `stg.item` | `raw.o_itm` |
| `stg.refresh_salesperson` | `stg.salesperson` | `raw.o_slp` |

All use `INSERT ... ON CONFLICT DO UPDATE` — idempotent and re-runnable.
Cancelled documents (`COALESCE(cancelled,'N') = 'Y'`) are excluded from STG.

### MART (`mart.refresh_all`)

| Function | Target table | Description |
|---|---|---|
| `mart.refresh_sales_daily` | `mart.sales_daily` | Gross/net sales per day |
| `mart.refresh_sales_monthly` | `mart.sales_monthly` | Gross/net sales per month |
| `mart.refresh_customer_sales` | `mart.customer_sales` | Sales totals per customer |
| `mart.refresh_item_sales` | `mart.item_sales` | Sales totals per item |
| `mart.refresh_salesperson_sales` | `mart.salesperson_sales` | Sales totals per salesperson |
| `mart.refresh_sales_kpi_summary` | `mart.sales_kpi_summary` | Single-row KPI summary |

All use `FULL OUTER JOIN` of invoice and credit memo aggregations to handle asymmetric dates.
`mart.sales_kpi_summary` uses `CROSS JOIN` of single-row subqueries.

## Typical pipeline sequence

```
# 1. Extract from SAP B1 (run nightly)
DataBision.Extractor --run-once

# 2. Refresh STG + MART after extraction completes
DataBision.Extractor --transform --include-mart

# 3. Validate in Supabase
-- See docs/sql/mart-validation-queries.sql
```

## Logging

Each object processed is logged individually:

```
[INF] STG refresh starting — company_id=KSDEPOR
[INF]   stg.refresh_all.sales_invoice: 1423 row(s) affected
[INF]   stg.refresh_all.sales_invoice_line: 5901 row(s) affected
...
[INF] STG refresh complete — 7 object(s) processed
[INF] MART refresh starting — company_id=KSDEPOR
[INF]   mart.refresh_all.sales_daily: 312 row(s) affected
...
[INF] MART refresh complete — 6 object(s) processed
[INF] === STG+MART Transform: COMPLETE (STG 1842ms + MART 603ms) ===
```

## Running as part of `--schedule` or `--service`

The scheduled execution modes do not currently auto-trigger transforms.
Transforms must be scheduled separately (e.g., Windows Task Scheduler, cron) after the extractor completes.
A post-extraction hook is planned for a future sprint.
