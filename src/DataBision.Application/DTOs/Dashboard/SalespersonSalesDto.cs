namespace DataBision.Application.DTOs.Dashboard;

public sealed class SalespersonSalesDto
{
    public string SalesPersonCode { get; init; } = string.Empty;
    public string? SalesPersonName { get; init; }
    public decimal SalesAmount { get; init; }
    public decimal CreditMemoAmount { get; init; }
    public decimal NetSalesAmount { get; init; }
    public int InvoiceCount { get; init; }
    public int CreditMemoCount { get; init; }
    public int ActiveCustomers { get; init; }
    public decimal AvgTicketAmount { get; init; }
}
