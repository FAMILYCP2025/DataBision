namespace DataBision.Application.DTOs.Dashboard;

public sealed class ItemSalesDto
{
    public string ItemCode { get; init; } = string.Empty;
    public string? ItemName { get; init; }
    public decimal QuantitySold { get; init; }
    public decimal GrossSalesAmount { get; init; }
    public int LineCount { get; init; }
    public int InvoiceCount { get; init; }
    public DateOnly? LastSaleDate { get; init; }
}
