using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace DataBision.Extractor.ServiceLayer;

/// <summary>
/// Paginates SAP Service Layer requests using $top/$skip and @odata.nextLink.
/// Handles multi-page extraction with per-page retry on transient errors.
/// </summary>
public sealed class ServiceLayerPaginator
{
    private readonly IServiceLayerClient _sl;
    private readonly ILogger<ServiceLayerPaginator> _log;
    private readonly Func<int, CancellationToken, Task> _delay;

    public ServiceLayerPaginator(
        IServiceLayerClient sl,
        ILogger<ServiceLayerPaginator> log,
        Func<int, CancellationToken, Task>? delayFactory = null)
    {
        _sl    = sl;
        _log   = log;
        _delay = delayFactory ?? ((seconds, ct) => Task.Delay(TimeSpan.FromSeconds(seconds), ct));
    }

    /// <summary>
    /// Paginates through all pages of a SAP Service Layer entity.
    /// </summary>
    /// <param name="sapObject">SAP object code for logging (e.g., "OINV").</param>
    /// <param name="entity">Service Layer endpoint (e.g., "Invoices").</param>
    /// <param name="baseQuery">Query string WITHOUT $top/$skip (e.g., "$select=...&$filter=...&$orderby=UpdateDate asc").</param>
    /// <param name="pageSize">Rows per page.</param>
    /// <param name="maxPages">Safety cap. Stops pagination and sets HitMaxPages when reached.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<PaginationResult> PaginateAsync(
        string sapObject, string entity, string baseQuery,
        int pageSize, int maxPages, CancellationToken ct)
    {
        var allRows = new JsonArray();
        var logs    = new List<PaginationPageLog>();
        var skip    = 0;
        var pageNumber = 0;
        string? currentQuery = null;
        string? lastError   = null;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            if (pageNumber >= maxPages)
            {
                _log.LogWarning("{Obj}: hit MaxPages cap ({Max}) — stopping pagination.", sapObject, maxPages);
                return new PaginationResult(allRows, logs, HitMaxPages: true, LastError: null);
            }

            pageNumber++;

            // Build query for this page
            currentQuery = currentQuery ?? $"$top={pageSize}&$skip=0&{baseQuery}";

            var sw = Stopwatch.StartNew();
            int rowsReceived;
            string? nextLink;

            try
            {
                var page = await FetchPageWithRetryAsync(entity, currentQuery, ct);
                sw.Stop();
                rowsReceived = page.Rows.Count;
                nextLink     = page.NextLink;

                foreach (var row in page.Rows)
                    allRows.Add(row?.DeepClone());

                logs.Add(new PaginationPageLog(
                    SapObject: sapObject, PageNumber: pageNumber, Skip: skip,
                    Top: pageSize, RowsReceived: rowsReceived,
                    ElapsedMs: sw.ElapsedMilliseconds, Status: "OK",
                    ErrorCode: null, ErrorMessage: null));

                _log.LogInformation("{Obj}: page {P} — {R} rows in {Ms}ms (skip={Skip}{NextLinkMarker})",
                    sapObject, pageNumber, rowsReceived, sw.ElapsedMilliseconds, skip,
                    nextLink is not null ? ", nextLink" : "");

                skip += rowsReceived;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                sw.Stop();
                lastError = ex.Message;
                logs.Add(new PaginationPageLog(
                    SapObject: sapObject, PageNumber: pageNumber, Skip: skip,
                    Top: pageSize, RowsReceived: 0,
                    ElapsedMs: sw.ElapsedMilliseconds, Status: "ERROR",
                    ErrorCode: "FETCH_FAILED", ErrorMessage: ex.Message));
                _log.LogError("{Obj}: page {P} failed permanently — {Msg}", sapObject, pageNumber, ex.Message);
                return new PaginationResult(allRows, logs, HitMaxPages: false, LastError: lastError);
            }

            // Determine next query
            if (nextLink is not null)
            {
                currentQuery = ExtractQueryFromNextLink(nextLink);
            }
            else if (rowsReceived < pageSize)
            {
                // Last page — no more data
                break;
            }
            else
            {
                currentQuery = $"$top={pageSize}&$skip={skip}&{baseQuery}";
            }
        }

        _log.LogInformation("{Obj}: pagination complete — {Total} rows, {Pages} page(s).",
            sapObject, allRows.Count, pageNumber);
        return new PaginationResult(allRows, logs, HitMaxPages: false, LastError: null);
    }

    private async Task<ServiceLayerPage> FetchPageWithRetryAsync(
        string entity, string query, CancellationToken ct)
    {
        const int maxAttempts = 2;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await _sl.GetPageAsync(entity, query, ct);
            }
            catch (Exception ex) when (IsTransient(ex, ct) && attempt < maxAttempts)
            {
                _log.LogWarning("Page fetch attempt {A}/{Max} failed — retrying in 2s. Error: {Msg}",
                    attempt, maxAttempts, ex.Message);
                await _delay(2, ct);
            }
        }
        // Final attempt — let exception propagate
        return await _sl.GetPageAsync(entity, query, ct);
    }

    private static bool IsTransient(Exception ex, CancellationToken ct) =>
        ex is HttpRequestException
        || (ex is TaskCanceledException tce && !ct.IsCancellationRequested
            && tce.InnerException is TimeoutException);

    private static string ExtractQueryFromNextLink(string nextLink)
    {
        try
        {
            var uri = new Uri(nextLink);
            return uri.Query.TrimStart('?');
        }
        catch
        {
            // Already a query string, not a full URL
            return nextLink;
        }
    }
}

public sealed record PaginationPageLog(
    string SapObject, int PageNumber, int Skip, int Top,
    int RowsReceived, long ElapsedMs, string Status,
    string? ErrorCode, string? ErrorMessage);

public sealed record PaginationResult(
    JsonArray AllRows,
    List<PaginationPageLog> Logs,
    bool HitMaxPages,
    string? LastError);
