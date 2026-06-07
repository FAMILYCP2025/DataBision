namespace DataBision.Application.DTOs.Dashboard;

public sealed class SalesDailyDto
{
    public DateOnly SalesDate { get; init; }
    public decimal GrossSalesAmount { get; init; }
    public decimal CreditMemoAmount { get; init; }
    public decimal NetSalesAmount { get; init; }
    public int InvoiceCount { get; init; }
    public int CreditMemoCount { get; init; }
    public int ActiveCustomers { get; init; }
    public decimal AvgTicketAmount { get; init; }
}
