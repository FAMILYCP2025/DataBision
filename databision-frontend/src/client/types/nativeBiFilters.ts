// Native BI — Filter type definitions

export type FilterType =
  | 'date-range'
  | 'year'
  | 'month'
  | 'select'
  | 'multi-select'
  | 'toggle'

export type FilterSource =
  | 'static'      // Options defined inline
  | 'endpoint'    // Options fetched from /api/client/bi/filters/*
  | 'config'      // Options from tenant config
  | 'udf'         // Item UDF configured per tenant

export type FilterModule =
  | 'dashboard'
  | 'sales'
  | 'purchasing'
  | 'inventory'
  | 'finance'
  | 'operations'
  | 'diagnostics'

export interface FilterOption {
  value: string
  label: string
}

export interface NativeBiFilterDefinition {
  key: keyof NativeBiFilterState
  label: string
  type: FilterType
  source: FilterSource
  modules: FilterModule[]
  options?: FilterOption[]          // For static source
  endpoint?: string                 // For endpoint source
  placeholder?: string
  isAdvanced?: boolean              // Show in collapsed "more filters" section
  isEnabled?: boolean               // Can be overridden by tenant config
}

// ── Filter State ──────────────────────────────────────────────────────────────

export interface NativeBiFilterState {
  // Date
  dateFrom?: string           // YYYY-MM-DD
  dateTo?: string             // YYYY-MM-DD
  year?: string               // YYYY
  month?: string              // 01..12

  // Sales
  salesType?: 'net' | 'gross' | 'both'
  salespersonCodes?: string   // comma-separated for multi
  customerGroupCodes?: string
  itemGroupCodes?: string

  // Purchasing / Inventory
  supplierGroupCodes?: string
  warehouseCodes?: string
  warehouseLocations?: string

  // Finance
  accountCodes?: string
  accountLevel?: string
  dimension1?: string
  dimension2?: string
  dimension3?: string
  dimension4?: string
  dimension5?: string

  // Operations
  severity?: string           // critical | warning | info
  processCode?: string
  objectCode?: string

  // Diagnostics
  schema?: string
  tableFilter?: string

  // Item UDFs (up to 6, tenant-configured)
  udf1?: string
  udf2?: string
  udf3?: string
  udf4?: string
  udf5?: string
  udf6?: string
}

// ── Tenant Filter Config (from API in Sprint 13, static defaults here) ────────

export interface NativeBiTenantFilterConfig {
  enabledFilters: (keyof NativeBiFilterState)[]
  itemUdfs: ItemUdfFilterConfig[]
  dimensions: DimensionConfig[]
}

export interface ItemUdfFilterConfig {
  key: 'udf1' | 'udf2' | 'udf3' | 'udf4' | 'udf5' | 'udf6'
  label: string
  sourceField: string         // MART column name e.g. "u_marca"
  modules: FilterModule[]
  isEnabled: boolean
  displayOrder: number
}

export interface DimensionConfig {
  code: 1 | 2 | 3 | 4 | 5
  label: string
  isEnabled: boolean
  modules: FilterModule[]
}

// ── Default filter config (used when no tenant config exists) ─────────────────

export const DEFAULT_FILTER_CONFIG: NativeBiTenantFilterConfig = {
  enabledFilters: [
    'dateFrom', 'dateTo', 'year', 'month',
    'salesType', 'salespersonCodes', 'customerGroupCodes', 'itemGroupCodes',
    'supplierGroupCodes', 'warehouseCodes',
  ],
  itemUdfs: [],
  dimensions: [],
}
