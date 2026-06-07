using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Application.Interfaces.Dashboard;

public interface IDashboardRepository
{
    // ── Summary ──────────────────────────────────────────────────────────────
    Task<DashboardSummaryDto?> GetSummaryAsync(string companyId, CancellationToken ct = default);

    // ── Time series ───────────────────────────────────────────────────────────
    Task<IReadOnlyList<SalesDailyDto>> GetSalesDailyLastNDaysAsync(
        string companyId, int days, CancellationToken ct = default);

    Task<IReadOnlyList<SalesDailyDto>> GetSalesDailyByRangeAsync(
        string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default);

    Task<IReadOnlyList<SalesMonthlyDto>> GetSalesMonthlyLastNMonthsAsync(
        string companyId, int months, CancellationToken ct = default);

    Task<IReadOnlyList<SalesMonthlyDto>> GetSalesMonthlyByRangeAsync(
        string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default);

    // ── Aggregated overview (from sales_daily) ────────────────────────────────
    Task<SalesOverviewDto> GetSalesOverviewByRangeAsync(
        string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default);

    // ── Ranking lists ─────────────────────────────────────────────────────────
    Task<IReadOnlyList<CustomerSalesDto>> GetCustomersAsync(
        string companyId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<ItemSalesDto>> GetItemsAsync(
        string companyId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<SalespersonSalesDto>> GetSalespersonsAsync(
        string companyId, int limit, CancellationToken ct = default);
}
