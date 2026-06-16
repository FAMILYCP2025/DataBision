using System.Diagnostics;
using System.Text.Json.Nodes;
using DataBision.Application.DTOs.Ingest.Rows;
using DataBision.Extractor.Checkpoint;
using DataBision.Extractor.DataBision;
using DataBision.Extractor.Mapping;
using DataBision.Extractor.Options;
using DataBision.Extractor.ServiceLayer;
using Microsoft.Extensions.Logging;

namespace DataBision.Extractor.Extraction.Jobs;

public sealed class OpchExtractorJob : IExtractorJob
{
    public string SapObject => "OPCH";

    private const string Endpoint   = "api/ingest/sap-b1/purchase-invoices";
    private const string FullSelect = "DocEntry,DocNum,DocDate,DocDueDate,CardCode,CardName,DocTotal,VatSum,Cancelled,SalesPersonCode,UpdateDate";

    private readonly IServiceLayerClient     _sl;
    private readonly IDataBisionIngestClient _ingest;
    private readonly ExtractorOptions        _options;
    private readonly ILogger<OpchExtractorJob> _log;
    private readonly ServiceLayerPaginator   _paginator;

    public OpchExtractorJob(
        IServiceLayerClient sl, IDataBisionIngestClient ingest,
        ExtractorOptions options, ILogger<OpchExtractorJob> log,
        ServiceLayerPaginator paginator)
    { _sl = sl; _ingest = ingest; _options = options; _log = log; _paginator = paginator; }

    public async Task<ExtractionResult> RunAsync(bool dryRun, bool send, CancellationToken ct = default)
    {
        if (dryRun) return ExtractionResult.DryRun(SapObject);
        var sw = Stopwatch.StartNew();
        try
        {
            var checkpoint = await _ingest.GetCheckpointAsync(_options.CompanyId, SapObject, ct);
            var (filter, _) = IncrementalQueryBuilder.Build(checkpoint, _options.LookbackMinutes);
            var filterPart = filter is not null ? $"&$filter={filter}" : "";
            var result = await _paginator.PaginateAsync(
                SapObject, "PurchaseInvoices",
                $"$select={FullSelect}{filterPart}&$orderby=UpdateDate asc",
                _options.PageSize, _options.MaxPages, ct);
            sw.Stop();
            _log.LogInformation("OPCH: {Count} rows in {Ms}ms", result.AllRows.Count, sw.ElapsedMilliseconds);
            if (!send)
                return new ExtractionResult { SapObject = SapObject, Success = true, RowsExtracted = result.AllRows.Count, Duration = sw.Elapsed, WatermarkDate = MaxUpdateDate(result.AllRows), PagesFetched = result.Logs.Count, HitMaxPages = result.HitMaxPages };

            return await SendAsync(result.AllRows, result.Logs.Count, result.HitMaxPages, sw.Elapsed, ct);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _log.LogError("OPCH: extraction failed — {Message}", ex.Message);
            return new ExtractionResult { SapObject = SapObject, Success = false, Error = ex.Message, Duration = sw.Elapsed };
        }
    }

    private async Task<ExtractionResult> SendAsync(JsonArray allRows, int pagesFetched, bool hitMaxPages, TimeSpan extractDuration, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var runId = Guid.NewGuid().ToString("N");
        var batchId = Guid.NewGuid().ToString("N");
        var ctx = new MappingContext(runId, batchId, DateTime.UtcNow, _options.Mode);
        var mapped = allRows.Where(r => r is not null).Select(r => SapToIngestMapper.MapOpchRow(r!, ctx)).ToList();
        var batch = new IngestBatch<SapOpchRow> { TenantId = _options.TenantId, CompanyId = _options.CompanyId, SapObject = SapObject, ExtractionRunId = runId, BatchId = batchId, IngestionMode = _options.Mode, Rows = mapped };
        var resp = await _ingest.SendAsync(Endpoint, batch, ct);
        sw.Stop();
        if (resp.Success) _log.LogInformation("OPCH sent: inserted={Ins}, updated={Upd}, skipped={Skp}", resp.RowsInserted, resp.RowsUpdated, resp.RowsSkipped);
        else _log.LogError("OPCH send failed (HTTP {Code}): {Error}", resp.StatusCode, resp.Error);
        return new ExtractionResult { SapObject = SapObject, Success = resp.Success, RowsExtracted = allRows.Count, RowsInserted = resp.RowsInserted, RowsUpdated = resp.RowsUpdated, RowsSkipped = resp.RowsSkipped, Duration = extractDuration + sw.Elapsed, WatermarkDate = MaxUpdateDate(allRows), Error = resp.Error, PagesFetched = pagesFetched, HitMaxPages = hitMaxPages };
    }

    private static string? MaxUpdateDate(JsonArray rows) =>
        rows.Select(r => r?["UpdateDate"]?.ToString()).Where(d => d is not null).OrderDescending().FirstOrDefault();
}
