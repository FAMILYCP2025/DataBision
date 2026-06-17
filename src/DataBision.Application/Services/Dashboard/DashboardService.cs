using DataBision.Application.Interfaces;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;

namespace DataBision.Application.Services.Dashboard;

public sealed class DashboardService(
    IDashboardRepository repo,
    IAnalyticsCompanyResolver analyticsResolver) : IDashboardService
{
    private const int MaxDays   = 365;
    private const int MaxMonths = 36;
    private const int MaxLimit  = 100;

    private Task<string> MapAsync(string companyId, CancellationToken ct = default)
        => analyticsResolver.ResolveAsync(companyId, ct);

    public async Task<DashboardSummaryDto?> GetSummaryAsync(string companyId, CancellationToken ct = default)
        => await repo.GetSummaryAsync(await MapAsync(companyId, ct), ct);

    public async Task<IReadOnlyList<SalesDailyDto>> GetSalesDailyAsync(
        string companyId, int days, CancellationToken ct = default)
    {
        days = Math.Clamp(days, 1, MaxDays);
        return await repo.GetSalesDailyLastNDaysAsync(await MapAsync(companyId, ct), days, ct);
    }

    public async Task<IReadOnlyList<SalesMonthlyDto>> GetSalesMonthlyAsync(
        string companyId, int months, CancellationToken ct = default)
    {
        months = Math.Clamp(months, 1, MaxMonths);
        return await repo.GetSalesMonthlyLastNMonthsAsync(await MapAsync(companyId, ct), months, ct);
    }

    public async Task<PagedResultDto<CustomerSalesDto>> GetTopCustomersAsync(
        string companyId, PaginationOptions pagination, CancellationToken ct = default)
    {
        var limit = Math.Clamp(pagination.Limit, 1, MaxLimit);
        var items = await repo.GetCustomersAsync(await MapAsync(companyId, ct), pagination with { Limit = limit + 1 }, ct);
        return BuildPaged(items, limit, pagination.Offset);
    }

    public async Task<PagedResultDto<ItemSalesDto>> GetTopItemsAsync(
        string companyId, PaginationOptions pagination, CancellationToken ct = default)
    {
        var limit = Math.Clamp(pagination.Limit, 1, MaxLimit);
        var items = await repo.GetItemsAsync(await MapAsync(companyId, ct), pagination with { Limit = limit + 1 }, ct);
        return BuildPaged(items, limit, pagination.Offset);
    }

    public async Task<PagedResultDto<SalespersonSalesDto>> GetSalespersonsAsync(
        string companyId, PaginationOptions pagination, CancellationToken ct = default)
    {
        var limit = Math.Clamp(pagination.Limit, 1, MaxLimit);
        var items = await repo.GetSalespersonsAsync(await MapAsync(companyId, ct), pagination with { Limit = limit + 1 }, ct);
        return BuildPaged(items, limit, pagination.Offset);
    }

    private static PagedResultDto<T> BuildPaged<T>(IReadOnlyList<T> items, int limit, int offset)
    {
        var hasMore = items.Count > limit;
        var data = hasMore ? items.Take(limit).ToList() : (IReadOnlyList<T>)items;
        return new PagedResultDto<T>
        {
            Data = data,
            Meta = new PagedMetaDto
            {
                Limit   = limit,
                Offset  = offset,
                Count   = data.Count,
                HasMore = hasMore,
            }
        };
    }
}
