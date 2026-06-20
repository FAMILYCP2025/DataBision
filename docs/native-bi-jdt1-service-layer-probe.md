# Sprint 17A — JDT1 Service Layer Single-Record Probe

**Date:** 2026-06-19  
**System:** SAP B1 HANA TST — CLTSTKSDEPOR  
**SL Version:** 1000290

## Context

Sprint 16 confirmed that `GET JournalEntries?$expand=JournalEntryLines` returns HTTP 400 on SL v1000290. Sprint 17A tests whether accessing a single record (`GET JournalEntries(N)`) exposes `JournalEntryLines` without `$expand`.

## Probe Methodology

```
GET /b1s/v1/JournalEntries(38)
Cookie: B1SESSION=<session>
```

No `$expand`, no `$select` — minimal single-record request.

## Results

| Property | Value |
|---|---|
| HTTP status | 200 OK |
| Top-level properties returned | 83 |
| `JournalEntryLines` present | **YES** — 2 lines for JournalEntries(38) |
| `WithholdingTaxDataCollection` | YES (array) |
| `ElectronicProtocols` | YES (array) |

### JournalEntryLine field names (confirmed)

`Line_ID, AccountCode, Debit, Credit, FCDebit, FCCredit, FCCurrency, DueDate, ShortName, ContraAccount, LineMemo, ReferenceDate1, ReferenceDate2, Reference1, Reference2, ProjectCode, CostingCode, TaxDate, BaseSum, TaxGroup, DebitSys, CreditSys, VatDate, VatLine, SystemBaseAmount, VatAmount, SystemVatAmount, GrossValue, AdditionalReference, CheckAbs, CostingCode2, CostingCode3, CostingCode4, TaxCode, TaxPostAccount, CostingCode5, LocationCode, ControlAccount, EqualizationTaxAmount, SystemEqualizationTaxAmount, TotalTax, SystemTotalTax, WTLiable, WTRow, PaymentBlock, BlockReason, FederalTaxID, BPLID, BPLName, VATRegNum, PaymentOrdered, ExposedTransNumber, DocumentArray, DocumentLine, CostElementCode, Cig, Cup, IncomeClassificationCategory, IncomeClassificationType, ExpensesClassificationCategory, ExpensesClassificationType, VATClassificationCategory, VATClassificationType, VATExemptionCause, LineAllocationNumber` + UDFs

### Critical field name

The line identifier is `Line_ID` (not `LineId` or `LineNum`). `GetInt(line, "LineId")` returns 0 — the initial mapper used the wrong field name, causing all lines to collapse to LineId=0 via upsert collision. Fixed by `GetIntAny(line, "Line_ID", "LineId", "LineNum")`.

## Conclusion

**Single-record GET on SL v1000290 exposes `JournalEntryLines` by default.** No `$expand` needed. This is the extraction path for JDT1.

The list endpoint (`GET JournalEntries?$expand=JournalEntryLines`) continues to reject the request with HTTP 400 on this version.
