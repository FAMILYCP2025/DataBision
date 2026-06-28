namespace DataBision.Application.DTOs.Dashboard;

// ── Summary KPI (calculated from mart.sales_period_kpi) ─────────────────────

public sealed record SalesMartKpiSummaryDto(
    decimal NetSalesLtm,
    decimal NetSalesPrevLtm,
    decimal GrowthPct,
    decimal AvgTicketLtm,
    decimal ReturnRatePct,
    int ActiveCustomersLtm,
    int OpenOrdersCount,
    decimal OpenOrdersAmount,
    int OverdueOrdersCount);

// ── Period KPIs (one row per year/month from mart.sales_period_kpi) ──────────

public sealed record SalesPeriodKpiDto(
    int Year,
    int Month,
    decimal GrossSales,
    decimal CreditMemoAmount,
    decimal NetSales,
    int InvoiceCount,
    int CreditMemoCount,
    int ActiveCustomers,
    decimal AvgTicket,
    decimal ReturnRatePct);

// ── Top customers (mart.top_customers) ───────────────────────────────────────

public sealed record TopCustomerMartDto(
    string CardCode,
    string? CardName,
    decimal GrossSales,
    decimal CreditMemoAmount,
    decimal NetSales,
    int InvoiceCount,
    DateOnly? LastInvoiceDate,
    decimal? DsoDays);

// ── Top items (mart.top_items) ────────────────────────────────────────────────

public sealed record TopItemMartDto(
    string ItemCode,
    string? ItemName,
    string? ItemGroupName,
    decimal GrossSales,
    decimal CreditMemoAmount,
    decimal NetSales,
    decimal QuantitySold,
    int InvoiceCount,
    decimal AvgUnitPrice);

// ── Top salespersons (mart.top_salespersons) ──────────────────────────────────

public sealed record TopSalespersonMartDto(
    int SalesPersonCode,
    string? SalesPersonName,
    decimal NetSales,
    decimal GrossSales,
    int InvoiceCount,
    int ActiveCustomers,
    decimal AvgTicket,
    decimal ReturnRatePct);

// ── Open sales orders pipeline (mart.open_sales_orders) ──────────────────────

public sealed record OpenSalesOrderMartDto(
    int DocNum,
    string? CardCode,
    string? CardName,
    DateOnly? DocDate,
    DateOnly? DocDueDate,
    decimal DocTotal,
    decimal OpenAmount,
    int? DaysOpen,
    bool IsOverdue,
    string? SalesPersonName);
