using Dapper;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Npgsql;

namespace DataBision.Infrastructure.Repositories.Dashboard;

public sealed class ProcessRepository(string connectionString) : IProcessRepository
{
    private NpgsqlConnection OpenConnection() => new(connectionString);

    public async Task<IReadOnlyList<ProcessDto>> GetEnabledProcessesAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                p.process_code      AS "ProcessCode",
                p.process_name      AS "ProcessName",
                p.description       AS "Description",
                p.display_order     AS "DisplayOrder",
                COALESCE(cpe.is_enabled, FALSE) AS "IsEnabled"
            FROM cfg.process p
            LEFT JOIN cfg.company_process_enabled cpe
                ON cpe.process_code = p.process_code
               AND cpe.company_id   = @company_id
            WHERE p.is_active = TRUE
            ORDER BY p.display_order;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<ProcessDto>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<DashboardItemDto>> GetDashboardsByProcessAsync(
        string companyId, string processCode, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                d.dashboard_code    AS "DashboardCode",
                d.dashboard_name    AS "DashboardName",
                d.dashboard_type    AS "DashboardType",
                d.process_code      AS "ProcessCode",
                d.is_active         AS "IsActive",
                d.display_order     AS "DisplayOrder"
            FROM cfg.dashboard d
            WHERE d.process_code = @process_code
              AND d.is_active     = TRUE
            ORDER BY d.display_order;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<DashboardItemDto>(
            new CommandDefinition(sql, new { process_code = processCode }, cancellationToken: ct));
        return rows.ToList();
    }
}
