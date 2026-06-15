using DataBision.Api.Security;
using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/processes")]
[AllowAnonymous]
public sealed class ClientProcessController(
    IProcessService processes,
    IConfiguration config) : ControllerBase
{
    // GET /api/client/processes
    [HttpGet]
    public async Task<IActionResult> GetProcesses(CancellationToken ct)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        var result = await processes.GetEnabledProcessesAsync(ctx.CompanyId!, ct);
        return this.OkData(result);
    }

    // GET /api/client/processes/{processCode}/dashboards
    [HttpGet("{processCode}/dashboards")]
    public async Task<IActionResult> GetDashboards(string processCode, CancellationToken ct)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        if (string.IsNullOrWhiteSpace(processCode))
            return this.BadRequestError("invalid_process_code", "processCode is required.");

        var result = await processes.GetDashboardsByProcessAsync(ctx.CompanyId!, processCode.ToUpperInvariant(), ct);
        return this.OkData(result);
    }
}
