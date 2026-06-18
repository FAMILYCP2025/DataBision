# Native BI — SAP B1 Accounting Extractor (Sprint 13A)

Generated: 2026-06-17

## Overview

Sprint 13A adds extractors for three SAP B1 accounting objects: **OACT** (Chart of Accounts), **OJDT** (Journal Entry headers), and **JDT1** (Journal Entry lines embedded in OJDT). These feed the Finance accounting sub-reports in Native BI (currently showing `FinancialDataPending` placeholders pending Sprint 13B ETL).

---

## Objects implemented

| SAP Object | SL Entity | Strategy | Watermark | Notes |
|---|---|---|---|---|
| OACT | `ChartOfAccounts` | Full-refresh | None | No `UpdateDate` in SL entity |
| OJDT | `JournalEntries` | Incremental | `ReferenceDate` | `UpdateDate` not exposed |
| JDT1 | `JournalEntryLines` | Embedded | (parent OJDT) | Via `$expand=JournalEntryLines` on JournalEntries |

---

## CLI usage

```sh
# Dry-run (no SAP connection, no data sent)
dotnet run -- --object OACT --dry-run
dotnet run -- --object OJDT --dry-run

# Extract only (no send)
dotnet run -- --object OACT
dotnet run -- --object OJDT

# Extract and send to Ingest API
dotnet run -- --object OACT --send
dotnet run -- --object OJDT --send
```

> **CRITICAL**: OACT and OJDT are **NOT included in `ALL`** and **NOT included in scheduled runs** (`--run-once`, `--schedule`). They must always be invoked explicitly with `--object OACT` or `--object OJDT`. This prevents accidental full accounting extraction during normal pipeline cycles.

---

## Ingest endpoints

| Object | Endpoint |
|---|---|
| OACT | `POST api/ingest/sap-b1/chart-of-accounts` |
| OJDT headers | `POST api/ingest/sap-b1/journal-entries` |
| JDT1 lines | `POST api/ingest/sap-b1/journal-entry-lines` |

JDT1 lines are extracted by OjdtExtractorJob and sent to the lines endpoint in the same job run immediately after the headers batch.

---

## Database schema

### RAW tables (verbatim from SL)

| Table | Key | Description |
|---|---|---|
| `raw.sap_oact` | `(company_id, "Code")` | Chart of accounts replica |
| `raw.sap_ojdt` | `(company_id, "TransId")` | Journal entry headers |
| `raw.sap_jdt1` | `(company_id, "TransId", "LineId")` | Journal entry lines |

### STG tables (normalized)

| Table | Key | Description |
|---|---|---|
| `stg.gl_account` | `(company_id, code)` | Normalized chart of accounts |
| `stg.journal_entry` | `(company_id, trans_id)` | Normalized journal entry headers |
| `stg.journal_entry_line` | `(company_id, trans_id, line_id)` | Normalized journal entry lines |

### CFG tables

| Table | Description |
|---|---|
| `cfg.account_classification_rules` | Manual override: maps account codes / format-code prefixes to P&L / BS statement lines |

### MART placeholders (empty — Sprint 13B)

| Table | Description |
|---|---|
| `mart.gl_accounts` | Chart of accounts with `statement_line` classification |
| `mart.account_balances` | Monthly debit/credit sums per account |
| `mart.income_statement_summary` | Monthly P&L aggregation by statement line |
| `mart.balance_sheet_summary` | Point-in-time balance sheet aggregation |

---

## OACT details

- **SL entity**: `ChartOfAccounts`
- **No UpdateDate** → always full-refresh. Every extraction replaces all rows for the company.
- **$select** (full): `Code,Name,FatherNum,Level,GroupMask,AccountType,Postable,Frozen,ValidFor,CashAccount,ControlAccount,Currency,FormatCode,ExternalCode`
- **Minimal fallback**: `Code,Name,FatherNum,Level,AccountType,Postable`
- **Field name validation required** before first live run against SL v1000290. Suspected differences: `Postable` vs `ExternallyManaged`, `Level` vs `Levels`.

