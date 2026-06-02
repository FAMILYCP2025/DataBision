using DataBision.Extractor.Options;
using Microsoft.Extensions.Logging;

namespace DataBision.Extractor.Extraction;

/// <summary>
/// Orchestrates one or more IExtractorJob instances by SAP object name.
/// Sprint 3B: skeleton — jobs wired up with NotImplemented stubs.
/// Sprint 3C: real IExtractorJob implementations replace the stubs.
/// </summary>
public sealed class ExtractorRunner
{
    private static readonly IReadOnlySet<string> SupportedObjects =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "OSLP", "OCRD", "OITM", "OINV", "ALL" };

    private readonly IReadOnlyDictionary<string, IExtractorJob> _jobs;
    private readonly ExtractorOptions _options;
    private readonly ILogger<ExtractorRunner> _log;

    public ExtractorRunner(
        IEnumerable<IExtractorJob> jobs,
        ExtractorOptions options,
        ILogger<ExtractorRunner> log)
    {
        _jobs    = jobs.ToDictionary(j => j.SapObject, StringComparer.OrdinalIgnoreCase);
        _options = options;
        _log     = log;
    }

    public static bool IsSupported(string objectName) =>
        SupportedObjects.Contains(objectName);

    public async Task<IReadOnlyList<ExtractionResult>> RunAsync(
        string objectName, bool dryRun, CancellationToken ct = default)
    {
        var targets = objectName.Equals("ALL", StringComparison.OrdinalIgnoreCase)
            ? SupportedObjects.Where(o => !o.Equals("ALL", StringComparison.OrdinalIgnoreCase)).ToList()
            : [objectName.ToUpperInvariant()];

        var results = new List<ExtractionResult>();

        foreach (var target in targets)
        {
            _log.LogInformation("=== Extracting {Object} (dry-run={DryRun}) ===", target, dryRun);

            if (_jobs.TryGetValue(target, out var job))
            {
                var result = await job.RunAsync(dryRun, ct);
                results.Add(result);
                LogResult(result);
            }
            else
            {
                // Sprint 3B: no job registered yet — stub result
                var stub = ExtractionResult.NotImplemented(target);
                results.Add(stub);
                _log.LogWarning("{Object}: {Error}", target, stub.Error);
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
            "{Object}: OK — extracted={Ext}, inserted={Ins}, updated={Upd}, skipped={Skp}, duration={Dur}ms",
            r.SapObject, r.RowsExtracted, r.RowsInserted, r.RowsUpdated, r.RowsSkipped,
            (int)r.Duration.TotalMilliseconds);
    }
}
