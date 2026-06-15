using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Application.Interfaces.Dashboard;

public interface IProcessRepository
{
    Task<IReadOnlyList<ProcessDto>> GetEnabledProcessesAsync(string companyId, CancellationToken ct = default);
    Task<IReadOnlyList<DashboardItemDto>> GetDashboardsByProcessAsync(string companyId, string processCode, CancellationToken ct = default);
}
