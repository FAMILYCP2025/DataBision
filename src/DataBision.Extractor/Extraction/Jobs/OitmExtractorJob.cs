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

public sealed class OitmExtractorJob : IExtractorJob
{
    public string SapObject => "OITM";

    private const string Endpoint      = "api/ingest/sap-b1/items";
    private const string FullSelect    = "ItemCode,ItemName,ItemsGroupCode,UpdateDate";
    private const string MinimalSelect = "ItemCode,ItemName,ItemsGroupCode,UpdateDate";

    private readonly IServiceLayerClient       _sl;
    private readonly IDataBisionIngestClient   _ingest;
    private readonly ExtractorOptions          _options;
    private readonly ILogger<OitmExtractorJob> _log;
    private readonly ServiceLayerPaginator     _paginator;

    public OitmExtractorJob(
        IServiceLayerClient sl, IDataBisionIngestClient ingest,
        ExtractorOptions options, ILogger<OitmExtractorJob> log,
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
            var (allRows, usedSelect) = await PaginateWithFallback(filter, ct);
            sw.Stop();

            _log.LogInformation("OITM: {Count} rows in {Ms}ms (select={Sel})",
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
            _log.LogError("OITM: extraction failed — {Message}", ex.Message);
            return new ExtractionResult { SapObject = SapObject, Success = false, Error = ex.Message, Duration = sw.Elapsed };
        }
    }

    private async Task<string?> ReadIncrementalFilter(CancellationToken ct)
    {
        var checkpoint = await _ingest.GetCheckpointAsync(_options.CompanyId, SapObject, ct);
        var (filter, effectiveFrom) = IncrementalQueryBuilder.Build(checkpoint, _options.LookbackMinutes);

        if (filter is not null)
            _log.LogInformation("OITM: incremental filter — UpdateDate ge '{From}'", effectiveFrom!.Value.ToString("yyyy-MM-dd"));
        else
            _log.LogInformation("OITM: no checkpoint — full extraction (pageSize={Top})", _options.PageSize);

        return filter;
    }

    private async Task<(JsonArray allRows, string usedSelect)> PaginateWithFallback(string? filter, CancellationToken ct)
    {
        var filterPart    = filter is not null ? $"&$filter={filter}" : "";
        var baseQueryFull = $"$select={FullSelect}{filterPart}&$orderby=UpdateDate asc";

        var result = await _paginator.PaginateAsync(SapObject, "Items", baseQueryFull,
            _options.PageSize, _options.MaxPages, ct);

        if (result.LastError is null)
            return (result.AllRows, FullSelect);

        if (result.LastError.Contains("400", StringComparison.Ordinal)
            || result.LastError.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            _log.LogWarning("OITM: full $select failed — retrying with minimal. Error: {Err}", result.LastError);
            var baseQueryMin = $"$select={MinimalSelect}{filterPart}&$orderby=UpdateDate asc";
            var minResult = await _paginator.PaginateAsync(SapObject, "Items", baseQueryMin,
                _options.PageSize, _options.MaxPages, ct);
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
                            .Select(r => SapToIngestMapper.MapOitmRow(r!, ctx))
                            .ToList();

        var batch = new IngestBatch<SapOitmRow>
        {
            TenantId = _options.TenantId, CompanyId = _options.CompanyId, SapObject = SapObject,
            ExtractionRunId = runId, BatchId = batchId, IngestionMode = _options.Mode, Rows = mapped
        };

        var resp = await _ingest.SendAsync(Endpoint, batch, ct);
        sw.Stop();

        if (resp.Success)
            _log.LogInformation("OITM sent: inserted={Ins}, updated={Upd}, skipped={Skp} in {Ms}ms",
                resp.RowsInserted, resp.RowsUpdated, resp.RowsSkipped, sw.ElapsedMilliseconds);
        else
            _log.LogError("OITM send failed (HTTP {Code}): {Error}", resp.StatusCode, resp.Error);

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
        {
            var grp = row?["ItemsGroupCode"];
            _log.LogInformation("  OITM sample: ItemCode={Code}, ItemsGroupCode={Grp}, UpdateDate={Upd}",
                row?["ItemCode"]?.ToString() ?? "?",
                grp is not null ? $"{grp} (type={grp.GetValueKind()})" : "(not present)",
                row?["UpdateDate"]?.ToString() ?? "?");
        }
    }

    private static string? MaxUpdateDate(JsonArray rows) =>
        rows.Select(r => r?["UpdateDate"]?.ToString()).Where(d => d is not null).OrderDescending().FirstOrDefault();
}
