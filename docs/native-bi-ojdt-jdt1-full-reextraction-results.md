# Sprint 18A — OJDT/JDT1 Full Re-extraction Results

**Date:** 2026-06-19  
**Sprint:** 18A — Re-extracción completa OJDT/JDT1

## Pre-conditions (after Sprint 17)

| Metric | Before 18A |
|---|---|
| raw.sap_jdt1 rows | 91 |
| Stale LineId=0 rows | 37 (17 truly broken + 20 legitimate first-lines) |
| OJDT checkpoint watermark | 2026-02-25 |
| JDT1 checkpoint total_rows | 139 (cumulative) |

## Cleanup Operations

### 1. Stale LineId=0 DELETE

```sql
DELETE FROM raw.sap_jdt1
WHERE company_id = 'company-dev-001' AND "LineId" = 0
```
**Result:** 37 rows deleted. Remaining: 54 rows (all with LineId > 0).

### 2. Checkpoint Reset

```sql
DELETE FROM ctl.ingest_checkpoint
WHERE company_id = 'company-dev-001' AND sap_object IN ('OJDT', 'JDT1')
```
**Result:** 2 checkpoints deleted (OJDT: watermark=2026-02-25; JDT1: no watermark).

## OACT Re-extraction

Command: `dotnet run --project src/DataBision.Extractor -- --company-id company-dev-001 --object OACT --send`

| Metric | Result |
|---|---|
| Accounts extracted | 20 |
| Inserted | 0 |
| Updated | 0 |
| Skipped (already current) | 20 |
| Duration | 11s |

Note: CLTSTKSDEPOR chart of accounts remains at 20 header-level accounts. No new accounts added to SAP since Sprint 16.

## OJDT Full Re-extraction

Command: `dotnet run --project src/DataBision.Extractor -- --company-id company-dev-001 --object OJDT --send`

| Metric | Result |
|---|---|
| Checkpoint state | No prior run (full extraction) |
| OJDT headers extracted | 20 |
| JDT1 lines via individual GET | 68 |
| OJDT headers sent: inserted | 0 |
| OJDT headers sent: skipped | 20 |
| JDT1 lines sent: inserted | 68 |
| JDT1 lines sent: updated | 0 |
| Duration | 32s |

## Final RAW State After 18A

| Table | Rows | Status |
|---|---|---|
| raw.sap_oact | 20 | ✅ |
| raw.sap_ojdt | 50 | ✅ |
| raw.sap_jdt1 | 122 | ✅ (54 pre-existing + 68 new) |

### JDT1 LineId Distribution

| LineId | Count |
|---|---|
| 0 | 20 |
| 1 | 40 |
| 2 | 26 |
| 3 | 21 |
| 4 | 12 |
| 5 | 2 |
| 6 | 1 |
| **Total** | **122** |

### JDT1 Summary

| Metric | Value |
|---|---|
| Unique TransIds | 40 |
| Date range | 2026-01-01 → 2026-02-25 |
| Orphan lines | 0 |
| Total debit | 1,891,028.54 |
| Total credit | 1,839,501.72 |

## Extraction Pattern Confirmed

The OJDT extractor falls back to the Sprint 17 individual-GET pattern:
1. GET JournalEntries (list, no `$expand`) — 20 headers in 1 page
2. Probe: GET JournalEntries(39) — confirms `JournalEntryLines` embedded
3. Loop all 20 headers: GET JournalEntries(N) individually
4. Collect `JournalEntryLines` arrays → inject JdtNum for correlation
5. Map lines with `Line_ID` field → correct LineId values

Total GET requests: 21 (1 probe + 20 individual)
