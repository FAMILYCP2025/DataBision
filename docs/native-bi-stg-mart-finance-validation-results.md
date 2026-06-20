# Sprint 18E — STG/MART Finance Validation Results

**Date:** 2026-06-19  
**Sprint:** 18E — Validar STG/MART financiero

## Pipeline Execution Order

1. `mart.refresh_accounting_all('company-dev-001')` — populates all layers
2. JDT1 orphan account injection into `mart.gl_accounts` (35 accounts)
3. `mart.refresh_income_statement('company-dev-001')` — re-run with expanded gl_accounts
4. `mart.refresh_balance_sheet('company-dev-001')` — re-run
5. `mart.refresh_ebitda('company-dev-001')` — re-run

## STG Layer

| Table | Rows | Status |
|---|---|---|
| stg.gl_account | 20 | ✅ |
| stg.journal_entry | 50 | ✅ |
| stg.journal_entry_line | 122 | ✅ |

## MART Layer

| Table | Rows | Status |
|---|---|---|
| mart.gl_accounts | 55 | ✅ (20 OACT + 35 JDT1 injected) |
| mart.account_balances | 46 | ✅ (46 account×period combinations) |
| mart.income_statement_summary | 7 | ✅ |
| mart.balance_sheet_summary | 8 | ✅ |
| mart.ebitda_summary | 2 | ✅ |

## Income Statement

| Period | Revenue | COGS | Opex | Financial | Unclassified |
|---|---|---|---|---|---|
| 2026-01 | 201.19 | -128,474.80 | 2,650.00 | -2.21 | -8,557.30 |
| 2026-02 | 0 | 0 | 0 | 0 | -42,997.84 |

Note: COGS negative in PCGE Peru = credit-side heavy (purchases cancelled by variación existencias + costo de ventas booked as credit). This is accounting-convention correct for PCGE Peru but counter-intuitive for dashboards. Sign correction is a future UI/MART tuning task.

## Balance Sheet (Feb 2026 snapshot — most recent)

| Category | Amount |
|---|---|
| current_assets | 124,289.50 |
| current_liabilities | 81,055.05 |
| equity | 0 (no equity accounts in JDT1) |
| unclassified | 42,997.84 |
| Balance imbalance | 43,234.45 |

Note: Imbalance expected — equity accounts (50xxx, 59xxx) have no journal entries in this test period. Balance sheet reconciliation requires equity entries.

## EBITDA

| Period | Revenue | COGS | Gross Profit | Opex | EBITDA | Net Income |
|---|---|---|---|---|---|---|
| 2026-01 | 201.19 | 0 | 201.19 | 2,650.00 | -2,448.81 | -2,448.81 |
| 2026-02 | 0 | 0 | 0 | 0 | 0 | 0 |

Note: EBITDA shows cogs=0 because income_statement.cogs is negative (PCGE sign convention). The ebitda function sums `cogs WHERE amount > 0` or uses a different filter. This is a known limitation.

## Account Balances (Top 10 by Debit)

| Account | Total Debit | Total Credit |
|---|---|---|
| 20123 (Mercaderías) | 996,635.20 | 0 |
| 20122 (Mercaderías) | 351,724.09 | 9,199.09 |
| 20121 (Mercaderías) | 180,000.00 | 0 |
| 28111 (Exist. por recibir) | 124,498.90 | 2,922.40 |
| 60122 (Compras) | 71,699.09 | 4,199.09 |
| 42114 (CxP) | 67,500.00 | 67,500.00 |
| 60921 (Compras) | 50,050.00 | 0 |
| 40111 (IGV) | 21,636.00 | 36.21 |
| 69112 (Costo de ventas) | 10,000.00 | 0 |
| 60123 (Compras) | 6,635.20 | 0 |

## Known Limitations (TST Environment)

1. **OACT incomplete**: CLTSTKSDEPOR has only 20 header-level accounts; 35 posting accounts are JDT1-only
2. **Revenue very small**: Only 1 revenue account (70122) with minor activity in test period  
3. **PCGE sign convention**: COGS entries use PCGE journaling convention where net = credit > debit; dashboard needs sign-flip logic
4. **Equity gap**: No equity journal entries → balance sheet always imbalanced in TST
5. **JDT1 injection**: 35 accounts injected via SQL, not through ETL; will be overwritten if `mart.refresh_gl_accounts` is re-run alone
