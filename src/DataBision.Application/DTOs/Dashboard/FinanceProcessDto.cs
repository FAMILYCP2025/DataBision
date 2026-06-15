namespace DataBision.Application.DTOs.Dashboard;

public sealed class FinanceExecutiveDto
{
    public DateOnly PeriodDate { get; init; }
    public decimal ArTotal { get; init; }
    public decimal ArOverdue { get; init; }
    public decimal ArOverduePct { get; init; }
    public decimal? ApTotal { get; init; }
    public decimal? ApOverdue { get; init; }
    public int NewInvoicesCount { get; init; }
    public decimal NewInvoicesAmount { get; init; }
}

public sealed class FinanceArAgingDto
{
    public string CardCode { get; init; } = string.Empty;
    public string? CardName { get; init; }
    public int InvoiceCount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal BalanceDue { get; init; }
    public decimal OverdueAmount { get; init; }
    public decimal Aging0To30 { get; init; }
    public decimal Aging31To60 { get; init; }
    public decimal Aging61To90 { get; init; }
    public decimal Aging90Plus { get; init; }
    public DateOnly? LastInvoiceDate { get; init; }
    public DateOnly? OldestOverdueDate { get; init; }
}

public sealed class FinanceApAgingDto
{
    public string SupplierCode { get; init; } = string.Empty;
    public string? SupplierName { get; init; }
    public int InvoiceCount { get; init; }
    public decimal BalanceDue { get; init; }
    public decimal OverdueAmount { get; init; }
    public decimal Aging0To30 { get; init; }
    public decimal Aging31To60 { get; init; }
    public decimal Aging61To90 { get; init; }
    public decimal Aging90Plus { get; init; }
}
