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
}
