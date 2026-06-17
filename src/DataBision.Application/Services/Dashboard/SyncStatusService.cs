using DataBision.Application.Interfaces;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;

namespace DataBision.Application.Services.Dashboard;

public sealed class SyncStatusService(
    ISyncStatusRepository repo,
    IAnalyticsCompanyResolver analyticsResolver) : ISyncStatusService
{
    private static readonly TimeSpan OkThreshold      = TimeSpan.FromHours(24);
    private static readonly TimeSpan WarningThreshold = TimeSpan.FromHours(48);

    private Task<string> MapAsync(string companyId, CancellationToken ct = default)
        => analyticsResolver.ResolveAsync(companyId, ct);

    public async Task<SyncStatusDto> GetStatusAsync(string companyId, CancellationToken ct = default)
    {
        var aid              = await MapAsync(companyId, ct);
        var checkpoints      = await repo.GetCheckpointsAsync(aid, ct);
        var lastExtraction   = await repo.GetLastExtractionRunAtUtcAsync(aid, ct);
        var martTransformed  = await repo.GetMartTransformedAtUtcAsync(aid, ct);
        var stgTransformed   = await repo.GetStgTransformedAtUtcAsync(aid, ct);

        var overallStatus = DetermineStatus(martTransformed);

        return new SyncStatusDto
        {
            CompanyId          = companyId,
            OverallStatus      = overallStatus,
            LastSyncAtUtc      = lastExtraction,
            LastTransformAtUtc = martTransformed,
            Objects            = checkpoints,
            DataFreshness      = new DataFreshnessDto
            {
                RawLastUpdatedAtUtc      = lastExtraction,
                StgLastTransformedAtUtc  = stgTransformed,
                MartLastTransformedAtUtc = martTransformed,
            }
        };
    }

    public async Task<IReadOnlyList<SyncObjectStatusDto>> GetObjectsAsync(
        string companyId, CancellationToken ct = default)
        => await repo.GetCheckpointsAsync(await MapAsync(companyId, ct), ct);

    public async Task<TransformStatusDto> GetTransformStatusAsync(
        string companyId, CancellationToken ct = default)
    {
        var aid             = await MapAsync(companyId, ct);
        var martTransformed = await repo.GetMartTransformedAtUtcAsync(aid, ct);
        var stgTransformed  = await repo.GetStgTransformedAtUtcAsync(aid, ct);
        var tables          = await repo.GetMartTableStatusAsync(aid, ct);

        return new TransformStatusDto
        {
            CompanyId            = companyId,
            MartTransformedAtUtc = martTransformed,
            StgTransformedAtUtc  = stgTransformed,
            MartTables           = tables,
        };
    }

    private static string DetermineStatus(DateTime? martTransformedAt)
    {
        if (martTransformedAt is null) return "unknown";
        var age = DateTime.UtcNow - martTransformedAt.Value;
        if (age <= OkThreshold)      return "ok";
        if (age <= WarningThreshold) return "warning";
        return "error";
    }
}
