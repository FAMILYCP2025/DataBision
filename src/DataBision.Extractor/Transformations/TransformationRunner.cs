using Microsoft.Extensions.Logging;
using Npgsql;

namespace DataBision.Extractor.Transformations;

/// <summary>
/// Executes stg.refresh_all and mart.refresh_all against the staging PostgreSQL database.
/// Uses a direct Npgsql connection — no EF, no SAP, no Ingest API required.
/// </summary>
public sealed class TransformationRunner(string connectionString, ILogger<TransformationRunner> logger)
    : ITransformationRunner
{
    public async Task<IReadOnlyList<(string Object, int RowsAffected)>> RefreshStgAsync(
        string companyId, CancellationToken ct = default)
    {
        logger.LogInformation("STG refresh starting — company_id={CompanyId}", companyId);
        var results = await ExecuteFunctionAsync("stg.refresh_all", companyId, ct);
        logger.LogInformation("STG refresh complete — {Count} object(s) processed", results.Count);
        return results;
    }

    public async Task<IReadOnlyList<(string Object, int RowsAffected)>> RefreshMartAsync(
        string companyId, CancellationToken ct = default)
    {
        logger.LogInformation("MART refresh starting — company_id={CompanyId}", companyId);
        var results = await ExecuteFunctionAsync("mart.refresh_all", companyId, ct);
        logger.LogInformation("MART refresh complete — {Count} object(s) processed", results.Count);
        return results;
    }

    public async Task<(IReadOnlyList<(string Object, int RowsAffected)> Stg,
                       IReadOnlyList<(string Object, int RowsAffected)> Mart)> RefreshAllAsync(
        string companyId, CancellationToken ct = default)
    {
        var stg  = await RefreshStgAsync(companyId, ct);
        var mart = await RefreshMartAsync(companyId, ct);
        return (stg, mart);
    }

    private async Task<IReadOnlyList<(string, int)>> ExecuteFunctionAsync(
        string functionName, string companyId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            $"SELECT object_name, rows_affected FROM {functionName}(@company_id);", conn);
        cmd.Parameters.AddWithValue("company_id", companyId);

        var results = new List<(string, int)>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var obj  = reader.GetString(0);
            var rows = reader.GetInt32(1);
            results.Add((obj, rows));
            logger.LogInformation("  {Function}.{Object}: {Rows} row(s) affected", functionName, obj, rows);
        }
        return results;
    }
}
