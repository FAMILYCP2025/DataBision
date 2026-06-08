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
