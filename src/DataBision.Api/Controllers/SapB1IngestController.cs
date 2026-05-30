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
}
