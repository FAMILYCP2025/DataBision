# Accounting Refresh Idempotency Validation

**Date:** 2026-06-20  
**Sprint:** 19E — Protect refresh_accounting_all so it never loses JDT1-injected accounts

## Problem

Before Sprint 19, `mart.refresh_accounting_all` called `mart.refresh_gl_accounts` which overwrote `mart.gl_accounts` from OACT only. Any accounts previously injected via `mart.refresh_gl_accounts_from_journal_lines` (JDT1 orphans) would be overwritten by the OACT-only refresh, losing their classification data on every subsequent pipeline run.

This made the pipeline non-idempotent: a second run of `refresh_accounting_all` would produce fewer classified accounts than the first run (which benefited from the manual post-injection).

## Fix Applied

`mart.refresh_accounting_all` now calls `mart.refresh_gl_accounts_from_journal_lines` immediately after `mart.refresh_gl_accounts`:

**Call Order (8 steps):**

| Step | Function | Input | Output |
|---|---|---|---|
| 1 | stg.refresh_gl_accounts | raw.sap_oact | stg.gl_account |
| 2 | stg.refresh_journal_entries | raw.sap_ojdt + raw.sap_jdt1 | stg.journal_entry + stg.journal_entry_line |
| 3 | mart.refresh_gl_accounts | stg.gl_account + cfg rules | mart.gl_accounts (OACT accounts) |
| 4 | mart.refresh_gl_accounts_from_journal_lines | stg.journal_entry_line + cfg rules | mart.gl_accounts (+ JDT1 orphans) |
| 5 | mart.refresh_account_balances | stg.journal_entry_line | mart.account_balances |
| 6 | mart.refresh_income_statement | mart.account_balances + mart.gl_accounts | mart.income_statement_summary |
| 7 | mart.refresh_balance_sheet | mart.account_balances + mart.gl_accounts | mart.balance_sheet_summary |
| 8 | mart.refresh_ebitda | mart.income_statement_summary | mart.ebitda_summary |

## Idempotency Proof

Two consecutive runs of `SELECT * FROM mart.refresh_accounting_all('company-dev-001')` executed on 2026-06-20:

| Run | Steps OK | Steps ERROR | mart.gl_accounts |
|---|---|---|---|
| PASS 1 | 8 | 0 | 55 |
| PASS 2 | 8 | 0 | 55 |

mart.gl_accounts count stable at 55 across both runs. No JDT1 accounts were lost.

## Additional Idempotency Guarantees

All functions use ON CONFLICT DO UPDATE:
- `stg.refresh_gl_accounts` → ON CONFLICT (company_id, code) DO UPDATE
- `stg.refresh_journal_entries` → ON CONFLICT (company_id, trans_id) / (company_id, trans_id, line_id) DO UPDATE
- `mart.refresh_gl_accounts` → ON CONFLICT (company_id, code) DO UPDATE
- `mart.refresh_gl_accounts_from_journal_lines` → ON CONFLICT (company_id, code) DO UPDATE
- `mart.refresh_account_balances` → ON CONFLICT (company_id, code, period_year, period_month) DO UPDATE
- `mart.refresh_income_statement` → ON CONFLICT (company_id, period_year, period_month, statement_line) DO UPDATE
- `mart.refresh_balance_sheet` → ON CONFLICT (company_id, snapshot_date, category, sub_category) DO UPDATE
- `mart.refresh_ebitda` → ON CONFLICT (company_id, period_year, period_month) DO UPDATE

The pipeline can be re-run safely at any time without data loss or duplication.

## Endpoint Validation (6/6 HTTP 200)

After both idempotency passes:

| Endpoint | HTTP Status |
|---|---|
| GET /api/client/bi/finance/readiness | 200 ✅ |
| GET /api/client/bi/finance/validations | 200 ✅ |
| GET /api/client/bi/finance/income-statement | 200 ✅ |
| GET /api/client/bi/finance/balance-sheet | 200 ✅ |
| GET /api/client/bi/finance/ebitda | 200 ✅ |
| GET /api/client/bi/finance/chart-of-accounts | 200 ✅ |
