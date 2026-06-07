# Data Reconciliation Checklist

Use after each extraction + transform cycle to verify data integrity across all layers.

---

## When to run

- After every `--run-once` + `--transform --include-mart` cycle
- Before any production demo or client delivery
- When a KPI value looks suspicious in the dashboard
- After any SAP data correction or manual document cancellation

---

## Layer 1 — RAW vs SAP

Verify that raw tables contain the expected documents from SAP B1.

- [ ] Total `raw.o_inv` row count ≈ `SELECT COUNT(*) FROM Invoices` in SAP (for the same company/date range)
- [ ] Most recent `doc_date` in `raw.o_inv` matches the latest invoice in SAP
- [ ] `raw.inv_1` line count ≈ total invoice lines in SAP
- [ ] `raw.o_rin` row count ≈ credit memos in SAP
- [ ] `raw.o_itm` row count ≈ active items in SAP
- [ ] `raw.o_slp` row count ≈ salesperson records in SAP
- [ ] `ctl.ingest_checkpoint` watermark dates are current (not stuck in the past)
- [ ] Last `ctl.extraction_run` has `status = 'completed'` (not `failed` or `running`)

**Quick query — RAW row counts:**
```sql
SELECT 'raw.o_inv'  AS t, COUNT(*) FROM raw.o_inv   WHERE company_id = 'KSDEPOR'
UNION ALL
SELECT 'raw.inv_1',        COUNT(*) FROM raw.inv_1   WHERE company_id = 'KSDEPOR'
UNION ALL
SELECT 'raw.o_rin',        COUNT(*) FROM raw.o_rin   WHERE company_id = 'KSDEPOR'
UNION ALL
SELECT 'raw.rin_1',        COUNT(*) FROM raw.rin_1   WHERE company_id = 'KSDEPOR'
UNION ALL
SELECT 'raw.o_itm',        COUNT(*) FROM raw.o_itm   WHERE company_id = 'KSDEPOR'
UNION ALL
SELECT 'raw.o_slp',        COUNT(*) FROM raw.o_slp   WHERE company_id = 'KSDEPOR';
```

---

## Layer 2 — STG vs RAW

Verify that STG tables correctly reflect non-cancelled RAW documents.

- [ ] `stg.sales_invoice` row count ≤ `raw.o_inv` (cancelled docs excluded)
- [ ] `stg.credit_memo` row count = `raw.o_rin` (credit memos are never cancelled in the STG filter)
- [ ] No cancelled invoices in `stg.sales_invoice`:
  ```sql
  SELECT COUNT(*) FROM stg.sales_invoice
  WHERE company_id = 'KSDEPOR' AND COALESCE(cancelled,'N') = 'Y';
  -- Expected: 0
  ```
- [ ] STG `transformed_at_utc` is recent (within last extraction window)
- [ ] `stg.item` and `stg.salesperson` row counts match `raw.o_itm` / `raw.o_slp`

**Quick query — RAW vs STG comparison:**
```sql
SELECT
    'invoices' AS object,
    (SELECT COUNT(*) FROM raw.o_inv WHERE company_id = 'KSDEPOR') AS raw_rows,
    (SELECT COUNT(*) FROM stg.sales_invoice WHERE company_id = 'KSDEPOR') AS stg_rows,
    (SELECT COUNT(*) FROM raw.o_inv
     WHERE company_id = 'KSDEPOR'
       AND COALESCE(cancelled,'N') = 'Y') AS cancelled_in_raw
UNION ALL
SELECT 'credit_memos',
    (SELECT COUNT(*) FROM raw.o_rin WHERE company_id = 'KSDEPOR'),
    (SELECT COUNT(*) FROM stg.credit_memo WHERE company_id = 'KSDEPOR'),
    0;
```

---

## Layer 3 — MART vs STG

Run `docs/sql/kpi-validation-queries.sql` — all 15 KPIs must have delta = 0.

- [ ] KPI-01 Gross Sales delta = 0
- [ ] KPI-02 Credit Memo Amount delta = 0
- [ ] KPI-03 Net Sales = gross - credit (internal consistency)
- [ ] KPI-04 Invoice Count delta = 0
- [ ] KPI-05 Credit Memo Count delta = 0
- [ ] KPI-06 Active Customers delta = 0
- [ ] KPI-07 Active Items delta = 0
- [ ] KPI-08 Avg Ticket delta < 0.01 (floating-point rounding acceptable)
- [ ] KPI-09 Last Invoice Date matches
- [ ] KPI-10 Last Credit Memo Date matches
- [ ] KPI-11 Daily Sales row count ≈ distinct invoice dates
- [ ] KPI-12 Monthly Gross Sales sum = KPI-01 Gross Sales
- [ ] KPI-13 Customer Net Sales sum = KPI-03 Net Sales
- [ ] KPI-14 Item Gross Sales sum ≈ STG invoice line totals
- [ ] KPI-15 Salesperson Count matches distinct codes in STG

---

## Layer 4 — MART vs Power BI

After Power BI dataset refresh:

- [ ] KPI card "Ventas Brutas" in dashboard = `mart.sales_kpi_summary.gross_sales_amount`
- [ ] KPI card "Ventas Netas" = `net_sales_amount`
- [ ] KPI card "N° Facturas" = `invoice_count`
- [ ] Sales by day chart matches `mart.sales_daily` (last 30 rows)
- [ ] Top customers table matches `mart.customer_sales` ordered by `net_sales_amount DESC`
- [ ] Power BI dataset refresh timestamp is after last `--transform` run

---

## Known data edge cases

| Scenario | Expected behavior |
|---|---|
| Invoice cancelled in SAP, already in RAW | Next extraction upserts the row; STG refresh excludes it |
| Invoice posted with doc_total = 0 | Included in counts, contributes 0 to amounts |
| Invoice with no salesperson code | Excluded from `mart.salesperson_sales`; included in all other marts |
| Credit memo date precedes any invoice date | `mart.sales_daily` row exists for that date (FULL OUTER JOIN) |
| Item with no item_name in SAP | `mart.item_sales.item_name` = NULL — shown as blank in dashboard |
| Extraction run partially failed (some objects ok, some failed) | `ctl.extraction_run` has `status = 'partial'`; re-run before transforming |

---

## Escalation thresholds

Investigate immediately if any of the following are true:

- Any KPI delta > 0.01
- `stg.sales_invoice` row count decreased since last run (rows should only grow or stay equal)
- `mart.sales_kpi_summary` has 0 rows or `gross_sales_amount = 0` for an active company
- `ctl.extraction_run` last row has `status = 'failed'`
- `ctl.ingest_checkpoint.watermark_date` has not advanced in > 24 hours
