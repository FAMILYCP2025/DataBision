namespace DataBision.Application.DTOs.Dashboard;

public sealed class SalesOverviewDto
{
    public decimal GrossSalesAmount { get; init; }
    public decimal CreditMemoAmount { get; init; }
    public decimal NetSalesAmount { get; init; }
    public int InvoiceCount { get; init; }
    public int CreditMemoCount { get; init; }
    public decimal AvgTicketAmount { get; init; }
    public int ActiveCustomers { get; init; }
    public DateOnly DateFrom { get; init; }
    public DateOnly DateTo { get; init; }
}
