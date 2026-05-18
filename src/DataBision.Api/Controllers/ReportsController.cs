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
    IPowerBIService powerBIService,
    IPermissionService permissionService,
    IAuditService auditService) : ControllerBase
{
    // ── GET /api/reports/{id}/embed-config ────────────────────────────────────
    // Returns the Power BI embed configuration for a report the user has access to.
    // 403 — no permission | 404 — report not found | 501 — PowerBI not configured | 200 — ok
    [HttpGet("{id:int}/embed-config")]
    public async Task<IActionResult> GetEmbedConfig(int id)
    {
        if (!int.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
            return Unauthorized(new { error = "invalid_token", message = "User identity missing from token." });

        var companyId = HttpContext.GetTenantCompanyId();
        if (companyId is null) return Forbid();

        var slug = HttpContext.GetTenantSlug()!;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        await auditService.LogAsync(
            "EMBED_CONFIG_REQUESTED",
            userId: userId,
            companyId: companyId,
            resourceType: "Report",
            resourceId: id.ToString(),
            metadata: new { reportId = id, companyId, userId, tenant = slug },
            ipAddress: ip);

        var canView = await permissionService.CanViewReportAsync(userId, id, companyId.Value);
        if (!canView)
        {
            await auditService.LogAsync(
                "EMBED_CONFIG_DENIED",
                userId: userId,
                companyId: companyId,
                resourceType: "Report",
                resourceId: id.ToString(),
                metadata: new { reportId = id, companyId, userId, tenant = slug, reason = "permission_denied" },
                ipAddress: ip);
            return StatusCode(403, new { error = "report_access_denied", message = "You do not have access to this report." });
        }

        try
        {
            var config = await powerBIService.GetEmbedConfigurationAsync(id, companyId.Value, slug);

            await auditService.LogAsync(
                "REPORT_VIEWED",
                userId: userId,
                companyId: companyId,
                resourceType: "Report",
                resourceId: id.ToString(),
                metadata: new { reportId = id, companyId = companyId.Value, moduleId = config.ModuleId, userId, tenant = slug },
                ipAddress: ip);

            return Ok(new { data = config });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "report_not_found", message = "Report not found." });
        }
        catch (NotImplementedException)
        {
            await auditService.LogAsync(
                "EMBED_CONFIG_DENIED",
                userId: userId,
                companyId: companyId,
                resourceType: "Report",
                resourceId: id.ToString(),
                metadata: new { reportId = id, companyId, userId, tenant = slug, reason = "powerbi_not_configured" },
                ipAddress: ip);
            return StatusCode(501, new { error = "powerbi_not_configured", message = "Power BI Embedded is not yet configured for this instance." });
        }
        catch (InvalidOperationException ex)
        {
            await auditService.LogAsync(
                "EMBED_CONFIG_DENIED",
                userId: userId,
                companyId: companyId,
                resourceType: "Report",
                resourceId: id.ToString(),
                metadata: new { reportId = id, companyId, userId, tenant = slug, reason = "powerbi_disabled" },
                ipAddress: ip);
            return StatusCode(501, new { error = "powerbi_not_configured", message = ex.Message });
        }
    }

    // ── POST /api/reports/{id}/embed-token ────────────────────────────────────
    // Legacy endpoint — direct embed token generation (will be removed in Phase 3
    // once embed-config is the sole entry point for the frontend).
    [HttpPost("{id:int}/embed-token")]
    public async Task<IActionResult> GetEmbedToken(int id)
    {
        if (!int.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
            return Unauthorized(new { error = "invalid_token", message = "User identity missing from token." });

        var companyId = HttpContext.GetTenantCompanyId();
        if (companyId is null) return Forbid();

        var canView = await permissionService.CanViewReportAsync(userId, id, companyId.Value);
        if (!canView)
            return StatusCode(403, new { error = "report_access_denied", message = "You do not have access to this report." });

        var slug = HttpContext.GetTenantSlug()!;

        try
        {
            var token = await powerBIService.GenerateEmbedTokenAsync(id, slug);

            await auditService.LogAsync(
                "REPORT_VIEWED",
                userId: userId,
                companyId: companyId,
                resourceType: "Report",
                resourceId: id.ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(new { data = token });
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, new { error = "powerbi_not_configured", message = "Power BI Embedded is not yet configured." });
        }
    }
}
