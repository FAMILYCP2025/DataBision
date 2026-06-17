namespace DataBision.Application.DTOs.Dashboard;

public sealed record SalesItemGroupSummaryDto
{
    public string ItemGroupCode { get; init; } = "";
    public string? ItemGroupName { get; init; }
    public decimal GrossSales { get; init; }
    public decimal NetSales { get; init; }
    public int InvoiceCount { get; init; }
    public int SkuCount { get; init; }
    public decimal GrossMarginPct { get; init; }
}
