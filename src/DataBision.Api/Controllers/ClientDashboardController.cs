using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/dashboard")]
[AllowAnonymous] // TODO Sprint-6E: enforce JWT company_id claim validation
public sealed class ClientDashboardController(IDashboardService dashboard) : ControllerBase
{
    // GET /api/client/dashboard/summary?companyId=company-dev-001
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string companyId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return BadRequest(new { error = "missing_company_id", message = "companyId is required." });

        var result = await dashboard.GetSummaryAsync(companyId, ct);
        if (result is null)
            return Ok(new { data = (object?)null });

        return Ok(new { data = result });
    }

    // GET /api/client/dashboard/sales-daily?companyId=...&days=30
    [HttpGet("sales-daily")]
    public async Task<IActionResult> GetSalesDaily(
        [FromQuery] string companyId,
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return BadRequest(new { error = "missing_company_id", message = "companyId is required." });

        if (days < 1 || days > 365)
            return BadRequest(new { error = "invalid_days", message = "days must be between 1 and 365." });

        var result = await dashboard.GetSalesDailyAsync(companyId, days, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/dashboard/sales-monthly?companyId=...&months=12
    [HttpGet("sales-monthly")]
    public async Task<IActionResult> GetSalesMonthly(
        [FromQuery] string companyId,
        [FromQuery] int months = 12,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return BadRequest(new { error = "missing_company_id", message = "companyId is required." });

        if (months < 1 || months > 36)
            return BadRequest(new { error = "invalid_months", message = "months must be between 1 and 36." });

        var result = await dashboard.GetSalesMonthlyAsync(companyId, months, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/dashboard/top-customers?companyId=...&limit=10
    [HttpGet("top-customers")]
    public async Task<IActionResult> GetTopCustomers(
        [FromQuery] string companyId,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return BadRequest(new { error = "missing_company_id", message = "companyId is required." });

        if (limit < 1 || limit > 100)
            return BadRequest(new { error = "invalid_limit", message = "limit must be between 1 and 100." });

        var result = await dashboard.GetTopCustomersAsync(companyId, limit, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/dashboard/top-items?companyId=...&limit=10
    [HttpGet("top-items")]
    public async Task<IActionResult> GetTopItems(
        [FromQuery] string companyId,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return BadRequest(new { error = "missing_company_id", message = "companyId is required." });

        if (limit < 1 || limit > 100)
            return BadRequest(new { error = "invalid_limit", message = "limit must be between 1 and 100." });

        var result = await dashboard.GetTopItemsAsync(companyId, limit, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/dashboard/salespersons?companyId=...&limit=20
    [HttpGet("salespersons")]
    public async Task<IActionResult> GetSalespersons(
        [FromQuery] string companyId,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return BadRequest(new { error = "missing_company_id", message = "companyId is required." });

        if (limit < 1 || limit > 100)
            return BadRequest(new { error = "invalid_limit", message = "limit must be between 1 and 100." });

        var result = await dashboard.GetSalespersonsAsync(companyId, limit, ct);
        return Ok(new { data = result });
    }
}
