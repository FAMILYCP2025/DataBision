import api from '../../lib/api'
import { nbQs } from './nativeBiClient'
import type { NbApiResponse, NbPagedApiResponse, PaginationParams } from '../types/nativeBi'
import type {
  SalesCustomerDashboard,
  SalesItemDashboard,
  SalesFulfillment,
  SalesItemGroupSummary,
  SalesWarehouseSummary,
  PurchasingExecutive,
  PurchasingSupplier,
  PurchasingReceiving,
  InventoryRotation,
  InventoryStock,
  InventoryWarehouse,
  FinanceExecutive,
  FinanceArAging,
  FinanceApAging,
  IncomeStatementPeriod,
  BalanceSheetSnapshot,
  EbitdaPeriod,
  ChartOfAccountEntry,
  OperationHealth,
  OperationAlert,
  OperationDataQuality,
} from '../types/processBi'

async function getTenant(): Promise<string | null> {
  const { useClientAuthStore } = await import('../store/useClientAuthStore')
  return useClientAuthStore.getState().tenant
}

// ── Sales process ─────────────────────────────────────────────────────────────

export async function getBiSalesCustomers(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<SalesCustomerDashboard>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<SalesCustomerDashboard>>(
    `/client/bi/sales/customers-dashboard${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getBiSalesItems(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<SalesItemDashboard>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<SalesItemDashboard>>(
    `/client/bi/sales/items-dashboard${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getBiSalesFulfillment(
  days = 30
): Promise<SalesFulfillment[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SalesFulfillment[]>>(
    `/client/bi/sales/fulfillment${nbQs({ companyId: tenant, days })}`
  )
  return data.data
}

export async function getSalesItemGroupSummary(
  params: Record<string, string | undefined> = {}
): Promise<SalesItemGroupSummary[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SalesItemGroupSummary[]>>(
    `/client/bi/sales/item-groups${nbQs({ companyId: tenant ?? undefined, ...params })}`
  )
  return data.data
}

export async function getSalesWarehouseSummary(
  params: Record<string, string | undefined> = {}
): Promise<SalesWarehouseSummary[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SalesWarehouseSummary[]>>(
    `/client/bi/sales/warehouses${nbQs({ companyId: tenant ?? undefined, ...params })}`
  )
  return data.data
}

// ── Purchasing process ────────────────────────────────────────────────────────

export async function getBiPurchasingExecutive(
  days = 30
): Promise<PurchasingExecutive[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<PurchasingExecutive[]>>(
    `/client/bi/purchasing/executive${nbQs({ companyId: tenant, days })}`
  )
  return data.data
}

export async function getBiPurchasingSuppliers(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<PurchasingSupplier>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<PurchasingSupplier>>(
    `/client/bi/purchasing/suppliers${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getBiPurchasingReceiving(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<PurchasingReceiving>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<PurchasingReceiving>>(
    `/client/bi/purchasing/receiving${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

// ── Inventory process ─────────────────────────────────────────────────────────

export async function getBiInventoryRotation(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<InventoryRotation>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<InventoryRotation>>(
    `/client/bi/inventory/rotation${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getBiInventoryStock(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<InventoryStock>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<InventoryStock>>(
    `/client/bi/inventory/stock${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getBiInventoryWarehouses(): Promise<InventoryWarehouse[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<InventoryWarehouse[]>>(
    `/client/bi/inventory/warehouses${nbQs({ companyId: tenant })}`
  )
  return data.data
}

// ── Finance process ───────────────────────────────────────────────────────────

export async function getBiFinanceExecutive(
  days = 30
): Promise<FinanceExecutive[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<FinanceExecutive[]>>(
    `/client/bi/finance/executive${nbQs({ companyId: tenant, days })}`
  )
  return data.data
}

export async function getBiFinanceArAging(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<FinanceArAging>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<FinanceArAging>>(
    `/client/bi/finance/ar-aging${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getBiFinanceApAging(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<FinanceApAging>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<FinanceApAging>>(
    `/client/bi/finance/ap-aging${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

// ── Finance accounting (Sprint 13C–13E) ─────────────────────────────────────

export async function getBiIncomeStatement(
  params: { year?: number; month?: number } = {}
): Promise<IncomeStatementPeriod[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<IncomeStatementPeriod[]>>(
    `/client/bi/finance/income-statement${nbQs({ companyId: tenant, ...params })}`
  )
  return data.data
}

export async function getBiBalanceSheet(
  snapshotDate?: string
): Promise<BalanceSheetSnapshot[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<BalanceSheetSnapshot[]>>(
    `/client/bi/finance/balance-sheet${nbQs({ companyId: tenant, snapshotDate })}`
  )
  return data.data
}

export async function getBiEbitda(
  months = 12
): Promise<EbitdaPeriod[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<EbitdaPeriod[]>>(
    `/client/bi/finance/ebitda${nbQs({ companyId: tenant, months })}`
  )
  return data.data
}

export async function getBiChartOfAccounts(
  postableOnly = false
): Promise<ChartOfAccountEntry[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<ChartOfAccountEntry[]>>(
    `/client/bi/finance/chart-of-accounts${nbQs({ companyId: tenant, postableOnly })}`
  )
  return data.data
}

// ── Operations process ────────────────────────────────────────────────────────

export async function getBiOperationsPipelineHealth(): Promise<OperationHealth | null> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<OperationHealth | null>>(
    `/client/bi/operations/pipeline-health${nbQs({ companyId: tenant })}`
  )
  return data.data
}

export async function getBiOperationsAlerts(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<OperationAlert>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<OperationAlert>>(
    `/client/bi/operations/alerts${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getBiOperationsDataQuality(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<OperationDataQuality>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<OperationDataQuality>>(
    `/client/bi/operations/data-quality${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}
