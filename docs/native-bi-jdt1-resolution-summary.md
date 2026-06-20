# Sprint 17F — JDT1 Resolution Summary and MART Go/No-Go

**Date:** 2026-06-19  
**Sprint:** 17 — Resolver Extracción JDT1

## Problem Statement (from Sprint 16)

`GET JournalEntries?$expand=JournalEntryLines` returns HTTP 400 on SAP B1 SL v1000290 (CLTSTKSDEPOR). raw.sap_jdt1 was empty. Accounting MART could not be refreshed without JDT1 data.

## Resolution Path

| Sprint | Finding | Result |
|---|---|---|
| 17A | `GET JournalEntries(N)` (single-record) returns `JournalEntryLines` inline without `$expand` | **SOLUTION FOUND** |
| 17B | $metadata probe — JournalEntry EntityType block not extracted (hit LandedCosts NavProp instead) | Informational only |
| 17C | Loop all OJDT headers, GET each individually, collect lines — 74 lines extracted | **IMPLEMENTED** |
| 17D | HANA direct read design | Not needed — SL path viable |
| 17E | raw.sap_jdt1 validated: 91 rows, 37 TransIds, 0 orphans, balanced entries confirmed | **VALIDATED** |

## Current raw.sap_jdt1 State

| Metric | Value | Status |
|---|---|---|
| Total rows | 91 | ✅ |
| Unique TransIds | 37 | ✅ |
| Date range | 2026-01-22 → 2026-02-25 | ✅ |
| Orphan lines (no OJDT match) | 0 | ✅ |
| Missing AccountCode | 0 | ✅ |
| Zero-amount lines | 6 | ✅ normal |
| Balanced entries (new run) | 20 entries with debit = credit | ✅ |
| Stale LineId=0 rows (broken first run) | 17 entries | ⚠️ Sprint 18 cleanup |

## MART Go/No-Go Decision

**DECISION: GO**

Raw.sap_jdt1 is populated with 91 rows across 37 journal entries. 20 recently extracted entries are fully correct (multi-line, balanced). The 17 stale entries (LineId=0 partial data) do not block demo functionality — the accounting MART joins on (TransId, LineId) and the balanced entries are sufficient for financial dashboard rendering.

Sprint 18 should issue a full re-extraction to clean up the stale entries before production.

## Recommended Sprint 18 Checklist

1. **Full OJDT re-extraction** — reset checkpoint and extract all historical entries with corrected LineId mapping
2. **Apply account classification** — run `refresh_accounting_all` or equivalent to populate STG/MART layers
3. **Validate STG/MART** — confirm stg.journal_entry_lines, mart.account_balances, mart.income_statement have correct balances
4. **Validate financial dashboard** — Income Statement, Balance Sheet, Cash Flow panels render correctly in demo tenant
5. **Performance test** — measure individual-GET loop at scale (expected 200+ entries in production); implement `SemaphoreSlim(4)` concurrent batching if >30s

## Files Modified in Sprint 17

| File | Change |
|---|---|
| `src/DataBision.Extractor/ServiceLayer/IServiceLayerClient.cs` | Added `GetObjectAsync`, `GetRawStringAsync` |
| `src/DataBision.Extractor/ServiceLayer/ServiceLayerClient.cs` | Implemented both methods |
| `src/DataBision.Extractor/Extraction/Jobs/OjdtExtractorJob.cs` | Added `ProbeIndividualEntryAsync`, `ProbeMetadataJournalEntryAsync`, `ExtractLinesViaIndividualGetAsync`, updated `RunAsync` probe sequence |
| `src/DataBision.Extractor/Mapping/SapToIngestMapper.cs` | Added `GetIntAny` helper; fixed `MapJdt1Row` to use `Line_ID` for LineId |

## Documentation Created in Sprint 17

| File | Content |
|---|---|
| `docs/native-bi-jdt1-service-layer-probe.md` | Sprint 17A probe results and field name discovery |
| `docs/native-bi-service-layer-journalentry-metadata.md` | Sprint 17B $metadata findings |
| `docs/native-bi-jdt1-service-layer-implementation.md` | Sprint 17C implementation details and bug fix |
| `docs/native-bi-jdt1-resolution-summary.md` | This document |

## Technical Decision: JDT1 Extraction Pattern

**Pattern:** Individual-entity GET loop  
**Endpoint:** `GET /b1s/v1/JournalEntries({JdtNum})` per OJDT header  
**Key field:** `JournalEntryLines[*].Line_ID` (not `LineId`)  
**Account field:** `JournalEntryLines[*].AccountCode`  
**Correlation:** `JdtNum` injected into each line node; matched to OJDT headers in `SendAsync`  
**Applicable to SL:** v1000290 and later (may differ on earlier versions)
