using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;

namespace DataBision.Application.Services.Dashboard;

public sealed class ProcessService(IProcessRepository repo) : IProcessService
{
    public Task<IReadOnlyList<ProcessDto>> GetEnabledProcessesAsync(string companyId, CancellationToken ct = default)
        => repo.GetEnabledProcessesAsync(companyId, ct);

    public Task<IReadOnlyList<DashboardItemDto>> GetDashboardsByProcessAsync(string companyId, string processCode, CancellationToken ct = default)
        => repo.GetDashboardsByProcessAsync(companyId, processCode, ct);
}
