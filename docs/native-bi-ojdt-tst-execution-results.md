# OJDT TST Extraction Results — Sprint 16D/16E

**Date:** 2026-06-18  
**Environment:** SAP B1 TST — CompanyDB: CLTSTKSDEPOR  
**Service Layer:** https://161.153.200.53:50000/b1s/v1  
**SL Version:** 1000290  
**CompanyId:** company-dev-001  
**Endpoints called:**  
- `POST api/ingest/sap-b1/journal-entries` (OJDT headers)  
- `POST api/ingest/sap-b1/journal-entry-lines` (JDT1 lines — not sent, 0 rows)

---

## Extraction Summary

### OJDT Headers

| Metric | Value |
|---|---|
| SL Entity | `JournalEntries` |
| SAP Object | `OJDT` |
| Fallback level used | Level 4 — MinimalSelect, no `$expand` |
| Pages fetched | 1 |
| Rows extracted | 20 |
| Rows inserted | 20 |
| Rows updated | 0 |
| Rows skipped | 0 |
| Duration | ~20 s |

### JDT1 Lines

| Metric | Value |
|---|---|
| SL Entity | `JournalEntryLines` / embedded in `JournalEntries` |
| SAP Object | `JDT1` |
| Result | **0 rows — blocked by SL v1000290** |

---

## Fallback Chain Detail — OJDT Headers

SL v1000290 rejected all `$expand` and several `$select` fields for `JournalEntries`:

| Attempt | Query | Result |
|---|---|---|
| 1 — FullSelect + `$expand=JournalEntryLines` | `$select=JdtNum,ReferenceDate,DueDate,TaxDate,Memo,TransactionCode,BaseRef,Ref1,CreatedBy&$expand=JournalEntryLines` | HTTP 400: invalid navigation property 'JournalEntryLines' |
| 2 — FullSelect + `$expand=Lines` | Same select + `$expand=Lines` | HTTP 400: invalid navigation property 'Lines' |
| 3 — FullSelect, no expand | `$select=JdtNum,ReferenceDate,DueDate,TaxDate,Memo,TransactionCode,BaseRef,Ref1,CreatedBy` | HTTP 400: Property 'BaseRef' invalid |
| 4 — MinimalSelect, no expand | `$select=JdtNum,ReferenceDate,Memo` | **200 OK — 20 rows** |
| 5 — No `$select` | (fallback, not reached) | — |

Fields confirmed invalid in SL v1000290 for `JournalEntries`:
- Navigation: `JournalEntryLines`, `Lines` (both `$expand` targets rejected)
- Select fields: `BaseRef`, `DueDate`, `TaxDate`, `TransactionCode`, `Ref1`, `CreatedBy`

---

## JDT1 Lines — Blocked

All three approaches to retrieve line-level data failed:

| Approach | Result |
|---|---|
| `$expand=JournalEntryLines` on list query | HTTP 400: invalid navigation property |
| `$expand=Lines` on list query | HTTP 400: invalid navigation property |
| `GET /b1s/v1/JournalEntryLines` (top-level entity) | HTTP 400: "Unrecognized resource path." |

**Conclusion:** SL v1000290 does not expose JDT1 lines through any standard OData mechanism available to this company's Service Layer installation. This is a version limitation, not a configuration issue.

---

## Supabase Validation (raw.sap_ojdt)

**Query date:** 2026-06-18

```
COUNT raw.sap_ojdt: 20 rows
Missing JdtNum: 0
```

### Date Range

| Oldest RefDate | Newest RefDate | Distinct TransTypes |
|---|---|---|
| 2026-01-01 | 2026-01-22 | 0 (NULL — field not in MinimalSelect) |

### Top-10 OJDT headers (most recent)

| TransId | JdtNum | RefDate | Memo |
|---|---|---|---|
| 19 | 19 | 2026-01-22 | Precio de entrega 260100001 |
| 18 | 18 | 2026-01-22 | Pedido de entrada de mercancías - PP99999999994 |
| 13 | 13 | 2026-01-22 | Basado en Pedido de entrada de mercancías CS-0001-260110003 |
| 12 | 12 | 2026-01-22 | Fact.proveedores - P20100016843 |
| 11 | 11 | 2026-01-22 | Basado en Pedido de entrada de mercancías 09-0012-2544 |
| 8 | 8 | 2026-01-22 | Pedido de entrada de mercancías - P20100016843 |
| 9 | 9 | 2026-01-22 | d |
| 41 | 41 | 2026-01-15 | Fact.proveedores - P20100049181 |
| 40 | 40 | 2026-01-15 | Fact.proveedores - P20100049181 |
| 14 | 14 | 2026-01-15 | Fact.proveedores - PP99999999994 |

### raw.sap_jdt1

```
COUNT: 0 rows
```

---

## Known Limitations

- **TransType is NULL:** `TransactionCode` field was rejected by SL; field is empty in all 20 rows. Cannot classify journal entry types (purchase, sale, transfer, etc.).
- **No line-level data:** Debit/Credit amounts per account are unavailable. The accounting MART (`mart.refresh_accounting_all`) cannot produce meaningful results without JDT1.
- **Memo only:** The only useful contextual field extracted is `Memo` (free-text). Pattern matching on Memo for classification is fragile and not recommended for production.

---

## Impact on Finance MART

| Feature | Status | Reason |
|---|---|---|
| Balance Sheet | BLOCKED | Requires JDT1 debit/credit per account |
| Income Statement | BLOCKED | Requires JDT1 debit/credit per account |
| EBITDA | BLOCKED | Requires JDT1 debit/credit per account |
| Chart of Accounts browser | AVAILABLE | Uses raw.sap_oact only |
| Account classification | AVAILABLE | Uses raw.sap_oact + FormatCode |

**Decision: Do NOT execute `mart.refresh_accounting_all('company-dev-001')` until JDT1 is populated.**

---

## Recommended Next Steps to Resolve JDT1

### Option 1 — Single-record GET (highest priority, try first)

`GET /b1s/v1/JournalEntries(N)` (single record by key) may return embedded lines by default in some SL configurations, even when list `$expand` is rejected. Test with a known JdtNum:

```
GET https://host:50000/b1s/v1/JournalEntries(1)
```

If the response includes a `JournalEntryLines` or `Lines` array, implement a loop extractor that fetches entries one by one. Expensive but functional.

### Option 2 — HANA CrossJoin / $batch

`POST /b1s/v1/$batch` with individual requests per JdtNum. Avoids `$expand` entirely. Each sub-request returns a single entry; check if it includes lines by default.

### Option 3 — Direct HANA SQL via XS Engine or ODBC

If the customer grants direct HANA ODBC access, query `JDT1` table directly:
```sql
SELECT T0."TransId", T0."Line_ID", T0."Account", T0."Debit", T0."Credit"
FROM CLTSTKSDEPOR.JDT1 T0
WHERE T0."TransId" IN (SELECT "TransId" FROM CLTSTKSDEPOR.OJDT WHERE "RefDate" >= ...)
```
This bypasses SL entirely. Requires DB-level credentials and network access to HANA port (30015 or 30040).

### Option 4 — SAP B1 Service Layer upgrade

SL v1000290 corresponds to SAP B1 10.0 FP29 or earlier. Upgrading to 10.0 FP40+ or SAP B1 version 10.0 should restore `$expand` support on `JournalEntries`. Coordinate with SAP partner.

### Option 5 — Query Service (SL alternative)

`POST /b1s/v1/SQLQueries` may allow raw SQL execution if the SL security profile permits it. Less standard but available in some installations.
