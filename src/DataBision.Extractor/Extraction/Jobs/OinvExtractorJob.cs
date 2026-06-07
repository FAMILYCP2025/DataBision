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

/// <summary>
/// Extracts Invoices (OINV) from SAP B1 Service Layer.
/// Incremental by UpdateDate using checkpoint from DataBision API.
/// Falls back to minimal $select if extended fields are unavailable.
/// </summary>
public sealed class OinvExtractorJob : IExtractorJob
{
    public string SapObject => "OINV";

    private const string Endpoint      = "api/ingest/sap-b1/sales-invoices";
    // Confirmed valid fields for Invoices in SL 1000290 (progressive validation Sprint 4D).
    // Removed: DocCur, DocStatus, ObjType, CreateDate, CreateTS (invalid in this SL version).
    private const string FullSelect    = "DocEntry,DocNum,DocDate,DocDueDate,TaxDate,CardCode,CardName,DocTotal,VatSum,SalesPersonCode,DocType,Cancelled,UpdateDate";
    private const string MinimalSelect = "DocEntry,DocNum,DocDate,CardCode,CardName,DocTotal,UpdateDate";

    private readonly IServiceLayerClient     _sl;
    private readonly IDataBisionIngestClient _ingest;
    private readonly ExtractorOptions        _options;
    private readonly ILogger<OinvExtractorJob> _log;

    public OinvExtractorJob(
        IServiceLayerClient sl, IDataBisionIngestClient ingest,
        ExtractorOptions options, ILogger<OinvExtractorJob> log)
    {
        _sl      = sl;
        _ingest  = ingest;
        _options = options;
        _log     = log;
    }

    public async Task<ExtractionResult> RunAsync(bool dryRun, bool send, CancellationToken ct = default)
    {
        if (dryRun) return ExtractionResult.DryRun(SapObject);

        var sw = Stopwatch.StartNew();
        try
        {
            var filter = await ReadIncrementalFilter(ct);
            var (rows, usedSelect) = await FetchWithFallback(filter, ct);
            sw.Stop();

            var isLastPage = rows.Count < _options.PageSize;
            _log.LogInformation("OINV: {Count} rows in {Ms}ms — last-page={Lp} (select={Sel})",
                rows.Count, sw.ElapsedMilliseconds, isLastPage, usedSelect);
            LogSample(rows);

            if (!send)
            {
                return new ExtractionResult
                {
                    SapObject     = SapObject,
                    Success       = true,
                    RowsExtracted = rows.Count,
                    Duration      = sw.Elapsed,
                    WatermarkDate = MaxUpdateDate(rows)
                };
            }

            return await SendAsync(rows, sw.Elapsed, ct);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _log.LogError("OINV: extraction failed — {Message}", ex.Message);
            return new ExtractionResult { SapObject = SapObject, Success = false, Error = ex.Message, Duration = sw.Elapsed };
        }
    }

    private async Task<string?> ReadIncrementalFilter(CancellationToken ct)
    {
        var checkpoint = await _ingest.GetCheckpointAsync(_options.CompanyId, SapObject, ct);
        var (filter, effectiveFrom) = IncrementalQueryBuilder.Build(checkpoint, _options.LookbackMinutes);

        if (filter is not null)
            _log.LogInformation("OINV: applying incremental filter — UpdateDate ge '{From}' (watermark={Wm})",
                effectiveFrom!.Value.ToString("yyyy-MM-dd"), checkpoint!.WatermarkDate);
        else
            _log.LogInformation("OINV: no checkpoint — running limited initial extraction (top={Top})", _options.PageSize);

        return filter;
    }

    private async Task<(JsonArray rows, string usedSelect)> FetchWithFallback(string? filter, CancellationToken ct)
    {
        var filterPart = filter is not null ? $"&$filter={filter}" : "";

        try
        {
            var q = $"$top={_options.PageSize}&$select={FullSelect}{filterPart}&$orderby=UpdateDate asc";
            return (await _sl.GetAsync("Invoices", q, ct), FullSelect);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase)
                                                 || ex.Message.Contains("400"))
        {
            _log.LogWarning("OINV: full $select failed — retrying with minimal. Error: {Msg}", ex.Message);
            var q = $"$top={_options.PageSize}&$select={MinimalSelect}{filterPart}&$orderby=UpdateDate asc";
            return (await _sl.GetAsync("Invoices", q, ct), MinimalSelect);
        }
    }

    private async Task<ExtractionResult> SendAsync(JsonArray rows, TimeSpan extractDuration, CancellationToken ct)
    {
        var sw      = Stopwatch.StartNew();
        var runId   = Guid.NewGuid().ToString("N");
        var batchId = Guid.NewGuid().ToString("N");
        var ctx     = new MappingContext(runId, batchId, DateTime.UtcNow, _options.Mode);

        var mapped = rows.Where(r => r is not null)
                         .Select(r => SapToIngestMapper.MapOinvRow(r!, ctx))
                         .ToList();

        var batch = new IngestBatch<SapOinvRow>
        {
            TenantId = _options.TenantId, CompanyId = _options.CompanyId, SapObject = SapObject,
            ExtractionRunId = runId, BatchId = batchId, IngestionMode = _options.Mode, Rows = mapped
        };

        var resp = await _ingest.SendAsync(Endpoint, batch, ct);
        sw.Stop();

        if (resp.Success)
            _log.LogInformation("OINV sent: inserted={Ins}, updated={Upd}, skipped={Skp} in {Ms}ms",
                resp.RowsInserted, resp.RowsUpdated, resp.RowsSkipped, sw.ElapsedMilliseconds);
        else
            _log.LogError("OINV send failed (HTTP {Code}): {Error}", resp.StatusCode, resp.Error);

        return new ExtractionResult
        {
            SapObject     = SapObject, Success   = resp.Success,
            RowsExtracted = rows.Count, RowsInserted = resp.RowsInserted,
            RowsUpdated   = resp.RowsUpdated, RowsSkipped = resp.RowsSkipped,
            Duration      = extractDuration + sw.Elapsed, WatermarkDate = MaxUpdateDate(rows),
            Error         = resp.Error
        };
    }

    private void LogSample(JsonArray rows)
    {
        foreach (var row in rows.Take(3))
            _log.LogInformation("  OINV sample: DocEntry={E}, CardCode={C}, DocTotal={T}, UpdateDate={U}",
                row?["DocEntry"]?.ToString() ?? "?", row?["CardCode"]?.ToString() ?? "?",
                row?["DocTotal"]?.ToString() ?? "?", row?["UpdateDate"]?.ToString() ?? "?");
    }

    private static string? MaxUpdateDate(JsonArray rows) =>
        rows.Select(r => r?["UpdateDate"]?.ToString()).Where(d => d is not null).OrderDescending().FirstOrDefault();
}
