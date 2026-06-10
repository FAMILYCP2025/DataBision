using System.Diagnostics;
using System.Text.Json.Nodes;
using DataBision.Application.DTOs.Ingest.Rows;
using DataBision.Extractor.DataBision;
using DataBision.Extractor.Mapping;
using DataBision.Extractor.Options;
using DataBision.Extractor.ServiceLayer;
using Microsoft.Extensions.Logging;

namespace DataBision.Extractor.Extraction.Jobs;

/// <summary>
/// Extracts SalesPersons (OSLP). Full-refresh — UpdateDate not available in SL 1000290.
/// Paginator handles multi-page extraction.
/// </summary>
public sealed class OslpExtractorJob : IExtractorJob
{
    public string SapObject => "OSLP";

    private const string Endpoint = "api/ingest/sap-b1/salespersons";
    private const string Select   = "SalesEmployeeCode,SalesEmployeeName";

    private readonly IServiceLayerClient       _sl;
    private readonly IDataBisionIngestClient   _ingest;
    private readonly ExtractorOptions          _options;
    private readonly ILogger<OslpExtractorJob> _log;
    private readonly ServiceLayerPaginator     _paginator;

    public OslpExtractorJob(
        IServiceLayerClient sl, IDataBisionIngestClient ingest,
        ExtractorOptions options, ILogger<OslpExtractorJob> log,
        ServiceLayerPaginator paginator)
    {
        _sl        = sl;
        _ingest    = ingest;
        _options   = options;
        _log       = log;
        _paginator = paginator;
    }

    public async Task<ExtractionResult> RunAsync(bool dryRun, bool send, CancellationToken ct = default)
    {
        if (dryRun) return ExtractionResult.DryRun(SapObject);

        var sw = Stopwatch.StartNew();
        try
        {
            _log.LogInformation("OSLP: fetching (strategy=full-refresh, pageSize={Top})", _options.PageSize);

            var result = await _paginator.PaginateAsync(
                SapObject, "SalesPersons", $"$select={Select}",
                _options.PageSize, _options.MaxPages, ct);

            sw.Stop();

            _log.LogInformation("OSLP: {Count} rows in {Ms}ms", result.AllRows.Count, sw.ElapsedMilliseconds);
            LogSample(result.AllRows);

            if (result.LastError is not null)
                throw new InvalidOperationException(result.LastError);

            if (!send)
            {
                return new ExtractionResult
                {
                    SapObject     = SapObject,
                    Success       = true,
                    RowsExtracted = result.AllRows.Count,
                    Duration      = sw.Elapsed
                };
            }

            return await SendAsync(result.AllRows, sw.Elapsed, ct);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _log.LogError("OSLP: extraction failed — {Message}", ex.Message);
            return new ExtractionResult { SapObject = SapObject, Success = false, Error = ex.Message, Duration = sw.Elapsed };
        }
    }

    private async Task<ExtractionResult> SendAsync(JsonArray allRows, TimeSpan extractDuration, CancellationToken ct)
    {
        var sw     = Stopwatch.StartNew();
        var runId  = Guid.NewGuid().ToString("N");
        var batchId = Guid.NewGuid().ToString("N");
        var ctx    = new MappingContext(runId, batchId, DateTime.UtcNow, _options.Mode);

        var mapped = allRows.Where(r => r is not null)
                            .Select(r => SapToIngestMapper.MapOslpRow(r!, ctx))
                            .ToList();

        var batch = new IngestBatch<SapOslpRow>
        {
            TenantId        = _options.TenantId,
            CompanyId       = _options.CompanyId,
            SapObject       = SapObject,
            ExtractionRunId = runId,
            BatchId         = batchId,
            IngestionMode   = _options.Mode,
            Rows            = mapped
        };

        var resp = await _ingest.SendAsync(Endpoint, batch, ct);
        sw.Stop();

        if (resp.Success)
            _log.LogInformation("OSLP sent: inserted={Ins}, updated={Upd}, skipped={Skp} in {Ms}ms",
                resp.RowsInserted, resp.RowsUpdated, resp.RowsSkipped, sw.ElapsedMilliseconds);
        else
            _log.LogError("OSLP send failed (HTTP {Code}): {Error}", resp.StatusCode, resp.Error);

        return new ExtractionResult
        {
            SapObject     = SapObject,
            Success       = resp.Success,
            RowsExtracted = allRows.Count,
            RowsInserted  = resp.RowsInserted,
            RowsUpdated   = resp.RowsUpdated,
            RowsSkipped   = resp.RowsSkipped,
            Duration      = extractDuration + sw.Elapsed,
            Error         = resp.Error
        };
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
