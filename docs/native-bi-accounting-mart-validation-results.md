# Native BI Accounting — MART Validation Results

Sprint 15C — 2026-06-18

---

## Current State

| Layer | Status | Reason |
|---|---|---|
| RAW (raw.sap_oact / sap_ojdt / sap_jdt1) | ⏳ Empty | Extraction pending CompanyId resolution (Sprint 15B/15D) |
| STG (stg.gl_account / journal_entry / journal_entry_line) | ⏳ Empty | Requires RAW data first |
| MART (all 5 tables) | ⏳ Empty | Requires STG data first |
| cfg.account_classification_rules | ⏳ Empty | Classification rules pending (Sprint 15D) |

**Pre-condition for this sprint:** Sprint 15B (extraction) must complete successfully before STG/MART can be refreshed.

---

## MART Refresh Execution

Once RAW tables are populated, run the full ETL pipeline:

```sql
-- In Supabase SQL Editor — replace '<AnalyticsCompanyId>' with actual value
SELECT step_name, status, executed_at_utc, message
FROM mart.refresh_accounting_all('<AnalyticsCompanyId>');
```

### Expected output (all 7 steps):

| step_name | status | message |
|---|---|---|
| stg.refresh_gl_accounts | OK | null |
| stg.refresh_journal_entries | OK | null |
| mart.refresh_gl_accounts | OK | null |
| mart.refresh_account_balances | OK | null |
| mart.refresh_income_statement | OK | null |
| mart.refresh_balance_sheet | OK | null |
| mart.refresh_ebitda | OK | null |

Any row with `status = 'ERROR'` halts that step only (subsequent steps may succeed or fail with empty inputs). Address errors before proceeding to validation.

---

## STG Layer Validation

### Row counts

```sql
SELECT 'stg.gl_account'         AS layer, COUNT(*) AS rows FROM stg.gl_account          WHERE company_id = '<AnalyticsCompanyId>'
UNION ALL
SELECT 'stg.journal_entry',               COUNT(*) FROM stg.journal_entry         WHERE company_id = '<AnalyticsCompanyId>'
UNION ALL
SELECT 'stg.journal_entry_line',          COUNT(*) FROM stg.journal_entry_line     WHERE company_id = '<AnalyticsCompanyId>'
ORDER BY layer;
```

Expected: same order-of-magnitude as RAW counts (STG is a normalized copy).

### Account type distribution (STG)

```sql
SELECT account_type, postable, COUNT(*) AS accounts
FROM stg.gl_account
WHERE company_id = '<AnalyticsCompanyId>'
GROUP BY account_type, postable
ORDER BY accounts DESC;
```

Check: `postable = true` accounts should represent the majority of accounts that will have journal activity.

### Boolean conversion check (from tYES/tNO)

```sql
SELECT code, name, postable, frozen, valid_for
FROM stg.gl_account
WHERE company_id = '<AnalyticsCompanyId>'
  AND postable IS NULL
LIMIT 10;
```

Expected: 0 rows. If NULLs appear, the OACT raw data has unexpected values in the `Postable` field — check the raw value and update `stg.refresh_gl_accounts` CASE expression if needed.

### Level conversion check

```sql
SELECT code, name, level
FROM stg.gl_account
WHERE company_id = '<AnalyticsCompanyId>'
  AND level IS NULL
  AND EXISTS (
      SELECT 1 FROM "raw"."sap_oact" oa
      WHERE oa.company_id = '<AnalyticsCompanyId>' AND oa."Code" = stg.gl_account.code
        AND oa."Levels" IS NOT NULL
  )
LIMIT 10;
```

If NULLs appear with non-null raw `Levels`, the value was non-numeric — check the raw `Levels` field format.

---

## MART GL Accounts Validation

### Row counts and classification coverage

```sql
SELECT statement_line, COUNT(*) AS accounts,
       ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (), 1) AS pct
FROM mart.gl_accounts
WHERE company_id = '<AnalyticsCompanyId>'
GROUP BY statement_line
ORDER BY accounts DESC;
```

**Target before demo:** `unclassified` < 5% of total accounts (by count). For a typical SAP B1 chart of accounts with 200–500 accounts, this means < 10–25 unclassified.

### Verify classification priority (code > prefix > SAP fallback)

