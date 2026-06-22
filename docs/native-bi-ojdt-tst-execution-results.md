# Native BI TST — Sprint 25 Execution Results

**Fecha ejecución:** 2026-06-21  
**Ambiente:** DEV local → SAP TST (CLTSTKSDEPOR)  
**Empresa:** ksdepor / AnalyticsCompanyId: company-dev-001  
**SL Version:** 1000290

---

## 25A — API + Perfil de Conexión

| Check | Resultado |
|---|---|
| SAP_PASSWORD_KSDEPOR | SET (len=12) — User scope |
| API health | HTTP 200 `{"status":"ok"}` |
| ksdepor en AppDB | Id=2, AnalyticsCompanyId=company-dev-001 |
| NativeBiConnectionProfile 'tst' | Id=1, CompanyId=2, SecretRef=env:SAP_PASSWORD_KSDEPOR, Active=1 |
| Resolve endpoint | HTTP 200 — profileId=1, companyDb=CLTSTKSDEPOR, user=dgoto, passwordLen=12 |

**Fix:** demo company tenía AnalyticsCompanyId=company-dev-001 duplicado → limpiado a NULL.

---

## 25D — Dry-Run

| Check | Resultado |
|---|---|
| Profile resolved | ✅ id=1 name=tst db=CLTSTKSDEPOR concurrency=3 |
| Password en logs | ✅ `[set]` — NO impreso |
| DRY-RUN configuration OK | ✅ |
| Exit code | 0 |

---

## 25E — Extracción OACT

| Métrica | Valor |
|---|---|
| SAP Login | ✅ Successful. SL Version=1000290 |
| Strategy | no-select (GroupMask, Postable inválidos en SL 1000290) |
| Cuentas extraídas | 20 |
| inserted/updated/skipped | 0 / 0 / 20 (ya existían) |
| SAP Logout | ✅ HTTP 204 |

---

## 25E — Extracción OJDT + JDT1

| Métrica | Valor |
|---|---|
| $expand fallbacks | JournalEntryLines ❌, Lines ❌, full-select ❌ |
| Strategy final | Minimal select sin $expand |
| Entradas OJDT extraídas | 20 |
| PROBE-17A | ✅ FOUND 'JournalEntryLines' — 3 lines en GET JournalEntries(39) |
| Estrategia líneas | individual GET, property='JournalEntryLines', concurrency=3 |
| Líneas JDT1 extraídas | 68 (avg 3.4/entrada, 0 failed GETs) |
| inserted/updated/skipped OJDT | 0 / 0 / 20 |
| inserted/updated/skipped JDT1 | 0 / 0 / 68 |
| SAP Logout | ✅ HTTP 204 |

---

## 25F — MART Refresh

**mart.refresh_all** (6 objects / 6515ms):
sales_daily=39, sales_monthly=6, customer_sales=21, item_sales=11, salesperson_sales=4, sales_kpi_summary=1

**mart.refresh_all processes** (12 objects / 3325ms):
sales_customer=18, sales_item=11, sales_fulfillment=28, finance_ar_aging=18, finance_ap_aging=0,
finance_executive_daily=3, inventory_rotation=41, inventory_stock=0, inventory_warehouse=12,
purchase_executive=17, purchase_supplier=18, purchase_receiving=10

---

## 25F — Finance Readiness

```json
{
  "rawOactCount": 20, "rawOjdtCount": 50, "rawJdt1Count": 122,
  "stgOactCount": 20, "stgOjdtCount": 50, "stgJdt1Count": 122,
  "martGlAccounts": 55, "martIncomeStatement": 5,
  "martBalanceSheet": 6, "martEbitda": 2,
  "classificationRules": 84, "unclassifiedPostable": 0,
  "readinessStatus": "ready",
  "blockingReasons": [], "warnings": []
}
```

## 25F — Finance Validations

```json
{
  "healthScore": 100, "healthStatus": "ok",
  "criticalIssues": 0, "warningIssues": 0, "infoIssues": 0,
  "lastPeriodValidated": "2026-02",
  "balanceImbalance": 0, "unclassifiedAccounts": 0, "orphanJournalLines": 0,
  "issues": [],
  "reconciliation": { "isBalanced": true, "imbalance": 0 }
}
```

## 25F — Dashboard Endpoints (7/7 HTTP 200)

| Endpoint | HTTP | Detalle |
|---|---|---|
| `/api/client/bi/finance/readiness` | **200** | readinessStatus=ready |
| `/api/client/bi/finance/income-statement` | **200** | 2 períodos |
| `/api/client/bi/finance/balance-sheet` | **200** | 1 snapshot |
| `/api/client/bi/finance/ebitda` | **200** | 2 períodos |
| `/api/client/bi/finance/chart-of-accounts` | **200** | 55 cuentas |
| `/api/client/bi/finance/validations` | **200** | healthScore=100 |
| `/api/client/bi/finance/refresh-status` | **200** | overallStatus=ok |

**Score: 7/7 HTTP 200** ✅

---

## Decisión: GO ✅

| Criterio GO | Cumplido |
|---|---|
| Resolve profile OK | ✅ |
| Dry-run profile OK | ✅ |
| raw.sap_oact > 0 | ✅ (20) |
| raw.sap_ojdt > 0 | ✅ (50) |
| mart.income_statement_summary > 0 | ✅ (5) |
| mart.balance_sheet_summary > 0 | ✅ (6) |
| mart.ebitda_summary > 0 | ✅ (2) |
| Endpoints ≥ 5/7 HTTP 200 | ✅ (7/7) |
| Sin secretos en logs | ✅ |

**→ GO: Flujo completo TST validado. Native BI Finance operativo sobre CLTSTKSDEPOR.**
