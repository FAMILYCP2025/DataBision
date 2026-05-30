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
}
