# Native BI Accounting — RAW Layer Validation

Sprint 15B — 2026-06-18

---

## Extractor Configuration Status

| Parameter | Status | Notes |
|---|---|---|
| SAP Service Layer URL | ✅ Configured | TST instance |
| SAP CompanyDB | ✅ Configured | CLTSTKSDEPOR (test company) |
| SAP credentials | ✅ Configured | See `appsettings.Development.json` (not reproduced here) |
| DataBision API BaseUrl | ✅ Configured | Local dev API |
| Extractor.CompanyId | ⚠️ Placeholder | Set to `company-dev-001` — must be updated to match actual AnalyticsCompanyId |
| Staging ConnectionString | ✅ Configured | Supabase (port 6543 — read-only queries OK; migrations need port 5432) |

---

## Blocker: CompanyId Must Match AnalyticsCompanyId

The `Extractor.CompanyId` setting in `appsettings.Development.json` is used as `company_id` when writing to staging tables (`raw.sap_oact`, `raw.sap_ojdt`, `raw.sap_jdt1`). This value **must match** the `Company.AnalyticsCompanyId` set for the KSDEPOR company in the app database — otherwise MART queries will not find the data.

**Resolution (Sprint 15D finding):**
The `Extractor.CompanyId = "company-dev-001"` is already correct — it matches the `AnalyticsCompanyId` configured in `NativeBi:CompanySlugMap` for both `ksdepor` and `demo` slugs. No update needed.

---

## Staged Execution Procedure

Once the CompanyId blocker is resolved, execute in this order:

### Stage 1 — Dry-run (safe, no SAP connection)

```bash
cd src/DataBision.Extractor

dotnet run -- --object OACT --dry-run
dotnet run -- --object OJDT --dry-run
```

Expected: exits immediately with `DryRun` status, no network call, no data written.

### Stage 2 — Read-only (connects to SAP, does NOT send to Ingest API)

```bash
dotnet run -- --object OACT --company-id <AnalyticsCompanyId>
dotnet run -- --object OJDT --company-id <AnalyticsCompanyId>
```

Inspect the logged output for:
- Row count (`OACT: N entries in Xms`)
- Field selection used (full or minimal fallback)
- Any `400 Bad Request` from Service Layer (indicates field name issues)
- Shape of first few rows

**Do NOT add `--send` until Stage 2 output is verified.**

### Stage 3 — Send (writes to RAW tables in Supabase)

```bash
dotnet run -- --object OACT --company-id <AnalyticsCompanyId> --send
dotnet run -- --object OJDT --company-id <AnalyticsCompanyId> --send
```

Expected log lines (OACT):
```
OACT: N accounts in Xms (select=FULL)
OACT headers sent: inserted=N, updated=0, skipped=0
```

Expected log lines (OJDT):
```
OJDT: N entries in Xms (select=FULL, filter=FULL)
OJDT headers sent: inserted=N, updated=0, skipped=0
JDT1 lines sent: inserted=N, updated=0, skipped=0
```

---

## RAW Layer Validation Queries

Run in Supabase SQL Editor after Stage 3. Replace `'<AnalyticsCompanyId>'` with the actual value.

### Row counts

```sql
SELECT 'raw.sap_oact' AS layer, COUNT(*) AS rows FROM "raw"."sap_oact" WHERE company_id = '<AnalyticsCompanyId>'
UNION ALL
SELECT 'raw.sap_ojdt', COUNT(*) FROM "raw"."sap_ojdt" WHERE company_id = '<AnalyticsCompanyId>'
UNION ALL
SELECT 'raw.sap_jdt1', COUNT(*) FROM "raw"."sap_jdt1" WHERE company_id = '<AnalyticsCompanyId>'
ORDER BY layer;
```

Expected ranges for a live SAP B1 company:
- `raw.sap_oact`: 50–2000 rows (chart of accounts)
- `raw.sap_ojdt`: 100–50000+ rows (journal entry headers — depends on history)
- `raw.sap_jdt1`: 2×–5× the OJDT count (2–5 lines per entry average)

### Account shape validation (OACT)

```sql
SELECT "Code", "Name", "AccountType", "Postable", "FormatCode"
FROM "raw"."sap_oact"
WHERE company_id = '<AnalyticsCompanyId>'
LIMIT 20;
```

Check:
- `Code` is not null (required for classification)
- `AccountType` values look like SAP enum strings (e.g. `act_Accounts`, `act_Sales`, `act_Expense`)
- `Postable` values: `tYES` / `tNO` / `Y` / `N` (converted by STG function)
- `FormatCode` present (used for format-code prefix classification)

### Journal entry shape validation (OJDT)

