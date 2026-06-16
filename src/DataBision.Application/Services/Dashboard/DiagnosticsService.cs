using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;

namespace DataBision.Application.Services.Dashboard;

public sealed class DiagnosticsService(IDiagnosticsRepository repo) : IDiagnosticsService
{
    private static readonly TimeSpan OkThreshold      = TimeSpan.FromHours(24);
    private static readonly TimeSpan WarningThreshold = TimeSpan.FromHours(48);

    public async Task<NativeBiDiagnosticsDto> GetDiagnosticsAsync(
        string companyId, CancellationToken ct = default)
    {
        var checks = new List<DiagnosticCheckDto>();
        var now    = DateTime.UtcNow;

        // 1. Staging connection
        var canConnect = await SafeCheck(() => repo.CanConnectAsync(ct), fallback: false);
        checks.Add(new DiagnosticCheckDto
        {
            Name   = "staging_connection",
            Status = canConnect ? "ok" : "error",
            Detail = canConnect ? "Supabase reachable." : "Cannot connect to staging database.",
        });

        // 2. MART summary exists + freshness
        var transformedAt = canConnect
            ? await SafeCheck(() => repo.GetMartLastTransformedAtAsync(companyId, ct))
            : null;

        var freshnessStatus = EvalFreshness(transformedAt, now);
        checks.Add(new DiagnosticCheckDto
        {
            Name   = "mart_data_freshness",
            Status = freshnessStatus,
            Detail = transformedAt.HasValue
                ? $"Last transform: {transformedAt:u} ({FormatAge(now - transformedAt.Value)} ago)."
                : "No MART data found for this company.",
        });

        // 3. MART tables populated
        var tables = canConnect
            ? await SafeCheck<IReadOnlyList<TableCountDto>?>(() => repo.GetTableCountsAsync(companyId, ct))
            : null;

        var martRows       = tables?.Where(t => t.Schema == "mart").ToList() ?? [];
        var emptyMartTable = martRows.Any(t => t.RowCount == 0);
        checks.Add(new DiagnosticCheckDto
        {
            Name   = "mart_tables_populated",
            Status = martRows.Count == 0 ? "unknown"
                   : emptyMartTable     ? "warning"
                                        : "ok",
            Detail = martRows.Count == 0
                ? "MART tables not accessible."
                : $"{martRows.Count} MART tables checked. {(emptyMartTable ? "One or more empty." : "All have rows.")}",
        });

        // 4. Checkpoints exist
        var hasCheckpoints = canConnect
            && await SafeCheck(() => repo.HasCheckpointsAsync(companyId, ct), fallback: false);

        checks.Add(new DiagnosticCheckDto
        {
            Name   = "checkpoints_exist",
            Status = hasCheckpoints ? "ok" : "warning",
            Detail = hasCheckpoints
                ? "Ingest checkpoints found."
                : "No ingest checkpoints — extractor may not have run yet.",
        });

        // 5. Last extraction run
        var lastRun = canConnect
            ? await SafeCheck(() => repo.GetLastExtractionRunAsync(companyId, ct))
            : null;

        checks.Add(new DiagnosticCheckDto
        {
            Name   = "last_extraction_run",
            Status = lastRun.HasValue ? "ok" : "warning",
            Detail = lastRun.HasValue
                ? $"Last extraction run: {lastRun:u} ({FormatAge(now - lastRun.Value)} ago)."
                : "No extraction run found for this company.",
        });

        var overallStatus = WorstStatus(checks.Select(c => c.Status));

        return new NativeBiDiagnosticsDto
        {
            CompanyId      = companyId,
            Status         = overallStatus,
            Checks         = checks,
            GeneratedAtUtc = now,
        };
    }

    public async Task<NativeBiTableCountsDto> GetTableCountsAsync(
        string companyId, CancellationToken ct = default)
    {
        var tables = await SafeCheck<IReadOnlyList<TableCountDto>?>(() => repo.GetTableCountsAsync(companyId, ct))
                     ?? [];

        return new NativeBiTableCountsDto
        {
            CompanyId      = companyId,
            Tables         = tables,
            GeneratedAtUtc = DateTime.UtcNow,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string EvalFreshness(DateTime? transformedAt, DateTime now)
    {
        if (!transformedAt.HasValue) return "unknown";
        var age = now - transformedAt.Value;
        if (age <= OkThreshold)      return "ok";
        if (age <= WarningThreshold) return "warning";
        return "error";
    }

    private static string WorstStatus(IEnumerable<string> statuses)
    {
        var order = new[] { "error", "warning", "unknown", "ok" };
        foreach (var s in order)
            if (statuses.Contains(s)) return s;
        return "unknown";
    }

    private static string FormatAge(TimeSpan age)
    {
        if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes}m";
        if (age.TotalHours   < 48) return $"{age.TotalHours:0.0}h";
        return $"{age.TotalDays:0.0}d";
    }

    private static async Task<T> SafeCheck<T>(Func<Task<T>> fn, T fallback = default!)
    {
        try { return await fn(); }
        catch { return fallback; }
    }
}
