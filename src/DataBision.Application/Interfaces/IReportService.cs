using DataBision.Application.DTOs.Admin;

namespace DataBision.Application.Interfaces;

public interface IReportService
{
    Task<IEnumerable<ReportAdminDto>> GetReportsAsync(int companyId, int moduleId);
    Task<(ReportAdminDto? report, string? error)> CreateReportAsync(int companyId, int moduleId, CreateReportDto dto);
    Task<(ReportAdminDto? report, string? error)> UpdateReportAsync(int companyId, int reportId, UpdateReportDto dto);
    Task<(ReportAdminDto? report, string? error)> UpdateStatusAsync(int companyId, int reportId, bool isActive);
}
