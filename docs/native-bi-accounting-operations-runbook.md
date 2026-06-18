# Native BI Accounting — Operations Runbook

Sprint 14F — 2026-06-18

---

## Overview

This runbook covers the operational lifecycle of the Native BI accounting module: extraction from SAP B1, transformation into staging, MART refresh, and ongoing maintenance.

---

## Execution Order (first setup for a new client)

```
1. Configure AnalyticsCompanyId          → SuperAdmin → Company → Analytics ID
2. Configure classification rules        → SuperAdmin → Company → Native BI → Clasificación Contable
3. Extract OACT (chart of accounts)      → dotnet run [extractor] -- --object OACT
4. Extract OJDT (journal headers)        → dotnet run [extractor] -- --object OJDT
5. Extract JDT1 (journal lines)          → dotnet run [extractor] -- --object JDT1
6. Run ETL                               → SELECT mart.refresh_accounting_all('company-id');
7. Validate                              → GET /api/client/bi/finance/readiness  (expect readinessStatus = "ready")
8. Validate                              → GET /api/client/bi/finance/validations (expect healthScore >= 80)
9. Demo                                  → FinanceDashboardPage → Resumen + Resultados + Balance + EBITDA
```

---

## Commands

### Extract SAP accounting objects

```bash
# From DataBision.Extractor project directory
# OACT — chart of accounts (extract once, re-extract when accounts change)
dotnet run --project src/DataBision.Extractor -- --object OACT --company-id <slug> --send

# OJDT — journal entry headers (incremental from last watermark)
dotnet run --project src/DataBision.Extractor -- --object OJDT --company-id <slug> --send

# JDT1 — journal entry lines (incremental from last watermark)
dotnet run --project src/DataBision.Extractor -- --object JDT1 --company-id <slug> --send
```

### Run MART refresh (Supabase)

```sql
-- Run in Supabase SQL Editor or psql
-- Replace 'company-analytics-id' with the company's AnalyticsCompanyId
SELECT * FROM mart.refresh_accounting_all('company-analytics-id');
```

This function runs in sequence:
1. `stg.load_oact(company_id)` — loads OACT from raw to stg
2. `stg.load_ojdt(company_id)` — loads OJDT from raw to stg
3. `stg.load_jdt1(company_id)` — loads JDT1 from raw to stg
4. `mart.build_gl_accounts(company_id)` — creates gl_accounts from stg + classification rules
5. `mart.build_income_statement(company_id)` — P&L summary
6. `mart.build_balance_sheet(company_id)` — balance sheet snapshots
7. `mart.build_ebitda(company_id)` — EBITDA summary

### Validate readiness (API)

```bash
# GET request with valid JWT for the company
curl -H "Authorization: Bearer <token>" \
     "https://<slug>.databision.app/api/client/bi/finance/readiness"
```

Expected response when ready:
```json
{ "data": { "readinessStatus": "ready", "blockingReasons": [], "warnings": [] } }
```

### Validate health score

```bash
curl -H "Authorization: Bearer <token>" \
     "https://<slug>.databision.app/api/client/bi/finance/validations"
```

Expected: `healthScore >= 80`, `criticalIssues = 0`

---

## ETL Step Log (Supabase)

If the refresh function exists, check for ETL step logs:

```sql
-- Check if ETL log table exists and recent runs
SELECT * FROM ctl.etl_step_log
WHERE company_id = 'your-company-id'
ORDER BY started_at DESC
LIMIT 20;
```

---

## Error Handling

### Error: "OACT not extracted"
**Symptom:** `rawOactCount = 0` in readiness endpoint  
**Resolution:**  
1. Check SAP connection in extractor config
2. Run `dotnet run -- --object OACT --company-id <slug> --send`
3. Verify rows in `raw.sap_oact WHERE company_id = 'slug'`

### Error: "mart.income_statement_summary empty"
**Symptom:** `martIncomeStatement = 0` in readiness endpoint  
**Resolution:**  
1. Verify OJDT + JDT1 are extracted (not 0)
2. Check classification rules exist: `SELECT COUNT(*) FROM cfg.account_classification_rules WHERE company_id = 'slug'`
3. Re-run `SELECT mart.refresh_accounting_all('slug')`
4. Check for SQL errors in Supabase logs

