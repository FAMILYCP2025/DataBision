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
/// Extracts JournalEntries (OJDT) with embedded JournalEntryLines (JDT1).
/// Incremental by ReferenceDate (UpdateDate is not available on the SL entity).
/// Sends headers to one ingest endpoint and lines to a second endpoint in a single job run.
/// Register only in SupportedObjects (explicit CLI), NOT in AllObjects (--run-once --send),
/// to prevent accidental accounting extraction during scheduled runs.
/// </summary>
public sealed class OjdtExtractorJob : IExtractorJob
{
    public string SapObject => "OJDT";

    private const string EndpointHeaders = "api/ingest/sap-b1/journal-entries";
    private const string EndpointLines   = "api/ingest/sap-b1/journal-entry-lines";

    // $expand=JournalEntryLines embeds lines inline per entry
    private const string FullSelect    = "JdtNum,ReferenceDate,DueDate,TaxDate,Memo,TransactionCode,BaseRef,Ref1,CreatedBy";
    private const string MinimalSelect = "JdtNum,ReferenceDate,Memo";

    private readonly IServiceLayerClient       _sl;
    private readonly IDataBisionIngestClient   _ingest;
    private readonly ExtractorOptions          _options;
    private readonly ILogger<OjdtExtractorJob> _log;
    private readonly ServiceLayerPaginator     _paginator;

