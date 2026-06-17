using DataBision.Application.Interfaces;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;

namespace DataBision.Application.Services.Dashboard;

public sealed class ProcessService(
    IProcessRepository repo,
    IAnalyticsCompanyResolver analyticsResolver) : IProcessService
{
    private Task<string> MapAsync(string companyId, CancellationToken ct = default)
        => analyticsResolver.ResolveAsync(companyId, ct);

    public async Task<IReadOnlyList<ProcessDto>> GetEnabledProcessesAsync(string companyId, CancellationToken ct = default)
        => await repo.GetEnabledProcessesAsync(await MapAsync(companyId, ct), ct);

    public async Task<IReadOnlyList<DashboardItemDto>> GetDashboardsByProcessAsync(string companyId, string processCode, CancellationToken ct = default)
        => await repo.GetDashboardsByProcessAsync(await MapAsync(companyId, ct), processCode, ct);
}
