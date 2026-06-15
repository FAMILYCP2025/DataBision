namespace DataBision.Application.DTOs.Dashboard;

public sealed class InventoryRotationDto
{
    public string ItemCode { get; init; } = string.Empty;
    public string? ItemName { get; init; }
    public string? ItemGroupCode { get; init; }
    public decimal QtySold30d { get; init; }
    public decimal QtySold90d { get; init; }
    public DateOnly? LastSaleDate { get; init; }
    public decimal AvgDailySalesQty { get; init; }
    public decimal? OnHandQty { get; init; }
    public decimal? CoverageDays { get; init; }
    public string RotationStatus { get; init; } = string.Empty;
}

public sealed class InventoryStockDto
{
    public string WarehouseCode { get; init; } = string.Empty;
    public string ItemCode { get; init; } = string.Empty;
    public string? ItemName { get; init; }
    public string? ItemGroupCode { get; init; }
    public decimal? OnHandQty { get; init; }
    public decimal? AvailableQty { get; init; }
    public decimal? StockValue { get; init; }
    public bool IsStockout { get; init; }
}

public sealed class InventoryWarehouseDto
{
    public string WarehouseCode { get; init; } = string.Empty;
    public string? WarehouseName { get; init; }
    public int TransferInCount { get; init; }
    public decimal TransferInQty { get; init; }
    public int TransferOutCount { get; init; }
    public decimal TransferOutQty { get; init; }
    public DateOnly? LastTransferDate { get; init; }
}