### Error: "Balance imbalance"
**Symptom:** `validations.balanceImbalance > 0.01`  
**Resolution:**  
1. Open `Finance → Plan de Cuentas` — look for accounts not classified
2. Review accounts classified as both asset and liability types
3. Add/fix classification rules
4. Re-run `mart.refresh_accounting_all()`
5. Check that equity accounts are properly classified as `equity`

### Error: "Unclassified accounts"
**Symptom:** `unclassifiedAccounts > 0`  
**Resolution:**  
1. SuperAdmin → Company → Native BI → Clasificación Contable
2. Use "Sugerencias desde OACT" to get auto-suggestions
3. Validate suggestions with client accountant
4. Apply approved rules
5. Re-run `mart.refresh_accounting_all()`

### Error: "Negative revenue"
**Symptom:** validation issue `negative_revenue`  
**Resolution:**  
In many Latin American SAP instances, revenue accounts have debit-credit polarity inverted.  
The ETL sign convention: revenue amounts are stored as positive in MART.  
If negative: check if the `mart.build_income_statement` function applies the correct sign flip for the company's accounting standard.

### Error: "Orphan journal lines"
**Symptom:** `orphanJournalLines > 0`  
**Resolution:**  
1. Usually indicates incomplete extraction (OJDT extracted but JDT1 not, or vice versa)
2. Re-run both extractions to ensure consistency:
   ```
   dotnet run -- --object OJDT --send
   dotnet run -- --object JDT1 --send
   ```
3. Re-run MART refresh

---

## Log Locations

| Log | Location |
|---|---|
| Extractor logs | Application stdout / Serilog sink |
| API logs | Application stdout / Serilog sink |
| Supabase MART refresh | Supabase Dashboard → Logs → Postgres |
| ETL step log (if enabled) | `ctl.etl_step_log` in Supabase |
| Ingest checkpoint | `ctl.ingest_checkpoint` in Supabase |

---

## Scheduled Maintenance

**Daily (recommended):**
- Run OJDT + JDT1 extractors (incremental)
- Run `mart.refresh_accounting_all()`

**Weekly:**
- Run OACT extractor (accounts change infrequently)
- Review readiness + validations endpoints
- Check for new unclassified accounts

**Monthly:**
- Review classification rules with client accountant
- Verify Balance cuadra (isBalanced = true)
- Review EBITDA trend with client CFO

---

## Rollback Procedures

### Rollback MART data (keep raw/stg intact)

```sql
-- Remove MART data for a company (re-runs from stg on next refresh)
DELETE FROM mart.income_statement_summary WHERE company_id = 'slug';
DELETE FROM mart.balance_sheet_summary    WHERE company_id = 'slug';
DELETE FROM mart.ebitda_summary           WHERE company_id = 'slug';
DELETE FROM mart.gl_accounts             WHERE company_id = 'slug';
DELETE FROM mart.account_balances        WHERE company_id = 'slug';
-- Then re-run: SELECT mart.refresh_accounting_all('slug');
```

### Rollback classification rules

```sql
-- Remove all rules for a company
DELETE FROM cfg.account_classification_rules WHERE company_id = 'slug';
-- Then re-classify and re-run ETL
```

### Rollback raw + stg data

```sql
-- WARNING: removes all extracted data — requires full re-extraction
DELETE FROM raw.sap_oact WHERE company_id = 'slug';
DELETE FROM raw.sap_ojdt WHERE company_id = 'slug';
DELETE FROM raw.sap_jdt1 WHERE company_id = 'slug';
DELETE FROM stg.sap_oact WHERE company_id = 'slug';
DELETE FROM stg.sap_ojdt WHERE company_id = 'slug';
DELETE FROM stg.sap_jdt1 WHERE company_id = 'slug';
-- Reset watermark
DELETE FROM ctl.ingest_checkpoint WHERE company_id = 'slug' AND object_name IN ('OACT', 'OJDT', 'JDT1');
```

---

## Contact / Escalation

- Supabase issues → check Supabase Dashboard → Logs
- ETL function errors → check Supabase → Logs → Postgres (look for function errors)
- Extractor connectivity → check SAP Service Layer credentials in extractor config
