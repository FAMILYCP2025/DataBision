using System.Diagnostics;
using System.Text.Json.Nodes;
using DataBision.Extractor.Options;
using DataBision.Extractor.ServiceLayer;
using Microsoft.Extensions.Logging;

namespace DataBision.Extractor.Extraction.Jobs;

/// <summary>
/// Extracts Invoices (OINV) from SAP B1 Service Layer.
/// Incremental by UpdateDate. Logs UpdateDate format and last-page signal.
/// </summary>
public sealed class OinvExtractorJob : IExtractorJob
{
    public string SapObject => "OINV";

    private const string Select = "DocEntry,DocNum,CardCode,DocDate,DocTotal,UpdateDate";

    private readonly IServiceLayerClient _sl;
    private readonly ExtractorOptions _options;
    private readonly ILogger<OinvExtractorJob> _log;

    public OinvExtractorJob(IServiceLayerClient sl, ExtractorOptions options, ILogger<OinvExtractorJob> log)
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
            var query = $"$top={_options.PageSize}&$select={Select}&$orderby=UpdateDate asc";
            _log.LogInformation("OINV: fetching (top={Top})", _options.PageSize);

            var rows = await _sl.GetAsync("Invoices", query, ct);
            sw.Stop();

            var isLastPage = rows.Count < _options.PageSize;
            _log.LogInformation("OINV: {Count} rows in {Ms}ms — last-page-signal={LastPage}",
                rows.Count, sw.ElapsedMilliseconds, isLastPage);
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
            _log.LogError("OINV: extraction failed — {Message}", ex.Message);
            return new ExtractionResult { SapObject = SapObject, Success = false, Error = ex.Message, Duration = sw.Elapsed };
        }
    }

    private void LogSample(JsonArray rows)
    {
        foreach (var row in rows.Take(3))
        {
            var entry = row?["DocEntry"]?.ToString()  ?? "?";
            var num   = row?["DocNum"]?.ToString()    ?? "?";
            var card  = row?["CardCode"]?.ToString()  ?? "?";
            var date  = row?["DocDate"]?.ToString()   ?? "?";
            var total = row?["DocTotal"]?.ToString()  ?? "?";
            var upd   = row?["UpdateDate"]?.ToString() ?? "?";
            _log.LogInformation(
                "  OINV sample: DocEntry={Entry}, DocNum={Num}, CardCode={Card}, DocDate={Date}, DocTotal={Total}, UpdateDate={Upd}",
                entry, num, card, date, total, upd);
        }
    }

    private static string? MaxUpdateDate(JsonArray rows) =>
        rows.Select(r => r?["UpdateDate"]?.ToString())
            .Where(d => d is not null)
            .OrderDescending()
            .FirstOrDefault();
}
