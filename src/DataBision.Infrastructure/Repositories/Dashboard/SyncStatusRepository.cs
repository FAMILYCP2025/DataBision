using Dapper;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Npgsql;

namespace DataBision.Infrastructure.Repositories.Dashboard;

public sealed class SyncStatusRepository(string connectionString) : ISyncStatusRepository
{
    private static readonly string[] SapObjects =
        ["OINV", "INV1", "ORIN", "RIN1", "OCRD", "OITM", "OSLP"];

    private NpgsqlConnection OpenConnection() => new(connectionString);

    public async Task<IReadOnlyList<SyncObjectStatusDto>> GetCheckpointsAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                sap_object              AS "SapObject",
                watermark_date          AS "WatermarkDate",
                last_successful_run_utc AS "LastSuccessfulRunUtc",
                total_rows_ingested     AS "TotalRowsIngested"
            FROM ctl.ingest_checkpoint
            WHERE company_id = @company_id
            ORDER BY sap_object;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var rows = await conn.QueryAsync<CheckpointRow>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));

        var found = rows.ToDictionary(r => r.SapObject, r => r);

        return SapObjects.Select(obj =>
        {
            if (!found.TryGetValue(obj, out var row))
                return new SyncObjectStatusDto { SapObject = obj, Status = "no_data" };

            var status = row.LastSuccessfulRunUtc.HasValue
                ? DetermineObjectStatus(row.LastSuccessfulRunUtc.Value)
                : "no_data";

            return new SyncObjectStatusDto
            {
                SapObject              = row.SapObject,
                WatermarkDate          = row.WatermarkDate,
                LastSuccessfulRunUtc   = row.LastSuccessfulRunUtc,
                TotalRowsIngested      = row.TotalRowsIngested,
                Status                 = status,
            };
        }).ToList();
    }

    public async Task<DateTime?> GetLastExtractionRunAtUtcAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT MAX(finished_at_utc)
            FROM ctl.extraction_run
            WHERE company_id = @company_id AND status = 'completed';
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<DateTime?>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));
    }

    public async Task<DateTime?> GetMartTransformedAtUtcAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT transformed_at_utc
            FROM mart.sales_kpi_summary
            WHERE company_id = @company_id
            LIMIT 1;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<DateTime?>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));
    }

    public async Task<DateTime?> GetStgTransformedAtUtcAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT MAX(transformed_at_utc)
            FROM stg.sales_invoice
            WHERE company_id = @company_id;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<DateTime?>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<MartTableStatusDto>> GetMartTableStatusAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 'sales_daily'       AS table_name,
                   COUNT(*)::int       AS row_count,
                   MAX(transformed_at_utc) AS transformed_at_utc
            FROM mart.sales_daily WHERE company_id = @company_id
            UNION ALL
            SELECT 'sales_monthly',      COUNT(*)::int, MAX(transformed_at_utc)
            FROM mart.sales_monthly WHERE company_id = @company_id
            UNION ALL
            SELECT 'customer_sales',     COUNT(*)::int, MAX(transformed_at_utc)
            FROM mart.customer_sales WHERE company_id = @company_id
            UNION ALL
            SELECT 'item_sales',         COUNT(*)::int, MAX(transformed_at_utc)
            FROM mart.item_sales WHERE company_id = @company_id
            UNION ALL
            SELECT 'salesperson_sales',  COUNT(*)::int, MAX(transformed_at_utc)
            FROM mart.salesperson_sales WHERE company_id = @company_id
            UNION ALL
            SELECT 'sales_kpi_summary',  COUNT(*)::int, MAX(transformed_at_utc)
            FROM mart.sales_kpi_summary WHERE company_id = @company_id;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var rows = await conn.QueryAsync<TableStatusRow>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));

        return rows.Select(r => new MartTableStatusDto
        {
            TableName        = r.TableName,
            RowCount         = r.RowCount,
            TransformedAtUtc = r.TransformedAtUtc,
        }).ToList();
    }

    private static string DetermineObjectStatus(DateTime lastRun)
    {
        var age = DateTime.UtcNow - lastRun;
        if (age.TotalHours <= 24) return "ok";
        if (age.TotalHours <= 48) return "warning";
        return "error";
    }

    private sealed class CheckpointRow
    {
        public string SapObject { get; set; } = string.Empty;
        public string? WatermarkDate { get; set; }
        public DateTime? LastSuccessfulRunUtc { get; set; }
        public long TotalRowsIngested { get; set; }
    }

    private sealed class TableStatusRow
    {
        public string TableName { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public DateTime? TransformedAtUtc { get; set; }
    }
}
