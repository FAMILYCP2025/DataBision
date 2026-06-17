# Native BI — Filter Options API

## Overview

Filter options endpoints return distinct values from MART tables to populate filter selects in the client UI. All endpoints are resilient: if the underlying table or column doesn't exist in the MART yet, they return an empty array instead of an error.

## Endpoints

Base path: `GET /api/client/bi/filters/{type}`

| Type | Source table | Use in |
|---|---|---|
| `item-groups` | `mart.sales_item_dashboard` | Sales, Inventory |
| `customer-groups` | `mart.dim_customers` | Sales |
| `supplier-groups` | `mart.dim_suppliers` | Purchasing |
| `warehouses` | `mart.inventory_warehouse` | Purchasing, Inventory |
| `salespersons` | `mart.sales_customer_dashboard` | Sales |

## Response shape

```json
{
  "data": [
    { "code": "GROUP-001", "name": "Electronics" },
    { "code": "GROUP-002", "name": "Clothing" }
  ]
}
```

## Company resolution

Company ID is resolved from the JWT or `?companyId=` query param (dev only). The resolved ID maps via `IAnalyticsCompanyResolver.ResolveAsync()` to the staging `company_id` used in MART queries.

## Frontend integration

- Hook file: `databision-frontend/src/client/hooks/useFilterOptions.ts`
- API file: `databision-frontend/src/client/api/nativeBiApi.ts` (getItemGroupOptions, etc.)
- Stale time: 5 minutes (options don't change frequently)

Each hook is registered in the `NativeBiFilterBar` via `optionsByKey` and `loadingKeys`.

## Filter bar integration per module

| Module | Primary filters | Advanced filters |
|---|---|---|
| Sales | date-range, year, salesType | month, salesperson, item group, customer group |
| Purchasing | date-range | supplier groups, warehouses |
| Inventory | warehouses | item groups |
| Finance | year | month |

## MART table availability

Some tables may not exist in early/partial MART setups:
- `dim_customers` — customer group filter returns empty if missing
- `dim_suppliers` — supplier group filter returns empty if missing
- `inventory_warehouse` — warehouse filter returns empty if missing

This is handled gracefully by `SafeQuery()` in `FilterOptionsRepository`.

## Future: salesperson codes

Currently `salesperson_code` may be null in `mart.sales_customer_dashboard`. The query falls back to `salesperson_name` as the code. When `dim_salespersons` is added to the MART, update `GetSalespersonsAsync` to query it instead.
