import type { NativeBiFilterState, FilterOption } from '../types/nativeBiFilters'

// Convert filter state to flat query params (undefined/null/'' are omitted by nbQs)
export function filterStateToParams(
  filters: NativeBiFilterState
): Record<string, string | undefined> {
  const params: Record<string, string | undefined> = {}

  const set = (k: string, v: string | undefined) => {
    if (v && v.trim() !== '') params[k] = v
  }

  set('dateFrom', filters.dateFrom)
  set('dateTo', filters.dateTo)
  set('year', filters.year)
  set('month', filters.month)
  set('salesType', filters.salesType)
  set('salespersonCodes', filters.salespersonCodes)
  set('customerGroupCodes', filters.customerGroupCodes)
  set('itemGroupCodes', filters.itemGroupCodes)
  set('supplierGroupCodes', filters.supplierGroupCodes)
  set('warehouseCodes', filters.warehouseCodes)
  set('warehouseLocations', filters.warehouseLocations)
  set('accountCodes', filters.accountCodes)
  set('accountLevel', filters.accountLevel)
  set('dimension1', filters.dimension1)
  set('dimension2', filters.dimension2)
  set('dimension3', filters.dimension3)
  set('dimension4', filters.dimension4)
  set('dimension5', filters.dimension5)
  set('severity', filters.severity)
  set('processCode', filters.processCode)
  set('objectCode', filters.objectCode)
  set('schema', filters.schema)
  set('tableFilter', filters.tableFilter)
  set('udf1', filters.udf1)
  set('udf2', filters.udf2)
  set('udf3', filters.udf3)
  set('udf4', filters.udf4)
  set('udf5', filters.udf5)
  set('udf6', filters.udf6)

  return params
}

// Count how many filters are active (non-empty)
export function countActiveFilters(filters: NativeBiFilterState): number {
  return Object.values(filterStateToParams(filters)).filter(Boolean).length
}

// Build year options: current year back to 2020
export function yearOptions(): FilterOption[] {
  const now = new Date().getFullYear()
  return Array.from({ length: now - 2019 }, (_, i) => {
    const y = String(now - i)
    return { value: y, label: y }
  })
}

// Month options
export const MONTH_OPTIONS: FilterOption[] = [
  { value: '01', label: 'Enero' },
  { value: '02', label: 'Febrero' },
  { value: '03', label: 'Marzo' },
  { value: '04', label: 'Abril' },
  { value: '05', label: 'Mayo' },
  { value: '06', label: 'Junio' },
  { value: '07', label: 'Julio' },
  { value: '08', label: 'Agosto' },
  { value: '09', label: 'Septiembre' },
  { value: '10', label: 'Octubre' },
  { value: '11', label: 'Noviembre' },
  { value: '12', label: 'Diciembre' },
]

// SalesType options
export const SALES_TYPE_OPTIONS: FilterOption[] = [
  { value: 'gross', label: 'Bruta' },
  { value: 'net',   label: 'Neta' },
  { value: 'both',  label: 'Ambas' },
]

// Severity options for Operations
export const SEVERITY_OPTIONS: FilterOption[] = [
  { value: 'critical', label: 'Crítico' },
  { value: 'warning',  label: 'Advertencia' },
  { value: 'info',     label: 'Información' },
]

// Convert dateFrom/dateTo into a year's date range (Jan 1 – Dec 31)
export function yearToDateRange(year: string): { dateFrom: string; dateTo: string } {
  return {
    dateFrom: `${year}-01-01`,
    dateTo:   `${year}-12-31`,
  }
}

// Derive dateFrom/dateTo from year+month selection
export function periodToDateRange(
  year?: string,
  month?: string
): { dateFrom?: string; dateTo?: string } {
  if (!year) return {}
  if (!month) return yearToDateRange(year)

  const lastDay = new Date(Number(year), Number(month), 0).getDate()
  return {
    dateFrom: `${year}-${month}-01`,
    dateTo:   `${year}-${month}-${String(lastDay).padStart(2, '0')}`,
  }
}

// Quick-period presets
export type QuickPeriod = 'this-month' | 'last-month' | 'this-year' | 'last-year' | 'last-30' | 'last-90'

export function quickPeriodToRange(preset: QuickPeriod): { dateFrom: string; dateTo: string } {
  const now   = new Date()
  const today = now.toISOString().slice(0, 10)

  const fmt = (d: Date) => d.toISOString().slice(0, 10)
  const sub = (d: Date, days: number) => { const r = new Date(d); r.setDate(r.getDate() - days); return r }

  switch (preset) {
    case 'last-30':
      return { dateFrom: fmt(sub(now, 30)), dateTo: today }
    case 'last-90':
      return { dateFrom: fmt(sub(now, 90)), dateTo: today }
    case 'this-month': {
      const f = new Date(now.getFullYear(), now.getMonth(), 1)
      return { dateFrom: fmt(f), dateTo: today }
    }
    case 'last-month': {
      const f = new Date(now.getFullYear(), now.getMonth() - 1, 1)
      const t = new Date(now.getFullYear(), now.getMonth(), 0)
      return { dateFrom: fmt(f), dateTo: fmt(t) }
    }
    case 'this-year': {
      return { dateFrom: `${now.getFullYear()}-01-01`, dateTo: today }
    }
    case 'last-year': {
      const y = now.getFullYear() - 1
      return { dateFrom: `${y}-01-01`, dateTo: `${y}-12-31` }
    }
  }
}
