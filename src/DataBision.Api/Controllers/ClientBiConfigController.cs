using System.Security.Claims;
using DataBision.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/bi/filter-config")]
[Authorize]
public sealed class ClientBiConfigController(INativeBiAdminConfigService svc) : ControllerBase
{
    // GET /api/client/bi/filter-config
    // Returns the enabled filter/dimension config for the current tenant.
    // Reads company_id (int) directly from JWT claim — not from subdomain.
    // Frontend uses this to apply label overrides and hide disabled filters.
    [HttpGet]
    public async Task<IActionResult> GetFilterConfig(CancellationToken ct)
    {
        var claim = User.FindFirstValue("company_id");
        if (!int.TryParse(claim, out var companyId))
            return Unauthorized(new { error = "no_company", message = "JWT missing company_id claim." });

        var filters    = await svc.GetFiltersAsync(companyId, ct);
        var udfFilters = await svc.GetItemUdfFiltersAsync(companyId, ct);
        var dimensions = await svc.GetDimensionsAsync(companyId, ct);

        return Ok(new
        {
            data = new
            {
                filters    = filters.Where(f => f.IsEnabled).OrderBy(f => f.DisplayOrder),
                itemUdfFilters = udfFilters.Where(f => f.IsEnabled).OrderBy(f => f.DisplayOrder),
                dimensions = dimensions.Where(d => d.IsEnabled).OrderBy(d => d.DimensionNumber),
            }
        });
    }
}
