using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces;
using DataBision.Application.Interfaces.Dashboard;

namespace DataBision.Application.Services.Dashboard;

public sealed class ProcessDashboardService(
    IProcessDashboardRepository repo,
    IAnalyticsCompanyResolver analyticsResolver) : IProcessDashboardService
{
    private const int MaxLimit = 200;
    private const int DefaultLimit = 50;

    // Maps the app company identifier (slug or numeric ID from JWT) to the analytics
    // company_id used in the staging/MART database.
    private string Map(string companyId) => analyticsResolver.Resolve(companyId);

    public async Task<PagedResultDto<SalesCustomerDashboardDto>> GetSalesCustomersAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetSalesCustomersAsync(Map(companyId), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public async Task<PagedResultDto<SalesItemDashboardDto>> GetSalesItemsAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetSalesItemsAsync(Map(companyId), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public Task<IReadOnlyList<SalesFulfillmentDto>> GetSalesFulfillmentAsync(
        string companyId, int days, CancellationToken ct = default)
        => repo.GetSalesFulfillmentAsync(Map(companyId), Math.Clamp(days, 1, 365), ct);

    public Task<IReadOnlyList<FinanceExecutiveDto>> GetFinanceExecutiveAsync(
        string companyId, int days, CancellationToken ct = default)
        => repo.GetFinanceExecutiveAsync(Map(companyId), Math.Clamp(days, 1, 365), ct);

    public async Task<PagedResultDto<FinanceArAgingDto>> GetFinanceArAgingAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetFinanceArAgingAsync(Map(companyId), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public async Task<PagedResultDto<FinanceApAgingDto>> GetFinanceApAgingAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetFinanceApAgingAsync(Map(companyId), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public async Task<PagedResultDto<InventoryRotationDto>> GetInventoryRotationAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetInventoryRotationAsync(Map(companyId), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public async Task<PagedResultDto<InventoryStockDto>> GetInventoryStockAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetInventoryStockAsync(Map(companyId), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public Task<IReadOnlyList<InventoryWarehouseDto>> GetInventoryWarehousesAsync(
        string companyId, CancellationToken ct = default)
        => repo.GetInventoryWarehousesAsync(Map(companyId), ct);

    public Task<IReadOnlyList<PurchasingExecutiveDto>> GetPurchasingExecutiveAsync(
        string companyId, int days, CancellationToken ct = default)
        => repo.GetPurchasingExecutiveAsync(Map(companyId), Math.Clamp(days, 1, 365), ct);

    public async Task<PagedResultDto<PurchasingSupplierDto>> GetPurchasingSuppliersAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetPurchasingSuppliersAsync(Map(companyId), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public async Task<PagedResultDto<PurchasingReceivingDto>> GetPurchasingReceivingAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetPurchasingReceivingAsync(Map(companyId), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public Task<OperationHealthDto?> GetPipelineHealthAsync(string companyId, CancellationToken ct = default)
        => repo.GetPipelineHealthAsync(Map(companyId), ct);

    public async Task<PagedResultDto<OperationAlertDto>> GetActiveAlertsAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetActiveAlertsAsync(Map(companyId), p with { Limit = limit + 1 }, ct);
        return BuildPaged(rows, limit, p.Offset);
    }

    public async Task<PagedResultDto<OperationDataQualityDto>> GetDataQualityIssuesAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var limit = Math.Clamp(p.Limit, 1, MaxLimit);
        var rows = await repo.GetDataQualityIssuesAsync(Map(companyId), p with { Limit = limit + 1 }, ct);
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
