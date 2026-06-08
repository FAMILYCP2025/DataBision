# Native BI E2E Validation Results

Run: `scripts/dev/test-native-bi-endpoints.ps1`

Date: ___________  
Company: ___________  
API URL: ___________  
Auth mode: DEV (query param) / JWT Bearer

---

## Dashboard endpoints

| Endpoint | HTTP | Data present | Count / detail | Expected | Result | Notes |
|---|---|---|---|---|---|---|
| GET /dashboard/summary | | | | 200 data present | | |
| GET /dashboard/sales-daily?days=30 | | | | 200 array ≤30 | | |
| GET /dashboard/sales-monthly?months=12 | | | | 200 array ≤12 | | |
| GET /dashboard/top-customers?limit=10 | | | | 200 array ≤10, meta present | | |
| GET /dashboard/top-items?limit=10 | | | | 200 array ≤10, meta present | | |
| GET /dashboard/salespersons?limit=20 | | | | 200 array ≤20, meta present | | |

## Sales endpoints

| Endpoint | HTTP | Data present | Count / detail | Expected | Result | Notes |
|---|---|---|---|---|---|---|
| GET /sales/overview | | | | 200 grossSalesAmount > 0 | | |
| GET /sales/daily?dateFrom=2026-01-01&dateTo=2026-12-31 | | | | 200 array | | |
| GET /sales/monthly?dateFrom=2026-01-01&dateTo=2026-12-31 | | | | 200 array | | |
| GET /sales/customers?limit=20 | | | | 200 data+meta | | |
| GET /sales/items?limit=20 | | | | 200 data+meta | | |
| GET /sales/salespersons?limit=20 | | | | 200 data+meta | | |

## Sync endpoints

| Endpoint | HTTP | Data present | Detail | Expected | Result | Notes |
|---|---|---|---|---|---|---|
| GET /sync/status | | | overallStatus= | 200 ok/warning | | |
| GET /sync/objects | | | count= | 200 7 objects | | |
| GET /sync/transform-status | | | table count= | 200 6 tables | | |

## Diagnostics endpoints

| Endpoint | HTTP | Status | Checks count | Expected | Result | Notes |
|---|---|---|---|---|---|---|
| GET /diagnostics/native-bi | | | | 200 status=ok/warning | | |
| GET /diagnostics/native-bi/tables | | | | 200 tables array | | |

## Pagination validation

| Endpoint | HTTP | meta.limit | meta.offset | meta.hasMore | Result | Notes |
|---|---|---|---|---|---|---|
| GET /dashboard/top-customers?limit=5&offset=10 | | | | | | |
| GET /sales/customers?limit=5&sortBy=cardCode&sortDir=asc | | | | | | |

## Error validation (DEV mode)

| Endpoint | HTTP | Expected | Result | Notes |
|---|---|---|---|---|
| GET /dashboard/summary (no companyId) | | 400 | | |
| GET /dashboard/sales-daily?days=0 | | 400 | | |
| GET /dashboard/top-customers?sortBy=badField | | 400 | | |
| GET /dashboard/top-customers?offset=-1 | | 400 | | |
| GET /sales/overview?dateFrom=not-a-date | | 400 | | |

---

## Transform status row counts (expected)

| Table | Expected rows | Actual rows | Match |
|---|---|---|---|
| mart.sales_daily | 36 | | |
| mart.sales_monthly | 6 | | |
| mart.customer_sales | 20 | | |
| mart.item_sales | 11 | | |
| mart.salesperson_sales | 3 | | |
| mart.sales_kpi_summary | 1 | | |

---

## Overall assessment

| Check | Status |
|---|---|
| All 15 endpoints return 200 | |
| Diagnostics endpoints return 200 | |
| Error cases return 400/401 | |
| Pagination meta returned for list endpoints | |
| Transform row counts match expected | |
| No stack traces exposed | |
| No secrets in responses | |
