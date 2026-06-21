using System.Collections.Concurrent;
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

            // If $expand did not deliver lines, run probe sequence (Sprint 17A/17B/17C)
            var hasEmbeddedLines = allEntries.Any(e => e?["JournalEntryLines"] is JsonArray);
            JsonArray? topLevelLines = null;

            if (!hasEmbeddedLines && allEntries.Count > 0)
            {
                var firstJdtNum = allEntries
                    .Select(e => e?["JdtNum"]?.GetValue<int>() ?? 0)
                    .FirstOrDefault(n => n > 0);

                // Sprint 17A: single-record GET probe
                if (firstJdtNum > 0)
                {
                    var (linesProperty, _) = await ProbeIndividualEntryAsync(firstJdtNum, ct);
                    if (linesProperty is not null)
                    {
                        // Single-record exposes lines — extract all entries individually
                        topLevelLines = await ExtractLinesViaIndividualGetAsync(allEntries, linesProperty, ct);
                    }
                }

                // Sprint 17B: $metadata probe (logged only — does not extract lines)
                await ProbeMetadataJournalEntryAsync(ct);

                // Sprint 16D fallback: top-level JournalEntryLines collection
                if (topLevelLines is null || topLevelLines.Count == 0)
                {
                    _log.LogInformation("OJDT: probing JournalEntryLines top-level resource");
                    topLevelLines = await TryFetchLinesTopLevelAsync(
                        allEntries.Select(e => (int)(e?["JdtNum"] ?? 0)).Where(n => n > 0), ct);
                }
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
    /// Sprint 17A: probes GET JournalEntries(N) to check if single-record access returns embedded lines.
    /// Returns (linesPropertyName, singleEntry) if lines found; (null, null) otherwise.
    /// Logs all top-level properties and any array collections detected.
    /// </summary>
    private async Task<(string? linesPropertyName, JsonObject? entry)> ProbeIndividualEntryAsync(
        int jdtNum, CancellationToken ct)
    {
        try
        {
            _log.LogInformation("OJDT-PROBE-17A: GET JournalEntries({JdtNum}) — probing for embedded lines", jdtNum);
            var entry = await _sl.GetObjectAsync($"JournalEntries({jdtNum})", ct);

            if (entry is null)
            {
                _log.LogWarning("OJDT-PROBE-17A: GET JournalEntries({JdtNum}) — null/error response", jdtNum);
                return (null, null);
            }

            var allKeys = entry.Select(kv => kv.Key).ToList();
            _log.LogInformation("OJDT-PROBE-17A: GET JournalEntries({JdtNum}) returned {Count} properties: [{Keys}]",
                jdtNum, allKeys.Count, string.Join(", ", allKeys));

            // Check known navigation property names first
            foreach (var candidate in new[] { "JournalEntryLines", "Lines", "DocumentLines", "TransactionLines", "Rows" })
            {
                if (entry[candidate] is JsonArray arr)
                {
                    _log.LogInformation(
                        "OJDT-PROBE-17A: FOUND '{Prop}' — {N} lines in GET JournalEntries({JdtNum}). Single-record GET EXPOSES LINES.",
                        candidate, arr.Count, jdtNum);
                    return (candidate, entry);
                }
            }

            // Log any other array properties (unknown candidates)
            var otherArrays = entry
                .Where(kv => kv.Value is JsonArray)
                .Select(kv => $"{kv.Key}[{((JsonArray)kv.Value!).Count}]")
                .ToList();

            if (otherArrays.Count > 0)
                _log.LogInformation("OJDT-PROBE-17A: Other array properties: {Arrays}", string.Join(", ", otherArrays));
            else
                _log.LogWarning(
                    "OJDT-PROBE-17A: GET JournalEntries({JdtNum}) has NO array properties — single-record GET does not expose JDT1 lines",
                    jdtNum);

            return (null, null);
        }
        catch (Exception ex)
        {
            _log.LogWarning("OJDT-PROBE-17A: GET JournalEntries({JdtNum}) exception — {Msg}", jdtNum, ex.Message);
            return (null, null);
        }
    }

    /// <summary>
    /// Sprint 17B: fetches SAP SL $metadata and extracts the JournalEntry EntityType definition.
    /// Logs navigation properties and scalar properties. Does NOT affect extraction — diagnostics only.
    /// </summary>
    private async Task ProbeMetadataJournalEntryAsync(CancellationToken ct)
    {
        try
        {
            _log.LogInformation("OJDT-PROBE-17B: fetching $metadata to inspect JournalEntry entity definition");
            var raw = await _sl.GetRawStringAsync("$metadata", ct);

            if (string.IsNullOrWhiteSpace(raw))
            {
                _log.LogWarning("OJDT-PROBE-17B: $metadata returned empty response");
                return;
            }

            _log.LogInformation("OJDT-PROBE-17B: $metadata received ({Len} chars)", raw.Length);

            // Find JournalEntry EntityType block
            var startIdx = raw.IndexOf("Name=\"JournalEntry\"", StringComparison.OrdinalIgnoreCase);
            if (startIdx < 0)
            {
                // Try alternate capitalization
                startIdx = raw.IndexOf("JournalEntry", StringComparison.OrdinalIgnoreCase);
                if (startIdx < 0)
                {
                    _log.LogWarning("OJDT-PROBE-17B: 'JournalEntry' not found in $metadata");
                    return;
                }
            }

            // Extract EntityType block
            var blockStart = raw.LastIndexOf('<', startIdx);
            var blockEnd   = raw.IndexOf("</EntityType>", startIdx, StringComparison.OrdinalIgnoreCase);
            var entityXml  = blockEnd > 0
                ? raw[blockStart..(blockEnd + "</EntityType>".Length)]
                : raw[blockStart..Math.Min(blockStart + 8000, raw.Length)];

            _log.LogInformation("OJDT-PROBE-17B: JournalEntry EntityType block ({Len} chars):\n{Xml}",
                entityXml.Length, entityXml.Length > 4000 ? entityXml[..4000] + "\n...[truncated]" : entityXml);

            // Extract NavigationProperty names
            var navMatches = System.Text.RegularExpressions.Regex.Matches(
                entityXml, @"NavigationProperty[^>]*Name=""([^""]+)""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (navMatches.Count > 0)
            {
                var navNames = navMatches.Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => m.Groups[1].Value)
                    .ToList();
                _log.LogInformation("OJDT-PROBE-17B: NavigationProperties found: [{Names}]",
                    string.Join(", ", navNames));
            }
            else
            {
                _log.LogWarning("OJDT-PROBE-17B: No NavigationProperty elements found for JournalEntry in $metadata");
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning("OJDT-PROBE-17B: $metadata probe failed — {Msg}", ex.Message);
        }
    }

    /// <summary>
    /// Sprint 17C/20C: extracts JDT1 lines by calling GET JournalEntries(N) for every header.
    /// Sprint 20C: concurrent fetches controlled by Extractor:JournalEntryLineFetchConcurrency (default 3).
    /// </summary>
    private async Task<JsonArray> ExtractLinesViaIndividualGetAsync(
        JsonArray allEntries, string linesPropertyName, CancellationToken ct)
    {
        var concurrency = _options.JournalEntryLineFetchConcurrency;
        var sem         = new SemaphoreSlim(concurrency, concurrency);
        var sw          = Stopwatch.StartNew();
        var bag         = new ConcurrentBag<(int index, JsonNode line)>();
        var firstLogged = 0; // CAS flag
        var successCount = 0;
        var failCount    = 0;

        _log.LogInformation(
            "OJDT-17C: extracting lines via individual GET ({N} entries, property='{Prop}', concurrency={C})",
            allEntries.Count, linesPropertyName, concurrency);

        var tasks = allEntries
            .Select((entry, idx) => (entry, idx))
            .Where(t => t.entry is not null)
            .Select(async t =>
            {
                var (entry, idx) = t;
                var jdtNum = entry!["JdtNum"]?.GetValue<int>() ?? 0;
                if (jdtNum == 0) return;

                await sem.WaitAsync(ct);
                var getStart = sw.ElapsedMilliseconds;
                try
                {
                    var fullEntry = await _sl.GetObjectAsync($"JournalEntries({jdtNum})", ct);
                    if (fullEntry is null)
                    {
                        _log.LogWarning("OJDT-17C: GET JournalEntries({JdtNum}) returned null — skipping", jdtNum);
                        Interlocked.Increment(ref failCount);
                        return;
                    }

                    if (fullEntry[linesPropertyName] is not JsonArray lineArr)
                    {
                        _log.LogWarning("OJDT-17C: GET JournalEntries({JdtNum}) missing '{Prop}' — skipping", jdtNum, linesPropertyName);
                        Interlocked.Increment(ref failCount);
                        return;
                    }

                    _log.LogDebug("OJDT-17C: JournalEntries({JdtNum}) — {N} lines in {Ms}ms",
                        jdtNum, lineArr.Count, sw.ElapsedMilliseconds - getStart);

                    if (Interlocked.CompareExchange(ref firstLogged, 1, 0) == 0
                        && lineArr.Count > 0 && lineArr[0] is JsonObject firstLine)
                    {
                        var lineKeys = firstLine.Select(kv => kv.Key).ToList();
                        _log.LogInformation(
                            "OJDT-17C: JournalEntryLine field names (first line of JournalEntries({JdtNum})): [{Keys}]",
                            jdtNum, string.Join(", ", lineKeys));
                    }

                    foreach (var line in lineArr)
                    {
                        if (line is null) continue;
                        var lineClone = line.DeepClone();
                        if (lineClone is JsonObject lineObj)
                            lineObj["JdtNum"] = jdtNum;
                        bag.Add((idx, lineClone));
                    }
                    Interlocked.Increment(ref successCount);
                }
                catch (Exception ex)
                {
                    _log.LogWarning("OJDT-17C: GET JournalEntries({JdtNum}) failed — {Msg}", jdtNum, ex.Message);
                    Interlocked.Increment(ref failCount);
                }
                finally
                {
                    sem.Release();
                }
            })
            .ToList();

        await Task.WhenAll(tasks);
        sw.Stop();

        var totalMs   = sw.ElapsedMilliseconds;
        var entryCount = allEntries.Count;
        var avgMs      = entryCount > 0 ? totalMs / entryCount : 0;
        _log.LogInformation(
            "OJDT-17C: extracted {Lines} lines from {Ok}/{Total} entries ({Fail} failed) in {TotalMs}ms (~{AvgMs}ms/GET, concurrency={C})",
            bag.Count, successCount, entryCount, failCount, totalMs, avgMs, concurrency);

        var result = new JsonArray();
        foreach (var (_, line) in bag.OrderBy(x => x.index))
            result.Add(line);
        return result;
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
