using DataBision.Application.DTOs.Admin;
using DataBision.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/admin/companies/{companyId:int}/native-bi")]
[Authorize(Roles = "SuperAdmin")]
public sealed class NativeBiAdminController(
    INativeBiAdminConfigService svc,
    INativeBiConnectionProfileService profiles) : ControllerBase
{
    // ── Connection Profiles ───────────────────────────────────────────────────

    // GET /api/admin/companies/{companyId}/native-bi/connection-profiles
    [HttpGet("connection-profiles")]
    public async Task<IActionResult> GetConnectionProfiles(int companyId, CancellationToken ct)
    {
        var result = await profiles.GetAllAsync(companyId, ct);
        return Ok(new { data = result });
    }

    // GET /api/admin/companies/{companyId}/native-bi/connection-profiles/{profileId}
    [HttpGet("connection-profiles/{profileId:int}")]
    public async Task<IActionResult> GetConnectionProfile(int companyId, int profileId, CancellationToken ct)
    {
        var result = await profiles.GetByIdAsync(companyId, profileId, ct);
        if (result is null)
            return NotFound(new { error = "profile_not_found", message = "Connection profile not found." });
        return Ok(new { data = result });
    }

    // POST /api/admin/companies/{companyId}/native-bi/connection-profiles
    [HttpPost("connection-profiles")]
    public async Task<IActionResult> CreateConnectionProfile(
        int companyId,
        [FromBody] CreateNativeBiConnectionProfileRequest request,
        CancellationToken ct)
    {
        var (result, error) = await profiles.CreateAsync(companyId, request, ct);
        return error switch
        {
            "company_not_found"    => NotFound(new { error, message = "Company not found." }),
            "profile_name_taken"   => Conflict(new { error, message = "A profile with this name already exists for this company." }),
            not null               => BadRequest(new { error, message = ToMessage(error) }),
            _                      => CreatedAtAction(nameof(GetConnectionProfile),
                                        new { companyId, profileId = result!.Id },
                                        new { data = result })
        };
    }

    // PUT /api/admin/companies/{companyId}/native-bi/connection-profiles/{profileId}
    [HttpPut("connection-profiles/{profileId:int}")]
    public async Task<IActionResult> UpdateConnectionProfile(
        int companyId, int profileId,
        [FromBody] UpdateNativeBiConnectionProfileRequest request,
        CancellationToken ct)
    {
        var (result, error) = await profiles.UpdateAsync(companyId, profileId, request, ct);
        return error switch
        {
            "profile_not_found"  => NotFound(new { error, message = "Connection profile not found." }),
            "profile_name_taken" => Conflict(new { error, message = "A profile with this name already exists for this company." }),
            not null             => BadRequest(new { error, message = ToMessage(error) }),
            _                    => Ok(new { data = result })
        };
    }

    // DELETE /api/admin/companies/{companyId}/native-bi/connection-profiles/{profileId}
    [HttpDelete("connection-profiles/{profileId:int}")]
    public async Task<IActionResult> DeleteConnectionProfile(int companyId, int profileId, CancellationToken ct)
    {
        var error = await profiles.DeleteAsync(companyId, profileId, ct);
        if (error == "profile_not_found")
            return NotFound(new { error, message = "Connection profile not found." });
        return NoContent();
    }

    // POST /api/admin/companies/{companyId}/native-bi/connection-profiles/{profileId}/test
    [HttpPost("connection-profiles/{profileId:int}/test")]
    public async Task<IActionResult> TestConnectionProfile(int companyId, int profileId, CancellationToken ct)
    {
        var result = await profiles.TestAsync(companyId, profileId, ct);
        return Ok(new { data = result });
    }

    private static string ToMessage(string error) => error switch
    {
        "profile_name_required"         => "ProfileName is required.",
        "profile_name_too_long"         => "ProfileName must be 100 characters or fewer.",
        "service_layer_base_url_required" => "ServiceLayerBaseUrl is required.",
        "company_db_required"           => "CompanyDb is required.",
        "sap_user_name_required"        => "SapUserName is required.",
        "secret_ref_required"           => "SecretRef is required (e.g. env:VARIABLE_NAME).",
        "timeout_seconds_out_of_range"  => "TimeoutSeconds must be between 10 and 300.",
        "fetch_concurrency_out_of_range" => "FetchConcurrency must be between 1 and 10.",
        _                               => error
    };


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
