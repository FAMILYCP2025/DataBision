namespace DataBision.Application.DTOs.Dashboard;

public sealed class CustomerSalesDto
{
    public string CardCode { get; init; } = string.Empty;
    public string? CardName { get; init; }
    public decimal SalesAmount { get; init; }
    public decimal CreditMemoAmount { get; init; }
    public decimal NetSalesAmount { get; init; }
    public int InvoiceCount { get; init; }
    public int CreditMemoCount { get; init; }
    public DateOnly? LastInvoiceDate { get; init; }
    public DateOnly? FirstInvoiceDate { get; init; }
    public decimal AvgTicketAmount { get; init; }
}
