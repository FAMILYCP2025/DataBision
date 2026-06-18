// Process BI — TypeScript type definitions (Sprint 8K process dashboard backend contract)

// ── Sales process ─────────────────────────────────────────────────────────────

export interface SalesCustomerDashboard {
  cardCode: string
  cardName: string | null
  cardType: string | null
  salespersonName: string | null
  grossSales: number
  creditMemos: number
  netSales: number
  invoiceCount: number
  avgTicket: number
  lastInvoiceDate: string | null   // "YYYY-MM-DD"
  isActive: boolean
}

export interface SalesItemDashboard {
  itemCode: string
  itemName: string | null
  itemGroupCode: string | null
  quantitySold: number
  grossSales: number
  grossMarginPct: number | null
  invoiceCount: number
  lastSaleDate: string | null      // "YYYY-MM-DD"
}

export interface SalesFulfillment {
  periodDate: string               // "YYYY-MM-DD"
  ordersCount: number
  ordersAmount: number
  deliveredCount: number
  deliveredAmount: number
  fillRatePct: number | null
  pendingOrders: number
}

// ── Purchasing process ────────────────────────────────────────────────────────

export interface PurchasingExecutive {
  purchaseDate: string             // "YYYY-MM-DD"
  poCount: number
  poAmount: number
  receivedCount: number
  receivedAmount: number
  activeSuppliers: number
}

export interface PurchasingSupplier {
  supplierCode: string
  supplierName: string | null
  poCount: number
  poAmount: number
  receivedAmount: number
  avgPoAmount: number
  lastPoDate: string | null        // "YYYY-MM-DD"
}

export interface PurchasingReceiving {
  supplierCode: string
  supplierName: string | null
  grCount: number
  grAmount: number
  lastGrDate: string | null        // "YYYY-MM-DD"
}

// ── Inventory process ─────────────────────────────────────────────────────────

export type RotationStatus = 'FAST' | 'NORMAL' | 'SLOW' | 'NO_MOVEMENT'

export interface InventoryRotation {
  itemCode: string
  itemName: string | null
  itemGroupCode: string | null
  qtySold30d: number
  qtySold90d: number
  lastSaleDate: string | null      // "YYYY-MM-DD"
  avgDailySalesQty: number
  onHandQty: number | null
  coverageDays: number | null
  rotationStatus: RotationStatus
}

export interface InventoryStock {
  warehouseCode: string
  itemCode: string
  itemName: string | null
  itemGroupCode: string | null
  onHandQty: number | null
  availableQty: number | null
  stockValue: number | null
  isStockout: boolean
}

export interface InventoryWarehouse {
  warehouseCode: string
  warehouseName: string | null
  transferInCount: number
  transferInQty: number
  transferOutCount: number
  transferOutQty: number
  lastTransferDate: string | null  // "YYYY-MM-DD"
}

// ── Finance process ───────────────────────────────────────────────────────────

export interface FinanceExecutive {
  periodDate: string               // "YYYY-MM-DD"
  arTotal: number
  arOverdue: number
  arOverduePct: number
  apTotal: number | null
  apOverdue: number | null
  newInvoicesCount: number
  newInvoicesAmount: number
}

export interface FinanceArAging {
  cardCode: string
  cardName: string | null
  invoiceCount: number
  totalAmount: number
  balanceDue: number
  overdueAmount: number
  aging0To30: number
  aging31To60: number
  aging61To90: number
  aging90Plus: number
  lastInvoiceDate: string | null   // "YYYY-MM-DD"
  oldestOverdueDate: string | null
}

export interface FinanceApAging {
  supplierCode: string
  supplierName: string | null
  invoiceCount: number
  balanceDue: number
  overdueAmount: number
  aging0To30: number
  aging31To60: number
  aging61To90: number
  aging90Plus: number
}

// ── Operations process ────────────────────────────────────────────────────────

