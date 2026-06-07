using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;

namespace DataBision.Application.Services.Dashboard;

public sealed class DashboardService(IDashboardRepository repo) : IDashboardService
{
    private const int MaxDays    = 365;
    private const int MaxMonths  = 36;
    private const int MaxLimit   = 100;

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

    public Task<IReadOnlyList<CustomerSalesDto>> GetTopCustomersAsync(
        string companyId, int limit, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, MaxLimit);
        return repo.GetCustomersAsync(companyId, limit, ct);
    }

    public Task<IReadOnlyList<ItemSalesDto>> GetTopItemsAsync(
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
}
