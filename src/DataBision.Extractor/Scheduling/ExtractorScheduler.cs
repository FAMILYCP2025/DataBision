using DataBision.Extractor.Extraction;
using DataBision.Extractor.Options;
using Microsoft.Extensions.Logging;

namespace DataBision.Extractor.Scheduling;

/// <summary>
/// Runs one extraction cycle over the configured object list.
/// Login/logout and scheduling loop are the caller's responsibility (Program.cs),
/// keeping the scheduler focused on per-object dispatch and error isolation.
/// </summary>
public sealed class ExtractorScheduler(
    ExtractorRunner runner,
    ExtractorOptions options,
    ILogger<ExtractorScheduler> log)
{
    public IReadOnlyList<string> Objects => options.Objects;

    /// <summary>
    /// Runs all configured objects sequentially. A failed object is logged and skipped;
    /// the cycle continues with the remaining objects.
    /// </summary>
    public async Task<IReadOnlyList<ScheduledExtractionSummary>> RunCycleAsync(
        bool send, CancellationToken ct = default)
    {
        var summaries = new List<ScheduledExtractionSummary>(options.Objects.Length);

        foreach (var obj in options.Objects)
        {
            if (ct.IsCancellationRequested)
            {
                log.LogInformation("Scheduler: cancellation requested — stopping after {Done} object(s).",
                    summaries.Count);
                break;
            }

            try
            {
                var results = await runner.RunAsync(obj, dryRun: false, send, ct);
                summaries.AddRange(results.Select(ScheduledExtractionSummary.FromResult));
            }
            catch (OperationCanceledException)
            {
                log.LogInformation("Scheduler: {Obj} cancelled.", obj);
                break;
            }
            catch (Exception ex)
            {
                log.LogError("Scheduler: {Obj} threw unhandled exception — {Msg}", obj, ex.Message);
                summaries.Add(ScheduledExtractionSummary.Failed(obj, ex.Message));
            }
        }

        return summaries;
    }

    /// <summary>Writes a structured summary table to the log after each cycle.</summary>
    public void LogCycleSummary(int cycleNum, IReadOnlyList<ScheduledExtractionSummary> summaries)
    {
        log.LogInformation("=== Cycle {N} summary ===", cycleNum);
        foreach (var s in summaries)
        {
            if (s.Success)
                log.LogInformation(
                    "  {Obj}: extracted={Ext}, inserted={Ins}, updated={Upd}, skipped={Skp}, duration={Dur}ms",
                    s.SapObject, s.RowsExtracted, s.RowsInserted, s.RowsUpdated, s.RowsSkipped,
                    (int)s.Duration.TotalMilliseconds);
            else
                log.LogError("  {Obj}: FAILED — {Error}", s.SapObject, s.Error);
        }

        var allOk = summaries.All(s => s.Success);
        var totalIns = summaries.Sum(s => s.RowsInserted);
        var totalUpd = summaries.Sum(s => s.RowsUpdated);
        var totalSkp = summaries.Sum(s => s.RowsSkipped);
        log.LogInformation("=== Cycle {N} {Status} — inserted={Ins}, updated={Upd}, skipped={Skp} ===",
            cycleNum, allOk ? "OK" : "PARTIAL FAILURE", totalIns, totalUpd, totalSkp);
    }
}
