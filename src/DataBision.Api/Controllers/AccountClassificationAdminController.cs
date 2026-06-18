using DataBision.Application.DTOs.Admin;
using DataBision.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/admin/companies/{companyId:int}/native-bi/account-classification-rules")]
[Authorize(Roles = "SuperAdmin")]
public sealed class AccountClassificationAdminController(
    IAccountClassificationService svc,
    ICompanyRepository companies) : ControllerBase
{
    private static readonly HashSet<string> AllowedLines = new(StringComparer.OrdinalIgnoreCase)
    {
        "revenue", "cogs", "opex", "other_income", "other_expense",
        "financial", "tax", "depreciation", "amortization",
        "current_assets", "non_current_assets",
        "current_liabilities", "non_current_liabilities",
        "equity", "unclassified"
    };

    // GET /api/admin/companies/{id}/native-bi/account-classification-rules
    [HttpGet]
    public async Task<IActionResult> GetRules(int companyId, CancellationToken ct)
    {
        var analyticsId = await ResolveAnalyticsIdAsync(companyId, ct);
        if (analyticsId is null)
            return NotFound(new { error = "company_not_found", message = "Company not found or has no Analytics Company ID." });

        var result = await svc.GetRulesAsync(analyticsId, ct);
        return Ok(new { data = result });
    }

    // POST /api/admin/companies/{id}/native-bi/account-classification-rules
    [HttpPost]
    public async Task<IActionResult> CreateRule(int companyId, [FromBody] UpsertAccountClassificationRuleDto dto, CancellationToken ct)
    {
        var validation = ValidateDto(dto);
        if (validation is not null) return validation;

        var analyticsId = await ResolveAnalyticsIdAsync(companyId, ct);
        if (analyticsId is null)
            return NotFound(new { error = "company_not_found", message = "Company not found or has no Analytics Company ID." });

        var result = await svc.CreateRuleAsync(analyticsId, dto, ct);
        return CreatedAtAction(nameof(GetRules), new { companyId }, new { data = result });
    }

    // PUT /api/admin/companies/{id}/native-bi/account-classification-rules/{ruleId}
    [HttpPut("{ruleId:int}")]
    public async Task<IActionResult> UpdateRule(int companyId, int ruleId, [FromBody] UpsertAccountClassificationRuleDto dto, CancellationToken ct)
    {
        var validation = ValidateDto(dto);
        if (validation is not null) return validation;

        var analyticsId = await ResolveAnalyticsIdAsync(companyId, ct);
        if (analyticsId is null)
            return NotFound(new { error = "company_not_found", message = "Company not found or has no Analytics Company ID." });

        var result = await svc.UpdateRuleAsync(analyticsId, ruleId, dto, ct);
        if (result is null)
            return NotFound(new { error = "rule_not_found", message = "Classification rule not found." });

        return Ok(new { data = result });
    }

    // DELETE /api/admin/companies/{id}/native-bi/account-classification-rules/{ruleId}
    [HttpDelete("{ruleId:int}")]
    public async Task<IActionResult> DeleteRule(int companyId, int ruleId, CancellationToken ct)
    {
        var analyticsId = await ResolveAnalyticsIdAsync(companyId, ct);
        if (analyticsId is null)
            return NotFound(new { error = "company_not_found", message = "Company not found or has no Analytics Company ID." });

        var deleted = await svc.DeleteRuleAsync(analyticsId, ruleId, ct);
        if (!deleted)
            return NotFound(new { error = "rule_not_found", message = "Classification rule not found." });

        return NoContent();
    }

    // POST /api/admin/companies/{id}/native-bi/account-classification-rules/import-template
    [HttpPost("import-template")]
    public async Task<IActionResult> GetImportTemplate(int companyId, CancellationToken ct)
    {
        var analyticsId = await ResolveAnalyticsIdAsync(companyId, ct);
        if (analyticsId is null)
            return NotFound(new { error = "company_not_found", message = "Company not found or has no Analytics Company ID." });

        var result = await svc.GetTemplateSuggestionsAsync(analyticsId, ct);
        return Ok(new { data = result, warning = "These are suggestions only. Review with the client's accountant before applying." });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string?> ResolveAnalyticsIdAsync(int companyId, CancellationToken ct)
    {
        var company = await companies.GetByIdAsync(companyId);
        return string.IsNullOrWhiteSpace(company?.AnalyticsCompanyId) ? null : company.AnalyticsCompanyId;
    }

    private IActionResult? ValidateDto(UpsertAccountClassificationRuleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.StatementLine) || !AllowedLines.Contains(dto.StatementLine))
            return BadRequest(new { error = "invalid_statement_line", message = $"statement_line must be one of: {string.Join(", ", AllowedLines.OrderBy(x => x))}." });

        if (string.IsNullOrWhiteSpace(dto.AccountCode) && string.IsNullOrWhiteSpace(dto.FormatCode))
            return BadRequest(new { error = "missing_account_identifier", message = "Either account_code or format_code must be provided." });

        return null;
    }
}
