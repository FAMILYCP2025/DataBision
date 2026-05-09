using DataBision.Application.DTOs.Admin;
using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;

namespace DataBision.Application.Services;

public class ReportService(IReportRepository reportRepository, ICompanyRepository companyRepository, IModuleRepository moduleRepository) : IReportService
{
    public async Task<IEnumerable<ReportAdminDto>> GetReportsAsync(int companyId, int moduleId)
    {
        var reports = await reportRepository.GetReportsAsync(companyId, moduleId);
        return reports.Select(r => new ReportAdminDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            WorkspaceId = r.WorkspaceId,
            ReportId = r.ReportId,
            DatasetId = r.DatasetId,
            EmbedUrl = r.EmbedUrl,
            IsActive = r.IsActive
        });
    }

    public async Task<(ReportAdminDto? report, string? error)> CreateReportAsync(int companyId, int moduleId, CreateReportDto dto)
    {
        var moduleExists = await moduleRepository.GetByIdAsync(moduleId);
        if (moduleExists == null) return (null, "module_not_found");

        var companyExists = await companyRepository.GetByIdAsync(companyId);
        if (companyExists == null) return (null, "company_not_found");

        var report = new Report
        {
            CompanyId = companyId,
            ModuleId = moduleId,
            Name = dto.Name,
            Description = dto.Description,
            WorkspaceId = dto.WorkspaceId,
            ReportId = dto.ReportId,
            DatasetId = dto.DatasetId,
            EmbedUrl = dto.EmbedUrl,
            IsActive = dto.IsActive
        };

        await reportRepository.AddAsync(report);

        return (new ReportAdminDto
        {
            Id = report.Id,
            Name = report.Name,
            Description = report.Description,
            WorkspaceId = report.WorkspaceId,
            ReportId = report.ReportId,
            DatasetId = report.DatasetId,
            EmbedUrl = report.EmbedUrl,
            IsActive = report.IsActive
        }, null);
    }

    public async Task<(ReportAdminDto? report, string? error)> UpdateReportAsync(int companyId, int reportId, UpdateReportDto dto)
    {
        var report = await reportRepository.GetReportAsync(reportId, companyId);
        if (report == null) return (null, "report_not_found");

        report.Name = dto.Name;
        report.Description = dto.Description;
        report.WorkspaceId = dto.WorkspaceId;
        report.ReportId = dto.ReportId;
        report.DatasetId = dto.DatasetId;
        report.EmbedUrl = dto.EmbedUrl;
        report.IsActive = dto.IsActive;

        await reportRepository.UpdateAsync(report);

        return (new ReportAdminDto
        {
            Id = report.Id,
            Name = report.Name,
            Description = report.Description,
            WorkspaceId = report.WorkspaceId,
            ReportId = report.ReportId,
            DatasetId = report.DatasetId,
            EmbedUrl = report.EmbedUrl,
            IsActive = report.IsActive
        }, null);
    }

    public async Task<(ReportAdminDto? report, string? error)> UpdateStatusAsync(int companyId, int reportId, bool isActive)
    {
        var report = await reportRepository.GetReportAsync(reportId, companyId);
        if (report == null) return (null, "report_not_found");

        report.IsActive = isActive;
        await reportRepository.UpdateAsync(report);

        return (new ReportAdminDto
        {
            Id = report.Id,
            Name = report.Name,
            Description = report.Description,
            WorkspaceId = report.WorkspaceId,
            ReportId = report.ReportId,
            DatasetId = report.DatasetId,
            EmbedUrl = report.EmbedUrl,
            IsActive = report.IsActive
        }, null);
    }
}
