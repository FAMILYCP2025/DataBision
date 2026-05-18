using DataBision.Application.DTOs.Reports;

namespace DataBision.Application.Interfaces;

public interface IPowerBIService
{
    bool IsEnabled { get; }

    /// <summary>Returns true when the report record has non-empty WorkspaceId/ReportId/DatasetId
    /// AND PowerBI:Enabled is true. Does not validate Azure connectivity.</summary>
    Task<bool> ValidateReportConfigurationAsync(int reportId, int companyId);

    /// <summary>Builds the embed configuration for the given report.
    /// Throws KeyNotFoundException if report not found for the tenant.
    /// Throws InvalidOperationException if PowerBI is disabled or report inactive.
    /// Throws NotImplementedException until Service Principal is configured.</summary>
    Task<ReportEmbedConfigDto> GetEmbedConfigurationAsync(int reportId, int companyId, string companySlug);

    /// <summary>Generates a Power BI embed token via Service Principal.
    /// STUB — throws NotImplementedException until Azure credentials are configured.</summary>
    Task<EmbedTokenResponseDto> GenerateEmbedTokenAsync(int reportId, string companySlug);
}
