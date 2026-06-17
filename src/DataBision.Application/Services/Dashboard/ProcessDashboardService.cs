using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces;
using DataBision.Application.Interfaces.Dashboard;

namespace DataBision.Application.Services.Dashboard;

public sealed class ProcessDashboardService(
    IProcessDashboardRepository repo,
    IAnalyticsCompanyResolver analyticsResolver) : IProcessDashboardService
{
    private const int MaxLimit = 200;

    private Task<string> MapAsync(string companyId, CancellationToken ct = default)
        => analyticsResolver.ResolveAsync(companyId, ct);

    public async Task<PagedResultDto<SalesCustomerDashboardDto>> GetSalesCustomersAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetSalesCustomersAsync(await MapAsync(companyId, ct), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public async Task<PagedResultDto<SalesItemDashboardDto>> GetSalesItemsAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetSalesItemsAsync(await MapAsync(companyId, ct), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public async Task<IReadOnlyList<SalesFulfillmentDto>> GetSalesFulfillmentAsync(
        string companyId, int days, CancellationToken ct = default)
        => await repo.GetSalesFulfillmentAsync(await MapAsync(companyId, ct), Math.Clamp(days, 1, 365), ct);

    public async Task<IReadOnlyList<FinanceExecutiveDto>> GetFinanceExecutiveAsync(
        string companyId, int days, CancellationToken ct = default)
        => await repo.GetFinanceExecutiveAsync(await MapAsync(companyId, ct), Math.Clamp(days, 1, 365), ct);

    public async Task<PagedResultDto<FinanceArAgingDto>> GetFinanceArAgingAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetFinanceArAgingAsync(await MapAsync(companyId, ct), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public async Task<PagedResultDto<FinanceApAgingDto>> GetFinanceApAgingAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetFinanceApAgingAsync(await MapAsync(companyId, ct), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public async Task<PagedResultDto<InventoryRotationDto>> GetInventoryRotationAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetInventoryRotationAsync(await MapAsync(companyId, ct), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public async Task<PagedResultDto<InventoryStockDto>> GetInventoryStockAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetInventoryStockAsync(await MapAsync(companyId, ct), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public async Task<IReadOnlyList<InventoryWarehouseDto>> GetInventoryWarehousesAsync(
        string companyId, CancellationToken ct = default)
        => await repo.GetInventoryWarehousesAsync(await MapAsync(companyId, ct), ct);

    public async Task<IReadOnlyList<PurchasingExecutiveDto>> GetPurchasingExecutiveAsync(
        string companyId, int days, CancellationToken ct = default)
        => await repo.GetPurchasingExecutiveAsync(await MapAsync(companyId, ct), Math.Clamp(days, 1, 365), ct);

    public async Task<PagedResultDto<PurchasingSupplierDto>> GetPurchasingSuppliersAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetPurchasingSuppliersAsync(await MapAsync(companyId, ct), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public async Task<PagedResultDto<PurchasingReceivingDto>> GetPurchasingReceivingAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetPurchasingReceivingAsync(await MapAsync(companyId, ct), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public async Task<OperationHealthDto?> GetPipelineHealthAsync(string companyId, CancellationToken ct = default)
        => await repo.GetPipelineHealthAsync(await MapAsync(companyId, ct), ct);

    public async Task<PagedResultDto<OperationAlertDto>> GetActiveAlertsAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetActiveAlertsAsync(await MapAsync(companyId, ct), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public async Task<PagedResultDto<OperationDataQualityDto>> GetDataQualityIssuesAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetDataQualityIssuesAsync(await MapAsync(companyId, ct), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    private static PagedResultDto<T> BuildPaged<T>(IReadOnlyList<T> rows, int limit, int offset)
    {
        var hasMore = rows.Count > limit;
        return new PagedResultDto<T>
        {
            Data = hasMore ? rows.Take(limit).ToList() : rows,
            Meta = new PagedMetaDto
            {
                Limit   = limit,
                Offset  = offset,
                Count   = Math.Min(rows.Count, limit),
                HasMore = hasMore,
            },
        };
    }
}
