using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DataBision.Api.Filters;
using DataBision.Api.Middleware;
using DataBision.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
[ValidateTenantClaim]
public class ReportsController(
    IEmbedTokenService embedTokenService,
    IPermissionService permissionService,
    IAuditService auditService) : ControllerBase
{
    [HttpPost("{id:int}/embed-token")]
    public async Task<IActionResult> GetEmbedToken(int id)
    {
        if (!int.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
            return Unauthorized(new { error = "invalid_token", message = "User identity missing from token." });

        var companyId = HttpContext.GetTenantCompanyId();

        if (companyId is null)
            return Forbid();

        var canView = await permissionService.CanViewReportAsync(userId, id, companyId.Value);
        if (!canView)
            return StatusCode(403, new { error = "report_access_denied", message = "You do not have access to this report." });

        var slug = HttpContext.GetTenantSlug()!;
        var token = await embedTokenService.GenerateAsync(id, slug);

        await auditService.LogAsync(
            "VIEW_REPORT",
            userId: userId,
            companyId: companyId,
            resourceType: "Report",
            resourceId: id.ToString(),
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new { data = token });
    }
}