export interface OperationHealth {
  lastExtractorRunUtc: string | null
  lastTransformRunUtc: string | null
  extractorStatus: string           // "ok" | "warning" | "error" | "unknown"
  transformStatus: string
  activeAlerts: number
  dqErrorsUnresolved: number
  objectsExtracted: number
  healthScore: number
  updatedAtUtc: string | null
}

export interface OperationAlert {
  id: number
  ruleCode: string
  severity: string                  // "info" | "warning" | "critical"
  triggeredValue: string | null
  message: string | null
  triggeredAtUtc: string
  isResolved: boolean
}

export interface OperationDataQuality {
  id: number
  sapObject: string
  issueType: string
  severity: string
  description: string
  affectedRows: number
  sampleKey: string | null
  detectedAtUtc: string
  isResolved: boolean
}

// ── Sales item groups ─────────────────────────────────────────────────────────

export interface SalesItemGroupSummary {
  itemGroupCode: string
  itemGroupName: string | null
  grossSales: number
  netSales: number
  invoiceCount: number
  skuCount: number
  grossMarginPct: number
}

export interface SalesWarehouseSummary {
  warehouseCode: string
  warehouseName: string | null
  grossSales: number
  netSales: number
  invoiceCount: number
  skuCount: number
}

// ── Finance accounting (Sprint 13C–13E) ──────────────────────────────────────

export interface IncomeStatementLine {
  statementLine: string
  amount: number
  pctOfRevenue: number
}

export interface IncomeStatementPeriod {
  periodYear: number
  periodMonth: number
  revenue: number
  cogs: number
  grossProfit: number
  grossProfitPct: number
  opex: number
  operatingIncome: number
  operatingPct: number
  financial: number
  tax: number
  netIncome: number
  netPct: number
  lines: IncomeStatementLine[]
}

export interface BalanceSheetEntry {
  category: string
  subCategory: string
  amount: number
}

export interface BalanceSheetSnapshot {
  snapshotDate: string
  totalAssets: number
  totalLiabilities: number
  totalEquity: number
  imbalance: number
  entries: BalanceSheetEntry[]
}

export interface EbitdaPeriod {
  periodYear: number
  periodMonth: number
  revenue: number
  cogs: number
  grossProfit: number
  opex: number
  ebitda: number
  depreciation: number
  amortization: number
  financialResult: number
  taxResult: number
  netIncome: number
  ebitdaMargin: number
  netMargin: number
}

export interface ChartOfAccountEntry {
  code: string
  name: string | null
  fatherNum: string | null
  level: number | null
  accountType: string | null
  statementLine: string | null
  postable: boolean
  balance: number
}

// ── Finance readiness (Sprint 14E) ────────────────────────────────────────────

export interface FinanceReadiness {
  rawOactCount: number
  rawOjdtCount: number
  rawJdt1Count: number
  stgOactCount: number
  stgOjdtCount: number
  stgJdt1Count: number
  martGlAccounts: number
  martIncomeStatement: number
  martBalanceSheet: number
  martEbitda: number
  classificationRules: number
  unclassifiedPostable: number
  readinessStatus: 'blocked' | 'warning' | 'ready'
  blockingReasons: string[]
  warnings: string[]
}

// ── Finance validation (Sprint 14C) ──────────────────────────────────────────

export interface FinanceValidationIssue {
  severity: 'critical' | 'warning' | 'info'
  issueType: string
  title: string
  description: string
  count: number
  period: string | null
}

export interface FinanceReconciliation {
  snapshotDate: string | null
  totalAssets: number
  totalLiabilities: number
  totalEquity: number
  imbalance: number
  isBalanced: boolean
}

export interface FinanceValidationSummary {
  healthScore: number
  healthStatus: 'ok' | 'warning' | 'critical'
  criticalIssues: number
  warningIssues: number
  infoIssues: number
  lastPeriodValidated: string | null
  balanceImbalance: number
  unclassifiedAccounts: number
  orphanJournalLines: number
  issues: FinanceValidationIssue[]
  reconciliation: FinanceReconciliation | null
}
