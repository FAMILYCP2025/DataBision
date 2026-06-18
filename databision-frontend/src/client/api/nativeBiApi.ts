import api from '../../lib/api'
import { nbQs } from './nativeBiClient'
import type {
  NbApiResponse,
  NbPagedApiResponse,
  DashboardSummary,
  SalesDaily,
  SalesMonthly,
  CustomerSales,
  ItemSales,
  SalespersonSales,
  SalesOverview,
  SyncStatus,
  SyncObjectStatus,
  SyncTransformStatus,
  NativeBiDiagnostics,
  NativeBiTableCounts,
  PaginationParams,
  DateRangeParams,
} from '../types/nativeBi'
import type { FilterOption } from '../types/nativeBiFilters'

async function getTenant(): Promise<string | null> {
  const { useClientAuthStore } = await import('../store/useClientAuthStore')
  return useClientAuthStore.getState().tenant
}

// ── Dashboard ────────────────────────────────────────────────────────────────

export async function getDashboardSummary(): Promise<DashboardSummary> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<DashboardSummary>>(
    `/client/dashboard/summary${nbQs({ companyId: tenant })}`
  )
  return data.data
}

export async function getDashboardSalesDaily(days = 30): Promise<SalesDaily[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SalesDaily[]>>(
    `/client/dashboard/sales-daily${nbQs({ companyId: tenant, days })}`
  )
  return data.data
}

export async function getDashboardSalesMonthly(months = 12): Promise<SalesMonthly[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SalesMonthly[]>>(
    `/client/dashboard/sales-monthly${nbQs({ companyId: tenant, months })}`
  )
  return data.data
}

export async function getDashboardTopCustomers(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<CustomerSales>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<CustomerSales>>(
    `/client/dashboard/top-customers${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getDashboardTopItems(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<ItemSales>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<ItemSales>>(
    `/client/dashboard/top-items${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getDashboardSalespersons(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<SalespersonSales>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<SalespersonSales>>(
    `/client/dashboard/salespersons${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

// ── Sales ────────────────────────────────────────────────────────────────────

export async function getSalesOverview(params: DateRangeParams = {}): Promise<SalesOverview> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SalesOverview>>(
    `/client/sales/overview${nbQs({ companyId: tenant, ...params })}`
  )
  return data.data
}

export async function getSalesDaily(params: DateRangeParams = {}): Promise<SalesDaily[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SalesDaily[]>>(
    `/client/sales/daily${nbQs({ companyId: tenant, ...params })}`
  )
  return data.data
}

export async function getSalesMonthly(params: DateRangeParams = {}): Promise<SalesMonthly[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SalesMonthly[]>>(
    `/client/sales/monthly${nbQs({ companyId: tenant, ...params })}`
  )
  return data.data
}

export async function getSalesCustomers(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<CustomerSales>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<CustomerSales>>(
    `/client/sales/customers${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getSalesItems(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<ItemSales>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<ItemSales>>(
    `/client/sales/items${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getSalesSalespersons(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<SalespersonSales>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<SalespersonSales>>(
    `/client/sales/salespersons${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

// ── Sync ─────────────────────────────────────────────────────────────────────

export async function getSyncStatus(): Promise<SyncStatus> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SyncStatus>>(
    `/client/sync/status${nbQs({ companyId: tenant })}`
  )
  return data.data
}

export async function getSyncObjects(): Promise<SyncObjectStatus[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SyncObjectStatus[]>>(
    `/client/sync/objects${nbQs({ companyId: tenant })}`
  )
  return data.data
}

export async function getSyncTransformStatus(): Promise<SyncTransformStatus> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SyncTransformStatus>>(
    `/client/sync/transform-status${nbQs({ companyId: tenant })}`
  )
  return data.data
}

// ── Diagnostics ──────────────────────────────────────────────────────────────

export async function getNativeBiDiagnostics(): Promise<NativeBiDiagnostics> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<NativeBiDiagnostics>>(
    `/client/diagnostics/native-bi${nbQs({ companyId: tenant })}`
  )
  return data.data
}

export async function getNativeBiTableCounts(): Promise<NativeBiTableCounts> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<NativeBiTableCounts>>(
    `/client/diagnostics/native-bi/tables${nbQs({ companyId: tenant })}`
  )
  return data.data
}

// ── Filter Options ────────────────────────────────────────────────────────────

type FilterOptionDto = { code: string; name: string }

type FilterOptionsEndpoint =
  | 'item-groups'
  | 'customer-groups'
  | 'supplier-groups'
  | 'warehouses'
  | 'salespersons'

async function getFilterOptions(type: FilterOptionsEndpoint): Promise<FilterOption[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<FilterOptionDto[]>>(
    `/client/bi/filters/${type}${nbQs({ companyId: tenant })}`
  )
  return data.data.map(d => ({ value: d.code, label: d.name }))
}

export const getItemGroupOptions     = () => getFilterOptions('item-groups')
export const getCustomerGroupOptions = () => getFilterOptions('customer-groups')
export const getSupplierGroupOptions = () => getFilterOptions('supplier-groups')
export const getWarehouseOptions     = () => getFilterOptions('warehouses')
export const getSalespersonOptions   = () => getFilterOptions('salespersons')

// ── Native BI client filter config (Sprint 14D) ───────────────────────────────

export interface BiFilterConfigItem {
  filterKey: string
  label: string | null
  isEnabled: boolean
  isAdvanced: boolean
  displayOrder: number
  defaultValue: string | null
}

export interface BiItemUdfFilterConfigItem {
  udfFieldName: string
  label: string | null
  isEnabled: boolean
  isMultiSelect: boolean
  displayOrder: number
}

export interface BiDimensionConfigItem {
  dimensionNumber: number
  label: string | null
  isEnabled: boolean
}

export interface BiClientFilterConfig {
  filters: BiFilterConfigItem[]
  itemUdfFilters: BiItemUdfFilterConfigItem[]
  dimensions: BiDimensionConfigItem[]
}

export async function getBiFilterConfig(): Promise<BiClientFilterConfig> {
  const { data } = await api.get<{ data: BiClientFilterConfig }>('/client/bi/filter-config')
  return data.data
}
