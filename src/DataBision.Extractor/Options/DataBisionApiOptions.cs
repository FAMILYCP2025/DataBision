namespace DataBision.Extractor.Options;

public sealed class DataBisionApiOptions
{
    public const string Section = "DataBisionApi";

    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new InvalidOperationException("DataBisionApi:BaseUrl is required.");
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("DataBisionApi:ApiKey is required.");
    }
}
