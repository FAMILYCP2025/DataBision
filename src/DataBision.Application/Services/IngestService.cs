using DataBision.Application.DTOs.Ingest;
using DataBision.Application.DTOs.Ingest.Rows;
using DataBision.Application.Interfaces.Ingest;
using DataBision.Shared.Hashing;
using DataBision.Shared.Watermarks;

namespace DataBision.Application.Services;

/// <summary>
/// Orchestrates ingest: normalises UpdateTS/CreateTS to CHAR(6), computes canonical hashes,
/// calls repository, updates checkpoint. The API is authoritative for both TSNorm and source_hash.
/// </summary>
public sealed class IngestService(
    ISapRawRepository rawRepo,
    IIngestCheckpointRepository checkpointRepo) : IIngestService
{
    // ── Sales Invoices (OINV) ──────────────────────────────────────────────────

    public async Task<IngestBatchResponse> IngestSalesInvoicesAsync(
        IngestBatchRequest<SapOinvRow> request, CancellationToken ct = default)
    {
        foreach (var r in request.Rows)
        {
            r.CreateTSNorm = TsNormalizer.Normalize(r.CreateTS);
            r.UpdateTSNorm = TsNormalizer.Normalize(r.UpdateTS);
        }
        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertSalesInvoicesAsync(request.CompanyId, request.Rows, ct);
        var (wmDate, wmTs) = GetWatermark(request.Rows, r => r.UpdateDate, r => r.UpdateTSNorm);
        await UpdateCheckpointAsync(request, ins + upd, wmDate, wmTs, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, wmDate, wmTs);
    }

    public async Task<IngestBatchResponse> IngestSalesInvoiceLinesAsync(
        IngestBatchRequest<SapInv1Row> request, CancellationToken ct = default)
    {
        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertSalesInvoiceLinesAsync(request.CompanyId, request.Rows, ct);
        await UpdateCheckpointAsync(request, ins + upd, null, null, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, null, null);
    }

    // ── Credit Memos (ORIN) ────────────────────────────────────────────────────

    public async Task<IngestBatchResponse> IngestCreditMemosAsync(
        IngestBatchRequest<SapOrinRow> request, CancellationToken ct = default)
    {
        foreach (var r in request.Rows)
        {
            r.CreateTSNorm = TsNormalizer.Normalize(r.CreateTS);
            r.UpdateTSNorm = TsNormalizer.Normalize(r.UpdateTS);
        }
        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertCreditMemosAsync(request.CompanyId, request.Rows, ct);
        var (wmDate, wmTs) = GetWatermark(request.Rows, r => r.UpdateDate, r => r.UpdateTSNorm);
        await UpdateCheckpointAsync(request, ins + upd, wmDate, wmTs, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, wmDate, wmTs);
    }

    public async Task<IngestBatchResponse> IngestCreditMemoLinesAsync(
        IngestBatchRequest<SapRin1Row> request, CancellationToken ct = default)
    {
        // Reject the entire batch if any line references a DocEntry without an ORIN header in raw.
        var docEntries = request.Rows.Select(r => r.DocEntry).Distinct().ToList();
        var existing = await rawRepo.GetExistingCreditMemoDocEntriesAsync(request.CompanyId, docEntries, ct);
        var missing = docEntries.Except(existing).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot ingest RIN1 lines without ORIN header. Missing DocEntries: {string.Join(",", missing)}");
        }

        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertCreditMemoLinesAsync(request.CompanyId, request.Rows, ct);
        await UpdateCheckpointAsync(request, ins + upd, null, null, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, null, null);
    }

    // ── Customers (OCRD) ──────────────────────────────────────────────────────

    public async Task<IngestBatchResponse> IngestCustomersAsync(
        IngestBatchRequest<SapOcrdRow> request, CancellationToken ct = default)
    {
        foreach (var r in request.Rows)
        {
            r.CreateTSNorm = TsNormalizer.Normalize(r.CreateTS);
            r.UpdateTSNorm = TsNormalizer.Normalize(r.UpdateTS);
        }
        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertCustomersAsync(request.CompanyId, request.Rows, ct);
        var (wmDate, wmTs) = GetWatermark(request.Rows, r => r.UpdateDate, r => r.UpdateTSNorm);
        await UpdateCheckpointAsync(request, ins + upd, wmDate, wmTs, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, wmDate, wmTs);
    }

    // ── Items (OITM) ──────────────────────────────────────────────────────────

    public async Task<IngestBatchResponse> IngestItemsAsync(
        IngestBatchRequest<SapOitmRow> request, CancellationToken ct = default)
    {
        foreach (var r in request.Rows)
        {
            r.CreateTSNorm = TsNormalizer.Normalize(r.CreateTS);
            r.UpdateTSNorm = TsNormalizer.Normalize(r.UpdateTS);
        }
        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertItemsAsync(request.CompanyId, request.Rows, ct);
        var (wmDate, wmTs) = GetWatermark(request.Rows, r => r.UpdateDate, r => r.UpdateTSNorm);
        await UpdateCheckpointAsync(request, ins + upd, wmDate, wmTs, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, wmDate, wmTs);
    }

    // ── Salespersons (OSLP) ────────────────────────────────────────────────────

    public async Task<IngestBatchResponse> IngestSalespersonsAsync(
        IngestBatchRequest<SapOslpRow> request, CancellationToken ct = default)
    {
        foreach (var r in request.Rows)
        {
            r.CreateTSNorm = TsNormalizer.Normalize(r.CreateTS);
            r.UpdateTSNorm = TsNormalizer.Normalize(r.UpdateTS);
        }
        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertSalespersonsAsync(request.CompanyId, request.Rows, ct);
        var (wmDate, wmTs) = GetWatermark(request.Rows, r => r.UpdateDate, r => r.UpdateTSNorm);
        await UpdateCheckpointAsync(request, ins + upd, wmDate, wmTs, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, wmDate, wmTs);
    }

    // ── Purchase Orders (OPOR) ─────────────────────────────────────────────────

    public async Task<IngestBatchResponse> IngestPurchaseOrdersAsync(
        IngestBatchRequest<SapOporRow> request, CancellationToken ct = default)
    {
        foreach (var r in request.Rows) { r.CreateTSNorm = TsNormalizer.Normalize(r.CreateTS); r.UpdateTSNorm = TsNormalizer.Normalize(r.UpdateTS); }
        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertPurchaseOrdersAsync(request.CompanyId, request.Rows, ct);
        var (wmDate, wmTs) = GetWatermark(request.Rows, r => r.UpdateDate, r => r.UpdateTSNorm);
        await UpdateCheckpointAsync(request, ins + upd, wmDate, wmTs, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, wmDate, wmTs);
    }

    // ── Purchase Receipts / Goods Receipts (OPDN) ─────────────────────────────

    public async Task<IngestBatchResponse> IngestPurchaseReceiptsAsync(
        IngestBatchRequest<SapOpdnRow> request, CancellationToken ct = default)
    {
        foreach (var r in request.Rows) { r.CreateTSNorm = TsNormalizer.Normalize(r.CreateTS); r.UpdateTSNorm = TsNormalizer.Normalize(r.UpdateTS); }
        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertPurchaseReceiptsAsync(request.CompanyId, request.Rows, ct);
        var (wmDate, wmTs) = GetWatermark(request.Rows, r => r.UpdateDate, r => r.UpdateTSNorm);
        await UpdateCheckpointAsync(request, ins + upd, wmDate, wmTs, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, wmDate, wmTs);
    }

    // ── Purchase Invoices (OPCH) ───────────────────────────────────────────────

    public async Task<IngestBatchResponse> IngestPurchaseInvoicesAsync(
        IngestBatchRequest<SapOpchRow> request, CancellationToken ct = default)
    {
        foreach (var r in request.Rows) { r.CreateTSNorm = TsNormalizer.Normalize(r.CreateTS); r.UpdateTSNorm = TsNormalizer.Normalize(r.UpdateTS); }
        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertPurchaseInvoicesAsync(request.CompanyId, request.Rows, ct);
        var (wmDate, wmTs) = GetWatermark(request.Rows, r => r.UpdateDate, r => r.UpdateTSNorm);
        await UpdateCheckpointAsync(request, ins + upd, wmDate, wmTs, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, wmDate, wmTs);
    }

    // ── Item Warehouse Levels (OITW) ───────────────────────────────────────────

    public async Task<IngestBatchResponse> IngestItemWarehousesAsync(
        IngestBatchRequest<SapOitwRow> request, CancellationToken ct = default)
    {
        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertItemWarehousesAsync(request.CompanyId, request.Rows, ct);
        await UpdateCheckpointAsync(request, ins + upd, null, null, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, null, null);
    }

    // ── Sales Orders (ORDR) ────────────────────────────────────────────────────

    public async Task<IngestBatchResponse> IngestSalesOrdersAsync(
        IngestBatchRequest<SapOrdrRow> request, CancellationToken ct = default)
    {
        foreach (var r in request.Rows) { r.CreateTSNorm = TsNormalizer.Normalize(r.CreateTS); r.UpdateTSNorm = TsNormalizer.Normalize(r.UpdateTS); }
        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertSalesOrdersAsync(request.CompanyId, request.Rows, ct);
        var (wmDate, wmTs) = GetWatermark(request.Rows, r => r.UpdateDate, r => r.UpdateTSNorm);
        await UpdateCheckpointAsync(request, ins + upd, wmDate, wmTs, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, wmDate, wmTs);
    }

    // ── Delivery Notes (ODLN) ─────────────────────────────────────────────────

    public async Task<IngestBatchResponse> IngestDeliveriesAsync(
        IngestBatchRequest<SapOdlnRow> request, CancellationToken ct = default)
    {
        foreach (var r in request.Rows) { r.CreateTSNorm = TsNormalizer.Normalize(r.CreateTS); r.UpdateTSNorm = TsNormalizer.Normalize(r.UpdateTS); }
        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertDeliveriesAsync(request.CompanyId, request.Rows, ct);
        var (wmDate, wmTs) = GetWatermark(request.Rows, r => r.UpdateDate, r => r.UpdateTSNorm);
        await UpdateCheckpointAsync(request, ins + upd, wmDate, wmTs, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, wmDate, wmTs);
    }

    // ── Stock Transfers (OWTR) ─────────────────────────────────────────────────

    public async Task<IngestBatchResponse> IngestStockTransfersAsync(
        IngestBatchRequest<SapOwtrRow> request, CancellationToken ct = default)
    {
        foreach (var r in request.Rows) { r.CreateTSNorm = TsNormalizer.Normalize(r.CreateTS); r.UpdateTSNorm = TsNormalizer.Normalize(r.UpdateTS); }
        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertStockTransfersAsync(request.CompanyId, request.Rows, ct);
        var (wmDate, wmTs) = GetWatermark(request.Rows, r => r.UpdateDate, r => r.UpdateTSNorm);
        await UpdateCheckpointAsync(request, ins + upd, wmDate, wmTs, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, wmDate, wmTs);
    }

    // ── Chart of Accounts (OACT) — full-refresh, no UpdateDate ────────────────

    public async Task<IngestBatchResponse> IngestChartOfAccountsAsync(
        IngestBatchRequest<SapOactRow> request, CancellationToken ct = default)
    {
        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertChartOfAccountsAsync(request.CompanyId, request.Rows, ct);
        await UpdateCheckpointAsync(request, ins + upd, null, null, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, null, null);
    }

    // ── Journal Entry Headers (OJDT) — incremental by RefDate ─────────────────

    public async Task<IngestBatchResponse> IngestJournalEntriesAsync(
        IngestBatchRequest<SapOjdtRow> request, CancellationToken ct = default)
    {
        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertJournalEntriesAsync(request.CompanyId, request.Rows, ct);
        var (wmDate, wmTs) = GetWatermark(request.Rows, r => r.RefDate, _ => null);
        await UpdateCheckpointAsync(request, ins + upd, wmDate, wmTs, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, wmDate, wmTs);
    }

    // ── Journal Entry Lines (JDT1) — embedded in OJDT, no independent watermark

    public async Task<IngestBatchResponse> IngestJournalEntryLinesAsync(
        IngestBatchRequest<SapJdt1Row> request, CancellationToken ct = default)
    {
        ComputeHashes(request.Rows);
        var (ins, upd) = await rawRepo.UpsertJournalEntryLinesAsync(request.CompanyId, request.Rows, ct);
        await UpdateCheckpointAsync(request, ins + upd, null, null, ct);
        return BuildResponse(request.SapObject, request.Rows.Count, ins, upd, null, null);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private static void ComputeHashes<T>(IEnumerable<T> rows) where T : IIngestRow
    {
        foreach (var row in rows)
            row.SourceHashHex = CanonicalHasher.ComputeHex(row.ToColumns());
    }

    private static (string? wmDate, string? wmTsNorm) GetWatermark<T>(
        IEnumerable<T> rows,
        Func<T, DateTime?> getDate,
        Func<T, string?> getTsNorm)
    {
        T? latest = default;
        DateTime? maxDate = null;
        string? latestTs = null;

        foreach (var row in rows)
        {
            var d = getDate(row);
            if (d is null) continue;
            var ts = getTsNorm(row) ?? "000000";
            if (maxDate is null
                || d > maxDate
                || (d == maxDate && string.CompareOrdinal(ts, latestTs ?? "000000") > 0))
            {
                maxDate = d;
                latest = row;
                latestTs = ts;
            }
        }

        if (latest is null) return (null, null);
        return (getDate(latest)?.ToString("yyyy-MM-dd"), getTsNorm(latest));
    }

    private async Task UpdateCheckpointAsync<T>(
        IngestBatchRequest<T> request, int processedCount,
        string? wmDate, string? wmTsNorm, CancellationToken ct)
        where T : IIngestRow
    {
        if (processedCount == 0) return;

        var existing = await checkpointRepo.GetAsync(request.TenantId, request.CompanyId, request.SapObject, ct);
        var totalSoFar = existing?.TotalRowsIngested ?? 0L;

        await checkpointRepo.UpsertAsync(new CheckpointDto
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            SapObject = request.SapObject,
            WatermarkDate = wmDate ?? existing?.WatermarkDate,
            WatermarkTs = wmTsNorm ?? existing?.WatermarkTs,
            LastSuccessfulRunUtc = DateTime.UtcNow,
            TotalRowsIngested = totalSoFar + processedCount,
        }, ct);
    }

    private static IngestBatchResponse BuildResponse(
        string sapObject, int received, int ins, int upd, string? wmDate, string? wmTsNorm)
        => new()
        {
            RunId = Guid.NewGuid().ToString(),
            SapObject = sapObject,
            RowsReceived = received,
            RowsInserted = ins,
            RowsUpdated = upd,
            RowsSkipped = received - ins - upd,
            ProcessedAtUtc = DateTime.UtcNow,
            WatermarkDate = wmDate,
            WatermarkTs = wmTsNorm,
        };
}
