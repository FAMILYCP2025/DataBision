# OACT TST Extraction Results — Sprint 16B/16C

**Date:** 2026-06-18  
**Environment:** SAP B1 TST — CompanyDB: CLTSTKSDEPOR  
**Service Layer:** https://161.153.200.53:50000/b1s/v1  
**SL Version:** 1000290  
**CompanyId:** company-dev-001  
**Endpoint called:** `POST api/ingest/sap-b1/chart-of-accounts`

---

## Extraction Summary

| Metric | Value |
|---|---|
| SL Entity | `ChartOfAccounts` |
| SAP Object | `OACT` |
| Fallback level used | Level 3 — no `$select` |
| Pages fetched | 1 |
| Rows extracted | 20 |
| Rows inserted | 20 |
| Rows updated | 0 |
| Rows skipped | 0 |
| Duration | ~15 s |

---

## Fallback Chain Detail

SL v1000290 rejected multiple `$select` fields for `ChartOfAccounts`:

| Attempt | Query | Result |
|---|---|---|
| 1 — FullSelect | `$select=Code,Name,GroupMask,AccountType,Postable,Frozen,ValidFor,CashAccount,ControlAccount,Currency,FormatCode,ExternalCode` | HTTP 400: Property 'GroupMask' invalid |
| 2 — MinimalSelect | `$select=Code,Name,AccountType,Postable` | HTTP 400: Property 'Postable' invalid |
| 3 — No `$select` | (no query param — returns all SL-available fields) | **200 OK — 20 rows** |

Fields confirmed invalid in SL v1000290 for `ChartOfAccounts`:
- `FatherNum`, `Father` (account hierarchy parent)
- `Level`, `GroupMask`
- `Postable`

---

## Supabase Validation (raw.sap_oact)

**Query date:** 2026-06-18

```
COUNT: 20 rows
Missing Code: 0
```

### FormatCode prefix distribution (PCGE — Plan Contable General Empresarial Peruano)

| Prefix | Count | PCGE Category |
|---|---|---|
| 01 | 2 | Bienes y valores |
| 02 | 2 | Saldos iniciales |
| 09 | 2 | Acreedoras por contra |
| 10 | 4 | Efectivo y equivalentes |
| 11 | 1 | Inversiones financieras |
| 12 | 2 | Cuentas por cobrar |
| 13 | 1 | Existencias |
| 14 | 1 | Servicios y otros |
| 16 | 1 | Otras cuentas |
| 17 | 1 | Activo diferido |
| 18 | 1 | Otros activos |
| 19 | 1 | Provisiones |
| 40 | 1 | Tributos |

### Account sample (top 10 by Code)

| Code | Name | AccountType | FormatCode | ValidFor |
|---|---|---|---|---|
| 01 | BIENES Y VALORES ENTREGADOS | at_Other | 01 | Y |
| 01311 | MERCADERIAS | at_Other | 01311 | Y |
| 02 | SALDO INICIALES | at_Other | 02 | Y |
| 02111 | TRASLADO DE SALDOS INICIALES CIERRE ANUAL SAP | at_Other | 02111 | Y |
| 09 | ACREEDORAS POR CONTRA | at_Other | 09 | Y |
| 09131 | MERCADERIAS | at_Other | 09131 | Y |
| 10 | EFECTIVO Y EQUIVALENTES DE EFECTIVO | at_Other | 10 | Y |
| 10110 | CAJA MATRIZ M.N. | at_Other | 10110 | Y |
| 10111 | CAJA MATRIZ M.E | at_Other | 10111 | Y |
| 10150 | CAJA CHICA TACNA M.N | at_Other | 10150 | Y |

---

## Known Limitations

- **AccountType = at_Other for all accounts:** SL v1000290 does not differentiate asset/liability/equity in the `AccountType` field via the no-select fallback. MART classification relies on `FormatCode` prefix (PCGE) instead.
- **Postable, GroupMask, FatherNum not available:** These fields are NULL in Supabase. The account hierarchy (parent/child) cannot be reconstructed from this SL version.
- **`Postable` column is empty:** Cannot distinguish header accounts from posting accounts. All accounts treated as postable by the MART.

---

## Status

**OACT extraction: PASSED**  
Data is in `raw.sap_oact` and ready for MART classification after account classification SQL is applied.
