# Native BI — MART Formulas Reference

This document details the SQL expressions and business rules used to compute each field in the MART layer (`mart.*` tables in Supabase). All formulas operate on data already normalized by the STG layer.

---

## mart.fact_sales_orders

| Field | Formula / Source | Notes |
|---|---|---|
| `gross_amount` | `SUM(stg.orin.line_total)` grouped by `doc_entry` | Before discount |
| `net_amount` | `gross_amount - discount_total` | Applied at header level |
| `discount_total` | `stg.oinv.disc_sum` | Header discount only |
| `margin_amount` | `net_amount - cost_amount` | Requires cost sync |
| `margin_pct` | `CASE WHEN net_amount > 0 THEN margin_amount / net_amount ELSE NULL END` | NULL if no revenue |
| `doc_date` | `stg.oinv.doc_date` | SAP posting date |
| `company_id` | Injected from extractor `company_id` context | Tenant isolation key |

---

## mart.dim_customers

| Field | Formula / Source | Notes |
|---|---|---|
| `total_sales_amount` | `SUM(fact_sales_orders.net_amount) WHERE status != 'cancelled'` | Lifetime value |
| `last_order_date` | `MAX(fact_sales_orders.doc_date)` | Per customer |
| `order_count` | `COUNT(DISTINCT fact_sales_orders.doc_num)` | Unique SAP doc numbers |
| `avg_order_value` | `total_sales_amount / NULLIF(order_count, 0)` | NULL-safe division |
| `is_active` | `last_order_date >= CURRENT_DATE - INTERVAL '365 days'` | Rolling 12-month window |

---

## mart.dim_items

| Field | Formula / Source | Notes |
|---|---|---|
| `total_quantity_sold` | `SUM(stg.orin.quantity)` | Includes returns (negative) |
| `total_revenue` | `SUM(stg.orin.line_total)` | Gross line total |
| `unit_cost` | `stg.oitm.avg_price` | SAP average price field |
| `total_margin` | `total_revenue - (total_quantity_sold * unit_cost)` | Approximate; cost snapshot |
| `rotation_score` | `total_quantity_sold / NULLIF(days_in_period, 0) * 30` | Monthly velocity |

---

## mart.fact_ar_aging

Aging buckets computed at transform time (not at query time) from `stg.oinv` where `DocStatus = 'O'` (open invoices).

| Bucket | Condition |
|---|---|
| `current` | `days_overdue <= 0` |
| `bucket_1_30` | `days_overdue BETWEEN 1 AND 30` |
| `bucket_31_60` | `days_overdue BETWEEN 31 AND 60` |
| `bucket_61_90` | `days_overdue BETWEEN 61 AND 90` |
| `bucket_over_90` | `days_overdue > 90` |

`days_overdue = CURRENT_DATE - doc_due_date`

**Snapshot behavior:** Aging is computed when the transform runs, not live. Queries against `mart.fact_ar_aging` reflect the state at last transform time (`mart_summary.transformed_at`).

---

## mart.fact_ap_aging

Same structure as `fact_ar_aging` but sourced from `stg.opch` (purchase invoices / accounts payable).

**Known limitation (demo):** AP data is currently empty for the KSDEPOR demo company. The MART layer returns zero rows for AP queries. See `native-bi-demo-limitations.md`.

---

## mart.fact_inventory_stock

| Field | Formula / Source | Notes |
|---|---|---|
| `on_hand_qty` | `stg.oitw.on_hand` | Per warehouse |
| `committed_qty` | `stg.oitw.is_committed` | Allocated to open orders |
| `ordered_qty` | `stg.oitw.on_order` | On open POs |
| `available_qty` | `on_hand_qty - committed_qty` | Can go negative |
| `stock_value` | `on_hand_qty * stg.oitm.avg_price` | Snapshot at transform time |
| `reorder_point` | `stg.oitm.min_level` | From SAP item master |
| `needs_reorder` | `available_qty < reorder_point` | Computed flag |

**Known limitation (demo):** Warehouse dimension data is empty for KSDEPOR demo. `GetInventoryWarehousesAsync` returns an empty list.

---

## mart.fact_purchasing

| Field | Formula / Source | Notes |
|---|---|---|
| `po_amount` | `SUM(stg.por1.line_total)` per `doc_entry` | Purchase order lines |
| `received_amount` | `SUM(stg.pdn1.line_total)` per `base_entry` | Goods receipt lines |
| `fulfillment_rate` | `received_amount / NULLIF(po_amount, 0)` | 0–1 range |
| `lead_time_days` | `grpo_date - po_date` | Days from PO to receipt |
| `avg_lead_time` | `AVG(lead_time_days)` per supplier | Supplier-level aggregate |

---

## mart.fact_pipeline_health

Computed from `stg.ops_pipeline_runs` and `stg.ops_alerts`.

| Field | Formula |
|---|---|
| `health_score` | `100 - (critical_count * 20) - (warning_count * 5)` clamped to `[0, 100]` |
| `error_rate` | `failed_runs / NULLIF(total_runs, 0)` over last 30 days |
| `avg_duration_sec` | `AVG(duration_seconds)` for successful runs |
| `sla_breach_count` | `COUNT(*) WHERE duration_seconds > sla_threshold_seconds` |

**Health score bands:**
- ≥ 80 → green (healthy)
- 50–79 → orange (degraded)
- < 50 → red (critical)

---

## mart.mart_summary

A single-row metadata table per `company_id` tracking transform timestamps.

| Field | Value |
|---|---|
| `company_id` | Analytics company identifier |
| `transformed_at` | UTC timestamp of last successful MART transform |
| `stg_transformed_at` | UTC timestamp of last STG transform |
| `extraction_last_run_at` | UTC timestamp of last raw extraction |
| `source_row_count` | Total rows extracted from SAP this run |

Used by `DiagnosticsService` and `SyncStatusService` for freshness checks.

---

## Transform Freshness SLAs

| Status | Age Threshold |
|---|---|
| `ok` | ≤ 24 hours since last transform |
| `warning` | 24–48 hours |
| `error` | > 48 hours |
| `unknown` | No transform record found |

These thresholds are defined as constants in `DiagnosticsService.cs` and `SyncStatusService.cs`.

---

## Null and Edge-Case Handling

- **Division by zero:** All ratio fields use `NULLIF(denominator, 0)` — never divide directly.
- **Empty periods:** If no sales exist in the selected date range, aggregate queries return 0 (not NULL) for amounts and counts. APIs return empty arrays.
- **Negative stock:** `available_qty` can be negative (oversold). Frontend displays with a warning badge.
- **Future dates:** SAP allows future `doc_due_date`. Aging treats these as `current` (not overdue).
- **Cancelled documents:** `DocStatus = 'C'` rows are excluded from all MART aggregations at STG→MART transform time.
