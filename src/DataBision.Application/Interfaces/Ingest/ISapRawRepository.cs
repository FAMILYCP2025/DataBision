using DataBision.Application.DTOs.Ingest.Rows;

namespace DataBision.Application.Interfaces.Ingest;

public interface ISapRawRepository
{
    Task<(int inserted, int updated)> UpsertSalesInvoicesAsync(string companyId, IEnumerable<SapOinvRow> rows, CancellationToken ct = default);
    Task<(int inserted, int updated)> UpsertSalesInvoiceLinesAsync(string companyId, IEnumerable<SapInv1Row> rows, CancellationToken ct = default);
    Task<(int inserted, int updated)> UpsertCreditMemosAsync(string companyId, IEnumerable<SapOrinRow> rows, CancellationToken ct = default);
    Task<(int inserted, int updated)> UpsertCreditMemoLinesAsync(string companyId, IEnumerable<SapRin1Row> rows, CancellationToken ct = default);
    Task<(int inserted, int updated)> UpsertCustomersAsync(string companyId, IEnumerable<SapOcrdRow> rows, CancellationToken ct = default);
    Task<(int inserted, int updated)> UpsertItemsAsync(string companyId, IEnumerable<SapOitmRow> rows, CancellationToken ct = default);
    Task<(int inserted, int updated)> UpsertSalespersonsAsync(string companyId, IEnumerable<SapOslpRow> rows, CancellationToken ct = default);

    /// <summary>Returns the subset of <paramref name="docEntries"/> that exist in raw.sap_orin for the company.</summary>
    Task<IReadOnlyList<int>> GetExistingCreditMemoDocEntriesAsync(string companyId, IEnumerable<int> docEntries, CancellationToken ct = default);
}
