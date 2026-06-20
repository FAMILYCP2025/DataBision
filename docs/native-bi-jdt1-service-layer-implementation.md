# Sprint 17C — JDT1 Extraction via Individual-GET Implementation

**Date:** 2026-06-19  
**Status:** IMPLEMENTED — raw.sap_jdt1 populated

## Architecture

For each OJDT header (JdtNum), issue one HTTP GET to `/b1s/v1/JournalEntries({JdtNum})` and extract the embedded `JournalEntryLines` array. Inject `JdtNum` into each line node for header correlation.

```
OJDT headers (20) → loop → GetObjectAsync("JournalEntries(N)") × 20 → collect JournalEntryLines
→ inject lineObj["JdtNum"] = jdtNum → SendAsync → api/ingest/sap-b1/journal-entry-lines
```

## Implementation

**`IServiceLayerClient.GetObjectAsync(string entityWithKey, CancellationToken)`**  
Single-entity GET, returns `JsonObject?`, returns null on HTTP error.

**`OjdtExtractorJob.ProbeIndividualEntryAsync(int jdtNum)`**  
Sprint 17A: probes first entry, confirms `JournalEntryLines` property exists, logs all 83 field names and all JournalEntryLine field names (including `Line_ID` key discovery).

**`OjdtExtractorJob.ExtractLinesViaIndividualGetAsync(JsonArray allEntries, string linesPropertyName)`**  
Sprint 17C: loops all 20 headers, collects all line arrays, injects JdtNum.

**`SapToIngestMapper.MapJdt1Row`**  
Key fix: `LineId = GetIntAny(line, "Line_ID", "LineId", "LineNum")` — SAP SL uses `Line_ID` (with underscore).  
Previous broken version used `GetInt(line, "LineId")` → returned 0 for all lines → upsert collision on LineId=0.

## Execution Results (2026-06-19)

| Metric | Value |
|---|---|
| OJDT headers extracted | 20 |
| JDT1 lines extracted | 74 |
| JDT1 lines sent: inserted | 71 |
| JDT1 lines sent: updated | 3 |
| JDT1 lines sent: skipped | 0 |
| Total raw.sap_jdt1 rows | 91 |
| Unique TransIds in DB | 37 |
| Orphan lines | 0 |
| Missing Account | 0 |
| Balanced journal entries (new run) | 20 (debit = credit, net = 0) |

## Bug Fixed in This Sprint

**LineId=0 collapse bug:** Initial run (broken) sent 71 lines but `Line_ID` field was not found → all lines mapped to `LineId=0`. Upsert key is `(company_id, TransId, LineId)` → only 1 row per TransId survived (20 rows total instead of 71). Fixed by adding `GetIntAny` helper and using `Line_ID` as primary lookup.

## Performance Profile

- 20 sequential HTTP GETs to SAP SL
- ~16s total extraction time (800ms per GET avg)
- Production recommendation: introduce concurrent batching with `SemaphoreSlim(4)` for 20+ header sets

## Known Residual State

17 stale entries (TransIds 8-33) from the initial broken run remain in `raw.sap_jdt1` with `LineId=0` and partial line data. Sprint 18 should clear these with a full re-extraction (`--checkpoint-reset` or explicit date-from override).
