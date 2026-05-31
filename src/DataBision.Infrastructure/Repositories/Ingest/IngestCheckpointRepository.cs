using Dapper;
using DataBision.Application.DTOs.Ingest;
using DataBision.Application.Interfaces.Ingest;
using Npgsql;

namespace DataBision.Infrastructure.Repositories.Ingest;

/// <summary>
/// Dapper-based repository for ctl.ingest_checkpoint.
/// Uses the same NpgsqlConnection pattern as SapRawRepository to avoid EF Core
/// connection pool contention with PgBouncer transaction mode.
/// </summary>
public sealed class IngestCheckpointRepository(string connectionString) : IIngestCheckpointRepository
{
    private NpgsqlConnection OpenConnection() => new(connectionString);

    public async Task<CheckpointDto?> GetAsync(
        string tenantId, string companyId, string sapObject, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                tenant_id               AS "TenantId",
                company_id              AS "CompanyId",
                sap_object              AS "SapObject",
                watermark_date          AS "WatermarkDate",
                watermark_ts            AS "WatermarkTs",
                last_successful_run_utc AS "LastSuccessfulRunUtc",
                total_rows_ingested     AS "TotalRowsIngested"
            FROM ctl.ingest_checkpoint
            WHERE tenant_id  = @tenant_id
              AND company_id  = @company_id
              AND sap_object  = @sap_object
            LIMIT 1;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var row = await conn.QueryFirstOrDefaultAsync<CheckpointRow>(
            new CommandDefinition(sql,
                new { tenant_id = tenantId, company_id = companyId, sap_object = sapObject },
                cancellationToken: ct));

        if (row is null) return null;

        return new CheckpointDto
        {
            TenantId             = row.TenantId,
            CompanyId            = row.CompanyId,
            SapObject            = row.SapObject,
            WatermarkDate        = row.WatermarkDate,
            WatermarkTs          = row.WatermarkTs,
            LastSuccessfulRunUtc = row.LastSuccessfulRunUtc,
            TotalRowsIngested    = row.TotalRowsIngested,
        };
    }

    public async Task UpsertAsync(CheckpointDto dto, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO ctl.ingest_checkpoint
                (tenant_id, company_id, sap_object,
                 watermark_date, watermark_ts, last_successful_run_utc,
                 total_rows_ingested, updated_at)
            VALUES
                (@tenant_id, @company_id, @sap_object,
                 @watermark_date, @watermark_ts, @last_successful_run_utc,
                 @total_rows_ingested, NOW())
            ON CONFLICT (tenant_id, company_id, sap_object) DO UPDATE SET
                watermark_date          = EXCLUDED.watermark_date,
                watermark_ts            = EXCLUDED.watermark_ts,
                last_successful_run_utc = EXCLUDED.last_successful_run_utc,
                total_rows_ingested     = EXCLUDED.total_rows_ingested,
                updated_at              = NOW();
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        await conn.ExecuteAsync(
            new CommandDefinition(sql,
                new
                {
                    tenant_id               = dto.TenantId,
                    company_id              = dto.CompanyId,
                    sap_object              = dto.SapObject,
                    watermark_date          = dto.WatermarkDate,
                    watermark_ts            = dto.WatermarkTs,
                    last_successful_run_utc = dto.LastSuccessfulRunUtc,
                    total_rows_ingested     = dto.TotalRowsIngested,
                },
                cancellationToken: ct));
    }

    // Private result type for Dapper column mapping (snake_case → PascalCase via SQL aliases)
    private sealed class CheckpointRow
    {
        public string TenantId { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public string SapObject { get; set; } = string.Empty;
        public string? WatermarkDate { get; set; }
        public string? WatermarkTs { get; set; }
        public DateTime? LastSuccessfulRunUtc { get; set; }
        public long TotalRowsIngested { get; set; }
    }
}
