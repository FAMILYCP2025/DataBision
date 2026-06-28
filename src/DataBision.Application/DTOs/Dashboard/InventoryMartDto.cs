namespace DataBision.Application.DTOs.Dashboard;

public sealed record InventoryMartKpiSummaryDto(
    decimal TotalStockValue,
    int TotalItems,
    int SlowMovingItemsCount,
    decimal SlowMovingStockValue,
    int ItemsBelowMin,
    int WarehouseCount);

public sealed record InventorySnapshotItemDto(
    string ItemCode,
    string? ItemName,
    string? ItemGroupName,
    decimal OnHand,
    decimal Committed,
    decimal Ordered,
    decimal Available,
    decimal AvgPrice,
    decimal StockValue);

public sealed record InventoryMovementKpiDto(
    int Year,
    int Month,
    decimal InboundQty,
    decimal OutboundQty,
    decimal NetQty,
    decimal InboundValue,
    decimal OutboundValue,
    int TransactionCount);

public sealed record SlowMovingItemDto(
    string ItemCode,
    string? ItemName,
    string? ItemGroupName,
    decimal OnHand,
    decimal StockValue,
    DateOnly? LastMovementDate,
    int DaysWithoutMovement);

public sealed record WarehouseStockDto(
    string WarehouseCode,
    string? WarehouseName,
    int TotalItems,
    decimal TotalOnHand,
    decimal TotalStockValue,
    int ItemsBelowMin);
