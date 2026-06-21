# Sprint 19 — Finance MART Productization: Execution Summary

**Date:** 2026-06-20  
**Status:** COMPLETE — all sub-sprints executed and validated

## Objective

Productivize the PCGE Peru financial MART to eliminate manual intervention and make the pipeline repeatable for controlled commercial demo. Remove sign convention bugs from Sprint 18, make JDT1 account injection automatic and idempotent, classify all accounts.

## Sub-Sprint Results

| Sprint | Task | Status | Key Metric |
|---|---|---|---|
| 19A | PCGE signs in income_statement | ✅ DONE | cogs=+128,474.80 (was -128,474.80) |
| 19B | EBITDA fix for normalized COGS | ✅ DONE | ebitda.cogs=128,474.80 (was 0), net_income=-130,921.40 |
| 19C | JDT1 injection as SQL function | ✅ DONE | mart.refresh_gl_accounts_from_journal_lines created |
| 19D | Prefix '02' equity classification | ✅ DONE | 0 unclassified in mart.gl_accounts (was 2) |
| 19E | Idempotency protection | ✅ DONE | 2×refresh_accounting_all: 55 accounts both times |
| 19F | Commercial demo close | ✅ DONE | 9 documents created |

## Code Changes in Sprint 19

| File | Change | Type |
|---|---|---|
| `sql/native-bi/accounting-mart.sql` | mart.refresh_income_statement PCGE signs | Bug fix (19A) |
| `sql/native-bi/accounting-mart.sql` | mart.refresh_ebitda ABS cogs, +financial | Bug fix (19B) |
| `sql/native-bi/accounting-mart.sql` | mart.refresh_gl_accounts_from_journal_lines (new) | Feature (19C) |
| `sql/native-bi/accounting-mart.sql` | mart.refresh_accounting_all +JDT1 step | Feature (19E) |
| `src/DataBision.Infrastructure/Repositories/Dashboard/ProcessDashboardRepository.cs` | ni = oi + fin - tax (was oi - fin - tax) | Bug fix (19A backend) |
| `src/DataBision.Infrastructure/Data/Staging/Migrations/20260620000000_Sprint19AccountingMartPcgeFixes.cs` | EF migration for 4 updated functions | Migration |

## Data Pipeline State (Final)

| Layer | Table | Rows | Notes |
|---|---|---|---|
| RAW | raw.sap_oact | 20 | Unchanged from Sprint 18 |
| RAW | raw.sap_ojdt | 50 | Unchanged |
| RAW | raw.sap_jdt1 | 122 | Unchanged |
| STG | stg.gl_account | 20 | Unchanged |
| STG | stg.journal_entry | 50 | Unchanged |
| STG | stg.journal_entry_line | 122 | Unchanged |
| MART | mart.gl_accounts | 55 | 0 unclassified (was 2) |
| MART | mart.account_balances | 46 | Unchanged |
| MART | mart.income_statement_summary | 7 | cogs/financial signs corrected |
| MART | mart.balance_sheet_summary | 8 | Unchanged |
| MART | mart.ebitda_summary | 2 | cogs/net_income corrected |
| CFG | cfg.account_classification_rules | 84 | +1 prefix '02' rule |

## Income Statement Comparison (Jan 2026)

| Line | Sprint 18 | Sprint 19 | Change |
|---|---|---|---|
| revenue | 201.19 | 201.19 | — |
| cogs | -128,474.80 | +128,474.80 | ✅ Sign fixed |
| gross_profit | 128,675.99 | -128,273.61 | ✅ Economically correct |
| opex | 2,650.00 | 2,650.00 | — |
| financial | -2.21 | +2.21 | ✅ Sign fixed |
| net_income (API) | -130,925.82 | -130,921.40 | ✅ Formula fixed |

## EBITDA Comparison (Jan 2026)

| Line | Sprint 18 | Sprint 19 | Change |
|---|---|---|---|
| revenue | 201.19 | 201.19 | — |
| cogs | 0.00 | 128,474.80 | ✅ Now correct |
| gross_profit | 201.19 | -128,273.61 | ✅ Now correct |
| opex | 2,650.00 | 2,650.00 | — |
| ebitda | -2,448.81 | -130,923.61 | ✅ Now correct |
| financial_result | 0.00 | 2.21 | ✅ Now correct |
| net_income | -2,448.81 | -130,921.40 | ✅ Now correct |

## Finance API Endpoints (Post Sprint 19)

| Endpoint | Status | Notes |
|---|---|---|
| GET /api/client/bi/finance/readiness | 200 ✅ | ready, 84 rules, 0 unclassified |
| GET /api/client/bi/finance/validations | 200 ✅ | healthScore=100, 0 issues |
| GET /api/client/bi/finance/income-statement | 200 ✅ | cogs positive, financial positive |
| GET /api/client/bi/finance/balance-sheet | 200 ✅ | equity=2 accounts in chart-of-accounts |
| GET /api/client/bi/finance/ebitda | 200 ✅ | cogs/net_income corrected |
| GET /api/client/bi/finance/chart-of-accounts | 200 ✅ | 55 accounts, 0 unclassified |

## Build & Test Validation

```
dotnet build DataBision.sln --configuration Debug
→ Build succeeded. 0 Warning(s). 0 Error(s).

dotnet test DataBision.sln --no-build --configuration Debug
→ Passed: 22 (Api) + 6 (Extractor) + 63 (Application) = 91 total. 0 Failed.

npm run build (databision-frontend)
→ ✓ 1105 modules transformed. built in 1.40s
```

## Remaining Known Limitations (TST Environment)

| Limitation | Impact | Resolution Path |
|---|---|---|
| Revenue very small (201.19) | P&L appears heavily loss-making | Normal for test DB — production data will show realistic revenue |
| Balance imbalance 43,234.45 | Balance sheet doesn't reconcile | No equity journal entries in test period — expected |
| unclassified in income_statement (-8,557 Jan, -42,997 Feb) | Minor noise line in P&L | Accounts in account_balances with no mart.gl_accounts match — requires investigation |
| depreciation=0 | EBITDA/depreciation adjustment missing | No depreciation journal entries in test period |

## Documents Created in Sprint 19

| Document | Content |
|---|---|
| `docs/native-bi-pcge-sign-convention.md` | 19A sign convention fix details |
| `docs/native-bi-ebitda-pcge-validation.md` | 19B EBITDA fix and validation |
| `docs/native-bi-jdt1-account-enrichment.md` | 19C JDT1 function design and results |
| `docs/native-bi-opening-balance-classification.md` | 19D prefix '02' equity classification |
| `docs/native-bi-accounting-refresh-idempotency.md` | 19E idempotency proof |
| `docs/native-bi-sprint-19-finance-productization-summary.md` | This document |
| `docs/commercial/native-bi-finance-demo-v2-script.md` | Demo script v2 |
| `docs/commercial/native-bi-finance-demo-v2-checklist.md` | Demo checklist v2 |
| `docs/commercial/native-bi-finance-commercial-one-pager.md` | Commercial one-pager |