## OJDT / JDT1 details

- **SL entity**: `JournalEntries` with `$expand=JournalEntryLines`
- **Incremental by `ReferenceDate`** — `UpdateDate` is not available in the SL `JournalEntries` entity.
- **$select** (full): `JdtNum,ReferenceDate,DueDate,TaxDate,Memo,TransactionCode,BaseRef,Ref1,CreatedBy`
- **$expand**: `JournalEntryLines` (all line fields — no nested $select to avoid v1000290 compatibility issues)
- **Watermark**: max `ReferenceDate` from fetched entries, stored in `ctl.ingest_checkpoint`.
- **Two sends per run**: headers → `journal-entries`, lines → `journal-entry-lines`.
- **SL key note**: `JdtNum` is the OData entity key. The internal `TransId` is not separately exposed in SL v1000290; both `SapOjdtRow.TransId` and `SapOjdtRow.JdtNum` map to the SL `JdtNum` field.

---

## Field name validation checklist

Before the first `--send` run against a production/test SL instance, validate these fields:

**OACT (ChartOfAccounts)**:
- [ ] `Code` — account code
- [ ] `Name` — account name
- [ ] `FatherNum` — parent account
- [ ] `Level` (SL may return `Levels`)
- [ ] `GroupMask`
- [ ] `AccountType` — expect enum like `act_Accounts`, `act_Other`, etc.
- [ ] `Postable`, `Frozen`, `ValidFor`, `CashAccount`, `ControlAccount` — tYES/tNO booleans

**OJDT (JournalEntries)**:
- [ ] `JdtNum` — OData key, user-visible journal number
- [ ] `ReferenceDate` — used as watermark
- [ ] `TransactionCode` — transaction type code
- [ ] `Ref1` — first user reference field
- [ ] `CreatedBy` — creator user code

**JDT1 (JournalEntryLines via $expand)**:
- [ ] `LineId` — line number (0-based or 1-based)
- [ ] `AccountCode` — GL account code
- [ ] `Debit`, `Credit` — local currency
- [ ] `FCDebit`, `FCCredit` — foreign currency
- [ ] `SystemDebit`, `SystemCredit` — system currency
- [ ] `ShortName` — BP short name
- [ ] `ContraAccount` — contra account
- [ ] `LineMemo` — line description
- [ ] `ReferenceDate1` — line-level reference date
- [ ] `CostingCode`, `CostingCode2`…`CostingCode5` — cost center dimensions
- [ ] `ProjectCode`

---

## Sprint 13B readiness

Once OACT and OJDT data flows into raw/stg tables, Sprint 13B can implement:

1. `stg.refresh_gl_accounts(p_company_id)` — upsert from raw.sap_oact into stg.gl_account
2. `stg.refresh_journal_entries(p_company_id)` — upsert from raw.sap_ojdt/sap_jdt1 into stg
3. `mart.refresh_gl_accounts(p_company_id)` — join stg.gl_account + cfg.account_classification_rules → mart.gl_accounts
4. `mart.refresh_account_balances(p_company_id)` — aggregate stg.journal_entry_line by account + period
5. `mart.refresh_income_statement(p_company_id)` — aggregate account_balances by statement_line → mart.income_statement_summary
6. `mart.refresh_balance_sheet(p_company_id)` — produce balance sheet snapshot → mart.balance_sheet_summary

After Sprint 13B, the Finance accounting tabs (`resultados`, `balance`, `ebitda`, `cuentas`) can be switched from `FinancialDataPending` to real data endpoints.

---

## Security rules

- **NO production SAP**: use DEV/TST SL instances only.
- **NO real extraction without `--send`**: default `--object OACT` without `--send` is safe (read-only).
- **NO ALL**: these objects are excluded from `--run-once` and `--schedule` by design.
- **Field validation first**: run `--object OACT --dry-run` then `--object OACT` (no send) to inspect row shapes before any `--send`.
