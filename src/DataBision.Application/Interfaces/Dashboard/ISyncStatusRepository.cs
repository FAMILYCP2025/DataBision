using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Application.Interfaces.Dashboard;

public interface ISyncStatusRepository
{
    Task<IReadOnlyList<SyncObjectStatusDto>> GetCheckpointsAsync(string companyId, CancellationToken ct = default);
    Task<DateTime?> GetLastExtractionRunAtUtcAsync(string companyId, CancellationToken ct = default);
    Task<DateTime?> GetMartTransformedAtUtcAsync(string companyId, CancellationToken ct = default);
    Task<DateTime?> GetStgTransformedAtUtcAsync(string companyId, CancellationToken ct = default);
    Task<IReadOnlyList<MartTableStatusDto>> GetMartTableStatusAsync(string companyId, CancellationToken ct = default);
}
