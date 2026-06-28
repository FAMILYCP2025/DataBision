using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Application.Interfaces.Dashboard;

public interface IFinanceMartRepository
{
    Task<FinanceMartSummaryDto?> GetSummaryAsync(string companyId, CancellationToken ct = default);

    Task<IReadOnlyList<ArAgingRowDto>> GetArAgingAsync(
        string companyId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<ApAgingRowDto>> GetApAgingAsync(
        string companyId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<FinancePeriodKpiDto>> GetPeriodKpiAsync(
        string companyId, int months, CancellationToken ct = default);
}
