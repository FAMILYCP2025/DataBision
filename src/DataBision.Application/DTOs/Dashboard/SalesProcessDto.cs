namespace DataBision.Application.DTOs.Dashboard;

public sealed class SalesCustomerDashboardDto
{
    public string CardCode { get; init; } = string.Empty;
    public string? CardName { get; init; }
    public string? CardType { get; init; }
    public string? SalespersonName { get; init; }
    public decimal GrossSales { get; init; }
    public decimal CreditMemos { get; init; }
    public decimal NetSales { get; init; }
    public int InvoiceCount { get; init; }
    public decimal AvgTicket { get; init; }
    public DateOnly? LastInvoiceDate { get; init; }
    public bool IsActive { get; init; }
}

public sealed class SalesItemDashboardDto
{
    public string ItemCode { get; init; } = string.Empty;
    public string? ItemName { get; init; }
    public string? ItemGroupCode { get; init; }
    public decimal QuantitySold { get; init; }
    public decimal GrossSales { get; init; }
    public decimal? GrossMarginPct { get; init; }
    public int InvoiceCount { get; init; }
    public DateOnly? LastSaleDate { get; init; }
}

public sealed class SalesFulfillmentDto
{
    public DateOnly PeriodDate { get; init; }
    public int OrdersCount { get; init; }
    public decimal OrdersAmount { get; init; }
    public int DeliveredCount { get; init; }
    public decimal DeliveredAmount { get; init; }
    public decimal? FillRatePct { get; init; }
    public int PendingOrders { get; init; }
}
