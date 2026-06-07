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
/// Extracts BusinessPartners (OCRD) from SAP B1 Service Layer.
/// Incremental by UpdateDate using checkpoint from DataBision API.
/// Falls back to minimal $select if extended fields are unavailable.
/// </summary>
public sealed class OcrdExtractorJob : IExtractorJob
{
    public string SapObject => "OCRD";

    private const string Endpoint      = "api/ingest/sap-b1/customers";
    // UpdateTS is not exposed by BusinessPartner in SL 1000290 — confirmed Sprint 4B/4C
    private const string FullSelect    = "CardCode,CardName,CardType,GroupCode,FederalTaxID,CurrentAccountBalance,SalesPersonCode,UpdateDate";
    private const string MinimalSelect = "CardCode,CardName,CardType,UpdateDate";

    private readonly IServiceLayerClient     _sl;
    private readonly IDataBisionIngestClient _ingest;
    private readonly ExtractorOptions        _options;
    private readonly ILogger<OcrdExtractorJob> _log;

    public OcrdExtractorJob(
        IServiceLayerClient sl, IDataBisionIngestClient ingest,
        ExtractorOptions options, ILogger<OcrdExtractorJob> log)
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

            _log.LogInformation("OCRD: {Count} rows in {Ms}ms (select={Sel})",
                rows.Count, sw.ElapsedMilliseconds, usedSelect);
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
            _log.LogError("OCRD: extraction failed — {Message}", ex.Message);
            return new ExtractionResult { SapObject = SapObject, Success = false, Error = ex.Message, Duration = sw.Elapsed };
        }
    }

    private async Task<string?> ReadIncrementalFilter(CancellationToken ct)
    {
        var checkpoint = await _ingest.GetCheckpointAsync(_options.CompanyId, SapObject, ct);
        var (filter, effectiveFrom) = IncrementalQueryBuilder.Build(checkpoint, _options.LookbackMinutes);

        if (filter is not null)
            _log.LogInformation("OCRD: applying incremental filter — UpdateDate ge '{From}' (watermark={Wm})",
                effectiveFrom!.Value.ToString("yyyy-MM-dd"), checkpoint!.WatermarkDate);
        else
            _log.LogInformation("OCRD: no checkpoint — running limited initial extraction (top={Top})", _options.PageSize);

        return filter;
    }

    private async Task<(JsonArray rows, string usedSelect)> FetchWithFallback(string? filter, CancellationToken ct)
    {
        var filterPart = filter is not null ? $"&$filter={filter}" : "";

        try
        {
            var q = $"$top={_options.PageSize}&$select={FullSelect}{filterPart}&$orderby=UpdateDate asc";
            return (await _sl.GetAsync("BusinessPartners", q, ct), FullSelect);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase)
                                                 || ex.Message.Contains("400"))
        {
            _log.LogWarning("OCRD: full $select failed — retrying with minimal. Error: {Msg}", ex.Message);
            var q = $"$top={_options.PageSize}&$select={MinimalSelect}{filterPart}&$orderby=UpdateDate asc";
            return (await _sl.GetAsync("BusinessPartners", q, ct), MinimalSelect);
        }
    }

    private async Task<ExtractionResult> SendAsync(JsonArray rows, TimeSpan extractDuration, CancellationToken ct)
    {
        var sw      = Stopwatch.StartNew();
        var runId   = Guid.NewGuid().ToString("N");
        var batchId = Guid.NewGuid().ToString("N");
        var ctx     = new MappingContext(runId, batchId, DateTime.UtcNow, _options.Mode);

        var mapped = rows.Where(r => r is not null)
                         .Select(r => SapToIngestMapper.MapOcrdRow(r!, ctx))
                         .ToList();

        var batch = new IngestBatch<SapOcrdRow>
        {
            TenantId = _options.TenantId, CompanyId = _options.CompanyId, SapObject = SapObject,
            ExtractionRunId = runId, BatchId = batchId, IngestionMode = _options.Mode, Rows = mapped
        };

        var resp = await _ingest.SendAsync(Endpoint, batch, ct);
        sw.Stop();

        if (resp.Success)
            _log.LogInformation("OCRD sent: inserted={Ins}, updated={Upd}, skipped={Skp} in {Ms}ms",
                resp.RowsInserted, resp.RowsUpdated, resp.RowsSkipped, sw.ElapsedMilliseconds);
        else
            _log.LogError("OCRD send failed (HTTP {Code}): {Error}", resp.StatusCode, resp.Error);

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
            _log.LogInformation("  OCRD sample: CardCode={Code}, CardType={Type}, UpdateDate={Upd}",
                row?["CardCode"]?.ToString() ?? "?",
                row?["CardType"]?.ToString() ?? "?",
                row?["UpdateDate"]?.ToString() ?? "?");
    }

    private static string? MaxUpdateDate(JsonArray rows) =>
        rows.Select(r => r?["UpdateDate"]?.ToString()).Where(d => d is not null).OrderDescending().FirstOrDefault();
}
