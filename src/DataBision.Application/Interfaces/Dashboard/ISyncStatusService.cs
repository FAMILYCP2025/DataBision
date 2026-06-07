using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Application.Interfaces.Dashboard;

public interface ISyncStatusService
{
    Task<SyncStatusDto> GetStatusAsync(string companyId, CancellationToken ct = default);
    Task<IReadOnlyList<SyncObjectStatusDto>> GetObjectsAsync(string companyId, CancellationToken ct = default);
    Task<TransformStatusDto> GetTransformStatusAsync(string companyId, CancellationToken ct = default);
}
