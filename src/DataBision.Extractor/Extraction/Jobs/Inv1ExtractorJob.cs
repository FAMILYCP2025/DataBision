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
/// Extracts AR Invoice lines (INV1) from SAP B1 Service Layer via Invoices/$expand=DocumentLines.
/// Falls back to inline embedded check if $expand is rejected.
/// PageSize is capped at 10 documents to keep line volume controlled.
/// </summary>
public sealed class Inv1ExtractorJob : IExtractorJob
{
    public string SapObject => "INV1";

    private const string Endpoint = "api/ingest/sap-b1/sales-invoice-lines";

    // Cap doc page at 10 — each doc can have many lines
    private int DocTop(int configured) => Math.Min(configured, 10);

    private readonly IServiceLayerClient     _sl;
    private readonly IDataBisionIngestClient _ingest;
    private readonly ExtractorOptions        _options;
    private readonly ILogger<Inv1ExtractorJob> _log;

    public Inv1ExtractorJob(
        IServiceLayerClient sl, IDataBisionIngestClient ingest,
        ExtractorOptions options, ILogger<Inv1ExtractorJob> log)
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
            var (docs, linesTotal, strategy) = await FetchDocsWithLines(ct);
            sw.Stop();

            _log.LogInformation("INV1: {Docs} docs, {Lines} lines total in {Ms}ms (strategy={Strat})",
                docs.Count, linesTotal, sw.ElapsedMilliseconds, strategy);

            if (!send)
            {
                return new ExtractionResult
                {
                    SapObject     = SapObject,
                    Success       = true,
                    RowsExtracted = linesTotal,
                    Duration      = sw.Elapsed
                };
            }

            if (linesTotal == 0)
            {
                _log.LogWarning("INV1: no lines found — nothing to send.");
                return new ExtractionResult { SapObject = SapObject, Success = true, RowsExtracted = 0, Duration = sw.Elapsed };
            }

            return await SendAsync(docs, linesTotal, sw.Elapsed, ct);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _log.LogError("INV1: extraction failed — {Message}", ex.Message);
            return new ExtractionResult { SapObject = SapObject, Success = false, Error = ex.Message, Duration = sw.Elapsed };
        }
    }

    private async Task<(List<JsonNode> docs, int linesTotal, string strategy)> FetchDocsWithLines(CancellationToken ct)
    {
        var top = DocTop(_options.PageSize);

        // Strategy 1: $expand=DocumentLines (OData standard — not supported in SL 1000290)
        try
        {
            var q = $"$top={top}&$select=DocEntry,DocNum,DocDate,CardCode,CardName,DocTotal,UpdateDate&$expand=DocumentLines";
            var rows = await _sl.GetAsync("Invoices", q, ct);
            var docs = rows.Where(r => r is not null).Cast<JsonNode>().ToList();
            var lines = CountAndLogLines(docs, "expand");
            if (lines > 0) return (docs, lines, "$expand=DocumentLines");
        }
        catch (InvalidOperationException ex)
            when (ex.Message.Contains("400") || ex.Message.Contains("expand", StringComparison.OrdinalIgnoreCase)
                                             || ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            _log.LogWarning("INV1: $expand not supported — trying full-document (no $select). Error: {Msg}", ex.Message);
        }

        // Strategy 2: full document without $select — DocumentLines is embedded in the complete response
        {
            var q = $"$top={top}&$orderby=UpdateDate asc";
            var rows = await _sl.GetAsync("Invoices", q, ct);
            var docs = rows.Where(r => r is not null).Cast<JsonNode>().ToList();
            var lines = CountAndLogLines(docs, "full-doc");
            return (docs, lines, "full-doc-no-select");
        }
    }

    private int CountAndLogLines(List<JsonNode> docs, string strategy)
    {
        var totalLines = 0;
        var firstLineDumped = false;

        foreach (var doc in docs)
        {
            var linesArr = doc["DocumentLines"] as JsonArray;
            if (linesArr is null || linesArr.Count == 0) continue;

            totalLines += linesArr.Count;

            if (!firstLineDumped)
            {
                firstLineDumped = true;
                var firstLine = linesArr[0];
                if (firstLine is JsonObject jo)
                {
                    var keys = jo.Select(kv => kv.Key).ToList();
                    _log.LogInformation("INV1 DocumentLines field names ({Strat}): {Fields}", strategy, string.Join(", ", keys.Take(30)));
                }
                LogLineSample(linesArr, doc["DocEntry"]?.ToString() ?? "?");
            }
        }

        if (totalLines == 0)
            _log.LogWarning("INV1: DocumentLines array absent or empty in all {Count} docs (strategy={Strat})", docs.Count, strategy);

        return totalLines;
    }

    private void LogLineSample(JsonArray lines, string docEntry)
    {
        foreach (var line in lines.Take(2))
        {
            var lineNum = line?["LineNum"]?.ToString() ?? "?";
            var item    = line?["ItemCode"]?.ToString() ?? "(no ItemCode)";
            var desc    = Truncate(line?["ItemDescription"]?.ToString() ?? line?["Dscription"]?.ToString() ?? "(no desc)");
            var qty     = line?["Quantity"]?.ToString() ?? "?";
            var total   = line?["LineTotal"]?.ToString() ?? "?";
            _log.LogInformation("  INV1 sample: DocEntry={D}, Line={L}, ItemCode={I}, Desc={Desc}, Qty={Q}, Total={T}",
                docEntry, lineNum, item, desc, qty, total);
        }
    }

    private async Task<ExtractionResult> SendAsync(
        List<JsonNode> docs, int totalLines, TimeSpan extractDuration, CancellationToken ct)
    {
        var sw      = Stopwatch.StartNew();
        var runId   = Guid.NewGuid().ToString("N");
        var batchId = Guid.NewGuid().ToString("N");
        var ctx     = new MappingContext(runId, batchId, DateTime.UtcNow, _options.Mode);

        var mapped = new List<SapInv1Row>(totalLines);
        foreach (var doc in docs)
        {
            var docEntry = SapToIngestMapper.GetIntPublic(doc, "DocEntry");
            var linesArr = doc["DocumentLines"] as JsonArray;
            if (linesArr is null) continue;

            foreach (var line in linesArr.Where(l => l is not null))
                mapped.Add(SapToIngestMapper.MapInv1Row(docEntry, line!, ctx));
        }

        if (mapped.Count == 0)
        {
            _log.LogWarning("INV1: mapper produced 0 rows — skipping send.");
            return new ExtractionResult { SapObject = SapObject, Success = true, RowsExtracted = 0, Duration = extractDuration };
        }

        var batch = new IngestBatch<SapInv1Row>
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
            _log.LogInformation("INV1 sent: inserted={Ins}, updated={Upd}, skipped={Skp} in {Ms}ms",
                resp.RowsInserted, resp.RowsUpdated, resp.RowsSkipped, sw.ElapsedMilliseconds);
        else
            _log.LogError("INV1 send failed (HTTP {Code}): {Error}", resp.StatusCode, resp.Error);

        return new ExtractionResult
        {
            SapObject     = SapObject,
            Success       = resp.Success,
            RowsExtracted = totalLines,
            RowsInserted  = resp.RowsInserted,
            RowsUpdated   = resp.RowsUpdated,
            RowsSkipped   = resp.RowsSkipped,
            Duration      = extractDuration + sw.Elapsed,
            Error         = resp.Error
        };
    }

    private static string Truncate(string s, int max = 40) =>
        s.Length > max ? s[..max] + "…" : s;
}