```sql
SELECT "TransId", "JdtNum", "RefDate", "Memo", "TransType"
FROM "raw"."sap_ojdt"
WHERE company_id = '<AnalyticsCompanyId>'
ORDER BY "RefDate" DESC
LIMIT 10;
```

Check:
- `RefDate` is not null (used as watermark for incremental extraction)
- `TransId` and `JdtNum` are populated

### Journal line shape validation (JDT1)

```sql
SELECT "TransId", "LineId", "Account", "Debit", "Credit"
FROM "raw"."sap_jdt1"
WHERE company_id = '<AnalyticsCompanyId>'
  AND ("Debit" > 0 OR "Credit" > 0)
ORDER BY "TransId" DESC
LIMIT 20;
```

Check:
- `Account` references a code that exists in `raw.sap_oact`
- `Debit` + `Credit` amounts are non-zero for most lines
- Lines with both `Debit = 0` and `Credit = 0` should be rare

### Orphan accounts (JDT1 lines with accounts not in OACT)

```sql
SELECT DISTINCT jl."Account", COUNT(*) AS line_count
FROM "raw"."sap_jdt1" jl
WHERE jl.company_id = '<AnalyticsCompanyId>'
  AND NOT EXISTS (
      SELECT 1 FROM "raw"."sap_oact" oa
      WHERE oa.company_id = jl.company_id AND oa."Code" = jl."Account"
  )
GROUP BY jl."Account"
ORDER BY line_count DESC;
```

Expected: 0 rows. If rows appear, OACT may be incomplete or there are control accounts not in the chart of accounts.

### Date range coverage

```sql
SELECT
    MIN("RefDate") AS earliest_entry,
    MAX("RefDate") AS latest_entry,
    COUNT(DISTINCT DATE_TRUNC('month', "RefDate"::DATE)) AS months_covered
FROM "raw"."sap_ojdt"
WHERE company_id = '<AnalyticsCompanyId>';
```

For a meaningful P&L / EBITDA, expect at least 3 months of history. 12+ months is recommended.

### Watermark state

```sql
SELECT object_name, last_watermark, updated_at
FROM ctl.ingest_checkpoint
WHERE company_id = '<AnalyticsCompanyId>'
ORDER BY object_name;
```

After the first OJDT extraction, a checkpoint row should exist for `OJDT` with the max `RefDate` from the extracted entries. Subsequent runs will use this as the `$filter=ReferenceDate ge '...'` value.

---

## Validation Checklist

- [ ] Stage 1 dry-run completed: exits with status DryRun, no errors
- [ ] Stage 2 read-only: row counts logged (OACT: N, OJDT: N, JDT1: N)
- [ ] Stage 2 read-only: no `400 Bad Request` from Service Layer
- [ ] Stage 2 read-only: account shape looks correct (AccountType, FormatCode present)
- [ ] Stage 3 send: OACT inserted successfully
- [ ] Stage 3 send: OJDT headers + JDT1 lines inserted successfully
- [ ] RAW row counts > 0 for all 3 tables
- [ ] `raw.sap_oact`: Code and AccountType are populated
- [ ] `raw.sap_jdt1`: Debit/Credit amounts are non-zero for most lines
- [ ] Orphan account query: 0 rows (or acceptable explanation)
- [ ] Date range: at least 3 months covered
- [ ] `ctl.ingest_checkpoint` has entry for OJDT after extraction

---

## Current Status (Sprint 15B)

| Check | Status |
|---|---|
| TST SAP configured (CLTSTKSDEPOR) | ✅ Yes |
| Extractor.CompanyId matches AnalyticsCompanyId | ✅ Both are `company-dev-001` (verified Sprint 15D) |
| Stage 1 dry-run executed | ⏳ Ready to execute |
| Stage 2 read-only executed | ⏳ Ready to execute |
| Stage 3 send executed | ⏳ Ready to execute |
| RAW layer validated | ⏳ Pending extraction |

**Next step:** Classification rules created in Sprint 15D (`sql/native-bi/accounting-classification-demo-ksdepor.sql`). Execute stages 1→2→3 against CLTSTKSDEPOR (TST SAP instance), then apply classification rules and run MART refresh.

---

## References

- Extractor config: `src/DataBision.Extractor/appsettings.Development.json`
- OJDT extractor: `src/DataBision.Extractor/Extraction/Jobs/OjdtExtractorJob.cs`
- OACT extractor: `src/DataBision.Extractor/Extraction/Jobs/OactExtractorJob.cs`
- Function map: `docs/native-bi-accounting-function-map.md`
- Deployment checklist: `docs/native-bi-accounting-deployment-checklist.md`
