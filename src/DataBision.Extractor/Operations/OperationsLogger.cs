using Microsoft.Extensions.Logging;
using Npgsql;

namespace DataBision.Extractor.Operations;

/// <summary>
/// Writes OPS observability data to the ops.* schema using Npgsql directly.
/// All methods catch exceptions and log a warning — never rethrow.
/// </summary>
public sealed class OperationsLogger(string connectionString, ILogger<OperationsLogger> log) : IOperationsLogger
{
    public async Task<long> StartExtractorRunAsync(
        string companyId, string sapObject, string mode,
        int pageSize, int maxPages, CancellationToken ct = default)
    {
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("""
                INSERT INTO ops.extractor_run
                    (company_id, sap_object, status, pages_fetched, rows_extracted, rows_inserted, rows_updated, hit_max_pages)
                VALUES (@company_id, @sap_object, 'RUNNING', 0, 0, 0, 0, false)
                RETURNING id;
                """, conn);
            cmd.Parameters.AddWithValue("company_id", companyId);
            cmd.Parameters.AddWithValue("sap_object", sapObject);
            var id = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt64(id);
        }
        catch (Exception ex)
        {
            log.LogWarning("OPS: StartExtractorRun({Obj}) failed — {Msg}", sapObject, ex.Message);
            return 0L;
        }
    }

    public async Task CompleteExtractorRunAsync(
        long runId, string status,
        int pagesFetched, int rowsExtracted, int rowsInserted, int rowsUpdated,
        bool hitMaxPages, string? lastError, string? watermarkDate,
        CancellationToken ct = default)
    {
        if (runId == 0L) return;
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("""
                UPDATE ops.extractor_run
                SET finished_at_utc = NOW(), status = @status,
                    pages_fetched = @pages_fetched, rows_extracted = @rows_extracted,
                    rows_inserted = @rows_inserted, rows_updated = @rows_updated,
                    hit_max_pages = @hit_max_pages, last_error = @last_error,
                    watermark_date = @watermark_date
                WHERE id = @id;
                """, conn);
            cmd.Parameters.AddWithValue("id", runId);
            cmd.Parameters.AddWithValue("status", status);
            cmd.Parameters.AddWithValue("pages_fetched", pagesFetched);
            cmd.Parameters.AddWithValue("rows_extracted", rowsExtracted);
            cmd.Parameters.AddWithValue("rows_inserted", rowsInserted);
            cmd.Parameters.AddWithValue("rows_updated", rowsUpdated);
            cmd.Parameters.AddWithValue("hit_max_pages", hitMaxPages);
            cmd.Parameters.AddWithValue("last_error", (object?)lastError ?? DBNull.Value);
            cmd.Parameters.AddWithValue("watermark_date", (object?)watermarkDate ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogWarning("OPS: CompleteExtractorRun(id={Id}) failed — {Msg}", runId, ex.Message);
        }
    }

    public async Task LogExtractorPageAsync(
        long runId, string sapObject,
        int pageNumber, int skipOffset, int topCount, int rowsReceived,
        long elapsedMs, string status, string? errorCode, string? errorMessage,
        CancellationToken ct = default)
    {
        if (runId == 0L) return;
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("""
                INSERT INTO ops.extractor_page_log
                    (run_id, sap_object, page_number, skip_offset, top_count, rows_received, elapsed_ms, status, error_code, error_message)
                VALUES (@run_id, @sap_object, @page_number, @skip_offset, @top_count, @rows_received, @elapsed_ms, @status, @error_code, @error_message);
                """, conn);
            cmd.Parameters.AddWithValue("run_id", runId);
            cmd.Parameters.AddWithValue("sap_object", sapObject);
            cmd.Parameters.AddWithValue("page_number", pageNumber);
            cmd.Parameters.AddWithValue("skip_offset", skipOffset);
            cmd.Parameters.AddWithValue("top_count", topCount);
            cmd.Parameters.AddWithValue("rows_received", rowsReceived);
            cmd.Parameters.AddWithValue("elapsed_ms", elapsedMs);
            cmd.Parameters.AddWithValue("status", status);
            cmd.Parameters.AddWithValue("error_code", (object?)errorCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("error_message", (object?)errorMessage ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogWarning("OPS: LogExtractorPage(runId={Id},page={P}) failed — {Msg}", runId, pageNumber, ex.Message);
        }
    }

    public async Task<long> StartTransformRunAsync(
        string companyId, string transformType, CancellationToken ct = default)
    {
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("""
                INSERT INTO ops.transform_run (company_id, transform_type, status, objects_refreshed)
                VALUES (@company_id, @transform_type, 'RUNNING', 0)
                RETURNING id;
                """, conn);
            cmd.Parameters.AddWithValue("company_id", companyId);
            cmd.Parameters.AddWithValue("transform_type", transformType);
            var id = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt64(id);
        }
        catch (Exception ex)
        {
            log.LogWarning("OPS: StartTransformRun({Type}) failed — {Msg}", transformType, ex.Message);
            return 0L;
        }
    }

    public async Task CompleteTransformRunAsync(
        long runId, string status, int objectsRefreshed,
        string? lastError, CancellationToken ct = default)
    {
        if (runId == 0L) return;
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("""
                UPDATE ops.transform_run
                SET finished_at_utc = NOW(), status = @status,
                    objects_refreshed = @objects_refreshed, last_error = @last_error
                WHERE id = @id;
                """, conn);
            cmd.Parameters.AddWithValue("id", runId);
            cmd.Parameters.AddWithValue("status", status);
            cmd.Parameters.AddWithValue("objects_refreshed", objectsRefreshed);
            cmd.Parameters.AddWithValue("last_error", (object?)lastError ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogWarning("OPS: CompleteTransformRun(id={Id}) failed — {Msg}", runId, ex.Message);
        }
    }

    public async Task RefreshPipelineHealthAsync(string companyId, CancellationToken ct = default)
    {
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT ops.refresh_pipeline_health(@company_id);", conn);
            cmd.Parameters.AddWithValue("company_id", companyId);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogWarning("OPS: RefreshPipelineHealth({CompanyId}) failed — {Msg}", companyId, ex.Message);
        }
    }

    public async Task EvaluateAlertRulesAsync(string companyId, CancellationToken ct = default)
    {
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT ops.evaluate_alert_rules(@company_id);", conn);
            cmd.Parameters.AddWithValue("company_id", companyId);
            var result = await cmd.ExecuteScalarAsync(ct);
            var fired = result is null ? 0 : Convert.ToInt32(result);
            if (fired > 0)
                log.LogWarning("OPS: {Fired} alert rule(s) fired for company={CompanyId}", fired, companyId);
        }
        catch (Exception ex)
        {
            log.LogWarning("OPS: EvaluateAlertRules({CompanyId}) failed — {Msg}", companyId, ex.Message);
        }
    }
}
