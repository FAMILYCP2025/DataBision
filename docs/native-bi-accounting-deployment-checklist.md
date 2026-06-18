# Native BI — Accounting Deployment Checklist

Sprint 14A — 2026-06-18

---

## Migration status (as of Sprint 14A)

### AppDbContext (SQLite/SQL Server — app DB)

| Migration | Status |
|---|---|
| 20260423134103_InitialCreate | ✅ Applied |
| 20260424204108_AddCompanyPlanLimits | ✅ Applied |
| 20260429040516_AddReportEmbedUrl | ✅ Applied |
| 20260515001006_AddUserPermissionUniqueIndex | ✅ Applied |
| 20260516021536_AddReportUpdatedAt | ✅ Applied |
| 20260617190816_AddCompanyAnalyticsId | ✅ Applied |
| 20260617200000_AddNativeBiAdvancedConfig | ✅ Applied |

**Verification command:**
```sh
dotnet ef migrations list \
  --project src\DataBision.Infrastructure \
  --startup-project src\DataBision.Api \
  --context AppDbContext
```

### StagingDbContext (PostgreSQL/Supabase — analytics DB)

| Migration | Status |
|---|---|
| 20260530221734_InitialStagingSchemaPostgres | ✅ Applied |
| 20260607020740_AddStgSchema | ✅ Applied |
| 20260607030000_AddMartSchema | ✅ Applied |
| 20260610183821_AddCfgSchema | ✅ Applied |
| 20260610202144_AddMartProcessSchemas | ✅ Applied |
| 20260610202613_AddOpsSchema | ✅ Applied |
| 20260615182500_FixMartProcessFunctions | ✅ Applied |
| 20260615182725_FixMartProcessColumnNames | ✅ Applied |
| 20260615210000_AddPurchaseFulfillmentSchemas | ✅ Applied |
| 20260615210100_FixStgRefreshAll | ✅ Applied |
| 20260615210200_FixMartProcessFunctions | ✅ Applied |
| 20260615210300_FixInventoryStockGroupBy | ✅ Applied |
| 20260616010000_DeduplicateOpsAlerts | ✅ Applied |
| 20260617120000_AddAccountingSchema | ✅ Applied |
| 20260617130000_AddAccountingMartFunctions | ✅ Applied |

**Verification command:**
```sh
dotnet ef migrations list \
  --project src\DataBision.Infrastructure \
  --startup-project src\DataBision.Api \
  --context StagingDbContext
```

---

## Deployment order (first-time)

### Phase 1 — App DB

```sh
dotnet ef database update \
  --project src\DataBision.Infrastructure \
  --startup-project src\DataBision.Api \
  --context AppDbContext
```

Verifies: companies, users, modules, reports, native_bi_*_configs tables.

### Phase 2 — Staging DB (Supabase / PostgreSQL)

⚠️ Only run against TST or a non-production instance unless confirmed safe.

```sh
dotnet ef database update \
  --project src\DataBision.Infrastructure \
  --startup-project src\DataBision.Api \
  --context StagingDbContext
```

Verifies: raw.*, stg.*, mart.*, cfg.*, ctl.*, ops.* schemas and all tables.

**Important:** Supabase with PgBouncer (port 6543 transaction pooler) does NOT support EF migrations. Use port 5432 (session mode) for migrations.

### Phase 3 — Extract accounting data from SAP B1

Run extractors explicitly (not included in `--object ALL`):

```sh
# Chart of accounts (full-refresh, ~seconds)
dotnet run --project src\DataBision.Extractor -- --object OACT --send

# Journal entries + lines (incremental by ReferenceDate)
dotnet run --project src\DataBision.Extractor -- --object OJDT --send
```

Verify ingestion in Supabase:
```sql
SELECT COUNT(*) FROM raw.sap_oact  WHERE company_id = 'YOUR_ANALYTICS_COMPANY_ID';
SELECT COUNT(*) FROM raw.sap_ojdt  WHERE company_id = 'YOUR_ANALYTICS_COMPANY_ID';
SELECT COUNT(*) FROM raw.sap_jdt1  WHERE company_id = 'YOUR_ANALYTICS_COMPANY_ID';
```

