using DataBision.Application.DTOs.Ingest;
using DataBision.Application.DTOs.Ingest.Rows;

namespace DataBision.Application.Interfaces.Ingest;

public interface IIngestService
{
    Task<IngestBatchResponse> IngestSalesInvoicesAsync(IngestBatchRequest<SapOinvRow> request, CancellationToken ct = default);
    Task<IngestBatchResponse> IngestSalesInvoiceLinesAsync(IngestBatchRequest<SapInv1Row> request, CancellationToken ct = default);
    Task<IngestBatchResponse> IngestCreditMemosAsync(IngestBatchRequest<SapOrinRow> request, CancellationToken ct = default);
    Task<IngestBatchResponse> IngestCreditMemoLinesAsync(IngestBatchRequest<SapRin1Row> request, CancellationToken ct = default);
    Task<IngestBatchResponse> IngestCustomersAsync(IngestBatchRequest<SapOcrdRow> request, CancellationToken ct = default);
    Task<IngestBatchResponse> IngestItemsAsync(IngestBatchRequest<SapOitmRow> request, CancellationToken ct = default);
    Task<IngestBatchResponse> IngestSalespersonsAsync(IngestBatchRequest<SapOslpRow> request, CancellationToken ct = default);
    Task<IngestBatchResponse> IngestPurchaseOrdersAsync(IngestBatchRequest<SapOporRow> request, CancellationToken ct = default);
    Task<IngestBatchResponse> IngestPurchaseReceiptsAsync(IngestBatchRequest<SapOpdnRow> request, CancellationToken ct = default);
    Task<IngestBatchResponse> IngestPurchaseInvoicesAsync(IngestBatchRequest<SapOpchRow> request, CancellationToken ct = default);
    Task<IngestBatchResponse> IngestItemWarehousesAsync(IngestBatchRequest<SapOitwRow> request, CancellationToken ct = default);
    Task<IngestBatchResponse> IngestSalesOrdersAsync(IngestBatchRequest<SapOrdrRow> request, CancellationToken ct = default);
    Task<IngestBatchResponse> IngestDeliveriesAsync(IngestBatchRequest<SapOdlnRow> request, CancellationToken ct = default);
    Task<IngestBatchResponse> IngestStockTransfersAsync(IngestBatchRequest<SapOwtrRow> request, CancellationToken ct = default);
    Task<IngestBatchResponse> IngestChartOfAccountsAsync(IngestBatchRequest<SapOactRow> request, CancellationToken ct = default);
    Task<IngestBatchResponse> IngestJournalEntriesAsync(IngestBatchRequest<SapOjdtRow> request, CancellationToken ct = default);
    Task<IngestBatchResponse> IngestJournalEntryLinesAsync(IngestBatchRequest<SapJdt1Row> request, CancellationToken ct = default);
}
