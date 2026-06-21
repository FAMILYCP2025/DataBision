# Native BI — Income Statement Unclassified Analysis (Sprint 20A)

**Date:** 2026-06-20  
**Sprint:** 20A  
**Status:** RESOLVED

---

## Problem

`mart.income_statement_summary` showed `unclassified` rows with significant amounts:
- Jan 2026: `unclassified = -8,557.30`
- Feb 2026: `unclassified = -42,997.84`

These appeared despite all 55 accounts in `mart.gl_accounts` having a valid, non-unclassified `statement_line`.

---

## Root Cause

**Stale rows from the INSERT ON CONFLICT pattern.**

The prior implementation of `mart.refresh_income_statement` used:
```sql
INSERT INTO mart.income_statement_summary (...)
SELECT ...
ON CONFLICT (company_id, period_year, period_month, statement_line) DO UPDATE SET ...;
```

This pattern only updates rows where the `(company_id, period_year, period_month, statement_line)` key already exists. It **never deletes** rows.

**Sequence that caused stale data:**
1. First extraction (pre-Sprint 19): some accounts were unclassified → `unclassified` rows inserted into IS
2. Sprint 19: those accounts reclassified (e.g., as `current_assets`, `current_liabilities`) via `refresh_gl_accounts_from_journal_lines`
3. Sprint 19 `refresh_income_statement` ran: reclassified accounts are now B/S accounts → they no longer qualify for the IS WHERE clause (which only includes IS categories) → no new data for `unclassified` → ON CONFLICT doesn't fire → old `unclassified` rows **remain forever**

**Confirmed by diagnostic:**
```sql
SELECT ab.code, ga.statement_line
FROM mart.account_balances ab
LEFT JOIN mart.gl_accounts ga ON ga.company_id = ab.company_id AND ga.code = ab.code
WHERE ab.company_id = 'company-dev-001'
  AND COALESCE(ga.statement_line, 'unclassified') = 'unclassified'
-- Result: 0 rows — all accounts are classified
```
All accounts in `account_balances` were classified — confirming the IS unclassified rows were stale, not reflecting real data.

---

## Fix

Added `DELETE FROM mart.income_statement_summary WHERE company_id = p_company_id;` at the start of `mart.refresh_income_statement`:

```sql
CREATE OR REPLACE FUNCTION mart.refresh_income_statement(p_company_id TEXT)
RETURNS VOID LANGUAGE plpgsql AS $func$
BEGIN
    DELETE FROM mart.income_statement_summary WHERE company_id = p_company_id;  -- ← NEW
    INSERT INTO mart.income_statement_summary (...)
    SELECT ...
    ON CONFLICT ... DO UPDATE ...;
END;
```

Same fix applied to `mart.refresh_balance_sheet` and `mart.refresh_ebitda` for consistency.

---

## Validation Results

After fix + `refresh_accounting_all` (2 passes):

**income_statement_summary:**
| period_year | period_month | statement_line | amount |
|---|---|---|---|
| 2026 | 1 | cogs | 128,474.80 |
| 2026 | 1 | financial | 2.21 |
| 2026 | 1 | opex | 2,650.00 |
| 2026 | 1 | revenue | 201.19 |
| 2026 | 2 | cogs | 0.00 |

- ✅ 0 unclassified rows
- ✅ Jan figures unchanged (cogs=128,474.80, net_income=-130,921.40)
- ✅ Feb cogs=0.00 is correct (Feb cogs accounts 60122/60123/61122/61123 net to zero — offsetting entries)

---

## Feb Period Explanation

Feb 2026 shows only `cogs=0.00` in the income statement. This is correct:
- Feb journal entries for cogs accounts (60122, 60123, 61122, 61123) have perfectly offsetting debits and credits: `ABS(SUM(debit - credit)) = 0`
- No revenue, opex, or financial accounts have Feb activity
- Account 67611 (depreciation) has Feb activity but is classified as `depreciation` which is excluded from IS filter — tracked separately

---

## Migration

`20260620000001_Sprint20StaleDataDeleteInsertFix.cs` — deployed to Supabase staging.
