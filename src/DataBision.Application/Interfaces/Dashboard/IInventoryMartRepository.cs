using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Application.Interfaces.Dashboard;

public interface IInventoryMartRepository
{
    Task<InventoryMartKpiSummaryDto?> GetKpiSummaryAsync(string companyId, CancellationToken ct = default);

    Task<IReadOnlyList<InventorySnapshotItemDto>> GetSnapshotAsync(
        string companyId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<InventoryMovementKpiDto>> GetMovementByPeriodAsync(
        string companyId, int months, CancellationToken ct = default);

    Task<IReadOnlyList<SlowMovingItemDto>> GetSlowMovingItemsAsync(
        string companyId, int minDays, CancellationToken ct = default);

    Task<IReadOnlyList<WarehouseStockDto>> GetWarehouseStockAsync(
        string companyId, CancellationToken ct = default);
}
