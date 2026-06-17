using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Application.Interfaces.Dashboard;

public interface IFilterOptionsService
{
    Task<IReadOnlyList<FilterOptionDto>> GetItemGroupsAsync(string companyId, CancellationToken ct = default);
    Task<IReadOnlyList<FilterOptionDto>> GetCustomerGroupsAsync(string companyId, CancellationToken ct = default);
    Task<IReadOnlyList<FilterOptionDto>> GetSupplierGroupsAsync(string companyId, CancellationToken ct = default);
    Task<IReadOnlyList<FilterOptionDto>> GetWarehousesAsync(string companyId, CancellationToken ct = default);
    Task<IReadOnlyList<FilterOptionDto>> GetSalespersonsAsync(string companyId, CancellationToken ct = default);
}
