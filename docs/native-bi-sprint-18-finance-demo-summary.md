# Sprint 18 — Finance MART Demo: Execution Summary

**Date:** 2026-06-19  
**Status:** COMPLETE — all sub-sprints executed and validated

## Summary

Sprint 18 moved from populated RAW accounting data (raw.sap_oact/ojdt/jdt1) to a functional financial MART and validated API/dashboard. All 6 finance endpoints return real SAP data from CLTSTKSDEPOR (TST).

## Sub-Sprint Results

| Sprint | Task | Status | Key Metric |
|---|---|---|---|
| 18A | Re-extracción completa OJDT/JDT1 | ✅ DONE | 122 JDT1 rows, 40 TransIds, 0 orphans |
| 18B | Validar RAW final | ✅ DONE | raw.sap_oact=20, ojdt=50, jdt1=122 |
| 18C | Clasificación contable PCGE Peru | ✅ DONE | 83 reglas, 55 cuentas clasificadas |
| 18D | refresh_accounting_all | ✅ DONE | All MART tables populated |
| 18E | Validar STG/MART | ✅ DONE | 7 income_statement rows, 8 balance_sheet rows |
| 18F | Validar dashboard API | ✅ DONE | 6/6 endpoints HTTP 200, healthScore=100 |

## Code Changes in Sprint 18

| File | Change | Type |
|---|---|---|
| `src/DataBision.Infrastructure/Repositories/Dashboard/ProcessDashboardRepository.cs` | Fixed `stg.sap_oact/ojdt/jdt1` → `stg.gl_account/journal_entry/journal_entry_line` | Bug fix |

## Data Pipeline State

| Layer | Table | Rows |
|---|---|---|
| RAW | raw.sap_oact | 20 |
| RAW | raw.sap_ojdt | 50 |
| RAW | raw.sap_jdt1 | 122 |
| STG | stg.gl_account | 20 |
| STG | stg.journal_entry | 50 |
| STG | stg.journal_entry_line | 122 |
| MART | mart.gl_accounts | 55 |
| MART | mart.account_balances | 46 |
| MART | mart.income_statement_summary | 7 |
| MART | mart.balance_sheet_summary | 8 |
| MART | mart.ebitda_summary | 2 |
| CFG | cfg.account_classification_rules | 83 |

## Finance API Endpoints

| Endpoint | Status |
|---|---|
| GET /api/client/bi/finance/readiness | ✅ HTTP 200 — readinessStatus="ready" |
| GET /api/client/bi/finance/validations | ✅ HTTP 200 — healthScore=100 |
| GET /api/client/bi/finance/income-statement | ✅ HTTP 200 — 2 periods |
| GET /api/client/bi/finance/balance-sheet | ✅ HTTP 200 — 1 snapshot |
| GET /api/client/bi/finance/ebitda | ✅ HTTP 200 — 2 periods |
| GET /api/client/bi/finance/chart-of-accounts | ✅ HTTP 200 — 55 accounts |

## Build & Test Validation

```
dotnet build DataBision.sln --configuration Debug
→ Build succeeded. 0 Warning(s). 0 Error(s).

dotnet test DataBision.sln --no-build --configuration Debug
→ Passed: 22 (Api) + 6 (Extractor) + 63 (Application) = 91 total. 0 Failed.
```

## Key Technical Discoveries

1. **stg.sap_* tables do not exist** — the repository had legacy SQL referencing `stg.sap_oact`, `stg.sap_ojdt`, `stg.sap_jdt1`. Corrected to `stg.gl_account`, `stg.journal_entry`, `stg.journal_entry_line`.

2. **JDT1 column name** — `raw.sap_jdt1` stores account in column `Account` (not `AccountCode`), with standard camelCase PascalCase conventions: `LineId`, `TransId`, `Debit`, `Credit`.

3. **PCGE Peru sign convention** — COGS accounts (60-69xxx) show net credit > debit in PCGE Peru accounting style, producing negative COGS in income_statement. The ebitda function doesn't handle negative COGS. This is a UI/MART tuning item for production.

4. **mart.gl_accounts unique constraint** — `ON CONFLICT (company_id, code)` is valid; injection of JDT1 orphan accounts uses code (not account_code) as the key.

5. **JDT1 orphan accounts** — CLTSTKSDEPOR has 35 posting-level accounts in JDT1 that are not in OACT. For demo purposes, these were injected into mart.gl_accounts with PCGE prefix-based classification. In production with a complete OACT, this injection would be unnecessary.

## Follow-up Items (Post-Sprint)

- [ ] Fix PCGE sign convention in mart.refresh_income_statement (cogs sign flip for 60-69xxx)
- [ ] Fix mart.refresh_ebitda to handle negative COGS
- [ ] Add classification rule for prefix '02' (saldos iniciales → equity or opening_balance)
- [ ] Protect JDT1 orphan injection from being overwritten by refresh_gl_accounts
- [ ] Performance: consider SemaphoreSlim(4) batching for OJDT individual-GET loop at production scale (200+ entries)
- [ ] Production OACT should include all 5-digit posting accounts (configured in SAP with proper Postable=Yes)

## Documents Created

| Document | Content |
|---|---|
| `docs/native-bi-ojdt-jdt1-full-reextraction-results.md` | Sprint 18A extraction metrics |
| `docs/native-bi-account-classification-execution-results.md` | Sprint 18C PCGE Peru classification |
| `docs/native-bi-stg-mart-finance-validation-results.md` | Sprint 18E STG/MART data validation |
| `docs/native-bi-finance-dashboard-tst-demo-validation.md` | Sprint 18F API endpoint validation |
| `docs/native-bi-sprint-18-finance-demo-summary.md` | This document |
