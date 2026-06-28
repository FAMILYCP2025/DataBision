namespace DataBision.Application.DTOs.Dashboard;

// ── Summary KPI (calculated from mart.purchase_period_kpi) ──────────────────

public sealed record PurchaseMartKpiSummaryDto(
    decimal GrossPurchasesLtm,
    decimal GrossPurchasesPrevLtm,
    decimal GrowthPct,
    decimal AvgTicketLtm,
    int ActiveSuppliersLtm,
    int OpenOrdersCount,
    decimal OpenOrdersAmount,
    int OverdueOrdersCount);

// ── Period KPIs (one row per year/month from mart.purchase_period_kpi) ───────

public sealed record PurchasePeriodKpiDto(
    int Year,
    int Month,
    decimal GrossPurchases,
    decimal CreditMemoAmount,
    decimal NetPurchases,
    int InvoiceCount,
    int CreditMemoCount,
    int ActiveSuppliers,
    decimal AvgTicket);

// ── Top suppliers (mart.top_suppliers) ───────────────────────────────────────

public sealed record TopSupplierMartDto(
    string CardCode,
    string? CardName,
    decimal GrossPurchases,
    decimal CreditMemoAmount,
    decimal NetPurchases,
    int InvoiceCount,
    DateOnly? LastInvoiceDate,
    decimal? DpoDays);

// ── Top purchase items (mart.top_purchase_items) ──────────────────────────────

public sealed record TopPurchaseItemMartDto(
    string ItemCode,
    string? ItemName,
    string? ItemGroupName,
    decimal GrossPurchases,
    decimal QuantityPurchased,
    int InvoiceCount,
    decimal AvgUnitPrice);

// ── Open purchase orders pipeline (mart.open_purchase_orders) ─────────────────

public sealed record OpenPurchaseOrderMartDto(
    int DocNum,
    string? CardCode,
    string? CardName,
    DateOnly? DocDate,
    DateOnly? DocDueDate,
    decimal DocTotal,
    decimal OpenAmount,
    int? DaysOpen,
    bool IsOverdue);
