using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Application.Interfaces.Dashboard;

public interface IDiagnosticsService
{
    Task<NativeBiDiagnosticsDto> GetDiagnosticsAsync(string companyId, CancellationToken ct = default);
    Task<NativeBiTableCountsDto> GetTableCountsAsync(string companyId, CancellationToken ct = default);
}
