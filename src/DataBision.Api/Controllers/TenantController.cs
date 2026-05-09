using DataBision.Api.Middleware;
using DataBision.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/tenant")]
public class TenantController(ITenantService tenantService) : ControllerBase
{
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        var slug = HttpContext.GetTenantSlug();
        if (string.IsNullOrEmpty(slug))
            return NotFound(new { error = "tenant_not_found", message = "No tenant resolved from host." });

        var config = await tenantService.GetConfigBySlugAsync(slug);
        if (config is null)
            return NotFound(new { error = "tenant_not_found", message = "Company not found." });

        return Ok(new { data = config });
    }
}
