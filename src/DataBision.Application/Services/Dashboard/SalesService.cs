using DataBision.Application.Interfaces;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;

namespace DataBision.Application.Services.Dashboard;

public sealed class SalesService(
    IDashboardRepository repo,
    IAnalyticsCompanyResolver analyticsResolver) : ISalesService
{
    private const int MaxLimit = 100;
    private static readonly TimeSpan DefaultLookback = TimeSpan.FromDays(30);

    private Task<string> MapAsync(string companyId, CancellationToken ct = default)
        => analyticsResolver.ResolveAsync(companyId, ct);

    public async Task<SalesOverviewDto> GetOverviewAsync(
        string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default)
        => await repo.GetSalesOverviewByRangeAsync(await MapAsync(companyId, ct), dateFrom, dateTo, ct);

    public async Task<IReadOnlyList<SalesDailyDto>> GetDailyAsync(
        string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default)
        => await repo.GetSalesDailyByRangeAsync(await MapAsync(companyId, ct), dateFrom, dateTo, ct);

    public async Task<IReadOnlyList<SalesMonthlyDto>> GetMonthlyAsync(
        string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default)
        => await repo.GetSalesMonthlyByRangeAsync(await MapAsync(companyId, ct), dateFrom, dateTo, ct);

    public async Task<PagedResultDto<CustomerSalesDto>> GetCustomersAsync(
        string companyId, PaginationOptions pagination, CancellationToken ct = default)
    {
        var limit = Math.Clamp(pagination.Limit, 1, MaxLimit);
        var items = await repo.GetCustomersAsync(await MapAsync(companyId, ct), pagination with { Limit = limit + 1 }, ct);
        return BuildPaged(items, limit, pagination.Offset);
    }

    public async Task<PagedResultDto<ItemSalesDto>> GetItemsAsync(
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

    public static (DateTime from, DateTime to) DefaultDateRange()
        => (DateTime.UtcNow.Date.Subtract(DefaultLookback), DateTime.UtcNow.Date);

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
