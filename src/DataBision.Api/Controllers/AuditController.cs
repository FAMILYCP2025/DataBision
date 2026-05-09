using System.Security.Claims;
using DataBision.Api.Filters;
using DataBision.Api.Middleware;
using DataBision.Application.Interfaces;
using DataBision.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "SuperAdmin,CompanyAdmin")]
[ValidateTenantClaim]
public class AuditController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));
        var companyId = isSuperAdmin ? (int?)null : HttpContext.GetTenantCompanyId();

        if (!isSuperAdmin && companyId is null)
            return Forbid();

        var logs = await auditService.GetPagedAsync(companyId, page, pageSize);
        return Ok(new { data = logs });
    }
}
