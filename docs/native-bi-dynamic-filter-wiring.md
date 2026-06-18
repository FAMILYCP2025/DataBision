# Native BI — Dynamic Filter Config Wiring

Sprint 14D — 2026-06-18

---

## Purpose

SuperAdmin can configure which filters appear in the client portal for each tenant. These settings are stored in AppDbContext (`NativeBiFilterConfig`, `NativeBiItemUdfFilterConfig`, `NativeBiDimensionConfig`) and served to the client via a dedicated endpoint.

---

## Endpoint

```
GET /api/client/bi/filter-config
Authorization: Bearer <JWT with company_id>
```

Returns only enabled configs, sorted by display_order:

```json
{
  "data": {
    "filters": [
      { "filterKey": "dateFrom", "label": null, "isEnabled": true, "isAdvanced": false, "displayOrder": 0, "defaultValue": null },
      { "filterKey": "year", "label": "Ejercicio", "isEnabled": true, "isAdvanced": false, "displayOrder": 1, "defaultValue": null }
    ],
    "itemUdfFilters": [],
    "dimensions": []
  }
}
```

**Company resolution:** reads `company_id` claim from JWT (integer app company ID). Does NOT use subdomain resolution. Returns 401 if claim is absent.

---

## Frontend wiring

`useBiFilterConfig` — React Query hook (`queryKey: ['bi-filter-config']`, staleTime 30min, retry false).

`applyFilterConfig(staticDefs, adminFilters)` — merges admin config into static filter definitions:
- Filters disabled by admin are removed
- Labels are overridden if set by admin
- `isAdvanced` flag is overridden
- Filters not in admin config are kept (additive config — admin config doesn't need to enumerate all filters)
- Return is sorted by admin `displayOrder`

**Integration point:** `NativeBiFilterBar` calls `useBiFilterConfig()` internally and applies `applyFilterConfig` to the `definitions` prop before rendering. No page-level changes required.

---

## When config is unavailable

- Endpoint returns 401 if user is not logged in (correct — filter config is per-tenant)
- If `stagingConnectionString` is absent but AppDbContext is available, the endpoint still works (configs live in AppDbContext, not staging)
- If no records exist for the company in `NativeBiFilterConfig`, all static filters are shown as-is (zero config = full fallback)

---

## SuperAdmin configuration path

1. SuperAdmin → Company → Native BI → Filtros
2. Enable/disable each filter key
3. Optionally set label overrides (e.g., "Ejercicio" instead of "Año")
4. Mark filters as advanced (collapsed by default)
5. Set display order

Client portal reflects changes on next page load (staleTime 30 min, or force refetch).
