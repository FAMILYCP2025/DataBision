namespace DataBision.Extractor.Transformations;

public interface ITransformationRunner
{
    Task<IReadOnlyList<(string Object, int RowsAffected)>> RefreshStgAsync(
        string companyId, CancellationToken ct = default);

    Task<IReadOnlyList<(string Object, int RowsAffected)>> RefreshMartAsync(
        string companyId, CancellationToken ct = default);

    Task<(IReadOnlyList<(string Object, int RowsAffected)> Stg,
          IReadOnlyList<(string Object, int RowsAffected)> Mart)> RefreshAllAsync(
        string companyId, CancellationToken ct = default);

    /// <summary>
    /// Calls mart.refresh_all_processes(@company_id) to populate process-dashboard MART tables.
    /// Returns empty list if the function does not exist yet (migration not applied).
    /// </summary>
    Task<IReadOnlyList<(string Object, int RowsAffected)>> RefreshProcessMartAsync(
        string companyId, CancellationToken ct = default);
}
