using DataBision.Application.DTOs.Ingest;

namespace DataBision.Application.Interfaces.Ingest;

public interface IIngestCheckpointRepository
{
    Task<CheckpointDto?> GetAsync(string tenantId, string companyId, string sapObject, CancellationToken ct = default);
    Task UpsertAsync(CheckpointDto checkpoint, CancellationToken ct = default);
}
