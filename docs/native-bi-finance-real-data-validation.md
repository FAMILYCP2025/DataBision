# Native BI — Finance Real Data Validation

Sprint 15E — 2026-06-18

---

## Current State

MART tables are empty pending extraction + MART refresh (Sprints 15B/15D).
This document defines the complete validation protocol to execute once data is available.
All validation checks, expected values, and sign convention tests are specified below.

---

## API Endpoints Under Test

| Endpoint | Controller | Auth |
|---|---|---|
| `GET /api/client/bi/finance/readiness` | `ClientBiFinanceController` | JWT (any company role) |
| `GET /api/client/bi/finance/validations` | `ClientBiFinanceController` | JWT (any company role) |
| `GET /api/client/bi/finance/income-statement` | `ClientBiFinanceController` | JWT (any company role) |
| `GET /api/client/bi/finance/balance-sheet` | `ClientBiFinanceController` | JWT (any company role) |
| `GET /api/client/bi/finance/ebitda` | `ClientBiFinanceController` | JWT (any company role) |
| `GET /api/client/bi/finance/chart-of-accounts` | `ClientBiFinanceController` | JWT (any company role) |

---

## 1. Readiness Endpoint

### Request

```bash
curl -s -H "Authorization: Bearer <JWT>" \
  "http://localhost:5103/api/client/bi/finance/readiness" | jq .
```

### Expected response structure

```json
{
  "data": {
    "rawOactCount":      250,
    "rawOjdtCount":     5000,
    "rawJdt1Count":    12500,
    "stgOactCount":      250,
    "stgOjdtCount":     5000,
    "stgJdt1Count":    12500,
    "martGlAccounts":    250,
    "martIncomeStatement": 36,
    "martBalanceSheet":   36,
    "martEbitda":          12,
    "classificationRules": 45,
    "unclassifiedPostable": 0,
    "readinessStatus": "ready",
    "blockingReasons": [],
    "warnings": []
  }
}
```

### Validation checks

| Field | Expected | Fail condition |
|---|---|---|
| `rawOactCount` | > 0 | = 0 → OACT not extracted |
| `rawOjdtCount` | > 0 | = 0 → OJDT not extracted |
| `rawJdt1Count` | > 0 | = 0 → JDT1 lines not embedded in OJDT |
| `stgOactCount` | ≥ rawOactCount | < rawOact → STG refresh not run |
| `martGlAccounts` | > 0 | = 0 → mart.refresh_gl_accounts not run |
| `martIncomeStatement` | > 0 | = 0 → no classified revenue/cost accounts + balances |
| `unclassifiedPostable` | 0 (ideal) or < 5 | > 10 → significant data quality issue |
| `readinessStatus` | `"ready"` | `"blocked"` → check blockingReasons |

---

## 2. Validations Endpoint

### Request

```bash
curl -s -H "Authorization: Bearer <JWT>" \
  "http://localhost:5103/api/client/bi/finance/validations" | jq .
```

### Expected response structure

```json
{
  "data": {
    "healthScore": 90,
    "healthStatus": "ok",
    "criticalIssues": 0,
    "warningIssues": 1,
    "infoIssues": 0,
    "lastPeriodValidated": "2026-05",
    "balanceImbalance": 0.00,
    "unclassifiedAccounts": 3,
    "orphanJournalLines": 0,
    "issues": [
      {
        "code": "unclassified_accounts",
        "severity": "warning",
        "message": "3 postable accounts have no classification",
        "count": 3
      }
    ],
    "reconciliation": {
      "totalAssets": 450000.00,
      "totalLiabilitiesEquity": 450000.00,
      "isBalanced": true
    }
  }
}
```

### Validation checks

| Field | Target | Action if fails |
|---|---|---|
| `healthScore` | ≥ 80 | Identify critical issues, fix classification |
| `criticalIssues` | = 0 | Address before demo |
| `balanceImbalance` | < 0.01 | Add missing equity/liability classification rules |
| `unclassifiedAccounts` | < 5 | Add specific account-code rules via SuperAdmin |
| `isBalanced` | `true` | Fix equity accounts, re-run MART refresh |
| `orphanJournalLines` | = 0 | Re-run OJDT extraction |

---

## 3. Income Statement Endpoint

### Request

```bash
curl -s -H "Authorization: Bearer <JWT>" \
  "http://localhost:5103/api/client/bi/finance/income-statement" | jq .
```

### Sign convention validation

The ETL applies this sign convention in `mart.refresh_income_statement`:

| statement_line | Formula | Sign expected |
|---|---|---|
| revenue | credit_sum − debit_sum | **Positive** = income |
| other_income | credit_sum − debit_sum | **Positive** = income |
| cogs | debit_sum − credit_sum | **Positive** = cost |
| opex | debit_sum − credit_sum | **Positive** = expense |
| other_expense | debit_sum − credit_sum | **Positive** = expense |
| financial | debit_sum − credit_sum | **Positive** = net expense |
| tax | debit_sum − credit_sum | **Positive** = tax charge |

