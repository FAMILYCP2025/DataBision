using DataBision.Domain.Entities;

namespace DataBision.Application.Interfaces;

public interface IReportRepository
{
    Task<IEnumerable<Report>> GetReportsAsync(int companyId, int moduleId);
    Task<Report?> GetReportAsync(int reportId, int companyId);
    Task<Report> AddAsync(Report report);
    Task UpdateAsync(Report report);
}
