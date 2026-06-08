using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;

namespace DataBision.Application.Services.Dashboard;

public sealed class DashboardService(IDashboardRepository repo) : IDashboardService
{
    private const int MaxDays   = 365;
    private const int MaxMonths = 36;
    private const int MaxLimit  = 100;

    public Task<DashboardSummaryDto?> GetSummaryAsync(string companyId, CancellationToken ct = default)
        => repo.GetSummaryAsync(companyId, ct);

    public Task<IReadOnlyList<SalesDailyDto>> GetSalesDailyAsync(
        string companyId, int days, CancellationToken ct = default)
    {
        days = Math.Clamp(days, 1, MaxDays);
        return repo.GetSalesDailyLastNDaysAsync(companyId, days, ct);
    }

    public Task<IReadOnlyList<SalesMonthlyDto>> GetSalesMonthlyAsync(
        string companyId, int months, CancellationToken ct = default)
    {
        months = Math.Clamp(months, 1, MaxMonths);
        return repo.GetSalesMonthlyLastNMonthsAsync(companyId, months, ct);
    }

    public async Task<PagedResultDto<CustomerSalesDto>> GetTopCustomersAsync(
        string companyId, PaginationOptions pagination, CancellationToken ct = default)
    {
        var limit = Math.Clamp(pagination.Limit, 1, MaxLimit);
        var items = await repo.GetCustomersAsync(companyId, pagination with { Limit = limit + 1 }, ct);
        return BuildPaged(items, limit, pagination.Offset);
    }

    public async Task<PagedResultDto<ItemSalesDto>> GetTopItemsAsync(
        string companyId, PaginationOptions pagination, CancellationToken ct = default)
    {
        var limit = Math.Clamp(pagination.Limit, 1, MaxLimit);
        var items = await repo.GetItemsAsync(companyId, pagination with { Limit = limit + 1 }, ct);
        return BuildPaged(items, limit, pagination.Offset);
    }

    public async Task<PagedResultDto<SalespersonSalesDto>> GetSalespersonsAsync(
        string companyId, PaginationOptions pagination, CancellationToken ct = default)
    {
        var limit = Math.Clamp(pagination.Limit, 1, MaxLimit);
        var items = await repo.GetSalespersonsAsync(companyId, pagination with { Limit = limit + 1 }, ct);
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
