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

            // If $expand did not deliver lines, try top-level JournalEntryLines resource
            var hasEmbeddedLines = allEntries.Any(e => e?["JournalEntryLines"] is JsonArray);
            JsonArray? topLevelLines = null;
            if (!hasEmbeddedLines && allEntries.Count > 0)
            {
                _log.LogInformation("OJDT: no embedded lines found — probing JournalEntryLines top-level resource");
                topLevelLines = await TryFetchLinesTopLevelAsync(
                    allEntries.Select(e => (int)(e?["JdtNum"] ?? 0)).Where(n => n > 0), ct);
            }

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

            return await SendAsync(allEntries, topLevelLines, sw.Elapsed, ct);
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
        var filterPart = filter is not null ? $"&$filter={filter}" : "";
        var orderBy    = "&$orderby=ReferenceDate asc";

        // Attempt 1: full $select + $expand=JournalEntryLines
        var q1 = $"$select={FullSelect}&$expand=JournalEntryLines{filterPart}{orderBy}";
        var r1 = await _paginator.PaginateAsync(SapObject, "JournalEntries", q1, _options.PageSize, _options.MaxPages, ct);
        if (r1.LastError is null) return (r1.AllRows, FullSelect + "+expand=JournalEntryLines");

        if (!IsExpandError(r1.LastError) && !Is400(r1.LastError))
            return (r1.AllRows, FullSelect);

        // Attempt 2: full $select + $expand=Lines (alternative navigation property name)
        _log.LogWarning("OJDT: $expand=JournalEntryLines invalid — trying $expand=Lines. Error: {Err}", r1.LastError);
        var q2 = $"$select={FullSelect}&$expand=Lines{filterPart}{orderBy}";
        var r2 = await _paginator.PaginateAsync(SapObject, "JournalEntries", q2, _options.PageSize, _options.MaxPages, ct);
        if (r2.LastError is null) return (r2.AllRows, FullSelect + "+expand=Lines");

        // Attempt 3: full $select, headers-only (no $expand)
        _log.LogWarning("OJDT: $expand=Lines also invalid — trying full select, no expand. Error: {Err}", r2.LastError);
        var q3 = $"$select={FullSelect}{filterPart}{orderBy}";
        var r3 = await _paginator.PaginateAsync(SapObject, "JournalEntries", q3, _options.PageSize, _options.MaxPages, ct);
        if (r3.LastError is null) return (r3.AllRows, FullSelect + " (headers-only)");

        // Attempt 4: minimal $select, headers-only
        _log.LogWarning("OJDT: full $select failed — trying minimal, no expand. Error: {Err}", r3.LastError);
        var q4 = $"$select={MinimalSelect}{filterPart}{orderBy}";
        var r4 = await _paginator.PaginateAsync(SapObject, "JournalEntries", q4, _options.PageSize, _options.MaxPages, ct);
        if (r4.LastError is null) return (r4.AllRows, MinimalSelect + " (headers-only)");

        // Attempt 5: no $select, no $expand
        _log.LogWarning("OJDT: minimal $select failed — trying no $select. Error: {Err}", r4.LastError);
        var filterOnly = filter is not null ? $"$filter={filter}{orderBy}" : "";
        var r5 = await _paginator.PaginateAsync(SapObject, "JournalEntries", filterOnly, _options.PageSize, _options.MaxPages, ct);
        return (r5.AllRows, "no-select (headers-only)");
    }

    private static bool Is400(string? error) =>
        error is not null && error.Contains("400", StringComparison.Ordinal);

    private static bool IsExpandError(string? error) =>
        error is not null && error.Contains("expand", StringComparison.OrdinalIgnoreCase);

    private async Task<ExtractionResult> SendAsync(JsonArray allEntries, JsonArray? topLevelLines, TimeSpan extractDuration, CancellationToken ct)
    {
        var sw      = Stopwatch.StartNew();
        var runId   = Guid.NewGuid().ToString("N");
        var batchId = Guid.NewGuid().ToString("N");
        var ctx     = new MappingContext(runId, batchId, DateTime.UtcNow, _options.Mode);

        // Split entries into OJDT headers + embedded JDT1 lines
        var (headers, lines) = MapEntries(allEntries, ctx);

        // If no embedded lines but top-level lines were fetched, map them instead
        if (lines.Count == 0 && topLevelLines is { Count: > 0 })
        {
            _log.LogInformation("OJDT: using {N} top-level JournalEntryLines", topLevelLines.Count);
            var transIdByJdtNum = headers.ToDictionary(h => h.JdtNum ?? 0, h => h.TransId);
            foreach (var line in topLevelLines)
            {
                if (line is null) continue;
                var jdtNum = line["JdtNum"]?.GetValue<int>() ?? 0;
                if (!transIdByJdtNum.TryGetValue(jdtNum, out var transId)) continue;
                lines.Add(SapToIngestMapper.MapJdt1Row(transId, line, ctx));
            }
        }
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

    /// <summary>
    /// Tries to fetch JDT1 lines via the top-level JournalEntryLines resource.
    /// SL v1000290 does not support $expand on JournalEntries, but the collection may be
    /// queryable directly with a $filter on JdtNum.
    /// Returns empty list if the entity is not accessible.
    /// </summary>
    private async Task<JsonArray> TryFetchLinesTopLevelAsync(IEnumerable<int> jdtNums, CancellationToken ct)
    {
        try
        {
            // Probe: check if JournalEntryLines is accessible as a top-level entity
            var probe = await _sl.GetPageAsync("JournalEntryLines", "$top=1", ct);
            if (probe.Rows.Count == 0 && probe.NextLink is null)
            {
                _log.LogInformation("OJDT: JournalEntryLines top-level entity returned 0 rows (may be empty)");
            }
            _log.LogInformation("OJDT: JournalEntryLines top-level accessible — fetching all lines");

            // Fetch all lines
            var allLines = new JsonArray();
            var result = await _paginator.PaginateAsync("JDT1", "JournalEntryLines", "", _options.PageSize, _options.MaxPages, ct);
            foreach (var row in result.AllRows)
                allLines.Add(row?.DeepClone());
            _log.LogInformation("OJDT: fetched {N} lines via JournalEntryLines top-level", allLines.Count);
            return allLines;
        }
        catch (Exception ex)
        {
            _log.LogWarning("OJDT: JournalEntryLines top-level not accessible — JDT1 will be empty. Error: {Msg}", ex.Message);
            return [];
        }
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
