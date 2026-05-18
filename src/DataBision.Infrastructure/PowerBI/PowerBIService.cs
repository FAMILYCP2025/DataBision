using DataBision.Application.DTOs.Reports;
using DataBision.Application.Interfaces;
using DataBision.Application.Options;
using Microsoft.Extensions.Options;

namespace DataBision.Infrastructure.PowerBI;

// Implements IPowerBIService as a validated stub.
// Phase 3 will replace GenerateEmbedTokenAsync with real Microsoft.PowerBI.Api calls
// once the Service Principal is provisioned in Azure AD.
public class PowerBIService(
    IReportRepository reportRepository,
    IOptions<PowerBISettingsOptions> settings) : IPowerBIService
{
    private readonly PowerBISettingsOptions _settings = settings.Value;

    public bool IsEnabled => _settings.Enabled;

    public async Task<bool> ValidateReportConfigurationAsync(int reportId, int companyId)
    {
        if (!_settings.Enabled) return false;

        var report = await reportRepository.GetReportAsync(reportId, companyId);
        return report is { IsActive: true }
            && !string.IsNullOrWhiteSpace(report.WorkspaceId)
            && !string.IsNullOrWhiteSpace(report.ReportId)
            && !string.IsNullOrWhiteSpace(report.DatasetId);
    }

    public async Task<ReportEmbedConfigDto> GetEmbedConfigurationAsync(int reportId, int companyId, string companySlug)
    {
        if (!_settings.Enabled)
            throw new InvalidOperationException(
                "Power BI is not enabled. Set PowerBI__Enabled=true in configuration once " +
                "TenantId, ClientId, ClientSecret and WorkspaceId are provisioned.");

        var report = await reportRepository.GetReportAsync(reportId, companyId);

        if (report is null)
            throw new KeyNotFoundException($"Report {reportId} not found for company {companyId}.");

        if (!report.IsActive)
            throw new InvalidOperationException($"Report {reportId} is inactive.");

        // Calls GenerateEmbedTokenAsync — throws NotImplementedException until
        // Service Principal is configured. This surfaces as 501 to the caller.
        var token = await GenerateEmbedTokenAsync(reportId, companySlug);

        return new ReportEmbedConfigDto(
            Id: report.Id,
            ModuleId: report.ModuleId,
            Name: report.Name,
            Description: report.Description,
            EmbedUrl: report.EmbedUrl ?? string.Empty,
            WorkspaceId: report.WorkspaceId,
            ReportId: report.ReportId,
            DatasetId: report.DatasetId,
            IsConfigured: true);
    }

    public Task<EmbedTokenResponseDto> GenerateEmbedTokenAsync(int reportId, string companySlug)
    {
        // Phase 3: replace with Microsoft.PowerBI.Api + Microsoft.Identity.Client.
        // Requires: PowerBI__TenantId, ClientId, ClientSecret, WorkspaceId in config.
        // RLS identity: username = companySlug, roles = ["CompanyRole"].
        throw new NotImplementedException(
            "Power BI Service Principal not yet configured. " +
            "Implement GenerateEmbedTokenAsync in Phase 3 with Microsoft.PowerBI.Api.");
    }
}
