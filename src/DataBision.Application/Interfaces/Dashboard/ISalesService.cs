using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Application.Interfaces.Dashboard;

public interface ISalesService
{
    Task<SalesOverviewDto> GetOverviewAsync(string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default);
    Task<IReadOnlyList<SalesDailyDto>> GetDailyAsync(string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default);
    Task<IReadOnlyList<SalesMonthlyDto>> GetMonthlyAsync(string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerSalesDto>> GetCustomersAsync(string companyId, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<ItemSalesDto>> GetItemsAsync(string companyId, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<SalespersonSalesDto>> GetSalespersonsAsync(string companyId, int limit, CancellationToken ct = default);
}
