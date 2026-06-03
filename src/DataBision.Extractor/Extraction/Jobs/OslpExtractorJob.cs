using System.Diagnostics;
using System.Text.Json.Nodes;
using DataBision.Extractor.Options;
using DataBision.Extractor.ServiceLayer;
using Microsoft.Extensions.Logging;

namespace DataBision.Extractor.Extraction.Jobs;

/// <summary>
/// Extracts SalesPersons (OSLP) from SAP B1 Service Layer.
/// Full-refresh strategy: UpdateDate is NOT available in SalesPersons on SL 1000290.
/// </summary>
public sealed class OslpExtractorJob : IExtractorJob
{
    public string SapObject => "OSLP";

    private const string Select = "SalesEmployeeCode,SalesEmployeeName";

    private readonly IServiceLayerClient _sl;
    private readonly ExtractorOptions _options;
    private readonly ILogger<OslpExtractorJob> _log;

    public OslpExtractorJob(IServiceLayerClient sl, ExtractorOptions options, ILogger<OslpExtractorJob> log)
    {
        _sl = sl;
        _options = options;
        _log = log;
    }

    public async Task<ExtractionResult> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        if (dryRun) return ExtractionResult.DryRun(SapObject);

        var sw = Stopwatch.StartNew();
        try
        {
            var query = $"$top={_options.PageSize}&$select={Select}";
            _log.LogInformation("OSLP: fetching (strategy=full-refresh, top={Top})", _options.PageSize);

            var rows = await _sl.GetAsync("SalesPersons", query, ct);
            sw.Stop();

            _log.LogInformation("OSLP: {Count} rows in {Ms}ms", rows.Count, sw.ElapsedMilliseconds);
            LogSample(rows);

            return new ExtractionResult
            {
                SapObject    = SapObject,
                Success      = true,
                RowsExtracted = rows.Count,
                Duration     = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _log.LogError("OSLP: extraction failed — {Message}", ex.Message);
            return new ExtractionResult { SapObject = SapObject, Success = false, Error = ex.Message, Duration = sw.Elapsed };
        }
    }

    private void LogSample(JsonArray rows)
    {
        foreach (var row in rows.Take(3))
        {
            var code = row?["SalesEmployeeCode"]?.ToString() ?? "?";
            var name = Truncate(row?["SalesEmployeeName"]?.ToString() ?? "");
            _log.LogInformation("  OSLP sample: SalesEmployeeCode={Code}, Name={Name}", code, name);
        }
    }

    private static string Truncate(string s, int max = 40) =>
        s.Length > max ? s[..max] + "…" : s;
}