**If `revenue` is negative:** SAP instance stores revenue as debit-side (inverted polarity). Resolution: flip the sign convention for revenue accounts by adjusting classification rules or updating `mart.refresh_income_statement` for this client.

### Formula verification

```
gross_profit = revenue − cogs                    (expect positive if profitable)
operating_income = gross_profit − opex           (expect positive if operationally profitable)
net_income = operating_income − financial − tax  (bottom line)
```

---

## 4. Balance Sheet Endpoint

### Request

```bash
curl -s -H "Authorization: Bearer <JWT>" \
  "http://localhost:5103/api/client/bi/finance/balance-sheet" | jq .
```

### Balance equation

```
Total Assets = Total Liabilities + Equity

current_assets + non_current_assets
= current_liabilities + non_current_liabilities + equity
```

**Known limitation:** The current `mart.refresh_balance_sheet` is flow-based (period aggregation), not cumulative. This means the balance sheet shows net debit/credit activity per period, not the true running balance. For a proper cumulative balance sheet, all periods from inception must be summed. This limitation is documented and noted in the UI.

### Balance sheet sign convention

| category | Formula | Sign expected |
|---|---|---|
| current_assets | debit_sum − credit_sum | Positive = asset |
| non_current_assets | debit_sum − credit_sum | Positive = asset |
| current_liabilities | credit_sum − debit_sum | Positive = liability |
| non_current_liabilities | credit_sum − debit_sum | Positive = liability |
| equity | credit_sum − debit_sum | Positive = equity |

---

## 5. EBITDA Endpoint

### Request

```bash
curl -s -H "Authorization: Bearer <JWT>" \
  "http://localhost:5103/api/client/bi/finance/ebitda" | jq .
```

### Formula verification

```
gross_profit = revenue − cogs
ebitda = gross_profit − opex                     (= EBIT when D&A = 0)
net_income = ebitda − financial_result − tax_result
```

Note: `depreciation` and `amortization` fields will be 0 until accounts are classified as `depreciation` / `amortization` statement_lines in `cfg.account_classification_rules`. Until then, EBITDA = EBIT for this company.

---

## 6. Chart of Accounts Endpoint

### Request

```bash
curl -s -H "Authorization: Bearer <JWT>" \
  "http://localhost:5103/api/client/bi/finance/chart-of-accounts" | jq .
```

### Validation

- Each account has a non-null `statementLine`
- No account has `statementLine = 'unclassified'` (after full classification)
- `postable = true` accounts should have balance activity in MART
- Accounts are grouped by `statementLine` in the UI

---

## Frontend Tab Validation

Open Finance dashboard with valid JWT for `ksdepor` or `demo` tenant:

| Tab | URL param | Test action | Expected |
|---|---|---|---|
| Resumen | `?tab=resumen` | Load page | Readiness panel visible; no FinancialDataPending |
| Estado de Resultados | `?tab=resultados` | Load; apply year filter | P&L rows visible; revenue positive |
| Balance General | `?tab=balance` | Load; check balance badge | Green "Cuadra" badge visible |
| EBITDA | `?tab=ebitda` | Load; check trend | 12 rows; gross_profit > 0 |
| Plan de Cuentas | `?tab=cuentas` | Load; check unclassified | < 5 unclassified accounts visible |
| Validaciones | `?tab=validaciones` | Load | healthScore badge green; 0 critical issues |

---

## Real Data Validation Log (fill after execution)

| Endpoint | Status | Key observations | Date |
|---|---|---|---|
| GET /finance/readiness | ⏳ Pending | — | — |
| GET /finance/validations | ⏳ Pending | — | — |
| GET /finance/income-statement | ⏳ Pending | — | — |
| GET /finance/balance-sheet | ⏳ Pending | — | — |
| GET /finance/ebitda | ⏳ Pending | — | — |
| GET /finance/chart-of-accounts | ⏳ Pending | — | — |
| Frontend: resumen tab | ⏳ Pending | — | — |
| Frontend: resultados tab | ⏳ Pending | — | — |
| Frontend: balance tab | ⏳ Pending | — | — |
| Frontend: ebitda tab | ⏳ Pending | — | — |
| Frontend: cuentas tab | ⏳ Pending | — | — |
| Frontend: validaciones tab | ⏳ Pending | — | — |
| Revenue positive | ⏳ Pending | — | — |
| Balance cuadra (< 0.01) | ⏳ Pending | — | — |
| EBITDA formula correct | ⏳ Pending | — | — |
| healthScore ≥ 80 | ⏳ Pending | — | — |

---

## References

- MART validation queries: `docs/sql/accounting-mart-validation-queries.sql`
- MART validation results: `docs/native-bi-accounting-mart-validation-results.md`
- RAW validation: `docs/native-bi-accounting-raw-validation.md`
- Classification plan: `docs/native-bi-ksdepor-account-classification-plan.md`
- Operations runbook: `docs/native-bi-accounting-operations-runbook.md`