```sql
-- Check if any explicit cfg rules were applied
SELECT ga.code, ga.name, ga.statement_line,
       r.statement_line AS rule_sl, r.account_code, r.format_code
FROM mart.gl_accounts ga
LEFT JOIN cfg.account_classification_rules r
    ON r.company_id = ga.company_id AND (r.account_code = ga.code OR ga.format_code LIKE r.format_code || '%')
WHERE ga.company_id = '<AnalyticsCompanyId>'
ORDER BY ga.code
LIMIT 20;
```

---

## MART Account Balances Validation

### Total debit/credit by period

```sql
SELECT period_year, period_month,
       ROUND(SUM(debit_sum), 2) AS total_debit,
       ROUND(SUM(credit_sum), 2) AS total_credit,
       ROUND(SUM(debit_sum) - SUM(credit_sum), 2) AS net
FROM mart.account_balances
WHERE company_id = '<AnalyticsCompanyId>'
GROUP BY period_year, period_month
ORDER BY period_year DESC, period_month DESC
LIMIT 12;
```

In double-entry accounting, `SUM(debit) = SUM(credit)` for any closed period. The `net` column should be near 0 for each period. Large net values indicate incomplete extraction or journal entries that span periods.

### Accounts with activity

```sql
SELECT COUNT(DISTINCT code) AS accounts_with_activity,
       MIN(period_year || '-' || LPAD(period_month::TEXT, 2, '0')) AS first_period,
       MAX(period_year || '-' || LPAD(period_month::TEXT, 2, '0')) AS last_period
FROM mart.account_balances
WHERE company_id = '<AnalyticsCompanyId>';
```

---

## MART Income Statement Validation

### P&L by period and statement_line

```sql
SELECT period_year, period_month, statement_line, ROUND(amount, 2) AS amount
FROM mart.income_statement_summary
WHERE company_id = '<AnalyticsCompanyId>'
ORDER BY period_year DESC, period_month DESC, statement_line
LIMIT 60;
```

**Sign convention checks:**
- `revenue` amounts should be **positive** (credit-heavy accounts)
- `cogs` amounts should be **positive** (debit-heavy accounts)
- `opex` amounts should be **positive** (debit-heavy accounts)
- If `revenue` is negative → sign convention issue; check if SAP instance uses reversed polarity

### Revenue check

```sql
SELECT period_year, period_month, ROUND(amount, 2) AS revenue,
       CASE WHEN amount < 0 THEN '⚠️ NEGATIVE' ELSE '✅ OK' END AS sign_check
FROM mart.income_statement_summary
WHERE company_id = '<AnalyticsCompanyId>'
  AND statement_line = 'revenue'
ORDER BY period_year DESC, period_month DESC
LIMIT 12;
```

---

## MART Balance Sheet Validation

### Latest balance sheet snapshot

```sql
SELECT snapshot_date, category, sub_category, ROUND(amount, 2) AS amount
FROM mart.balance_sheet_summary
WHERE company_id = '<AnalyticsCompanyId>'
  AND snapshot_date = (
      SELECT MAX(snapshot_date) FROM mart.balance_sheet_summary WHERE company_id = '<AnalyticsCompanyId>'
  )
ORDER BY category, sub_category;
```

### Balance equation check (Activos = Pasivos + Patrimonio)

```sql
SELECT
    ROUND(SUM(CASE WHEN category IN ('current_assets', 'non_current_assets') THEN amount ELSE 0 END), 2)           AS total_assets,
    ROUND(SUM(CASE WHEN category IN ('current_liabilities', 'non_current_liabilities', 'equity') THEN amount ELSE 0 END), 2) AS total_liab_equity,
    ROUND(
        SUM(CASE WHEN category IN ('current_assets', 'non_current_assets') THEN amount ELSE 0 END)
        - SUM(CASE WHEN category IN ('current_liabilities', 'non_current_liabilities', 'equity') THEN amount ELSE 0 END)
    , 2) AS imbalance
FROM mart.balance_sheet_summary
WHERE company_id = '<AnalyticsCompanyId>'
  AND snapshot_date = (
      SELECT MAX(snapshot_date) FROM mart.balance_sheet_summary WHERE company_id = '<AnalyticsCompanyId>'
  );
```

**Accept:** `|imbalance| < 0.01` (rounding). Larger imbalance = missing account classifications (likely equity accounts not classified as `equity`).

