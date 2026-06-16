using DataBision.Extractor.Operations;
using DataBision.Extractor.Options;
using DataBision.Extractor.ServiceLayer;
using Microsoft.Extensions.Logging;

namespace DataBision.Extractor.Extraction;

public sealed class ExtractorRunner
{
    private static readonly IReadOnlySet<string> SupportedObjects =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "OSLP", "OCRD", "OITM", "OINV", "INV1", "ORIN", "RIN1",
            "OPOR", "OPDN", "OPCH", "ORDR", "ODLN", "OWTR",
            "ALL"
        };

    // ALL excludes line objects; OITW is in Prepared (no top-level SL entity in v1000290).
    private static readonly IReadOnlySet<string> AllObjects =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "OSLP", "OCRD", "OITM", "OINV", "OPOR", "OPDN", "OPCH", "ORDR", "ODLN", "OWTR" };

    private readonly IReadOnlyDictionary<string, IExtractorJob> _jobs;
    private readonly ExtractorOptions _options;
    private readonly ILogger<ExtractorRunner> _log;
    private readonly IOperationsLogger? _ops;
    private readonly ServiceLayerPaginator? _paginator;

    public ExtractorRunner(
        IEnumerable<IExtractorJob> jobs,
        ExtractorOptions options,
        ILogger<ExtractorRunner> log,
        IOperationsLogger? opsLogger = null,
        ServiceLayerPaginator? paginator = null)
    {
        _jobs      = jobs.ToDictionary(j => j.SapObject, StringComparer.OrdinalIgnoreCase);
        _options   = options;
        _log       = log;
        _ops       = opsLogger;
        _paginator = paginator;
    }

    public static bool IsSupported(string objectName) =>
        SupportedObjects.Contains(objectName);

    public async Task<IReadOnlyList<ExtractionResult>> RunAsync(
        string objectName, bool dryRun, bool send, CancellationToken ct = default)
    {
        var targets = objectName.Equals("ALL", StringComparison.OrdinalIgnoreCase)
            ? AllObjects.ToList()
            : [objectName.ToUpperInvariant()];

        var results = new List<ExtractionResult>();

        foreach (var target in targets)
        {
            _log.LogInformation("=== Extracting {Object} (dry-run={DryRun}, send={Send}) ===", target, dryRun, send);

            long opsRunId = 0L;
            if (_ops is not null && !dryRun)
                opsRunId = await _ops.StartExtractorRunAsync(
                    _options.CompanyId, target, _options.Mode,
                    _options.PageSize, _options.MaxPages, ct);

            if (_paginator is not null && _ops is not null && opsRunId != 0L)
            {
                var capturedId  = opsRunId;
                var capturedObj = target;
                _paginator.OnPage = (pl, innerCt) => _ops.LogExtractorPageAsync(
                    capturedId, capturedObj,
                    pl.PageNumber, pl.Skip, pl.Top, pl.RowsReceived,
                    pl.ElapsedMs, pl.Status, pl.ErrorCode, pl.ErrorMessage, innerCt);
            }

            ExtractionResult result;
            try
            {
                if (_jobs.TryGetValue(target, out var job))
                {
                    result = await job.RunAsync(dryRun, send, ct);
                    results.Add(result);
                    LogResult(result);
                }
                else
                {
                    result = ExtractionResult.NotImplemented(target);
                    results.Add(result);
                    _log.LogWarning("{Object}: {Error}", target, result.Error);
                }
            }
            finally
            {
                if (_paginator is not null) _paginator.OnPage = null;
            }

            if (_ops is not null && !dryRun)
            {
                await _ops.CompleteExtractorRunAsync(
                    opsRunId,
                    result.Success ? "SUCCESS" : "ERROR",
                    pagesFetched: result.PagesFetched,
                    rowsExtracted: result.RowsExtracted,
                    rowsInserted: result.RowsInserted,
                    rowsUpdated: result.RowsUpdated,
                    hitMaxPages: result.HitMaxPages,
                    lastError: result.Error,
                    watermarkDate: result.WatermarkDate,
                    ct);
                await _ops.RefreshPipelineHealthAsync(_options.CompanyId, ct);
                await _ops.EvaluateAlertRulesAsync(_options.CompanyId, ct);
            }
        }

        return results;
    }

    private void LogResult(ExtractionResult r)
    {
        if (r.IsDryRun)
        {
            _log.LogInformation("{Object}: dry-run — no data sent", r.SapObject);
            return;
        }
        if (!r.Success)
        {
            _log.LogError("{Object}: FAILED — {Error}", r.SapObject, r.Error);
            return;
        }
        _log.LogInformation(
            "{Object}: OK — extracted={Ext}, inserted={Ins}, updated={Upd}, skipped={Skp}, pages={Pages}, duration={Dur}ms",
            r.SapObject, r.RowsExtracted, r.RowsInserted, r.RowsUpdated, r.RowsSkipped,
            r.PagesFetched, (int)r.Duration.TotalMilliseconds);
    }
}
