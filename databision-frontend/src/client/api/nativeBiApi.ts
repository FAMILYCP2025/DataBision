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
  SalesMartKpiSummary,
  SalesPeriodKpi,
  TopCustomerMart,
  TopItemMart,
  TopSalespersonMart,
  OpenSalesOrderMart,
  PurchaseMartKpiSummary,
  PurchasePeriodKpi,
  TopSupplierMart,
  TopPurchaseItemMart,
  OpenPurchaseOrderMart,
  InventoryMartKpiSummary,
  InventorySnapshotItem,
  InventoryMovementKpi,
  SlowMovingItem,
  WarehouseStock,
  FinanceMartSummary,
  ArAgingRow,
  ApAgingRow,
  FinancePeriodKpi,
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

// ── Sprint 3 — Sales MART ─────────────────────────────────────────────────────

export async function getSalesMartKpi(): Promise<SalesMartKpiSummary> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SalesMartKpiSummary>>(
    `/client/bi/sales/mart/kpi${nbQs({ companyId: tenant })}`
  )
  return data.data
}

export async function getSalesMartByPeriod(months = 12): Promise<SalesPeriodKpi[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SalesPeriodKpi[]>>(
    `/client/bi/sales/mart/by-period${nbQs({ companyId: tenant, months })}`
  )
  return data.data
}

export async function getSalesMartTopCustomers(limit = 10): Promise<TopCustomerMart[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<TopCustomerMart[]>>(
    `/client/bi/sales/mart/top-customers${nbQs({ companyId: tenant, limit })}`
  )
  return data.data
}

export async function getSalesMartTopItems(limit = 10): Promise<TopItemMart[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<TopItemMart[]>>(
    `/client/bi/sales/mart/top-items${nbQs({ companyId: tenant, limit })}`
  )
  return data.data
}

export async function getSalesMartTopSalespersons(): Promise<TopSalespersonMart[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<TopSalespersonMart[]>>(
    `/client/bi/sales/mart/top-salespersons${nbQs({ companyId: tenant })}`
  )
  return data.data
}

export async function getSalesMartOpenOrders(overdueOnly = false): Promise<OpenSalesOrderMart[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<OpenSalesOrderMart[]>>(
    `/client/bi/sales/mart/open-orders${nbQs({ companyId: tenant, overdueOnly })}`
  )
  return data.data
}

// ── Sprint 4 — Purchase MART API ─────────────────────────────────────────────

export async function getPurchaseMartKpi(): Promise<PurchaseMartKpiSummary> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<PurchaseMartKpiSummary>>(
    `/client/bi/purchase/mart/kpi${nbQs({ companyId: tenant })}`
  )
  return data.data
}

export async function getPurchaseMartByPeriod(months = 12): Promise<PurchasePeriodKpi[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<PurchasePeriodKpi[]>>(
    `/client/bi/purchase/mart/by-period${nbQs({ companyId: tenant, months })}`
  )
  return data.data
}

export async function getPurchaseMartTopSuppliers(limit = 10): Promise<TopSupplierMart[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<TopSupplierMart[]>>(
    `/client/bi/purchase/mart/top-suppliers${nbQs({ companyId: tenant, limit })}`
  )
  return data.data
}

export async function getPurchaseMartTopItems(limit = 10): Promise<TopPurchaseItemMart[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<TopPurchaseItemMart[]>>(
    `/client/bi/purchase/mart/top-items${nbQs({ companyId: tenant, limit })}`
  )
  return data.data
}

export async function getPurchaseMartOpenOrders(overdueOnly = false): Promise<OpenPurchaseOrderMart[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<OpenPurchaseOrderMart[]>>(
    `/client/bi/purchase/mart/open-orders${nbQs({ companyId: tenant, overdueOnly })}`
  )
  return data.data
}

// ── Sprint 5 — Inventory MART API ────────────────────────────────────────────

export async function getInventoryMartKpi(): Promise<InventoryMartKpiSummary> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<InventoryMartKpiSummary>>(
    `/client/bi/inventory/mart/kpi${nbQs({ companyId: tenant })}`
  )
  return data.data
}

export async function getInventoryMartSnapshot(limit = 50): Promise<InventorySnapshotItem[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<InventorySnapshotItem[]>>(
    `/client/bi/inventory/mart/snapshot${nbQs({ companyId: tenant, limit })}`
  )
  return data.data
}

export async function getInventoryMartMovement(months = 12): Promise<InventoryMovementKpi[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<InventoryMovementKpi[]>>(
    `/client/bi/inventory/mart/movement${nbQs({ companyId: tenant, months })}`
  )
  return data.data
}

export async function getInventoryMartSlowMoving(minDays = 90): Promise<SlowMovingItem[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SlowMovingItem[]>>(
    `/client/bi/inventory/mart/slow-moving${nbQs({ companyId: tenant, minDays })}`
  )
  return data.data
}

export async function getInventoryMartWarehouses(): Promise<WarehouseStock[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<WarehouseStock[]>>(
    `/client/bi/inventory/mart/warehouses${nbQs({ companyId: tenant })}`
  )
  return data.data
}

// ── Sprint 6 — Finance MART API ───────────────────────────────────────────────

export async function getFinanceMartSummary(): Promise<FinanceMartSummary> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<FinanceMartSummary>>(
    `/client/bi/finance/mart/summary${nbQs({ companyId: tenant })}`
  )
  return data.data
}

export async function getFinanceMartArAging(limit = 50): Promise<ArAgingRow[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<ArAgingRow[]>>(
    `/client/bi/finance/mart/ar-aging${nbQs({ companyId: tenant, limit })}`
  )
  return data.data
}

export async function getFinanceMartApAging(limit = 50): Promise<ApAgingRow[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<ApAgingRow[]>>(
    `/client/bi/finance/mart/ap-aging${nbQs({ companyId: tenant, limit })}`
  )
  return data.data
}

export async function getFinanceMartPeriodKpi(months = 12): Promise<FinancePeriodKpi[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<FinancePeriodKpi[]>>(
    `/client/bi/finance/mart/period-kpi${nbQs({ companyId: tenant, months })}`
  )
  return data.data
}
