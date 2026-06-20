# Native BI Accounting — TST Extraction Summary

**Sprint:** 16 (16A–16F)  
**Date:** 2026-06-18  
**Environment:** SAP B1 TST — CompanyDB: CLTSTKSDEPOR  
**SL Version:** 1000290  
**CompanyId:** company-dev-001  
**Engineer:** DataBision extraction sprint

---

## Sprint Execution Log

| Sprint | Task | Status | Notes |
|---|---|---|---|
| 16A | API local en puerto 5103 | PASSED | `ASPNETCORE_ENVIRONMENT=Development` required explicitly |
| 16B | OACT dry-run | PASSED | CompanyDB=CLTSTKSDEPOR confirmed |
| 16B | OACT read-only | PASSED | 20 accounts via no-select fallback |
| 16C | OACT --send | PASSED | 20 rows inserted in raw.sap_oact |
| 16C | raw.sap_oact validation | PASSED | 20 rows, 0 missing Code |
| 16D | OJDT dry-run | PASSED | CompanyDB=CLTSTKSDEPOR confirmed |
| 16D | OJDT read-only | PASSED | 20 headers via MinimalSelect fallback |
| 16D | OJDT --send | PASSED | 20 rows inserted in raw.sap_ojdt |
| 16E | raw.sap_ojdt validation | PASSED | 20 rows, 0 missing JdtNum, RefDate 2026-01-01 to 2026-01-22 |
| 16E | raw.sap_jdt1 validation | BLOCKED | 0 rows — SL v1000290 limitation |
| 16F | Documentation | COMPLETE | This document + per-object result docs |

---

## Raw Data State — 2026-06-18

| Table | Rows | Status |
|---|---|---|
| raw.sap_oact | 20 | Populated — PCGE accounts from CLTSTKSDEPOR |
| raw.sap_ojdt | 20 | Populated — headers only, RefDate 2026-01 |
| raw.sap_jdt1 | 0 | Empty — SL v1000290 cannot expose line-level data |

---

## Service Layer v1000290 Confirmed Limitations

The following standard OData operations are rejected by this SL installation for accounting entities:

### ChartOfAccounts
- `$select=GroupMask` → 400
- `$select=Postable` → 400
- `$select=FatherNum`, `$select=Father` → 400
- `$select=Level` → 400
- **Workaround:** No `$select` (returns all SL-available fields) — 20 accounts extracted

### JournalEntries
- `$expand=JournalEntryLines` → 400 (invalid navigation property)
- `$expand=Lines` → 400 (invalid navigation property)
- `$select=BaseRef` → 400
- `$select=DueDate`, `TaxDate`, `TransactionCode`, `Ref1`, `CreatedBy` → 400
- **Workaround:** `$select=JdtNum,ReferenceDate,Memo` — 20 headers extracted

### JournalEntryLines
- `GET /b1s/v1/JournalEntryLines` → 400 "Unrecognized resource path"
- **No workaround available at current SL version**

---

## Go / No-Go Decision — Finance MART

| Component | Go/No-Go | Reason |
|---|---|---|
| Chart of Accounts browser | **GO** | raw.sap_oact has 20 accounts with FormatCode |
| Account classification | **GO** | Can apply PCGE classification SQL against raw.sap_oact |
| Balance Sheet | **NO-GO** | Requires JDT1 debit/credit amounts |
| Income Statement | **NO-GO** | Requires JDT1 debit/credit amounts |
| EBITDA | **NO-GO** | Requires JDT1 debit/credit amounts |
| `mart.refresh_accounting_all` | **DO NOT EXECUTE** | Would produce empty/wrong results — no JDT1 data |

---

## Extractor Code Changes (not committed — per security rules)

Files modified during Sprint 16:

| File | Change |
|---|---|
| `OactExtractorJob.cs` | 3-level `$select` fallback (FullSelect → MinimalSelect → no select) |
| `OjdtExtractorJob.cs` | 5-level fallback chain; `TryFetchLinesTopLevelAsync` probe; `SendAsync` updated to accept `topLevelLines` parameter |
| `SapToIngestMapper.cs` | `MapOactRow` — `FatherNum=null`, `Levels=GetStrAny("Levels","Level")` |
| `ServiceLayerPaginator.cs` | Fixed trailing `&` when `baseQuery` is empty |

These changes are staged only — no git operations performed per sprint security rules. Commit when approved.

---

## Priority Next Steps

### Immediate (before next sprint)

1. **Test single-record GET:** `GET /b1s/v1/JournalEntries(1)` — single-key access may return embedded lines even when list `$expand` fails. If lines are present, implement per-entry loop extractor in `OjdtExtractorJob`.

2. **Apply account classification SQL:** Execute `sql/native-bi/accounting-classification-demo-ksdepor.sql` against Supabase to classify the 20 OACT rows. This unblocks the Chart of Accounts view even without JDT1.

3. **Commit extractor changes** once approved (4 modified files).

### Medium term

4. **Investigate HANA direct access** — if customer allows ODBC to HANA port 30015/30040, JDT1 can be queried directly bypassing SL.

5. **SL version upgrade** — coordinate with SAP partner to upgrade SL to 10.0 FP40+. This should restore `$expand=JournalEntryLines` support.

6. **`mart.refresh_accounting_all`** — execute only after JDT1 is populated (either via single-record loop or HANA direct).

---

## References

- [OACT extraction results](native-bi-oact-tst-execution-results.md)
- [OJDT extraction results](native-bi-ojdt-tst-execution-results.md)
- [Accounting MART functions](native-bi-accounting-mart.md)
- [Accounting extractor design](native-bi-accounting-extractor.md)
- [Raw validation queries](../sql/native-bi/accounting-mart-validation-queries.sql)
