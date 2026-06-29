namespace DataBision.Application.Interfaces;

public interface IMartAdminRefreshService
{
    Task<IReadOnlyList<(string Module, string Object, int RowsAffected)>> RefreshAllMartAsync(
        string companyId, CancellationToken ct = default);
}
