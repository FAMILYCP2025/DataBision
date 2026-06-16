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
/// Extracts Item-Warehouse stock levels (OITW). Always full-refresh — no UpdateDate watermark.
/// Page size capped at 20 to avoid SL timeouts (ItemWarehouseInfoCollection can be very large).
/// </summary>
public sealed class OitwExtractorJob : IExtractorJob
{
    public string SapObject => "OITW";

    private const string Endpoint   = "api/ingest/sap-b1/item-warehouses";
    private const string FullSelect = "ItemCode,WarehouseCode,InStock,Committed,OnOrder";

    private readonly IServiceLayerClient     _sl;
    private readonly IDataBisionIngestClient _ingest;
    private readonly ExtractorOptions        _options;
    private readonly ILogger<OitwExtractorJob> _log;
    private readonly ServiceLayerPaginator   _paginator;

    public OitwExtractorJob(
        IServiceLayerClient sl, IDataBisionIngestClient ingest,
        ExtractorOptions options, ILogger<OitwExtractorJob> log,
        ServiceLayerPaginator paginator)
    { _sl = sl; _ingest = ingest; _options = options; _log = log; _paginator = paginator; }

    public async Task<ExtractionResult> RunAsync(bool dryRun, bool send, CancellationToken ct = default)
    {
        if (dryRun) return ExtractionResult.DryRun(SapObject);
        var sw = Stopwatch.StartNew();
        try
        {
            // OITW: no watermark — always full. Cap page size at 20 for safety.
            var pageSize = Math.Min(_options.PageSize, 20);
            var result = await _paginator.PaginateAsync(
                SapObject, "ItemWarehouseInfoCollection",
                $"$select={FullSelect}",
                pageSize, _options.MaxPages, ct);
            sw.Stop();
            _log.LogInformation("OITW: {Count} rows in {Ms}ms (pages={Pages})", result.AllRows.Count, sw.ElapsedMilliseconds, result.Logs.Count);
            if (!send)
                return new ExtractionResult { SapObject = SapObject, Success = true, RowsExtracted = result.AllRows.Count, Duration = sw.Elapsed, PagesFetched = result.Logs.Count, HitMaxPages = result.HitMaxPages };

            return await SendAsync(result.AllRows, result.Logs.Count, result.HitMaxPages, sw.Elapsed, ct);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _log.LogError("OITW: extraction failed — {Message}", ex.Message);
            return new ExtractionResult { SapObject = SapObject, Success = false, Error = ex.Message, Duration = sw.Elapsed };
        }
    }

    private async Task<ExtractionResult> SendAsync(JsonArray allRows, int pagesFetched, bool hitMaxPages, TimeSpan extractDuration, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var runId = Guid.NewGuid().ToString("N");
        var batchId = Guid.NewGuid().ToString("N");
        var ctx = new MappingContext(runId, batchId, DateTime.UtcNow, _options.Mode);
        var mapped = allRows.Where(r => r is not null).Select(r => SapToIngestMapper.MapOitwRow(r!, ctx)).ToList();
        var batch = new IngestBatch<SapOitwRow> { TenantId = _options.TenantId, CompanyId = _options.CompanyId, SapObject = SapObject, ExtractionRunId = runId, BatchId = batchId, IngestionMode = _options.Mode, Rows = mapped };
        var resp = await _ingest.SendAsync(Endpoint, batch, ct);
        sw.Stop();
        if (resp.Success) _log.LogInformation("OITW sent: inserted={Ins}, updated={Upd}, skipped={Skp}", resp.RowsInserted, resp.RowsUpdated, resp.RowsSkipped);
        else _log.LogError("OITW send failed (HTTP {Code}): {Error}", resp.StatusCode, resp.Error);
        return new ExtractionResult { SapObject = SapObject, Success = resp.Success, RowsExtracted = allRows.Count, RowsInserted = resp.RowsInserted, RowsUpdated = resp.RowsUpdated, RowsSkipped = resp.RowsSkipped, Duration = extractDuration + sw.Elapsed, Error = resp.Error, PagesFetched = pagesFetched, HitMaxPages = hitMaxPages };
    }
}
