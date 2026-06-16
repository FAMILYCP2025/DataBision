using DataBision.Application.Interfaces;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;

namespace DataBision.Application.Services.Dashboard;

public sealed class ProcessService(
    IProcessRepository repo,
    IAnalyticsCompanyResolver analyticsResolver) : IProcessService
{
    // Maps app company identifier (slug from JWT) → analytics company_id in cfg.* tables.
    private string Map(string companyId) => analyticsResolver.Resolve(companyId);

    public Task<IReadOnlyList<ProcessDto>> GetEnabledProcessesAsync(string companyId, CancellationToken ct = default)
        => repo.GetEnabledProcessesAsync(Map(companyId), ct);

    public Task<IReadOnlyList<DashboardItemDto>> GetDashboardsByProcessAsync(string companyId, string processCode, CancellationToken ct = default)
        => repo.GetDashboardsByProcessAsync(Map(companyId), processCode, ct);
}
