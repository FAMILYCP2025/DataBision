namespace DataBision.Application.DTOs.Dashboard;

public sealed record SalesWarehouseSummaryDto
{
    public string WarehouseCode { get; init; } = "";
    public string? WarehouseName { get; init; }
    public decimal GrossSales { get; init; }
    public decimal NetSales { get; init; }
    public int InvoiceCount { get; init; }
    public int SkuCount { get; init; }
}
