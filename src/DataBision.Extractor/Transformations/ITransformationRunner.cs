namespace DataBision.Extractor.Transformations;

public interface ITransformationRunner
{
    Task<IReadOnlyList<(string Object, int RowsAffected)>> RefreshAllAsync(
        string companyId, CancellationToken ct = default);
}
