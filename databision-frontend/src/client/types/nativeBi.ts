// Native BI — TypeScript type definitions (Sprint 6A–6L backend contract)

export interface NbApiResponse<T> {
  data: T
  traceId: string
}

export interface NbPagedApiResponse<T> {
  data: T[]
  meta: NbPagedMeta
  traceId: string
}

export interface NbPagedMeta {
  limit: number
  offset: number
  count: number
  hasMore: boolean
}

export interface NbApiErrorResponse {
  error: string
  message: string
  traceId: string
  details?: Record<string, string[]>
}

// ── Dashboard ────────────────────────────────────────────────────────────────

export interface DashboardSummary {
  companyId: string
  grossSalesAmount: number
  creditMemoAmount: number
  netSalesAmount: number
  invoiceCount: number
  creditMemoCount: number
  activeCustomers: number
  activeItems: number
  avgTicketAmount: number
  lastInvoiceDate: string | null        // ISO date "YYYY-MM-DD"
  lastCreditMemoDate: string | null
  lastSyncAtUtc: string | null          // ISO datetime UTC
  transformedAtUtc: string | null
}

export interface SalesDaily {
  salesDate: string                     // "YYYY-MM-DD"
  grossSalesAmount: number
  creditMemoAmount: number
  netSalesAmount: number
  invoiceCount: number
  creditMemoCount: number
  activeCustomers: number
  avgTicketAmount: number
}

export interface SalesMonthly {
  salesMonth: string                    // "YYYY-MM-01" (first day of month)
  grossSalesAmount: number
  creditMemoAmount: number
  netSalesAmount: number
  invoiceCount: number
  creditMemoCount: number
  activeCustomers: number
  avgTicketAmount: number
}

export interface CustomerSales {
  cardCode: string
  cardName: string
  salesAmount: number
  creditMemoAmount: number
  netSalesAmount: number
  invoiceCount: number
  creditMemoCount: number
  lastInvoiceDate: string | null
  firstInvoiceDate: string | null
  avgTicketAmount: number
}

export interface ItemSales {
  itemCode: string
  itemName: string
  quantitySold: number
  grossSalesAmount: number
  lineCount: number
  invoiceCount: number
  lastSaleDate: string | null
}

export interface SalespersonSales {
  salesPersonCode: string
  salesPersonName: string
  salesAmount: number
  creditMemoAmount: number
  netSalesAmount: number
  invoiceCount: number
  creditMemoCount: number
  activeCustomers: number
  avgTicketAmount: number
}

// ── Sales ────────────────────────────────────────────────────────────────────

export interface SalesOverview {
  grossSalesAmount: number
  creditMemoAmount: number
  netSalesAmount: number
  invoiceCount: number
  creditMemoCount: number
  avgTicketAmount: number
  activeCustomers: number
  dateFrom: string
  dateTo: string
}

// ── Sync ─────────────────────────────────────────────────────────────────────

export type SyncStatusLevel = 'ok' | 'warning' | 'error' | 'unknown'

export interface SyncStatus {
  companyId: string
  overallStatus: SyncStatusLevel
  lastSyncAtUtc: string | null
  lastTransformAtUtc: string | null
  objects: SyncObjectStatus[]
  dataFreshness: DataFreshness
}

export interface SyncObjectStatus {
  sapObject: string                     // "OINV" | "INV1" | "ORIN" | "RIN1" | "OCRD" | "OITM" | "OSLP"
  watermarkDate: string | null
  lastSuccessfulRunUtc: string | null
  totalRowsIngested: number
  status: 'ok' | 'warning' | 'no_data'
}

export interface DataFreshness {
  rawLastUpdatedAtUtc: string | null
  stgLastTransformedAtUtc: string | null
  martLastTransformedAtUtc: string | null
}

export interface SyncTransformStatus {
  companyId: string
  martTransformedAtUtc: string | null
  stgTransformedAtUtc: string | null
  martTables: MartTableStatus[]
}

export interface MartTableStatus {
  tableName: string
  rowCount: number
  transformedAtUtc: string | null
}

// ── Diagnostics ──────────────────────────────────────────────────────────────

export interface NativeBiDiagnostics {
  companyId: string
  status: SyncStatusLevel
  checks: DiagnosticCheck[]
  generatedAtUtc: string
}

export interface DiagnosticCheck {
  name: string                          // "staging_connection" | "mart_data_freshness" | etc.
  status: SyncStatusLevel
  detail: string | null
}

export interface NativeBiTableCounts {
  companyId: string
  tables: TableCount[]
  generatedAtUtc: string
}

export interface TableCount {
  schema: string                        // "stg" | "mart"
  tableName: string
  rowCount: number
  transformedAtUtc: string | null
}

// ── Query param types ─────────────────────────────────────────────────────────

export interface PaginationParams {
  limit?: number
  offset?: number
  sortBy?: string
  sortDir?: 'asc' | 'desc'
}

export interface DateRangeParams {
  dateFrom?: string                     // "YYYY-MM-DD"
  dateTo?: string
}

// ── Sprint 3 — Sales MART types ──────────────────────────────────────────────

