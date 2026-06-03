using System.Diagnostics;
using System.Text.Json.Nodes;
using DataBision.Extractor.Options;
using DataBision.Extractor.ServiceLayer;
using Microsoft.Extensions.Logging;

namespace DataBision.Extractor.Extraction.Jobs;

/// <summary>
/// Extracts BusinessPartners (OCRD) from SAP B1 Service Layer.
/// Incremental by UpdateDate. Falls back to minimal $select if full set fails.
/// </summary>
public sealed class OcrdExtractorJob : IExtractorJob
{
    public string SapObject => "OCRD";

    private const string FullSelect    = "CardCode,CardName,CardType,GroupCode,FederalTaxID,CurrentAccountBalance,UpdateDate";
    private const string MinimalSelect = "CardCode,CardName,CardType,UpdateDate";

    private readonly IServiceLayerClient _sl;
    private readonly ExtractorOptions _options;
    private readonly ILogger<OcrdExtractorJob> _log;

    public OcrdExtractorJob(IServiceLayerClient sl, ExtractorOptions options, ILogger<OcrdExtractorJob> log)
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

            _log.LogInformation("OCRD: {Count} rows in {Ms}ms (select={Sel})", rows.Count, sw.ElapsedMilliseconds, usedSelect);
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
            _log.LogError("OCRD: extraction failed — {Message}", ex.Message);
            return new ExtractionResult { SapObject = SapObject, Success = false, Error = ex.Message, Duration = sw.Elapsed };
        }
    }

    private async Task<(JsonArray rows, string usedSelect)> FetchWithFallback(CancellationToken ct)
    {
        try
        {
            var query = $"$top={_options.PageSize}&$select={FullSelect}&$orderby=UpdateDate asc";
            return (await _sl.GetAsync("BusinessPartners", query, ct), FullSelect);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase)
                                                 || ex.Message.Contains("400"))
        {
            _log.LogWarning("OCRD: full $select failed — retrying with minimal fields. Error: {Msg}", ex.Message);
            var q = $"$top={_options.PageSize}&$select={MinimalSelect}&$orderby=UpdateDate asc";
            return (await _sl.GetAsync("BusinessPartners", q, ct), MinimalSelect);
        }
    }

    private void LogSample(JsonArray rows)
    {
        foreach (var row in rows.Take(3))
        {
            var code = row?["CardCode"]?.ToString()  ?? "?";
            var type = row?["CardType"]?.ToString()  ?? "?";
            var upd  = row?["UpdateDate"]?.ToString() ?? "?";
            _log.LogInformation("  OCRD sample: CardCode={Code}, CardType={Type}, UpdateDate={Upd}", code, type, upd);
        }
    }

    private static string? MaxUpdateDate(JsonArray rows) =>
        rows.Select(r => r?["UpdateDate"]?.ToString()).Where(d => d != null).OrderDescending().FirstOrDefault();
}
