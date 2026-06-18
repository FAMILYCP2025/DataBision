using DataBision.Application.DTOs.Admin;
using DataBision.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/admin/companies/{companyId:int}/native-bi")]
[Authorize(Roles = "SuperAdmin")]
public sealed class NativeBiAdminController(INativeBiAdminConfigService svc) : ControllerBase
{
    // ── Filters ───────────────────────────────────────────────────────────────

    // GET /api/admin/companies/{id}/native-bi/filters
    [HttpGet("filters")]
    public async Task<IActionResult> GetFilters(int companyId, CancellationToken ct)
    {
        var result = await svc.GetFiltersAsync(companyId, ct);
        return Ok(new { data = result });
    }

    // PUT /api/admin/companies/{id}/native-bi/filters/{filterKey}
    [HttpPut("filters/{filterKey}")]
    public async Task<IActionResult> UpsertFilter(
        int companyId, string filterKey,
        [FromBody] UpsertNativeBiFilterConfigDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(filterKey) || filterKey.Length > 100)
            return BadRequest(new { error = "invalid_filter_key", message = "filterKey must be 1–100 characters." });

        var result = await svc.UpsertFilterAsync(companyId, filterKey, dto, ct);
        return Ok(new { data = result });
    }

    // ── Item UDF filters ──────────────────────────────────────────────────────

    // GET /api/admin/companies/{id}/native-bi/item-udf-filters
    [HttpGet("item-udf-filters")]
    public async Task<IActionResult> GetItemUdfFilters(int companyId, CancellationToken ct)
    {
        var result = await svc.GetItemUdfFiltersAsync(companyId, ct);
        return Ok(new { data = result });
    }

    // PUT /api/admin/companies/{id}/native-bi/item-udf-filters/{udfFieldName}
    [HttpPut("item-udf-filters/{udfFieldName}")]
    public async Task<IActionResult> UpsertItemUdfFilter(
        int companyId, string udfFieldName,
        [FromBody] UpsertNativeBiItemUdfFilterConfigDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(udfFieldName) || udfFieldName.Length > 100)
            return BadRequest(new { error = "invalid_udf_field_name", message = "udfFieldName must be 1–100 characters." });

        var result = await svc.UpsertItemUdfFilterAsync(companyId, udfFieldName, dto, ct);
        return Ok(new { data = result });
    }

    // ── Dimensions ────────────────────────────────────────────────────────────

    // GET /api/admin/companies/{id}/native-bi/dimensions
    [HttpGet("dimensions")]
    public async Task<IActionResult> GetDimensions(int companyId, CancellationToken ct)
    {
        var result = await svc.GetDimensionsAsync(companyId, ct);
        return Ok(new { data = result });
    }

    // PUT /api/admin/companies/{id}/native-bi/dimensions/{dimensionNumber}
    [HttpPut("dimensions/{dimensionNumber:int}")]
    public async Task<IActionResult> UpsertDimension(
        int companyId, int dimensionNumber,
        [FromBody] UpsertNativeBiDimensionConfigDto dto,
        CancellationToken ct)
    {
        if (dimensionNumber < 1 || dimensionNumber > 5)
            return BadRequest(new { error = "invalid_dimension_number", message = "dimensionNumber must be 1–5." });

        var result = await svc.UpsertDimensionAsync(companyId, dimensionNumber, dto, ct);
        return Ok(new { data = result });
    }
}
