using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces;
using DataBision.Application.Interfaces.Dashboard;

namespace DataBision.Application.Services.Dashboard;

public sealed class FilterOptionsService(
    IFilterOptionsRepository repo,
    IAnalyticsCompanyResolver analyticsResolver) : IFilterOptionsService
{
    private Task<string> MapAsync(string companyId, CancellationToken ct = default)
        => analyticsResolver.ResolveAsync(companyId, ct);

    public async Task<IReadOnlyList<FilterOptionDto>> GetItemGroupsAsync(string companyId, CancellationToken ct = default)
        => await repo.GetItemGroupsAsync(await MapAsync(companyId, ct), ct);

    public async Task<IReadOnlyList<FilterOptionDto>> GetCustomerGroupsAsync(string companyId, CancellationToken ct = default)
        => await repo.GetCustomerGroupsAsync(await MapAsync(companyId, ct), ct);

    public async Task<IReadOnlyList<FilterOptionDto>> GetSupplierGroupsAsync(string companyId, CancellationToken ct = default)
        => await repo.GetSupplierGroupsAsync(await MapAsync(companyId, ct), ct);

    public async Task<IReadOnlyList<FilterOptionDto>> GetWarehousesAsync(string companyId, CancellationToken ct = default)
        => await repo.GetWarehousesAsync(await MapAsync(companyId, ct), ct);

    public async Task<IReadOnlyList<FilterOptionDto>> GetSalespersonsAsync(string companyId, CancellationToken ct = default)
        => await repo.GetSalespersonsAsync(await MapAsync(companyId, ct), ct);
}
