using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Application.Interfaces.Dashboard;

public interface ISalesService
{
    Task<SalesOverviewDto> GetOverviewAsync(string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default);
    Task<IReadOnlyList<SalesDailyDto>> GetDailyAsync(string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default);
    Task<IReadOnlyList<SalesMonthlyDto>> GetMonthlyAsync(string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default);
    Task<PagedResultDto<CustomerSalesDto>> GetCustomersAsync(string companyId, PaginationOptions pagination, CancellationToken ct = default);
    Task<PagedResultDto<ItemSalesDto>> GetItemsAsync(string companyId, PaginationOptions pagination, CancellationToken ct = default);
    Task<PagedResultDto<SalespersonSalesDto>> GetSalespersonsAsync(string companyId, PaginationOptions pagination, CancellationToken ct = default);
}