export interface SalesMartKpiSummary {
  netSalesLtm: number
  netSalesPrevLtm: number
  growthPct: number
  avgTicketLtm: number
  returnRatePct: number
  activeCustomersLtm: number
  openOrdersCount: number
  openOrdersAmount: number
  overdueOrdersCount: number
}

export interface SalesPeriodKpi {
  year: number
  month: number
  grossSales: number
  creditMemoAmount: number
  netSales: number
  invoiceCount: number
  creditMemoCount: number
  activeCustomers: number
  avgTicket: number
  returnRatePct: number
}

export interface TopCustomerMart {
  cardCode: string
  cardName: string | null
  grossSales: number
  creditMemoAmount: number
  netSales: number
  invoiceCount: number
  lastInvoiceDate: string | null
  dsoDays: number | null
}

export interface TopItemMart {
  itemCode: string
  itemName: string | null
  itemGroupName: string | null
  grossSales: number
  creditMemoAmount: number
  netSales: number
  quantitySold: number
  invoiceCount: number
  avgUnitPrice: number
}

export interface TopSalespersonMart {
  salesPersonCode: number
  salesPersonName: string | null
  netSales: number
  grossSales: number
  invoiceCount: number
  activeCustomers: number
  avgTicket: number
  returnRatePct: number
}

export interface OpenSalesOrderMart {
  docNum: number
  cardCode: string | null
  cardName: string | null
  docDate: string | null
  docDueDate: string | null
  docTotal: number
  openAmount: number
  daysOpen: number | null
  isOverdue: boolean
  salesPersonName: string | null
}

// ── Sprint 4 — Purchase MART types ───────────────────────────────────────────

export interface PurchaseMartKpiSummary {
  grossPurchasesLtm: number
  grossPurchasesPrevLtm: number
  growthPct: number
  avgTicketLtm: number
  activeSuppliersLtm: number
  openOrdersCount: number
  openOrdersAmount: number
  overdueOrdersCount: number
}

export interface PurchasePeriodKpi {
  year: number
  month: number
  grossPurchases: number
  creditMemoAmount: number
  netPurchases: number
  invoiceCount: number
  creditMemoCount: number
  activeSuppliers: number
  avgTicket: number
}

export interface TopSupplierMart {
  cardCode: string
  cardName: string | null
  grossPurchases: number
  creditMemoAmount: number
  netPurchases: number
  invoiceCount: number
  lastInvoiceDate: string | null
  dpoDays: number | null
}

export interface TopPurchaseItemMart {
  itemCode: string
  itemName: string | null
  itemGroupName: string | null
  grossPurchases: number
  quantityPurchased: number
  invoiceCount: number
  avgUnitPrice: number
}

export interface OpenPurchaseOrderMart {
  docNum: number
  cardCode: string | null
  cardName: string | null
  docDate: string | null
  docDueDate: string | null
  docTotal: number
  openAmount: number
  daysOpen: number | null
  isOverdue: boolean
}

// ── Sprint 5 — Inventory MART types ─────────────────────────────────────────

export interface InventoryMartKpiSummary {
  totalStockValue: number
  totalItems: number
  slowMovingItemsCount: number
  slowMovingStockValue: number
  itemsBelowMin: number
  warehouseCount: number
}

export interface InventorySnapshotItem {
  itemCode: string
  itemName: string | null
  itemGroupName: string | null
  onHand: number
  committed: number
  ordered: number
  available: number
  avgPrice: number
  stockValue: number
}

export interface InventoryMovementKpi {
  year: number
  month: number
  inboundQty: number
  outboundQty: number
  netQty: number
  inboundValue: number
  outboundValue: number
  transactionCount: number
}

export interface SlowMovingItem {
  itemCode: string
  itemName: string | null
  itemGroupName: string | null
  onHand: number
  stockValue: number
  lastMovementDate: string | null
  daysWithoutMovement: number
}

export interface WarehouseStock {
  warehouseCode: string
  warehouseName: string | null
  totalItems: number
  totalOnHand: number
  totalStockValue: number
  itemsBelowMin: number
}

// ── Sprint 6 — Finance MART types ────────────────────────────────────────────

export interface FinanceMartSummary {
  totalOpenAr: number
  totalOverdueAr: number
  arCustomerCount: number
  dsoDays: number | null
  totalOpenAp: number
  totalOverdueAp: number
  apSupplierCount: number
  dpoDays: number | null
}

export interface ArAgingRow {
  cardCode: string
  cardName: string | null
  currentAmount: number
  bucket1To30: number
  bucket31To60: number
  bucket61To90: number
  bucket91To120: number
  bucketOver120: number
  totalOpen: number
  invoiceCount: number
  oldestDueDate: string | null
}

export interface ApAgingRow {
  cardCode: string
  cardName: string | null
  currentAmount: number
  bucket1To30: number
  bucket31To60: number
  bucket61To90: number
  bucket91To120: number
  bucketOver120: number
  totalOpen: number
  invoiceCount: number
  oldestDueDate: string | null
}

export interface FinancePeriodKpi {
  year: number
  month: number
  arBilled: number
  arCreditMemo: number
  arNet: number
  arInvoiceCount: number
  apBilled: number
  apCreditMemo: number
  apNet: number
  apInvoiceCount: number
}
