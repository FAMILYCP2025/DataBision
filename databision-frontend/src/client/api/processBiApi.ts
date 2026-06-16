import api from '../../lib/api'
import { nbQs } from './nativeBiClient'
import type { NbApiResponse, NbPagedApiResponse, PaginationParams } from '../types/nativeBi'
import type {
  SalesCustomerDashboard,
  SalesItemDashboard,
  SalesFulfillment,
  PurchasingExecutive,
  PurchasingSupplier,
  PurchasingReceiving,
  InventoryRotation,
  InventoryStock,
  InventoryWarehouse,
  FinanceExecutive,
  FinanceArAging,
  FinanceApAging,
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
    `/api/client/bi/sales/customers-dashboard${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getBiSalesItems(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<SalesItemDashboard>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<SalesItemDashboard>>(
    `/api/client/bi/sales/items-dashboard${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getBiSalesFulfillment(
  days = 30
): Promise<SalesFulfillment[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<SalesFulfillment[]>>(
    `/api/client/bi/sales/fulfillment${nbQs({ companyId: tenant, days })}`
  )
  return data.data
}

// ── Purchasing process ────────────────────────────────────────────────────────

export async function getBiPurchasingExecutive(
  days = 30
): Promise<PurchasingExecutive[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<PurchasingExecutive[]>>(
    `/api/client/bi/purchasing/executive${nbQs({ companyId: tenant, days })}`
  )
  return data.data
}

export async function getBiPurchasingSuppliers(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<PurchasingSupplier>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<PurchasingSupplier>>(
    `/api/client/bi/purchasing/suppliers${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getBiPurchasingReceiving(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<PurchasingReceiving>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<PurchasingReceiving>>(
    `/api/client/bi/purchasing/receiving${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

// ── Inventory process ─────────────────────────────────────────────────────────

export async function getBiInventoryRotation(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<InventoryRotation>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<InventoryRotation>>(
    `/api/client/bi/inventory/rotation${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getBiInventoryStock(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<InventoryStock>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<InventoryStock>>(
    `/api/client/bi/inventory/stock${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getBiInventoryWarehouses(): Promise<InventoryWarehouse[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<InventoryWarehouse[]>>(
    `/api/client/bi/inventory/warehouses${nbQs({ companyId: tenant })}`
  )
  return data.data
}

// ── Finance process ───────────────────────────────────────────────────────────

export async function getBiFinanceExecutive(
  days = 30
): Promise<FinanceExecutive[]> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<FinanceExecutive[]>>(
    `/api/client/bi/finance/executive${nbQs({ companyId: tenant, days })}`
  )
  return data.data
}

export async function getBiFinanceArAging(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<FinanceArAging>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<FinanceArAging>>(
    `/api/client/bi/finance/ar-aging${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getBiFinanceApAging(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<FinanceApAging>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<FinanceApAging>>(
    `/api/client/bi/finance/ap-aging${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

// ── Operations process ────────────────────────────────────────────────────────

export async function getBiOperationsPipelineHealth(): Promise<OperationHealth | null> {
  const tenant = await getTenant()
  const { data } = await api.get<NbApiResponse<OperationHealth | null>>(
    `/api/client/bi/operations/pipeline-health${nbQs({ companyId: tenant })}`
  )
  return data.data
}

export async function getBiOperationsAlerts(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<OperationAlert>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<OperationAlert>>(
    `/api/client/bi/operations/alerts${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}

export async function getBiOperationsDataQuality(
  params: PaginationParams = {}
): Promise<NbPagedApiResponse<OperationDataQuality>> {
  const tenant = await getTenant()
  const { data } = await api.get<NbPagedApiResponse<OperationDataQuality>>(
    `/api/client/bi/operations/data-quality${nbQs({ companyId: tenant, ...params })}`
  )
  return data
}
