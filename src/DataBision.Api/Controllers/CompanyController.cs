using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DataBision.Api.Filters;
using DataBision.Api.Middleware;
using DataBision.Application.DTOs.Admin;
using DataBision.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/company")]
[Authorize(Roles = "CompanyAdmin,SuperAdmin")]
[ValidateTenantClaim]
public class CompanyController(
    IUserService userService,
    IPermissionService permissionService,
    IBrandingService brandingService,
    IAuditService auditService,
    IModuleRepository moduleRepository) : ControllerBase
{
    private int GetUserId() => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? "0");

    // CompanyAdmin can only assign roles Viewer/CompanyAdmin. SuperAdmin is reserved
    // for the global admin path and never assignable via /api/company/* endpoints.
    private static readonly HashSet<string> CompanyAdminAssignableRoles =
        new(StringComparer.OrdinalIgnoreCase) { "Viewer", "CompanyAdmin" };

    private bool IsRoleAllowedForCaller(string role)
    {
        var callerRole = User.FindFirstValue("role");
        if (callerRole == nameof(Domain.Enums.UserRole.SuperAdmin))
            return Enum.TryParse<Domain.Enums.UserRole>(role, ignoreCase: true, out _);
        return CompanyAdminAssignableRoles.Contains(role);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var companyId = HttpContext.GetTenantCompanyId();
        if (companyId is null) return Forbid();

        var users = await userService.GetForCompanyAsync(companyId.Value);
        return Ok(new { data = users });
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        var companyId = HttpContext.GetTenantCompanyId();
        if (companyId is null) return Forbid();

        if (!IsRoleAllowedForCaller(dto.Role))
            return BadRequest(new { error = "invalid_role", message = "Role not allowed for this operation." });

        var (result, error) = await userService.CreateForCompanyAsync(companyId.Value, dto);
        if (error == "email_taken")
            return Conflict(new { error, message = "Email already in use." });

        await auditService.LogAsync("USER_CREATED",
            userId: GetUserId(), companyId: companyId,
            resourceType: "User", resourceId: result!.Id.ToString(),
            metadata: System.Text.Json.JsonSerializer.Serialize(new { targetUser = result.Id, targetEmail = dto.Email, role = dto.Role }));

        return CreatedAtAction(nameof(GetUsers), new { data = result });
    }

    [HttpPut("users/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        var companyId = HttpContext.GetTenantCompanyId();
        if (companyId is null) return Forbid();

        if (!IsRoleAllowedForCaller(dto.Role))
            return BadRequest(new { error = "invalid_role", message = "Role not allowed for this operation." });

        var (result, error) = await userService.UpdateAsync(id, companyId.Value, dto);
        if (error == "user_not_found")
            return NotFound(new { error, message = "User not found or does not belong to this company." });

        await auditService.LogAsync("USER_UPDATED",
            userId: GetUserId(), companyId: companyId,
            resourceType: "User", resourceId: id.ToString(),
            metadata: System.Text.Json.JsonSerializer.Serialize(new { targetUser = id, role = dto.Role }));

        return Ok(new { data = result });
    }

    [HttpPatch("users/{id:int}/status")]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusDto dto)
    {
        var companyId = HttpContext.GetTenantCompanyId();
        if (companyId is null) return Forbid();

        var (result, error) = await userService.UpdateStatusAsync(id, companyId.Value, dto);
        if (error == "user_not_found")
            return NotFound(new { error, message = "User not found or does not belong to this company." });
        if (error == "user_limit_reached")
            return BadRequest(new { error, message = "Company user limit reached. Upgrade plan or deactivate another user." });

        await auditService.LogAsync(dto.IsActive ? "USER_ACTIVATED" : "USER_DEACTIVATED",
            userId: GetUserId(), companyId: companyId,
            resourceType: "User", resourceId: id.ToString(),
            metadata: System.Text.Json.JsonSerializer.Serialize(new { targetUser = id }));

        return Ok(new { data = result });
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions()
    {
        var companyId = HttpContext.GetTenantCompanyId();
        if (companyId is null) return Forbid();

        var users = await userService.GetForCompanyAsync(companyId.Value);
        var permissions = await permissionService.GetForCompanyAsync(companyId.Value);
        var modules = await moduleRepository.GetAllAsync();
        var moduleMap = modules.ToDictionary(m => m.Id, m => m.Slug);

        var grouped = users.Select(u => new
        {
            userId = u.Id,
            email = u.Email,
            permissions = permissions
                .Where(p => p.UserId == u.Id && moduleMap.ContainsKey(p.ModuleId))
                .Select(p => new
                {
                    moduleSlug = moduleMap[p.ModuleId],
                    reportId = p.ReportId,
                    enabled = p.CanView
                }).ToList()
        });

        return Ok(new { data = grouped });
    }

    [HttpPut("permissions")]
    public async Task<IActionResult> UpdatePermissions([FromBody] UpdateUserPermissionsDto dto)
    {
        var companyId = HttpContext.GetTenantCompanyId();
        if (companyId is null) return Forbid();

        // Block cross-tenant permission writes: dto.UserId must belong to the current tenant.
        var belongs = await permissionService.UserBelongsToCompanyAsync(dto.UserId, companyId.Value);
        if (!belongs)
            return BadRequest(new { error = "user_not_in_company", message = "Target user does not belong to this company." });

        var map = await moduleRepository.GetModuleSlugMapAsync();

        var updates = dto.Permissions
            .Where(p => map.ContainsKey(p.ModuleSlug))
            .Select(p => new PermissionUpdateDto(dto.UserId, map[p.ModuleSlug], p.ReportId, p.Enabled))
            .ToList();

        // Reemplazar transaccionalmente los permisos para este usuario
        await permissionService.ReplaceUserPermissionsAsync(companyId.Value, dto.UserId, GetUserId(), updates);

        await auditService.LogAsync("PERMISSIONS_UPDATED", userId: GetUserId(), companyId: companyId,
            metadata: System.Text.Json.JsonSerializer.Serialize(new { updatedUser = dto.UserId, count = updates.Count }));

        return Ok(new { data = new { updated = updates.Count } });
    }

    [HttpPut("branding")]
    public async Task<IActionResult> UpdateBranding([FromBody] BrandingUpdateDto dto)
    {
        var companyId = HttpContext.GetTenantCompanyId();
        if (companyId is null) return Forbid();

        var branding = new Domain.Entities.CompanyBranding
        {
            PrimaryColor = dto.PrimaryColor,
            SecondaryColor = dto.SecondaryColor,
            AccentColor = dto.AccentColor,
            BackgroundColor = dto.BackgroundColor,
            SidebarColor = dto.SidebarColor,
            CompanyDisplayName = dto.CompanyDisplayName
        };

        var result = await brandingService.UpdateAsync(companyId.Value, branding);
        await auditService.LogAsync("UPDATE_BRANDING", userId: GetUserId(), companyId: companyId);

        return Ok(new { data = result });
    }

    [HttpPost("branding/logo")]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        var companyId = HttpContext.GetTenantCompanyId();
        if (companyId is null) return Forbid();

        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(new { error = "file_too_large", message = "Max file size is 2MB." });

        var allowed = new[] { "image/png", "image/svg+xml", "image/x-icon" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(new { error = "invalid_file_type", message = "Only PNG, SVG, ICO allowed." });

        using var stream = file.OpenReadStream();
        var url = await brandingService.UploadLogoAsync(companyId.Value, stream, file.FileName, file.ContentType);
        return Ok(new { data = new { logoUrl = url } });
    }
}

public record UpdateUserPermissionsDto(int UserId, List<ClientPermissionDto> Permissions);
public record ClientPermissionDto(string ModuleSlug, int? ReportId, bool Enabled);
public record BrandingUpdateDto(
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string BackgroundColor,
    string SidebarColor,
    string? CompanyDisplayName);
