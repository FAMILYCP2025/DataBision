using DataBision.Application.Interfaces;
using Npgsql;

namespace DataBision.Infrastructure.Repositories.Dashboard;

public sealed class MartAdminRefreshService(string connectionString) : IMartAdminRefreshService
{
    private static readonly (string Function, string Module)[] Modules =
    [
        ("mart.refresh_sales",     "sales"),
        ("mart.refresh_purchases", "purchases"),
        ("mart.refresh_inventory", "inventory"),
        ("mart.refresh_finance",   "finance"),
    ];

    public async Task<IReadOnlyList<(string Module, string Object, int RowsAffected)>> RefreshAllMartAsync(
        string companyId, CancellationToken ct = default)
    {
        var results = new List<(string, string, int)>();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        foreach (var (function, module) in Modules)
        {
            try
            {
                await using var cmd = new NpgsqlCommand(
                    $"SELECT object_name, rows_affected FROM {function}($1)", conn);
                cmd.Parameters.AddWithValue(companyId);

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var objName = reader.GetString(0);
                    var rows    = reader.GetInt32(1);
                    results.Add((module, objName, rows));
                }
            }
            catch (PostgresException ex) when (ex.SqlState == "42883")
            {
                results.Add((module, $"{function} (not found)", 0));
            }
        }

        return results;
    }
}
