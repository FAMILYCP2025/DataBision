using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DataBision.Api.Filters;
using DataBision.Api.Middleware;
using DataBision.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/modules")]
[Authorize]
[ValidateTenantClaim]
public class ModulesController(IModuleService moduleService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetModules()
    {
        var userId = int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? "0");
        var companyId = HttpContext.GetTenantCompanyId();
        var isSuperAdmin = User.IsInRole("SuperAdmin");

        var modules = await moduleService.GetAccessibleAsync(userId, companyId, isSuperAdmin);
        return Ok(new { data = modules });
    }

    [HttpGet("{slug}/reports")]
    public async Task<IActionResult> GetReports(string slug)
    {
        var companyId = HttpContext.GetTenantCompanyId();
        if (companyId is null) return Forbid();

        var userId = int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? "0");

        var (reports, error) = await moduleService.GetAccessibleReportsAsync(slug, userId, companyId.Value);
        if (error is not null)
            return NotFound(new { error, message = $"Module '{slug}' not found." });

        return Ok(new { data = reports });
    }
}
