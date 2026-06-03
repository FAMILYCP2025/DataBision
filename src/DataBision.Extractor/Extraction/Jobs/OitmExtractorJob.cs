using System.Diagnostics;
using System.Text.Json.Nodes;
using DataBision.Extractor.Options;
using DataBision.Extractor.ServiceLayer;
using Microsoft.Extensions.Logging;

namespace DataBision.Extractor.Extraction.Jobs;

/// <summary>
/// Extracts Items (OITM) from SAP B1 Service Layer.
/// Incremental by UpdateDate. Falls back to minimal $select if ItemsGroupCode is unavailable.
/// </summary>
public sealed class OitmExtractorJob : IExtractorJob
{
    public string SapObject => "OITM";

    private const string FullSelect    = "ItemCode,ItemName,ItemsGroupCode,UpdateDate";
    private const string MinimalSelect = "ItemCode,ItemName,UpdateDate";

    private readonly IServiceLayerClient _sl;
    private readonly ExtractorOptions _options;
    private readonly ILogger<OitmExtractorJob> _log;

    public OitmExtractorJob(IServiceLayerClient sl, ExtractorOptions options, ILogger<OitmExtractorJob> log)
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
            var (rows, usedSelect) = await FetchWithFallback(ct);
            sw.Stop();

            _log.LogInformation("OITM: {Count} rows in {Ms}ms (select={Sel})",
                rows.Count, sw.ElapsedMilliseconds, usedSelect);
            LogSample(rows);

            return new ExtractionResult
            {
                SapObject     = SapObject,
                Success       = true,
                RowsExtracted = rows.Count,
                Duration      = sw.Elapsed,
                WatermarkDate = MaxUpdateDate(rows)
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _log.LogError("OITM: extraction failed — {Message}", ex.Message);
            return new ExtractionResult { SapObject = SapObject, Success = false, Error = ex.Message, Duration = sw.Elapsed };
        }
    }

    private async Task<(JsonArray rows, string usedSelect)> FetchWithFallback(CancellationToken ct)
    {
        try
        {
            var query = $"$top={_options.PageSize}&$select={FullSelect}&$orderby=UpdateDate asc";
            return (await _sl.GetAsync("Items", query, ct), FullSelect);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase)
                                                 || ex.Message.Contains("400"))
        {
            _log.LogWarning("OITM: full $select failed — retrying with minimal fields. Error: {Msg}", ex.Message);
            var q = $"$top={_options.PageSize}&$select={MinimalSelect}&$orderby=UpdateDate asc";
            return (await _sl.GetAsync("Items", q, ct), MinimalSelect);
        }
    }

    private void LogSample(JsonArray rows)
    {
        foreach (var row in rows.Take(3))
        {
            var code = row?["ItemCode"]?.ToString()  ?? "?";
            var name = Truncate(row?["ItemName"]?.ToString() ?? "");
            var grp  = row?["ItemsGroupCode"];
            var grpInfo = grp is not null
                ? $"{grp} (type={grp.GetValueKind()})"
                : "(not present)";
            var upd  = row?["UpdateDate"]?.ToString() ?? "?";
            _log.LogInformation(
                "  OITM sample: ItemCode={Code}, Name={Name}, ItemsGroupCode={Grp}, UpdateDate={Upd}",
                code, name, grpInfo, upd);
        }
    }

    private static string? MaxUpdateDate(JsonArray rows) =>
        rows.Select(r => r?["UpdateDate"]?.ToString())
            .Where(d => d is not null)
            .OrderDescending()
            .FirstOrDefault();

    private static string Truncate(string s, int max = 40) =>
        s.Length > max ? s[..max] + "…" : s;
}