### Phase 4 — Configure account classification rules

In SuperAdmin → CompanyDetailPage → Native BI → Clasificación contable:
- Add rules for revenue/cogs/opex/assets/liabilities/equity accounts
- Or use the import-template endpoint to seed defaults
- Verify with contador/finanzas del cliente before final run

### Phase 5 — Run ETL refresh

```sql
-- Run full accounting ETL pipeline (in Supabase SQL editor or via API)
SELECT step_name, status, executed_at_utc, message
FROM mart.refresh_accounting_all('YOUR_ANALYTICS_COMPANY_ID');
```

Expected steps: stg_gl_accounts, stg_journal_entries, mart_gl_accounts,
mart_account_balances, mart_income_statement, mart_balance_sheet, mart_ebitda.

### Phase 6 — Validate MART data

Run `docs/sql/accounting-deployment-smoke-test.sql` against Supabase.

---

## Rollback procedure

### If staging schema is wrong

Staging migrations are idempotent (`CREATE TABLE IF NOT EXISTS`). To rollback a specific migration:

```sh
dotnet ef database update <previous-migration-id> \
  --project src\DataBision.Infrastructure \
  --startup-project src\DataBision.Api \
  --context StagingDbContext
```

This calls `Down()` for each reverted migration in reverse order.

### If MART data is wrong

MART tables are fully refreshed by `mart.refresh_accounting_all()`. Simply fix classification rules and rerun:

```sql
SELECT * FROM mart.refresh_accounting_all('YOUR_ANALYTICS_COMPANY_ID');
```

No raw or STG data is modified. RAW → STG → MART is idempotent.

### If raw data is wrong

Full-refresh OACT at any time:
```sh
dotnet run --project src\DataBision.Extractor -- --object OACT --send
```

OJDT is incremental — re-extract from a specific date:
```sh
dotnet run --project src\DataBision.Extractor -- --object OJDT --from 2024-01-01 --send
```

---

## Validation checklist (pre-demo)

- [ ] AppDbContext migrations all applied
- [ ] StagingDbContext migrations all applied
- [ ] `raw.sap_oact` has rows for this company
- [ ] `raw.sap_ojdt` has rows for this company
- [ ] `raw.sap_jdt1` has rows for this company
- [ ] `stg.gl_account` has rows (after STG refresh)
- [ ] `stg.journal_entry` + `stg.journal_entry_line` have rows
- [ ] `cfg.account_classification_rules` has rules for this company
- [ ] `mart.gl_accounts` has rows with `statement_line != 'unclassified'`
- [ ] Unclassified account % is acceptable (ideally < 5%)
- [ ] `mart.income_statement_summary` has rows
- [ ] `mart.balance_sheet_summary` has rows
- [ ] `mart.ebitda_summary` has rows
- [ ] Balance sheet imbalance is zero or near-zero
- [ ] Revenue is positive for at least one period
- [ ] API `/api/client/bi/finance/readiness` returns `ready` status
- [ ] Finance dashboard tabs load without FinancialDataPending fallback
- [ ] Validated with contador/finanzas of client company

---

## Risks

| Risk | Mitigation |
|---|---|
| Supabase PgBouncer blocks migrations | Use port 5432 (session mode) for `dotnet ef database update` |
| OACT has no UpdateDate (full-refresh) | Re-run OACT at any time safely — it's an upsert |
| OJDT watermark drift | Check `ctl.ingest_checkpoints` for last effective watermark |
| All accounts unclassified | Add classification rules before running MART refresh |
| Balance sheet imbalance | Usually means missing accounts in classification; add rules and re-refresh |
| Revenue negative | Check sign convention: credit > debit for income accounts |
| P&L empty | Verify raw.sap_jdt1 has debit/credit amounts != 0 |
