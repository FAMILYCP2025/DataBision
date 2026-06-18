using DataBision.Api.Filters;
using DataBision.Application.DTOs.Ingest;
using DataBision.Application.DTOs.Ingest.Rows;
using DataBision.Application.Interfaces.Ingest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

/// <summary>
/// SAP B1 ingest endpoints. Auth via X-DataBision-ApiKey header.
/// TenantId/CompanyId are resolved from the API key and validated against the body
/// — see <see cref="ApiKeyAuthFilter"/>.
/// </summary>
[ApiController]
[Route("api/ingest")]
[AllowAnonymous]
public sealed class SapB1IngestController(IIngestService ingestService) : ControllerBase
{
    // ── Health (no ApiKey) ─────────────────────────────────────────────────────

    [HttpGet("health")]
    public IActionResult Health() =>
        Ok(new { status = "ok", endpoint = "ingest", timestamp = DateTime.UtcNow });

    // ── Sales Invoices (OINV / INV1) ───────────────────────────────────────────

    [HttpPost("sap-b1/sales-invoices")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestSalesInvoices(
        [FromBody] IngestBatchRequest<SapOinvRow> request, CancellationToken ct)
    {
        var result = await ingestService.IngestSalesInvoicesAsync(request, ct);
        return Ok(new { data = result });
    }

    [HttpPost("sap-b1/sales-invoice-lines")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestSalesInvoiceLines(
        [FromBody] IngestBatchRequest<SapInv1Row> request, CancellationToken ct)
    {
        var result = await ingestService.IngestSalesInvoiceLinesAsync(request, ct);
        return Ok(new { data = result });
    }

    // ── Credit Memos (ORIN / RIN1) ─────────────────────────────────────────────

    [HttpPost("sap-b1/credit-memos")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestCreditMemos(
        [FromBody] IngestBatchRequest<SapOrinRow> request, CancellationToken ct)
    {
        var result = await ingestService.IngestCreditMemosAsync(request, ct);
        return Ok(new { data = result });
    }

    [HttpPost("sap-b1/credit-memo-lines")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestCreditMemoLines(
        [FromBody] IngestBatchRequest<SapRin1Row> request, CancellationToken ct)
    {
        try
        {
            var result = await ingestService.IngestCreditMemoLinesAsync(request, ct);
            return Ok(new { data = result });
        }
        catch (InvalidOperationException ex)
        {
            // RIN1 sin cabecera ORIN — rechazar con 422
            return UnprocessableEntity(new { error = "missing_credit_memo_header", message = ex.Message });
        }
    }

    // ── Master Data ────────────────────────────────────────────────────────────

    [HttpPost("sap-b1/customers")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestCustomers(
        [FromBody] IngestBatchRequest<SapOcrdRow> request, CancellationToken ct)
    {
        var result = await ingestService.IngestCustomersAsync(request, ct);
        return Ok(new { data = result });
    }

    [HttpPost("sap-b1/items")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestItems(
        [FromBody] IngestBatchRequest<SapOitmRow> request, CancellationToken ct)
    {
        var result = await ingestService.IngestItemsAsync(request, ct);
        return Ok(new { data = result });
    }

    [HttpPost("sap-b1/salespersons")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestSalespersons(
        [FromBody] IngestBatchRequest<SapOslpRow> request, CancellationToken ct)
    {
        var result = await ingestService.IngestSalespersonsAsync(request, ct);
        return Ok(new { data = result });
    }

    // ── Purchasing ─────────────────────────────────────────────────────────────

    [HttpPost("sap-b1/purchase-orders")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestPurchaseOrders(
        [FromBody] IngestBatchRequest<SapOporRow> request, CancellationToken ct)
    {
        var result = await ingestService.IngestPurchaseOrdersAsync(request, ct);
        return Ok(new { data = result });
    }

    [HttpPost("sap-b1/purchase-receipts")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestPurchaseReceipts(
        [FromBody] IngestBatchRequest<SapOpdnRow> request, CancellationToken ct)
    {
        var result = await ingestService.IngestPurchaseReceiptsAsync(request, ct);
        return Ok(new { data = result });
    }

    [HttpPost("sap-b1/purchase-invoices")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestPurchaseInvoices(
        [FromBody] IngestBatchRequest<SapOpchRow> request, CancellationToken ct)
    {
        var result = await ingestService.IngestPurchaseInvoicesAsync(request, ct);
        return Ok(new { data = result });
    }

    // ── Inventory ─────────────────────────────────────────────────────────────

    [HttpPost("sap-b1/item-warehouses")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestItemWarehouses(
        [FromBody] IngestBatchRequest<SapOitwRow> request, CancellationToken ct)
    {
        var result = await ingestService.IngestItemWarehousesAsync(request, ct);
        return Ok(new { data = result });
    }

    [HttpPost("sap-b1/stock-transfers")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestStockTransfers(
        [FromBody] IngestBatchRequest<SapOwtrRow> request, CancellationToken ct)
    {
        var result = await ingestService.IngestStockTransfersAsync(request, ct);
        return Ok(new { data = result });
    }

    // ── Sales Fulfillment ─────────────────────────────────────────────────────

    [HttpPost("sap-b1/sales-orders")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestSalesOrders(
        [FromBody] IngestBatchRequest<SapOrdrRow> request, CancellationToken ct)
    {
        var result = await ingestService.IngestSalesOrdersAsync(request, ct);
        return Ok(new { data = result });
    }

    [HttpPost("sap-b1/deliveries")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestDeliveries(
        [FromBody] IngestBatchRequest<SapOdlnRow> request, CancellationToken ct)
    {
        var result = await ingestService.IngestDeliveriesAsync(request, ct);
        return Ok(new { data = result });
    }

    // ── Accounting ─────────────────────────────────────────────────────────────

    [HttpPost("sap-b1/chart-of-accounts")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestChartOfAccounts(
        [FromBody] IngestBatchRequest<SapOactRow> request, CancellationToken ct)
    {
        var result = await ingestService.IngestChartOfAccountsAsync(request, ct);
        return Ok(new { data = result });
    }

    [HttpPost("sap-b1/journal-entries")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestJournalEntries(
        [FromBody] IngestBatchRequest<SapOjdtRow> request, CancellationToken ct)
    {
        var result = await ingestService.IngestJournalEntriesAsync(request, ct);
        return Ok(new { data = result });
    }

    [HttpPost("sap-b1/journal-entry-lines")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<IngestBatchResponse>> IngestJournalEntryLines(
        [FromBody] IngestBatchRequest<SapJdt1Row> request, CancellationToken ct)
    {
        var result = await ingestService.IngestJournalEntryLinesAsync(request, ct);
        return Ok(new { data = result });
    }
}
