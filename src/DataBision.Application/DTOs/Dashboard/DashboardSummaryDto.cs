namespace DataBision.Application.DTOs.Dashboard;

public sealed class DashboardSummaryDto
{
    public string CompanyId { get; init; } = string.Empty;
    public decimal GrossSalesAmount { get; init; }
    public decimal CreditMemoAmount { get; init; }
    public decimal NetSalesAmount { get; init; }
    public int InvoiceCount { get; init; }
    public int CreditMemoCount { get; init; }
    public int ActiveCustomers { get; init; }
    public int ActiveItems { get; init; }
    public decimal AvgTicketAmount { get; init; }
    public DateOnly? LastInvoiceDate { get; init; }
    public DateOnly? LastCreditMemoDate { get; init; }
    public DateTime? LastSyncAtUtc { get; init; }
    public DateTime TransformedAtUtc { get; init; }
}
