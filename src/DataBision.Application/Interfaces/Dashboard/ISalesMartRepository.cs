using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Application.Interfaces.Dashboard;

/// <summary>
/// Reads from Sprint 3 Sales MART tables:
///   mart.sales_period_kpi, mart.top_customers, mart.top_items,
///   mart.top_salespersons, mart.open_sales_orders
/// All methods filter by company_id.
/// </summary>
public interface ISalesMartRepository
{
    Task<SalesMartKpiSummaryDto?> GetKpiSummaryAsync(string companyId, CancellationToken ct = default);

    Task<IReadOnlyList<SalesPeriodKpiDto>> GetByPeriodAsync(
        string companyId, int months, CancellationToken ct = default);

    Task<IReadOnlyList<TopCustomerMartDto>> GetTopCustomersAsync(
        string companyId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<TopItemMartDto>> GetTopItemsAsync(
        string companyId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<TopSalespersonMartDto>> GetTopSalespersonsAsync(
        string companyId, CancellationToken ct = default);

    Task<IReadOnlyList<OpenSalesOrderMartDto>> GetOpenOrdersAsync(
        string companyId, bool overdueOnly, CancellationToken ct = default);
}
