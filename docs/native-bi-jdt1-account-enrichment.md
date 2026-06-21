# JDT1 Account Enrichment — mart.refresh_gl_accounts_from_journal_lines

**Date:** 2026-06-20  
**Sprint:** 19C — Convert JDT1 orphan injection into controlled SQL function

## Problem

CLTSTKSDEPOR's SAP B1 instance has only 20 header-level accounts in the OACT table. The actual posting accounts (5-digit) that appear in JDT1 journal lines are NOT in OACT — they are "orphan" accounts from the system's perspective.

`mart.refresh_gl_accounts` only processes accounts in `stg.gl_account` (from OACT). So the 35 JDT1 orphan accounts never appeared in `mart.gl_accounts` from the standard ETL.

In Sprint 18, these 35 accounts were manually injected via a one-time SQL script. The problem: every time `mart.refresh_accounting_all` was re-run, `mart.refresh_gl_accounts` would overwrite `mart.gl_accounts` from OACT only — losing all 35 injected accounts. The manual injection was not idempotent and would need to be re-run after each pipeline refresh.

## Fix Applied

New function: `mart.refresh_gl_accounts_from_journal_lines(p_company_id TEXT)`

**Logic:**
1. Reads all distinct `account` values from `stg.journal_entry_line` for the company
2. Filters to accounts NOT in `stg.gl_account` (true orphans — not in OACT)
3. Classifies each account via `cfg.account_classification_rules` with longest-prefix matching:
   - First: exact `account_code` match
   - Then: `format_code` prefix match, ordered by `LENGTH(format_code) DESC` (5→4→3→2→1)
   - Fallback: `'unclassified'`
4. UPSERTs into `mart.gl_accounts` with `name = 'JDT1:' || account`
5. ON CONFLICT (company_id, code): updates `statement_line` and `refreshed_at`

**Idempotency:** Running the function multiple times produces identical results — injected accounts are re-classified (not duplicated or lost).

## Integration into refresh_accounting_all

`mart.refresh_accounting_all` now calls this function at step 4, immediately after `mart.refresh_gl_accounts`:

```
Step 1: stg.refresh_gl_accounts
Step 2: stg.refresh_journal_entries
Step 3: mart.refresh_gl_accounts                          ← OACT accounts
Step 4: mart.refresh_gl_accounts_from_journal_lines  NEW ← JDT1 orphan accounts
Step 5: mart.refresh_account_balances
Step 6: mart.refresh_income_statement
Step 7: mart.refresh_balance_sheet
Step 8: mart.refresh_ebitda
```

This guarantees JDT1 accounts are always present in `mart.gl_accounts` before balances, income statement, and balance sheet are computed.

## Validation Results

| Run | mart.gl_accounts count | All steps OK? |
|---|---|---|
| refresh_accounting_all PASS 1 | 55 | ✅ |
| refresh_accounting_all PASS 2 | 55 | ✅ |

JDT1 accounts preserved across both runs — idempotency confirmed.

## Classification Applied (35 accounts)

| Prefix | Examples | statement_line |
|---|---|---|
| 70 | 70122 | revenue |
| 60,61,69 | 60122,60123,60911-6,60921,61122,61123,69111,69112,69115 | cogs |
| 10,12,18,20,28 | 10711,12130,18211,18213,20111,20121-3,28111 | current_assets |
| 30 | 30101 | non_current_assets |
| 40,42,48 | 40111,42114,42120-2,48911 | current_liabilities |
| 65,95,97 | 65610,65622,95305,97762 | opex |
| 67 | 67611 | depreciation |
| 77 | 77611 | financial |

## Production Note

In a production SAP B1 database with a complete OACT (all posting accounts configured with Postable=Yes), this function would typically inject 0 accounts — the OACT would already cover all posting accounts. The function is a safety net for incomplete OACT configurations common in new or test SAP instances.