> **Note:** The current implementation of `mart.refresh_balance_sheet` is flow-based (aggregates debit/credit per period), not cumulative. True balance sheet requires running totals over all prior periods. This is a known limitation (documented in `docs/native-bi-accounting-mart.md`).

---

## MART EBITDA Validation

### EBITDA trend

```sql
SELECT
    period_year, period_month,
    ROUND(revenue,      2) AS revenue,
    ROUND(cogs,         2) AS cogs,
    ROUND(gross_profit, 2) AS gross_profit,
    ROUND(opex,         2) AS opex,
    ROUND(ebitda,       2) AS ebitda,
    ROUND(net_income,   2) AS net_income
FROM mart.ebitda_summary
WHERE company_id = '<AnalyticsCompanyId>'
ORDER BY period_year DESC, period_month DESC
LIMIT 12;
```

**Consistency checks:**
- `gross_profit = revenue − cogs`
- `ebitda = gross_profit − opex` (D&A = 0 until classified)
- `net_income = ebitda − financial_result − tax_result`

---

## API Endpoint Validation

### Readiness endpoint

```bash
curl -H "Authorization: Bearer <valid-jwt>" \
     "http://localhost:5103/api/client/bi/finance/readiness"
```

Expected when MART is populated:
```json
{
  "data": {
    "rawOactCount": 250,
    "rawOjdtCount": 5000,
    "rawJdt1Count": 12500,
    "stgOactCount": 250,
    "stgOjdtCount": 5000,
    "stgJdt1Count": 12500,
    "martGlAccounts": 250,
    "martIncomeStatement": 36,
    "martBalanceSheet": 36,
    "martEbitda": 12,
    "classificationRules": 25,
    "unclassifiedPostable": 3,
    "readinessStatus": "ready",
    "blockingReasons": [],
    "warnings": ["3 postable accounts are unclassified"]
  }
}
```

### Validations endpoint

```bash
curl -H "Authorization: Bearer <valid-jwt>" \
     "http://localhost:5103/api/client/bi/finance/validations"
```

Expected when healthy:
```json
{
  "data": {
    "healthScore": 90,
    "healthStatus": "ok",
    "criticalIssues": 0,
    "warningIssues": 1,
    "infoIssues": 0,
    "isBalanced": true,
    "balanceImbalance": 0.00,
    "unclassifiedAccounts": 3
  }
}
```

---

## Frontend Tab Validation

After readiness = `ready`, open the Finance dashboard and verify each tab loads without `FinancialDataPending`:

| Tab | URL route | Expected content |
|---|---|---|
| Resumen | `/finanzas` | Readiness panel + KPIs |
| Estado de Resultados | `/finanzas?tab=resultados` | P&L table with periods |
| Balance General | `/finanzas?tab=balance` | Balance sheet with cuadra badge |
| EBITDA | `/finanzas?tab=ebitda` | EBITDA trend table |
| Plan de Cuentas | `/finanzas?tab=cuentas` | GL accounts with statement_line |
| Validaciones | `/finanzas?tab=validaciones` | Health score + issue list |

---

## Validation Results Log (to fill after execution)

| Check | Result | Date | Notes |
|---|---|---|---|
| mart.refresh_accounting_all — all 7 steps OK | ⏳ Pending | — | — |
| stg.gl_account row count | ⏳ Pending | — | — |
| stg.journal_entry_line row count | ⏳ Pending | — | — |
| mart.gl_accounts unclassified < 5% | ⏳ Pending | — | — |
| Period debit == credit (net ≈ 0) | ⏳ Pending | — | — |
| Revenue positive in all periods | ⏳ Pending | — | — |
| Balance imbalance < 0.01 | ⏳ Pending | — | — |
| Readiness endpoint: status = ready | ⏳ Pending | — | — |
| Validations endpoint: healthScore >= 80 | ⏳ Pending | — | — |
| All 6 Finance tabs load without FinancialDataPending | ⏳ Pending | — | — |

---

## References

- ETL function map: `docs/native-bi-accounting-function-map.md`
- RAW validation: `docs/native-bi-accounting-raw-validation.md`
- Smoke test SQL: `docs/sql/accounting-deployment-smoke-test.sql`
- Validation queries: `docs/sql/accounting-mart-validation-queries.sql`
- Operations runbook: `docs/native-bi-accounting-operations-runbook.md`
