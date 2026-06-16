using DataBision.Extractor.Operations;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DataBision.Extractor.Transformations;

/// <summary>
/// Executes stg.refresh_all and mart.refresh_all against the staging PostgreSQL database.
/// Uses a direct Npgsql connection — no EF, no SAP, no Ingest API required.
/// </summary>
public sealed class TransformationRunner(
    string connectionString,
    ILogger<TransformationRunner> logger,
    IOperationsLogger? opsLogger = null) : ITransformationRunner
{
    public async Task<IReadOnlyList<(string Object, int RowsAffected)>> RefreshStgAsync(
        string companyId, CancellationToken ct = default)
    {
        logger.LogInformation("STG refresh starting — company_id={CompanyId}", companyId);
        var runId = opsLogger is not null
            ? await opsLogger.StartTransformRunAsync(companyId, "STG", ct)
            : 0L;
        string? lastErr = null;
        IReadOnlyList<(string, int)> results;
        try
        {
            results = await ExecuteFunctionAsync("stg.refresh_all", companyId, ct);
            logger.LogInformation("STG refresh complete — {Count} object(s) processed", results.Count);
        }
        catch (Exception ex)
        {
            lastErr = ex.Message;
            if (opsLogger is not null)
                await opsLogger.CompleteTransformRunAsync(runId, "ERROR", 0, lastErr, ct);
            throw;
        }
        if (opsLogger is not null)
        {
            await opsLogger.CompleteTransformRunAsync(runId, "SUCCESS", results.Count, null, ct);
            await opsLogger.RefreshPipelineHealthAsync(companyId, ct);
        }
        return results;
    }

    public async Task<IReadOnlyList<(string Object, int RowsAffected)>> RefreshMartAsync(
        string companyId, CancellationToken ct = default)
    {
        logger.LogInformation("MART refresh starting — company_id={CompanyId}", companyId);
        var runId = opsLogger is not null
            ? await opsLogger.StartTransformRunAsync(companyId, "MART", ct)
            : 0L;
        IReadOnlyList<(string, int)> results;
        try
        {
            results = await ExecuteFunctionAsync("mart.refresh_all", companyId, ct);
            logger.LogInformation("MART refresh complete — {Count} object(s) processed", results.Count);
        }
        catch (Exception ex)
        {
            if (opsLogger is not null)
                await opsLogger.CompleteTransformRunAsync(runId, "ERROR", 0, ex.Message, ct);
            throw;
        }
        if (opsLogger is not null)
        {
            await opsLogger.CompleteTransformRunAsync(runId, "SUCCESS", results.Count, null, ct);
            await opsLogger.RefreshPipelineHealthAsync(companyId, ct);
            await opsLogger.EvaluateAlertRulesAsync(companyId, ct);
        }
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

    // Dashboard tables populated by mart.refresh_all_processes
    private static readonly (string Table, string Label)[] ProcessDashboardTables =
    [
        ("mart.sales_customer_dashboard",    "sales_customer_dashboard"),
        ("mart.sales_item_dashboard",        "sales_item_dashboard"),
        ("mart.sales_fulfillment_dashboard", "sales_fulfillment_dashboard"),
        ("mart.finance_ar_aging_dashboard",  "finance_ar_aging_dashboard"),
        ("mart.finance_ap_aging_dashboard",  "finance_ap_aging_dashboard"),
        ("mart.finance_executive_daily",     "finance_executive_daily"),
        ("mart.inventory_rotation_dashboard","inventory_rotation_dashboard"),
        ("mart.inventory_stock_dashboard",   "inventory_stock_dashboard"),
        ("mart.inventory_warehouse_dashboard","inventory_warehouse_dashboard"),
        ("mart.purchase_executive_daily",    "purchase_executive_daily"),
        ("mart.purchase_supplier_dashboard", "purchase_supplier_dashboard"),
        ("mart.purchase_receiving_dashboard","purchase_receiving_dashboard"),
    ];

    public async Task<IReadOnlyList<(string Object, int RowsAffected)>> RefreshProcessMartAsync(
        string companyId, CancellationToken ct = default)
    {
        logger.LogInformation("MART process-dashboard refresh starting — company_id={CompanyId}", companyId);
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);

            // mart.refresh_all_processes returns VOID — use ExecuteNonQueryAsync
            await using (var cmd = new NpgsqlCommand(
                "SELECT mart.refresh_all_processes(@company_id)", conn))
            {
                cmd.Parameters.AddWithValue("company_id", companyId);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // Query row counts per dashboard table to report results
            var results = new List<(string, int)>();
            foreach (var (table, label) in ProcessDashboardTables)
            {
                await using var countCmd = new NpgsqlCommand(
                    $"SELECT COUNT(*)::int FROM {table} WHERE company_id = @company_id", conn);
                countCmd.Parameters.AddWithValue("company_id", companyId);
                var count = (int)(await countCmd.ExecuteScalarAsync(ct) ?? 0);
                results.Add((label, count));
                logger.LogInformation("  mart.refresh_all_processes.{Label}: {Count} row(s)", label, count);
            }

            logger.LogInformation("MART process-dashboard refresh complete — {Count} table(s) populated", results.Count);
            return results;
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "42883")
        {
            // 42883 = undefined_function — migration not yet applied
            logger.LogError("mart.refresh_all_processes does not exist in Supabase. Apply Sprint 8C migrations first.");
            throw;
        }
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
