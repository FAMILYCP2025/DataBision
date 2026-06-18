# Native BI Accounting — ETL Function Map

Sprint 15A — 2026-06-18

Source of truth: `sql/native-bi/accounting-mart.sql` and migration `20260617130000_AddAccountingMartFunctions`.

---

## Extractor Commands (CLI)

| Object | Command | Notes |
|---|---|---|
| OACT | `--object OACT` | Full-refresh — no watermark |
| OJDT + JDT1 | `--object OJDT` | JDT1 lines embedded via `$expand=JournalEntryLines` — **no `--object JDT1`** |

> **Critical:** There is no `--object JDT1` command. JDT1 is extracted automatically inside the OJDT job (`OjdtExtractorJob.cs`). Running `--object OJDT` sends headers to `api/ingest/sap-b1/journal-entries` and lines to `api/ingest/sap-b1/journal-entry-lines` in the same job run.

---

## PostgreSQL ETL Functions

### Orchestrator

| Function | Returns | Description |
|---|---|---|
| `mart.refresh_accounting_all(p_company_id TEXT)` | `TABLE(step_name, status, executed_at_utc, message)` | Runs all 7 steps in sequence; returns per-step log |

Usage:
```sql
SELECT * FROM mart.refresh_accounting_all('your-analytics-company-id');
```

### Individual Steps (called by orchestrator in order)

| Step | Function | Source → Destination |
|---|---|---|
| 1 | `stg.refresh_gl_accounts(p_company_id TEXT)` | `raw.sap_oact` → `stg.gl_account` |
| 2 | `stg.refresh_journal_entries(p_company_id TEXT)` | `raw.sap_ojdt` + `raw.sap_jdt1` → `stg.journal_entry` + `stg.journal_entry_line` |
| 3 | `mart.refresh_gl_accounts(p_company_id TEXT)` | `stg.gl_account` + `cfg.account_classification_rules` → `mart.gl_accounts` |
| 4 | `mart.refresh_account_balances(p_company_id TEXT)` | `stg.journal_entry_line` → `mart.account_balances` |
| 5 | `mart.refresh_income_statement(p_company_id TEXT)` | `mart.account_balances` + `mart.gl_accounts` → `mart.income_statement_summary` |
| 6 | `mart.refresh_balance_sheet(p_company_id TEXT)` | `mart.account_balances` + `mart.gl_accounts` → `mart.balance_sheet_summary` |
| 7 | `mart.refresh_ebitda(p_company_id TEXT)` | `mart.income_statement_summary` → `mart.ebitda_summary` |

All functions return `VOID`. Use the orchestrator (`refresh_accounting_all`) for logging.

---

## Deprecated / Incorrect Names (do NOT use)

These names do not exist in the database. Any documentation or runbook referencing them was incorrect and has been fixed in Sprint 15A.

| Incorrect name | Correct name |
|---|---|
| `stg.load_oact(company_id)` | `stg.refresh_gl_accounts(p_company_id)` |
| `stg.load_ojdt(company_id)` | `stg.refresh_journal_entries(p_company_id)` |
| `stg.load_jdt1(company_id)` | *(embedded in `stg.refresh_journal_entries`)* |
| `mart.build_gl_accounts(company_id)` | `mart.refresh_gl_accounts(p_company_id)` |
| `mart.build_income_statement(company_id)` | `mart.refresh_income_statement(p_company_id)` |
| `mart.build_balance_sheet(company_id)` | `mart.refresh_balance_sheet(p_company_id)` |
| `mart.build_ebitda(company_id)` | `mart.refresh_ebitda(p_company_id)` |
| `--object JDT1` (CLI) | *(not a valid CLI object — use `--object OJDT`)* |

---

## RAW Tables (written by Ingest API)

| Table | Ingest Endpoint | Key |
|---|---|---|
| `raw.sap_oact` | `POST api/ingest/sap-b1/chart-of-accounts` | `(company_id, "Code")` |
| `raw.sap_ojdt` | `POST api/ingest/sap-b1/journal-entries` | `(company_id, "TransId")` |
| `raw.sap_jdt1` | `POST api/ingest/sap-b1/journal-entry-lines` | `(company_id, "TransId", "LineId")` |

## STG Tables (written by ETL steps 1–2)

| Table | Written by | Key |
|---|---|---|
| `stg.gl_account` | `stg.refresh_gl_accounts` | `(company_id, code)` |
| `stg.journal_entry` | `stg.refresh_journal_entries` | `(company_id, trans_id)` |
| `stg.journal_entry_line` | `stg.refresh_journal_entries` | `(company_id, trans_id, line_id)` |

## MART Tables (written by ETL steps 3–7)

| Table | Written by | Key |
|---|---|---|
| `mart.gl_accounts` | `mart.refresh_gl_accounts` | `(company_id, code)` |
| `mart.account_balances` | `mart.refresh_account_balances` | `(company_id, code, period_year, period_month)` |
| `mart.income_statement_summary` | `mart.refresh_income_statement` | `(company_id, period_year, period_month, statement_line)` |
| `mart.balance_sheet_summary` | `mart.refresh_balance_sheet` | `(company_id, snapshot_date, category, sub_category)` |
| `mart.ebitda_summary` | `mart.refresh_ebitda` | `(company_id, period_year, period_month)` |

---

## Verify Functions Exist (Supabase)

```sql
SELECT routine_schema, routine_name
FROM information_schema.routines
WHERE routine_schema IN ('stg', 'mart')
  AND routine_name IN (
      'refresh_gl_accounts',
      'refresh_journal_entries',
      'refresh_account_balances',
      'refresh_income_statement',
      'refresh_balance_sheet',
      'refresh_ebitda',
      'refresh_accounting_all'
  )
ORDER BY routine_schema, routine_name;
```

Expected: 8 rows (2 in `stg`, 6 in `mart`).

---

## References

- SQL source: `sql/native-bi/accounting-mart.sql`
- Migration: `src/DataBision.Infrastructure/Data/Staging/Migrations/20260617130000_AddAccountingMartFunctions.cs`
- Extractor: `src/DataBision.Extractor/Extraction/Jobs/OjdtExtractorJob.cs`
- Extractor: `src/DataBision.Extractor/Extraction/Jobs/OactExtractorJob.cs`
- Runbook: `docs/native-bi-accounting-operations-runbook.md`
- Deployment checklist: `docs/native-bi-accounting-deployment-checklist.md`
