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
/// Extracts ChartOfAccounts (OACT). Full-refresh only — SL entity has no UpdateDate.
/// Register only in SupportedObjects (explicit CLI), NOT in AllObjects (--run-once --send),
/// to prevent accidental full-refresh of the chart of accounts during scheduled runs.
/// </summary>
public sealed class OactExtractorJob : IExtractorJob
{
    public string SapObject => "OACT";

    private const string Endpoint      = "api/ingest/sap-b1/chart-of-accounts";
    private const string FullSelect    = "Code,Name,FatherNum,Level,GroupMask,AccountType,Postable,Frozen,ValidFor,CashAccount,ControlAccount,Currency,FormatCode,ExternalCode";
    private const string MinimalSelect = "Code,Name,FatherNum,Level,AccountType,Postable";

    private readonly IServiceLayerClient       _sl;
    private readonly IDataBisionIngestClient   _ingest;
    private readonly ExtractorOptions          _options;
    private readonly ILogger<OactExtractorJob> _log;
    private readonly ServiceLayerPaginator     _paginator;

    public OactExtractorJob(
        IServiceLayerClient sl, IDataBisionIngestClient ingest,
        ExtractorOptions options, ILogger<OactExtractorJob> log,
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
            _log.LogInformation("OACT: full-refresh (pageSize={Top})", _options.PageSize);

            var (allRows, usedSelect) = await PaginateWithFallback(ct);
            sw.Stop();

            _log.LogInformation("OACT: {Count} accounts in {Ms}ms (select={Sel})",
                allRows.Count, sw.ElapsedMilliseconds, usedSelect);
            LogSample(allRows);

            if (!send)
            {
                return new ExtractionResult
                {
                    SapObject     = SapObject,
                    Success       = true,
                    RowsExtracted = allRows.Count,
                    Duration      = sw.Elapsed
                };
            }

            return await SendAsync(allRows, sw.Elapsed, ct);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _log.LogError("OACT: extraction failed — {Message}", ex.Message);
            return new ExtractionResult { SapObject = SapObject, Success = false, Error = ex.Message, Duration = sw.Elapsed };
        }
    }

    private async Task<(JsonArray allRows, string usedSelect)> PaginateWithFallback(CancellationToken ct)
    {
        var result = await _paginator.PaginateAsync(
            SapObject, "ChartOfAccounts", $"$select={FullSelect}",
            _options.PageSize, _options.MaxPages, ct);

        if (result.LastError is null)
            return (result.AllRows, FullSelect);

        if (result.LastError.Contains("400", StringComparison.Ordinal)
            || result.LastError.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            _log.LogWarning("OACT: full $select failed — retrying minimal. Error: {Err}", result.LastError);
            var minResult = await _paginator.PaginateAsync(
                SapObject, "ChartOfAccounts", $"$select={MinimalSelect}",
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
                            .Select(r => SapToIngestMapper.MapOactRow(r!, ctx))
                            .ToList();

        var batch = new IngestBatch<SapOactRow>
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
            _log.LogInformation("OACT sent: inserted={Ins}, updated={Upd}, skipped={Skp} in {Ms}ms",
                resp.RowsInserted, resp.RowsUpdated, resp.RowsSkipped, sw.ElapsedMilliseconds);
        else
            _log.LogError("OACT send failed (HTTP {Code}): {Error}", resp.StatusCode, resp.Error);

        return new ExtractionResult
        {
            SapObject     = SapObject,
            Success       = resp.Success,
            RowsExtracted = allRows.Count,
            RowsInserted  = resp.RowsInserted,
            RowsUpdated   = resp.RowsUpdated,
            RowsSkipped   = resp.RowsSkipped,
            Duration      = extractDuration + sw.Elapsed,
            Error         = resp.Error
        };
    }

    private void LogSample(JsonArray rows)
    {
        foreach (var row in rows.Take(3))
            _log.LogInformation("  OACT sample: Code={C}, Name={N}, Type={T}, Postable={P}",
                row?["Code"]?.ToString() ?? "?",
                Truncate(row?["Name"]?.ToString() ?? "?"),
                row?["AccountType"]?.ToString() ?? "?",
                row?["Postable"]?.ToString() ?? "?");
    }

    private static string Truncate(string s, int max = 40) =>
        s.Length > max ? s[..max] + "…" : s;
}
