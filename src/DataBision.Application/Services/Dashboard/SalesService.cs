using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;

namespace DataBision.Application.Services.Dashboard;

public sealed class SalesService(IDashboardRepository repo) : ISalesService
{
    private const int MaxLimit = 100;
    private static readonly TimeSpan DefaultLookback = TimeSpan.FromDays(30);

    public Task<SalesOverviewDto> GetOverviewAsync(
        string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default)
        => repo.GetSalesOverviewByRangeAsync(companyId, dateFrom, dateTo, ct);

    public Task<IReadOnlyList<SalesDailyDto>> GetDailyAsync(
        string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default)
        => repo.GetSalesDailyByRangeAsync(companyId, dateFrom, dateTo, ct);

    public Task<IReadOnlyList<SalesMonthlyDto>> GetMonthlyAsync(
        string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default)
        => repo.GetSalesMonthlyByRangeAsync(companyId, dateFrom, dateTo, ct);

    public Task<IReadOnlyList<CustomerSalesDto>> GetCustomersAsync(
        string companyId, int limit, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, MaxLimit);
        return repo.GetCustomersAsync(companyId, limit, ct);
    }

    public Task<IReadOnlyList<ItemSalesDto>> GetItemsAsync(
        string companyId, int limit, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, MaxLimit);
        return repo.GetItemsAsync(companyId, limit, ct);
    }

    public Task<IReadOnlyList<SalespersonSalesDto>> GetSalespersonsAsync(
        string companyId, int limit, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, MaxLimit);
        return repo.GetSalespersonsAsync(companyId, limit, ct);
    }

    public static (DateTime from, DateTime to) DefaultDateRange()
        => (DateTime.UtcNow.Date.Subtract(DefaultLookback), DateTime.UtcNow.Date);
}
