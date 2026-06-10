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
/// Extracts Credit Memo headers (ORIN). Incremental by UpdateDate. Multi-page via ServiceLayerPaginator.
/// PageSize capped at 10 on initial extraction (credit memos are low-volume).
/// </summary>
public sealed class OrinExtractorJob : IExtractorJob
{
    public string SapObject => "ORIN";

    private const string Endpoint      = "api/ingest/sap-b1/credit-memos";
    // Confirmed valid fields for CreditNotes in SL 1000290 (Sprint 4D).
    private const string FullSelect    = "DocEntry,DocNum,DocDate,DocDueDate,TaxDate,CardCode,CardName,DocTotal,VatSum,SalesPersonCode,DocType,Cancelled,UpdateDate";
    private const string MinimalSelect = "DocEntry,DocNum,DocDate,CardCode,CardName,DocTotal,UpdateDate";

    // Cap initial extraction to avoid large line-document volume
    private int InitialTop(int configured) => Math.Min(configured, 10);

    private readonly IServiceLayerClient       _sl;
    private readonly IDataBisionIngestClient   _ingest;
    private readonly ExtractorOptions          _options;
    private readonly ILogger<OrinExtractorJob> _log;
    private readonly ServiceLayerPaginator     _paginator;

    public OrinExtractorJob(
        IServiceLayerClient sl, IDataBisionIngestClient ingest,
        ExtractorOptions options, ILogger<OrinExtractorJob> log,
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
            var filter = await ReadIncrementalFilter(ct);
            var effectivePageSize = filter is null ? InitialTop(_options.PageSize) : _options.PageSize;
            var (allRows, usedSelect) = await PaginateWithFallback(filter, effectivePageSize, ct);
            sw.Stop();

            _log.LogInformation("ORIN: {Count} rows in {Ms}ms (select={Sel})",
                allRows.Count, sw.ElapsedMilliseconds, usedSelect);
            LogSample(allRows);

            if (!send)
            {
                return new ExtractionResult
                {
                    SapObject     = SapObject,
                    Success       = true,
                    RowsExtracted = allRows.Count,
                    Duration      = sw.Elapsed,
                    WatermarkDate = MaxUpdateDate(allRows)
                };
            }

            return await SendAsync(allRows, sw.Elapsed, ct);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _log.LogError("ORIN: extraction failed — {Message}", ex.Message);
            return new ExtractionResult { SapObject = SapObject, Success = false, Error = ex.Message, Duration = sw.Elapsed };
        }
    }

    private async Task<string?> ReadIncrementalFilter(CancellationToken ct)
    {
        var checkpoint = await _ingest.GetCheckpointAsync(_options.CompanyId, SapObject, ct);
        var (filter, effectiveFrom) = IncrementalQueryBuilder.Build(checkpoint, _options.LookbackMinutes);

        if (filter is not null)
            _log.LogInformation("ORIN: incremental filter — UpdateDate ge '{From}'", effectiveFrom!.Value.ToString("yyyy-MM-dd"));
        else
            _log.LogInformation("ORIN: no checkpoint — capped initial extraction (top={Top})", InitialTop(_options.PageSize));

        return filter;
    }

    private async Task<(JsonArray allRows, string usedSelect)> PaginateWithFallback(
        string? filter, int pageSize, CancellationToken ct)
    {
        var filterPart    = filter is not null ? $"&$filter={filter}" : "";
        var baseQueryFull = $"$select={FullSelect}{filterPart}&$orderby=UpdateDate asc";

        var result = await _paginator.PaginateAsync(SapObject, "CreditNotes", baseQueryFull,
            pageSize, _options.MaxPages, ct);

        if (result.LastError is null)
            return (result.AllRows, FullSelect);

        if (result.LastError.Contains("400", StringComparison.Ordinal)
            || result.LastError.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            _log.LogWarning("ORIN: full $select failed — retrying with minimal. Error: {Err}", result.LastError);
            var baseQueryMin = $"$select={MinimalSelect}{filterPart}&$orderby=UpdateDate asc";
            var minResult = await _paginator.PaginateAsync(SapObject, "CreditNotes", baseQueryMin,
                pageSize, _options.MaxPages, ct);
            return (minResult.AllRows, MinimalSelect);
        }

        return (result.AllRows, FullSelect);
    }

    private async Task<ExtractionResult> SendAsync(JsonArray allRows, TimeSpan extractDuration, CancellationToken ct)
    {
        var sw      = Stopwatch.StartNew();
        var runId   = Guid.NewGuid().ToString("N");
        var batchId = Guid.NewGuid().ToString("N");
        var ctx     = new MappingContext(runId, batchId, DateTime.UtcNow, _options.Mode);

        var mapped = allRows.Where(r => r is not null)
                            .Select(r => SapToIngestMapper.MapOrinRow(r!, ctx))
                            .ToList();

        var batch = new IngestBatch<SapOrinRow>
        {
            TenantId = _options.TenantId, CompanyId = _options.CompanyId, SapObject = SapObject,
            ExtractionRunId = runId, BatchId = batchId, IngestionMode = _options.Mode, Rows = mapped
        };

        var resp = await _ingest.SendAsync(Endpoint, batch, ct);
        sw.Stop();

        if (resp.Success)
            _log.LogInformation("ORIN sent: inserted={Ins}, updated={Upd}, skipped={Skp} in {Ms}ms",
                resp.RowsInserted, resp.RowsUpdated, resp.RowsSkipped, sw.ElapsedMilliseconds);
        else
            _log.LogError("ORIN send failed (HTTP {Code}): {Error}", resp.StatusCode, resp.Error);

        return new ExtractionResult
        {
            SapObject     = SapObject, Success   = resp.Success,
            RowsExtracted = allRows.Count, RowsInserted = resp.RowsInserted,
            RowsUpdated   = resp.RowsUpdated, RowsSkipped = resp.RowsSkipped,
            Duration      = extractDuration + sw.Elapsed, WatermarkDate = MaxUpdateDate(allRows),
            Error         = resp.Error
        };
    }

    private void LogSample(JsonArray rows)
    {
        foreach (var row in rows.Take(3))
            _log.LogInformation("  ORIN sample: DocEntry={E}, CardCode={C}, DocTotal={T}, UpdateDate={U}",
                row?["DocEntry"]?.ToString() ?? "?", row?["CardCode"]?.ToString() ?? "?",
                row?["DocTotal"]?.ToString() ?? "?", row?["UpdateDate"]?.ToString() ?? "?");
    }

    private static string? MaxUpdateDate(JsonArray rows) =>
        rows.Select(r => r?["UpdateDate"]?.ToString()).Where(d => d is not null).OrderDescending().FirstOrDefault();
}
