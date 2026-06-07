# KPI Validation Results

**Company:** ___________________
**Environment:** Dev / Staging / Production
**Date run:** ___________________
**Extractor version (git SHA):** ___________________
**Data range:** from ___________ to ___________

## Pre-conditions

- [ ] `--run-once` completed successfully (or scheduled extraction ran)
- [ ] `--transform --include-mart` completed successfully
- [ ] `dotnet build` 0 errors, 0 warnings
- [ ] All 34 unit tests pass

## KPI Results

Run `docs/sql/kpi-validation-queries.sql` against Supabase — replace `company_id` parameter first.

| KPI | MART value | STG value | Delta | Status |
|---|---|---|---|---|
| KPI-01 Gross Sales | | | | |
| KPI-02 Credit Memo Amount | | | | |
| KPI-03 Net Sales | | | | |
| KPI-04 Invoice Count | | | | |
| KPI-05 Credit Memo Count | | | | |
| KPI-06 Active Customers | | | | |
| KPI-07 Active Items | | | | |
| KPI-08 Avg Ticket | | | | |
| KPI-09 Last Invoice Date | | | | |
| KPI-10 Last Credit Memo Date | | | | |
| KPI-11 Daily Sales Row Count | | | | |
| KPI-12 Monthly Gross Sales Sum | | | | |
| KPI-13 Customer Net Sales Sum | | | | |
| KPI-14 Item Gross Sales Sum | | | | |
| KPI-15 Salesperson Count | | | | |

**Acceptance criteria:** all deltas = 0 (or < 0.01 for floating-point rounding).

## Row counts

| Table | Rows |
|---|---|
| `raw.o_inv` | |
| `raw.inv_1` | |
| `raw.o_rin` | |
| `raw.rin_1` | |
| `raw.o_itm` | |
| `raw.o_slp` | |
| `stg.sales_invoice` | |
| `stg.sales_invoice_line` | |
| `stg.credit_memo` | |
| `stg.credit_memo_line` | |
| `stg.order` | |
| `stg.item` | |
| `stg.salesperson` | |
| `mart.sales_daily` | |
| `mart.sales_monthly` | |
| `mart.customer_sales` | |
| `mart.item_sales` | |
| `mart.salesperson_sales` | |
| `mart.sales_kpi_summary` | |

## KPI Summary snapshot

Paste output of the KPI summary query:

```
company_id             | 
gross_sales_amount     | 
credit_memo_amount     | 
net_sales_amount       | 
invoice_count          | 
credit_memo_count      | 
active_customers       | 
active_items           | 
avg_ticket             | 
last_invoice_date      | 
last_credit_memo_date  | 
transformed_at_utc     | 
```

## Anomalies / observations

_Describe any unexpected values, known exclusions, or data quality issues._

## Sign-off

| | Name | Date |
|---|---|---|
| Validated by | | |
| Reviewed by | | |
