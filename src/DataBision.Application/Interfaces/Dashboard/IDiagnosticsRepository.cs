using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Application.Interfaces.Dashboard;

public interface IDiagnosticsRepository
{
    Task<bool> CanConnectAsync(CancellationToken ct = default);
    Task<DateTime?> GetMartLastTransformedAtAsync(string companyId, CancellationToken ct = default);
    Task<IReadOnlyList<TableCountDto>> GetTableCountsAsync(string companyId, CancellationToken ct = default);
    Task<bool> HasCheckpointsAsync(string companyId, CancellationToken ct = default);
    Task<DateTime?> GetLastExtractionRunAsync(string companyId, CancellationToken ct = default);
}
