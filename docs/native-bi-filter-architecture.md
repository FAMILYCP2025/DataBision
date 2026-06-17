# Native BI — Filter Architecture

## Overview

The Native BI filter system is a forward-compatible, modular filter bar designed to work across all analytical modules (Sales, Purchasing, Inventory, Finance, Operations, Diagnostics). Filters are metadata-driven — each filter is a definition object that describes its type, source, and which modules use it.

## Core concepts

### Filter definition (`NativeBiFilterDefinition`)

A filter definition describes a single filter control:

```typescript
interface NativeBiFilterDefinition {
  key: keyof NativeBiFilterState   // which state key this filter controls
  label: string                    // display label
  type: FilterType                 // 'date-range' | 'year' | 'month' | 'select' | 'multi-select' | 'toggle'
  source: FilterSource             // 'static' | 'endpoint' | 'config' | 'udf'
  modules: FilterModule[]          // which pages this filter applies to
  options?: FilterOption[]         // for source='static'
  endpoint?: string                // for source='endpoint' (handled by useFilterOptions hooks)
  placeholder?: string
  isAdvanced?: boolean             // hidden behind "Más filtros" toggle
  isEnabled?: boolean              // can disable without removing definition
}
```

### Filter state (`NativeBiFilterState`)

Flat object of all possible filter values across all modules:

```typescript
interface NativeBiFilterState {
  dateFrom?: string; dateTo?: string       // date range
  year?: string; month?: string            // period selectors
  salesType?: 'net' | 'gross' | 'both'
  salespersonCodes?: string                // comma-separated codes
  customerGroupCodes?: string
  itemGroupCodes?: string
  supplierGroupCodes?: string
  warehouseCodes?: string
  // ... dimensions, UDFs for Sprint 12-13
}
```

### `useNativeBiFilters` hook

```typescript
const { filters, setFilter, resetFilter, resetAll, params, hasActiveFilters }
  = useNativeBiFilters(module, initialState)
```

- `filters` — current state
- `setFilter(key, value)` — set a single filter value
- `resetFilter(key)` — remove a single filter
- `resetAll()` — clear all filters
- `params` — flat `Record<string, string | undefined>` ready for `nbQs()`
- `hasActiveFilters` — true if any filter is set

### `NativeBiFilterBar` component

Renders the filter bar with:
1. **Primary row** — always-visible filters
2. **Advanced row** — collapsed by default, shown via "Más filtros" button
3. **Active chips** — removable badges for active filters

Props:
- `filters` — current `NativeBiFilterState`
- `definitions` — array of `NativeBiFilterDefinition`
- `optionsByKey` — pre-fetched options for `source='endpoint'` filters
- `loadingKeys` — set of keys currently loading options
- `onFilterChange`, `onFilterReset`, `onResetAll` — callbacks
- `hasActiveFilters` — shows "Limpiar" button

## Data flow

```
NativeBiFilterBar
  ↓ user selects filter
useNativeBiFilters.setFilter(key, value)
  ↓ filter state updates
filterStateToParams(filters) → params: Record<string, string | undefined>
  ↓ passed to API calls
nbQs({ companyId, ...params }) → querystring
  ↓ HTTP GET
Backend controller → NativeBiFilterDto → (wired in Sprint 12+)
```

## Filter options flow

```
useFilterOptions.ts (useItemGroupOptions, etc.)
  → TanStack Query (stale: 5min)
  → nativeBiApi.getFilterOptions('item-groups')
  → GET /api/client/bi/filters/item-groups
  → FilterOptionsRepository.GetItemGroupsAsync(companyId)
  → mart.sales_item_dashboard (DISTINCT)
  → FilterOptionDto[] → FilterOption[]
```

## Files

| File | Purpose |
|---|---|
| `src/client/types/nativeBiFilters.ts` | Type definitions |
| `src/client/utils/nativeBiFilterUtils.ts` | filterStateToParams, yearOptions, MONTH_OPTIONS, etc. |
| `src/client/hooks/useNativeBiFilters.ts` | Filter state management hook |
| `src/client/hooks/useFilterOptions.ts` | TanStack Query hooks for option fetching |
| `src/client/api/nativeBiApi.ts` | getFilterOptions API functions |
| `src/client/components/nativebi/NativeBiFilterBar.tsx` | Filter bar component |
| `src/client/components/nativebi/NativeBiFilterSelect.tsx` | Select control |
| `src/client/components/nativebi/NativeBiFilterChip.tsx` | Active filter chip |
| `src/DataBision.Application/DTOs/Dashboard/NativeBiFilterDto.cs` | Backend DTO |
| `src/DataBision.Application/DTOs/Dashboard/FilterOptionDto.cs` | Option DTO |
| `src/DataBision.Application/Interfaces/Dashboard/IFilterOptionsRepository.cs` | Interface |
| `src/DataBision.Application/Services/Dashboard/FilterOptionsService.cs` | Service |
| `src/DataBision.Infrastructure/Repositories/Dashboard/FilterOptionsRepository.cs` | Dapper queries |
| `src/DataBision.Api/Controllers/ClientBiFilterOptionsController.cs` | API endpoints |

## Sprint roadmap

| Sprint | Feature |
|---|---|
| 10 | Types, hooks, components, backend DTO (done) |
| 11 | Filter options endpoints, visual integration in all pages (done) |
| 12 | Year/month quick selectors, salesType, dimension config |
| 13 | SuperAdmin configurable filters, Item UDF filters |

## Design constraints

- **No new dependencies** — uses existing TanStack Query, Zustand, React
- **Resilient** — MART tables may not exist; SafeQuery returns empty lists
- **Forward compat** — filter params sent in querystring; backend ignores until wired up
- **Tenant-isolated** — all queries include `company_id` from JWT
- **No hardcoded per-tenant filters** — filter definitions are configurable per tenant (Sprint 13)
