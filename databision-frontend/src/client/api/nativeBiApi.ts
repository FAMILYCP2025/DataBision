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

async function getTenant(): Promise<string | null> {
  const { useClientAuthStore } = await import('../store/useClientAuthStore')
  return useClientAuthStore.getState().tenant
}

// ── Dashboard ────────────────────────────────────────────────────────────────

export async function getDashboardSummary(): Promise<DashboardSummary> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<DashboardSummary>>(
    `/api/client/dashboard/summary${nbQs({ companyId: tenant })}`
  )
  return data.data
}

export async function getDashboardSalesDaily(days = 30): Promise<SalesDaily[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SalesDaily[]>>(
    `/api/client/dashboard/sales-daily${nbQs({ companyId: tenant, days })}`
  )
  return data.data
}

export async function getDashboardSalesMonthly(months = 12): Promise<SalesMonthly[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SalesMonthly[]>>(
    `/api/client/dashboard/sales-monthly${nbQs({ companyId: tenant, months })}`
  )
  return data.data
}

export async function getDashboardTopCustomers(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<CustomerSales>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<CustomerSales>>(
    `/api/client/dashboard/top-customers${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getDashboardTopItems(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<ItemSales>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<ItemSales>>(
    `/api/client/dashboard/top-items${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getDashboardSalespersons(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<SalespersonSales>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<SalespersonSales>>(
    `/api/client/dashboard/salespersons${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

// ── Sales ────────────────────────────────────────────────────────────────────

export async function getSalesOverview(params: DateRangeParams = {}): Promise<SalesOverview> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SalesOverview>>(
    `/api/client/sales/overview${nbQs({ companyId: tenant, ...params })}`
  )
  return data.data
}

export async function getSalesDaily(params: DateRangeParams = {}): Promise<SalesDaily[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SalesDaily[]>>(
    `/api/client/sales/daily${nbQs({ companyId: tenant, ...params })}`
  )
  return data.data
}

export async function getSalesMonthly(params: DateRangeParams = {}): Promise<SalesMonthly[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SalesMonthly[]>>(
    `/api/client/sales/monthly${nbQs({ companyId: tenant, ...params })}`
  )
  return data.data
}

export async function getSalesCustomers(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<CustomerSales>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<CustomerSales>>(
    `/api/client/sales/customers${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getSalesItems(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<ItemSales>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<ItemSales>>(
    `/api/client/sales/items${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getSalesSalespersons(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<SalespersonSales>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<SalespersonSales>>(
    `/api/client/sales/salespersons${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

// ── Sync ─────────────────────────────────────────────────────────────────────

export async function getSyncStatus(): Promise<SyncStatus> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SyncStatus>>(
    `/api/client/sync/status${nbQs({ companyId: tenant })}`
  )
  return data.data
}

export async function getSyncObjects(): Promise<SyncObjectStatus[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SyncObjectStatus[]>>(
    `/api/client/sync/objects${nbQs({ companyId: tenant })}`
  )
  return data.data
}

export async function getSyncTransformStatus(): Promise<SyncTransformStatus> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SyncTransformStatus>>(
    `/api/client/sync/transform-status${nbQs({ companyId: tenant })}`
  )
  return data.data
}

// ── Diagnostics ──────────────────────────────────────────────────────────────

export async function getNativeBiDiagnostics(): Promise<NativeBiDiagnostics> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<NativeBiDiagnostics>>(
    `/api/client/diagnostics/native-bi${nbQs({ companyId: tenant })}`
  )
  return data.data
}

export async function getNativeBiTableCounts(): Promise<NativeBiTableCounts> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<NativeBiTableCounts>>(
    `/api/client/diagnostics/native-bi/tables${nbQs({ companyId: tenant })}`
  )
  return data.data
}
