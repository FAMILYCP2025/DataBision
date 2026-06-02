namespace DataBision.Extractor.Options;

public sealed class ExtractorOptions
{
    public const string Section = "Extractor";

    public string TenantId { get; init; } = string.Empty;
    public string CompanyId { get; init; } = string.Empty;
    public string Mode { get; init; } = "INCREMENTAL";
    public int PageSize { get; init; } = 100;
    public int LookbackMinutes { get; init; } = 10;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(TenantId))
            throw new InvalidOperationException("Extractor:TenantId is required.");
        if (string.IsNullOrWhiteSpace(CompanyId))
            throw new InvalidOperationException("Extractor:CompanyId is required.");
    }
}
