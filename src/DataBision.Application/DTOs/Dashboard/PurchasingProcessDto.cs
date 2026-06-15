namespace DataBision.Application.DTOs.Dashboard;

public sealed class PurchasingExecutiveDto
{
    public DateOnly PurchaseDate { get; init; }
    public int PoCount { get; init; }
    public decimal PoAmount { get; init; }
    public int ReceivedCount { get; init; }
    public decimal ReceivedAmount { get; init; }
    public int ActiveSuppliers { get; init; }
}

public sealed class PurchasingSupplierDto
{
    public string SupplierCode { get; init; } = string.Empty;
    public string? SupplierName { get; init; }
    public int PoCount { get; init; }
    public decimal PoAmount { get; init; }
    public decimal ReceivedAmount { get; init; }
    public decimal AvgPoAmount { get; init; }
    public DateOnly? LastPoDate { get; init; }
}

public sealed class PurchasingReceivingDto
{
    public string SupplierCode { get; init; } = string.Empty;
    public string? SupplierName { get; init; }
    public int GrCount { get; init; }
    public decimal GrAmount { get; init; }
    public DateOnly? LastGrDate { get; init; }
}
