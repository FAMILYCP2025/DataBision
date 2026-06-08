using Dapper;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Npgsql;

namespace DataBision.Infrastructure.Repositories.Dashboard;

public sealed class DiagnosticsRepository(string connectionString) : IDiagnosticsRepository
{
    private NpgsqlConnection OpenConnection() => new(connectionString);

    public async Task<bool> CanConnectAsync(CancellationToken ct = default)
    {
        try
        {
            await using var conn = OpenConnection();
            await conn.OpenAsync(ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<DateTime?> GetMartLastTransformedAtAsync(
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

    public async Task<IReadOnlyList<TableCountDto>> GetTableCountsAsync(
        string companyId, CancellationToken ct = default)
    {
        // One query for stg tables, one UNION for mart tables
        const string sql = """
            SELECT 'stg'           AS schema,
                   'sales_invoice' AS table_name,
                   COUNT(*)        AS row_count,
                   NULL::timestamptz AS transformed_at_utc
            FROM stg.sales_invoice WHERE company_id = @company_id

            UNION ALL

            SELECT 'stg', 'credit_memo', COUNT(*), NULL
            FROM stg.credit_memo WHERE company_id = @company_id

            UNION ALL

            SELECT 'mart', 'sales_daily', COUNT(*), MAX(transformed_at_utc)
            FROM mart.sales_daily WHERE company_id = @company_id

            UNION ALL

            SELECT 'mart', 'sales_monthly', COUNT(*), MAX(transformed_at_utc)
            FROM mart.sales_monthly WHERE company_id = @company_id

            UNION ALL

            SELECT 'mart', 'customer_sales', COUNT(*), MAX(transformed_at_utc)
            FROM mart.customer_sales WHERE company_id = @company_id

            UNION ALL

            SELECT 'mart', 'item_sales', COUNT(*), MAX(transformed_at_utc)
            FROM mart.item_sales WHERE company_id = @company_id

            UNION ALL

            SELECT 'mart', 'salesperson_sales', COUNT(*), MAX(transformed_at_utc)
            FROM mart.salesperson_sales WHERE company_id = @company_id

            UNION ALL

            SELECT 'mart', 'sales_kpi_summary', COUNT(*), MAX(transformed_at_utc)
            FROM mart.sales_kpi_summary WHERE company_id = @company_id

            ORDER BY 1, 2;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var rows = await conn.QueryAsync<TableCountRow>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));

        return rows.Select(r => new TableCountDto
        {
            Schema          = r.Schema,
            TableName       = r.TableName,
            RowCount        = r.RowCount,
            TransformedAtUtc = r.TransformedAtUtc,
        }).ToList();
    }

    public async Task<bool> HasCheckpointsAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM ctl.ingest_checkpoint WHERE company_id = @company_id
            );
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        return await conn.QueryFirstAsync<bool>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));
    }

    public async Task<DateTime?> GetLastExtractionRunAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT MAX(completed_at)
            FROM ctl.extraction_run
            WHERE company_id = @company_id
              AND status = 'completed';
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        return await conn.QueryFirstOrDefaultAsync<DateTime?>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));
    }

    private sealed class TableCountRow
    {
        public string Schema { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public long RowCount { get; set; }
        public DateTime? TransformedAtUtc { get; set; }
    }
}
