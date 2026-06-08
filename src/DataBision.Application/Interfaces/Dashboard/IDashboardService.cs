using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Application.Interfaces.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto?> GetSummaryAsync(string companyId, CancellationToken ct = default);
    Task<IReadOnlyList<SalesDailyDto>> GetSalesDailyAsync(string companyId, int days, CancellationToken ct = default);
    Task<IReadOnlyList<SalesMonthlyDto>> GetSalesMonthlyAsync(string companyId, int months, CancellationToken ct = default);
    Task<PagedResultDto<CustomerSalesDto>> GetTopCustomersAsync(string companyId, PaginationOptions pagination, CancellationToken ct = default);
    Task<PagedResultDto<ItemSalesDto>> GetTopItemsAsync(string companyId, PaginationOptions pagination, CancellationToken ct = default);
    Task<PagedResultDto<SalespersonSalesDto>> GetSalespersonsAsync(string companyId, PaginationOptions pagination, CancellationToken ct = default);
}
