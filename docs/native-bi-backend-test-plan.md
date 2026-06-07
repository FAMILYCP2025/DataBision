# Native BI Backend — Test Plan

Manual + automated tests for Sprint 6A–6C endpoints.

---

## Pre-conditions

- [ ] `dotnet run --project src/DataBision.Api` running on `http://localhost:5000`
- [ ] Supabase `StagingConnection` configured in `appsettings.Development.json`
- [ ] `--transform --include-mart --company company-dev-001` executed — MART tables populated
- [ ] KPI validation passed (docs/kpi-validation-results-template.md)

---

## 1. Dashboard summary

### 1.1 Happy path
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/dashboard/summary?companyId=company-dev-001" | ConvertTo-Json -Depth 5
```
Expected:
- HTTP 200
- `data.companyId` = `"company-dev-001"`
- `data.grossSalesAmount` > 0
- `data.invoiceCount` = 72
- `data.activeCustomers` = 20
- `data.activeItems` = 11
- `data.transformedAtUtc` is recent

### 1.2 Missing companyId
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/dashboard/summary" -ErrorAction SilentlyContinue
```
Expected: HTTP 400, `error = "missing_company_id"`

### 1.3 Unknown company (no MART data)
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/dashboard/summary?companyId=nonexistent-co"
```
Expected: HTTP 200, `data = null`

---

## 2. Sales daily

### 2.1 Default (last 30 days)
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/dashboard/sales-daily?companyId=company-dev-001" | ConvertTo-Json -Depth 5
```
Expected: HTTP 200, `data` array with ≤ 30 items, each with `salesDate`, `grossSalesAmount`, `invoiceCount`

### 2.2 Custom days
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/dashboard/sales-daily?companyId=company-dev-001&days=7"
```
Expected: ≤ 7 items

### 2.3 days = 0 → clamped to 1
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/dashboard/sales-daily?companyId=company-dev-001&days=0"
```
Expected: HTTP 400 or 1 day of data (400 because 0 is outside 1-365)

### 2.4 days = 999 → clamped to 365
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/dashboard/sales-daily?companyId=company-dev-001&days=999"
```
Expected: HTTP 400 (value outside allowed range)

---

## 3. Sales monthly

### 3.1 Default (last 12 months)
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/dashboard/sales-monthly?companyId=company-dev-001" | ConvertTo-Json -Depth 5
```
Expected: ≤ 12 items, each with `salesMonth`, `grossSalesAmount`, `invoiceCount`

### 3.2 Months = 6
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/dashboard/sales-monthly?companyId=company-dev-001&months=6"
```
Expected: ≤ 6 items

---

## 4. Top customers

### 4.1 Default (top 10)
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/dashboard/top-customers?companyId=company-dev-001" | ConvertTo-Json -Depth 5
```
Expected: ≤ 10 items ordered by `netSalesAmount DESC`, each has `cardCode`, `cardName`

### 4.2 Limit 3
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/dashboard/top-customers?companyId=company-dev-001&limit=3"
```
Expected: exactly 3 items (if ≥ 3 customers exist)

### 4.3 Limit 200 → clamped to 100 or rejected with 400
Based on spec: limit 1–100, out of range returns 400.

---

## 5. Sales overview (date range)

### 5.1 Explicit range
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/sales/overview?companyId=company-dev-001&dateFrom=2026-01-01&dateTo=2026-06-30" | ConvertTo-Json -Depth 5
```
Expected: `data.grossSalesAmount` > 0, `data.dateFrom = "2026-01-01"`, `data.dateTo = "2026-06-30"`

### 5.2 No dates → default 30 days
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/sales/overview?companyId=company-dev-001"
```
Expected: `data.dateFrom` ≈ today - 30 days

### 5.3 Invalid dateFrom format
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/sales/overview?companyId=company-dev-001&dateFrom=not-a-date"
```
Expected: HTTP 400, `error = "invalid_date_from"`

### 5.4 dateFrom > dateTo
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/sales/overview?companyId=company-dev-001&dateFrom=2026-06-01&dateTo=2026-01-01"
```
Expected: HTTP 400, `error = "invalid_date_range"`

---

## 6. Sync status

### 6.1 Status with real data
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/sync/status?companyId=company-dev-001" | ConvertTo-Json -Depth 10
```
Expected:
- `data.overallStatus` = `"ok"` (if transform ran < 24h ago)
- `data.objects` has 7 entries (one per SAP object)
- `data.dataFreshness.martLastTransformedAtUtc` is recent

### 6.2 Objects list
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/sync/objects?companyId=company-dev-001" | ConvertTo-Json -Depth 5
```
Expected: 7 objects (`OINV`, `INV1`, `ORIN`, `RIN1`, `OCRD`, `OITM`, `OSLP`)

### 6.3 Transform status
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/sync/transform-status?companyId=company-dev-001" | ConvertTo-Json -Depth 5
```
Expected: 6 MART tables with row counts matching transform output:
- `sales_daily`: 36 rows
- `sales_monthly`: 6 rows
- `customer_sales`: 20 rows
- `item_sales`: 11 rows
- `salesperson_sales`: 3 rows
- `sales_kpi_summary`: 1 row

### 6.4 Missing companyId
```powershell
Invoke-RestMethod "http://localhost:5000/api/client/sync/status"
```
Expected: HTTP 400, `error = "missing_company_id"`

---

## 7. Automated tests (unit)

Run: `dotnet test DataBision.sln --no-build`

| Test class | Count | What it verifies |
|---|---|---|
| `DashboardServiceTests` | 11 | limit/days/months clamping, null passthrough, list ordering |
| `SalesServiceTests` | 7 | date range passthrough, limit clamping, default range |

All tests mock `IDashboardRepository` — no Supabase connection required.

---

## 8. Acceptance criteria

| Criterion | Status |
|---|---|
| All 6 dashboard endpoints return 200 for valid companyId | — |
| All 6 sales endpoints return 200 for valid companyId | — |
| All 3 sync endpoints return 200 for valid companyId | — |
| Missing companyId returns 400 on all endpoints | — |
| Invalid date format returns 400 | — |
| dateFrom > dateTo returns 400 | — |
| `dotnet build` 0 errors, 0 warnings | ✅ |
| `dotnet test` 52/52 pass | ✅ |
| No secrets in code | ✅ |
| No frontend touched | ✅ |
| No DB migrations created | ✅ |
