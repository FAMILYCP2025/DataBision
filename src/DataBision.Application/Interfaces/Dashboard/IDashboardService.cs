using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Application.Interfaces.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto?> GetSummaryAsync(string companyId, CancellationToken ct = default);
    Task<IReadOnlyList<SalesDailyDto>> GetSalesDailyAsync(string companyId, int days, CancellationToken ct = default);
    Task<IReadOnlyList<SalesMonthlyDto>> GetSalesMonthlyAsync(string companyId, int months, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerSalesDto>> GetTopCustomersAsync(string companyId, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<ItemSalesDto>> GetTopItemsAsync(string companyId, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<SalespersonSalesDto>> GetSalespersonsAsync(string companyId, int limit, CancellationToken ct = default);
}
