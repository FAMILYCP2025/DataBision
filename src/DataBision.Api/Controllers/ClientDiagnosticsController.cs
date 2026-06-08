using DataBision.Api.Security;
using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/diagnostics")]
[AllowAnonymous]
public sealed class ClientDiagnosticsController(
    IDiagnosticsService diagnostics,
    IConfiguration config) : ControllerBase
{
    // GET /api/client/diagnostics/native-bi
    // Returns health checks: connection, MART freshness, checkpoints, extraction run.
    [HttpGet("native-bi")]
    public async Task<IActionResult> GetDiagnostics(CancellationToken ct)
    {
        var (companyId, err) = CompanyContextResolver.TryResolve(HttpContext, config);
        if (err is not null) return err;

        var result = await diagnostics.GetDiagnosticsAsync(companyId!, ct);
        return Ok(result);
    }

    // GET /api/client/diagnostics/native-bi/tables
    // Returns row counts and transformed_at_utc for stg.* and mart.* tables.
    [HttpGet("native-bi/tables")]
    public async Task<IActionResult> GetTableCounts(CancellationToken ct)
    {
        var (companyId, err) = CompanyContextResolver.TryResolve(HttpContext, config);
        if (err is not null) return err;

        var result = await diagnostics.GetTableCountsAsync(companyId!, ct);
        return Ok(result);
    }
}
