using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DataBision.Application.DTOs.Admin;
using DataBision.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "SuperAdmin")]
public class AdminController(
    ICompanyService companyService, 
    IUserService userService, 
    IAuditService auditService,
    IModuleService moduleService,
    IReportService reportService) : ControllerBase
{
    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? "0");

    // ── Companies ──────────────────────────────────────────────────────────

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies()
    {
        var companies = await companyService.GetAllAsync();
        return Ok(new { data = companies });
    }

    [HttpPost("companies")]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyDto dto)
    {
        var (result, error) = await companyService.CreateAsync(dto);
        if (error is not null)
            return Conflict(new { error, message = "A company with this slug already exists." });

        await auditService.LogAsync("COMPANY_CREATED",
            userId: GetCurrentUserId(),
            resourceType: "Company", resourceId: result!.Id.ToString(),
            metadata: System.Text.Json.JsonSerializer.Serialize(new { slug = dto.Slug, name = dto.Name }));

        return CreatedAtAction(nameof(GetCompanies), new { data = result });
    }

    [HttpPut("companies/{id:int}")]
    public async Task<IActionResult> UpdateCompany(int id, [FromBody] UpdateCompanyDto dto)
    {
        var (result, error) = await companyService.UpdateAsync(id, dto);
        if (error == "company_not_found")
            return NotFound(new { error, message = "Company not found." });

        await auditService.LogAsync("COMPANY_UPDATED",
            userId: GetCurrentUserId(),
            resourceType: "Company", resourceId: id.ToString(),
            metadata: System.Text.Json.JsonSerializer.Serialize(new { name = dto.Name, plan = dto.PlanName, limit = dto.UserLimit }));

        return Ok(new { data = result });
    }

    // ── Users scoped to a company ──────────────────────────────────────────

    [HttpGet("companies/{companyId:int}/users")]
    public async Task<IActionResult> GetCompanyUsers(int companyId)
    {
        var users = await companyService.GetUsersAsync(companyId);
        return Ok(new { data = users });
    }

    [HttpPost("companies/{companyId:int}/users")]
    public async Task<IActionResult> CreateCompanyUser(int companyId, [FromBody] CreateUserDto dto)
    {
        var (result, error) = await companyService.CreateUserAsync(companyId, dto);

        if (error == "company_not_found")
            return NotFound(new { error, message = "Company not found." });

        if (error == "email_taken")
            return Conflict(new { error, message = "Email already in use." });

        await auditService.LogAsync("USER_CREATED",
            userId: GetCurrentUserId(),
            companyId: companyId,
            resourceType: "User", resourceId: result!.Id.ToString(),
            metadata: System.Text.Json.JsonSerializer.Serialize(new { targetUser = result.Id, targetEmail = dto.Email, role = dto.Role }));

        return CreatedAtAction(nameof(GetCompanyUsers), new { companyId }, new { data = result });
    }

    [HttpPut("companies/{companyId:int}/users/{userId:int}")]
    public async Task<IActionResult> UpdateCompanyUser(int companyId, int userId, [FromBody] UpdateUserDto dto)
    {
        var (result, error) = await userService.UpdateAsync(userId, companyId, dto);
        if (error == "user_not_found")
            return NotFound(new { error, message = "User not found or does not belong to this company." });

        await auditService.LogAsync("USER_UPDATED",
            userId: GetCurrentUserId(),
            companyId: companyId,
            resourceType: "User", resourceId: userId.ToString(),
            metadata: System.Text.Json.JsonSerializer.Serialize(new { targetUser = userId, role = dto.Role }));

        return Ok(new { data = result });
    }

    [HttpPatch("companies/{companyId:int}/users/{userId:int}/status")]
    public async Task<IActionResult> UpdateCompanyUserStatus(int companyId, int userId, [FromBody] UpdateUserStatusDto dto)
    {
        var (result, error) = await userService.UpdateStatusAsync(userId, companyId, dto);
        if (error == "user_not_found")
            return NotFound(new { error, message = "User not found or does not belong to this company." });
        if (error == "user_limit_reached")
            return BadRequest(new { error, message = "Company user limit reached. Upgrade plan or deactivate another user." });

        await auditService.LogAsync(dto.IsActive ? "USER_ACTIVATED" : "USER_DEACTIVATED",
            userId: GetCurrentUserId(),
            companyId: companyId,
            resourceType: "User", resourceId: userId.ToString(),
            metadata: System.Text.Json.JsonSerializer.Serialize(new { targetUser = userId }));

        return Ok(new { data = result });
    }

    // ── Reports scoped to a company ────────────────────────────────────────

    [HttpGet("companies/{companyId:int}/modules")]
    public async Task<IActionResult> GetModules()
    {
        // SuperAdmin gets all modules, we can use the existing ModuleService method
        var modules = await moduleService.GetAccessibleAsync(GetCurrentUserId(), null, isSuperAdmin: true);
        return Ok(new { data = modules });
    }

    [HttpGet("companies/{companyId:int}/modules/{moduleId:int}/reports")]
    public async Task<IActionResult> GetCompanyReports(int companyId, int moduleId)
    {
        var reports = await reportService.GetReportsAsync(companyId, moduleId);
        return Ok(new { data = reports });
    }

    [HttpPost("companies/{companyId:int}/modules/{moduleId:int}/reports")]
    public async Task<IActionResult> CreateCompanyReport(int companyId, int moduleId, [FromBody] CreateReportDto dto)
    {
        var (result, error) = await reportService.CreateReportAsync(companyId, moduleId, dto);

        if (error == "company_not_found")
            return NotFound(new { error, message = "Company not found." });
        if (error == "module_not_found")
            return NotFound(new { error, message = "Module not found." });

        await auditService.LogAsync("REPORT_CREATED",
            userId: GetCurrentUserId(),
            companyId: companyId,
            resourceType: "Report", resourceId: result!.Id.ToString(),
            metadata: System.Text.Json.JsonSerializer.Serialize(new { name = dto.Name, moduleId }));

        return CreatedAtAction(nameof(GetCompanyReports), new { companyId, moduleId }, new { data = result });
    }

    [HttpPut("companies/{companyId:int}/reports/{reportId:int}")]
    public async Task<IActionResult> UpdateCompanyReport(int companyId, int reportId, [FromBody] UpdateReportDto dto)
    {
        var (result, error) = await reportService.UpdateReportAsync(companyId, reportId, dto);
        if (error == "report_not_found")
            return NotFound(new { error, message = "Report not found or does not belong to this company." });

        await auditService.LogAsync("REPORT_UPDATED",
            userId: GetCurrentUserId(),
            companyId: companyId,
            resourceType: "Report", resourceId: reportId.ToString(),
            metadata: System.Text.Json.JsonSerializer.Serialize(new { name = dto.Name }));

        return Ok(new { data = result });
    }

    [HttpPatch("companies/{companyId:int}/reports/{reportId:int}/status")]
    public async Task<IActionResult> UpdateCompanyReportStatus(int companyId, int reportId, [FromBody] UpdateReportStatusDto dto)
    {
        var (result, error) = await reportService.UpdateStatusAsync(companyId, reportId, dto.IsActive);
        if (error == "report_not_found")
            return NotFound(new { error, message = "Report not found or does not belong to this company." });

        await auditService.LogAsync(dto.IsActive ? "REPORT_ACTIVATED" : "REPORT_DEACTIVATED",
            userId: GetCurrentUserId(),
            companyId: companyId,
            resourceType: "Report", resourceId: reportId.ToString(),
            metadata: System.Text.Json.JsonSerializer.Serialize(new { reportId }));

        return Ok(new { data = result });
    }

    // ── MART Admin Refresh (Sprint 10) ────────────────────────────────────────

    // POST /api/admin/mart/refresh/{companyId}
    [HttpPost("mart/refresh/{companyId}")]
    public async Task<IActionResult> RefreshMart(
        string companyId,
        [FromServices] IMartAdminRefreshService refreshSvc,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return this.BadRequestError("invalid_company", "companyId is required.");

        if (refreshSvc is null)
            return this.BadRequestError("staging_unavailable", "Staging connection is not configured on this server.");

        var rows = await refreshSvc.RefreshAllMartAsync(companyId, ct);

        var results = rows.Select(r => new { module = r.Module, @object = r.Object, rowsAffected = r.RowsAffected }).ToList();

        return this.OkData(new { companyId, refreshedAt = DateTime.UtcNow, results });
    }
}
