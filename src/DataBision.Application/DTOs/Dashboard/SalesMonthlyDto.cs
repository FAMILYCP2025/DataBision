namespace DataBision.Application.DTOs.Dashboard;

public sealed class SalesMonthlyDto
{
    public DateOnly SalesMonth { get; init; }
    public decimal GrossSalesAmount { get; init; }
    public decimal CreditMemoAmount { get; init; }
    public decimal NetSalesAmount { get; init; }
    public int InvoiceCount { get; init; }
    public int CreditMemoCount { get; init; }
    public int ActiveCustomers { get; init; }
    public decimal AvgTicketAmount { get; init; }
}
