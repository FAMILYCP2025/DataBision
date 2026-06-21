# Opening Balance Classification — Prefix '02' as Equity

**Date:** 2026-06-20  
**Sprint:** 19D — Add classification rule for prefix '02' (opening balances → equity)

## Problem

CLTSTKSDEPOR uses accounts with prefix `02` to record opening balances (saldos iniciales) when migrating data into SAP B1:

| Account | Name | Use |
|---|---|---|
| 02 | SALDO INICIALES | Opening balance header |
| 02111 | TRASLADO DE SALDOS INICIALES CIERRE ANUAL SAP | Year-end opening balance transfer |

These accounts appear in `raw.sap_oact` (they are OACT accounts) and `mart.gl_accounts`, but had no classification rule in `cfg.account_classification_rules`. They defaulted to the SAP account_type fallback → `'at_Other'` → `'unclassified'`.

Result: 2 unclassified accounts in `mart.gl_accounts`, appearing in income_statement as unclassified (counter-intuitive in P&L view).

## Fix Applied

Added a prefix classification rule to `cfg.account_classification_rules`:

```sql
INSERT INTO cfg.account_classification_rules
    (company_id, account_code, format_code, statement_line)
VALUES
    ('company-dev-001', NULL, '02', 'equity');
```

- `account_code = NULL` — prefix rule, not exact-match
- `format_code = '02'` — matches accounts whose format_code starts with '02'
- `statement_line = 'equity'` — opening balance accounts are equity by nature

## Rationale

Opening balance accounts in PCGE Peru represent transfers of prior-year equity positions. They are not income/expense items and do not belong in the P&L. Classifying them as `equity` ensures:
1. They are excluded from `mart.income_statement_summary` (not in the income statement WHERE clause)
2. They appear in `mart.balance_sheet_summary` under the `equity` category
3. They stop showing as `unclassified` in the chart of accounts

## Validation Results

| Metric | Before 19D | After 19D |
|---|---|---|
| cfg.account_classification_rules count | 83 | 84 |
| mart.gl_accounts unclassified | 2 | 0 |
| mart.gl_accounts equity | 0 | 2 |
| income_statement unclassified (Jan 2026) | -8,557.30 | -8,557.30 |

**Note:** The `unclassified` amount in income_statement_summary (-8,557.30 in Jan, -42,997.84 in Feb) comes from accounts in `mart.account_balances` that have no matching row in `mart.gl_accounts`. This is a separate issue from the `02` accounts (which ARE in `mart.gl_accounts` and are now classified as equity). The unclassified in income_statement represents accounts without full classification coverage in the current cfg rules — documented as a known TST limitation.

## Balance Sheet Impact

The `02` accounts have no JDT1 journal entry activity in the test period, so they appear in `mart.gl_accounts` with `equity` classification but do NOT contribute to `mart.balance_sheet_summary` (which aggregates from `mart.account_balances` — requires actual journal movement). This is expected for opening-balance-only accounts in a limited test dataset.