    public OjdtExtractorJob(
        IServiceLayerClient sl, IDataBisionIngestClient ingest,
        ExtractorOptions options, ILogger<OjdtExtractorJob> log,
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
            var (filter, effectiveFrom) = await BuildFilter(ct);
            var (allEntries, usedSelect) = await PaginateWithFallback(filter, ct);
            sw.Stop();

            _log.LogInformation("OJDT: {Count} entries in {Ms}ms (select={Sel}, filter={Filter})",
                allEntries.Count, sw.ElapsedMilliseconds, usedSelect,
                effectiveFrom.HasValue ? effectiveFrom.Value.ToString("yyyy-MM-dd") : "FULL");

            if (!send)
            {
                return new ExtractionResult
                {
                    SapObject     = SapObject,
                    Success       = true,
                    RowsExtracted = allEntries.Count,
                    Duration      = sw.Elapsed,
                    WatermarkDate = MaxRefDate(allEntries)
                };
            }

            return await SendAsync(allEntries, sw.Elapsed, ct);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _log.LogError("OJDT: extraction failed — {Message}", ex.Message);
            return new ExtractionResult { SapObject = SapObject, Success = false, Error = ex.Message, Duration = sw.Elapsed };
        }
    }

    private async Task<(string? filter, DateTime? effectiveFrom)> BuildFilter(CancellationToken ct)
    {
        var checkpoint = await _ingest.GetCheckpointAsync(_options.CompanyId, SapObject, ct);
        // Reuse IncrementalQueryBuilder logic to get effectiveFrom from checkpoint + lookback minutes,
        // but substitute ReferenceDate since JournalEntries exposes ReferenceDate (not UpdateDate).
        var (_, effectiveFrom) = IncrementalQueryBuilder.Build(checkpoint, _options.LookbackMinutes);

        if (effectiveFrom.HasValue)
        {
            _log.LogInformation("OJDT: incremental — ReferenceDate ge '{From}'",
                effectiveFrom.Value.ToString("yyyy-MM-dd"));
            return ($"ReferenceDate ge '{effectiveFrom.Value:yyyy-MM-dd}'", effectiveFrom);
        }

        _log.LogInformation("OJDT: no checkpoint — full extraction (pageSize={Top})", _options.PageSize);
        return (null, null);
    }

    private async Task<(JsonArray allEntries, string usedSelect)> PaginateWithFallback(string? filter, CancellationToken ct)
    {
        var filterPart    = filter is not null ? $"&$filter={filter}" : "";
        var baseQueryFull = $"$select={FullSelect}&$expand=JournalEntryLines{filterPart}&$orderby=ReferenceDate asc";

        var result = await _paginator.PaginateAsync(
            SapObject, "JournalEntries", baseQueryFull,
            _options.PageSize, _options.MaxPages, ct);

        if (result.LastError is null)
            return (result.AllRows, FullSelect);

        if (result.LastError.Contains("400", StringComparison.Ordinal)
            || result.LastError.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            _log.LogWarning("OJDT: full $select failed — retrying minimal. Error: {Err}", result.LastError);
            var baseQueryMin = $"$select={MinimalSelect}&$expand=JournalEntryLines{filterPart}&$orderby=ReferenceDate asc";
            var minResult = await _paginator.PaginateAsync(
                SapObject, "JournalEntries", baseQueryMin,
                _options.PageSize, _options.MaxPages, ct);
            return (minResult.AllRows, MinimalSelect);
        }

        return (result.AllRows, FullSelect);
    }

    private async Task<ExtractionResult> SendAsync(JsonArray allEntries, TimeSpan extractDuration, CancellationToken ct)
    {
        var sw      = Stopwatch.StartNew();
        var runId   = Guid.NewGuid().ToString("N");
        var batchId = Guid.NewGuid().ToString("N");
        var ctx     = new MappingContext(runId, batchId, DateTime.UtcNow, _options.Mode);

        // Split entries into OJDT headers + JDT1 lines
        var (headers, lines) = MapEntries(allEntries, ctx);
        _log.LogInformation("OJDT: mapped {H} headers, {L} lines", headers.Count, lines.Count);

        // Send OJDT headers
        var headerBatch = new IngestBatch<SapOjdtRow>
        {
            TenantId        = _options.TenantId,
            CompanyId       = _options.CompanyId,
            SapObject       = "OJDT",
            ExtractionRunId = runId,
            BatchId         = batchId,
            IngestionMode   = _options.Mode,
            Rows            = headers
        };
        var hResp = await _ingest.SendAsync(EndpointHeaders, headerBatch, ct);

        if (hResp.Success)
            _log.LogInformation("OJDT headers sent: inserted={Ins}, updated={Upd}, skipped={Skp}",
                hResp.RowsInserted, hResp.RowsUpdated, hResp.RowsSkipped);
        else
            _log.LogError("OJDT headers send failed (HTTP {Code}): {Error}", hResp.StatusCode, hResp.Error);

        // Send JDT1 lines (separate batch, separate endpoint)
        ExtractionResult lineResult = new() { SapObject = "JDT1", Success = true };
        if (lines.Count > 0)
        {
            var lineBatch = new IngestBatch<SapJdt1Row>
            {
                TenantId        = _options.TenantId,
                CompanyId       = _options.CompanyId,
                SapObject       = "JDT1",
                ExtractionRunId = runId,
                BatchId         = batchId,
                IngestionMode   = _options.Mode,
                Rows            = lines
            };
            var lResp = await _ingest.SendAsync(EndpointLines, lineBatch, ct);

            if (lResp.Success)
                _log.LogInformation("JDT1 lines sent: inserted={Ins}, updated={Upd}, skipped={Skp}",
                    lResp.RowsInserted, lResp.RowsUpdated, lResp.RowsSkipped);
            else
                _log.LogError("JDT1 lines send failed (HTTP {Code}): {Error}", lResp.StatusCode, lResp.Error);

            lineResult = new ExtractionResult
            {
                SapObject    = "JDT1",
                Success      = lResp.Success,
                RowsInserted = lResp.RowsInserted,
                RowsUpdated  = lResp.RowsUpdated,
                RowsSkipped  = lResp.RowsSkipped,
                Error        = lResp.Error
            };
        }

        sw.Stop();

        var success = hResp.Success && lineResult.Success;
        return new ExtractionResult
        {
            SapObject     = SapObject,
            Success       = success,
            RowsExtracted = allEntries.Count,
            RowsInserted  = hResp.RowsInserted + lineResult.RowsInserted,
            RowsUpdated   = hResp.RowsUpdated  + lineResult.RowsUpdated,
            RowsSkipped   = hResp.RowsSkipped  + lineResult.RowsSkipped,
            Duration      = extractDuration + sw.Elapsed,
            WatermarkDate = MaxRefDate(allEntries),
            Error         = success ? null : hResp.Error ?? lineResult.Error
        };
    }

    private static (List<SapOjdtRow> headers, List<SapJdt1Row> lines) MapEntries(JsonArray entries, MappingContext ctx)
    {
        var headers = new List<SapOjdtRow>();
        var lines   = new List<SapJdt1Row>();

        foreach (var entry in entries)
        {
            if (entry is null) continue;

            var header = SapToIngestMapper.MapOjdtRow(entry, ctx);
            headers.Add(header);

            if (entry["JournalEntryLines"] is not JsonArray lineArr) continue;
            foreach (var line in lineArr)
            {
                if (line is null) continue;
                lines.Add(SapToIngestMapper.MapJdt1Row(header.TransId, line, ctx));
            }
        }

        return (headers, lines);
    }

    private static string? MaxRefDate(JsonArray rows) =>
        rows.Select(r => r?["ReferenceDate"]?.ToString())
            .Where(d => d is not null)
            .OrderDescending()
            .FirstOrDefault();
}
