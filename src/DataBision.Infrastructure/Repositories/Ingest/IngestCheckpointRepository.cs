using DataBision.Application.DTOs.Ingest;
using DataBision.Application.Interfaces.Ingest;
using DataBision.Infrastructure.Data.Staging;
using DataBision.Infrastructure.Data.Staging.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataBision.Infrastructure.Repositories.Ingest;

public sealed class IngestCheckpointRepository(StagingDbContext db) : IIngestCheckpointRepository
{
    public async Task<CheckpointDto?> GetAsync(
        string tenantId, string companyId, string sapObject, CancellationToken ct = default)
    {
        var entity = await db.IngestCheckpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.CompanyId == companyId && c.SapObject == sapObject,
                ct);

        if (entity is null) return null;

        return new CheckpointDto
        {
            TenantId = entity.TenantId,
            CompanyId = entity.CompanyId,
            SapObject = entity.SapObject,
            WatermarkDate = entity.WatermarkDate,
            WatermarkTs = entity.WatermarkTs,
            LastSuccessfulRunUtc = entity.LastSuccessfulRunUtc,
            TotalRowsIngested = entity.TotalRowsIngested,
        };
    }

    public async Task UpsertAsync(CheckpointDto dto, CancellationToken ct = default)
    {
        var entity = await db.IngestCheckpoints
            .FirstOrDefaultAsync(
                c => c.TenantId == dto.TenantId && c.CompanyId == dto.CompanyId && c.SapObject == dto.SapObject,
                ct);

        if (entity is null)
        {
            entity = new IngestCheckpoint
            {
                TenantId = dto.TenantId,
                CompanyId = dto.CompanyId,
                SapObject = dto.SapObject,
            };
            db.IngestCheckpoints.Add(entity);
        }

        entity.WatermarkDate = dto.WatermarkDate;
        entity.WatermarkTs = dto.WatermarkTs;
        entity.LastSuccessfulRunUtc = dto.LastSuccessfulRunUtc;
        entity.TotalRowsIngested = dto.TotalRowsIngested;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
