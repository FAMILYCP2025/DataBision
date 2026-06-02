namespace DataBision.Extractor.Options;

public sealed class SapServiceLayerOptions
{
    public const string Section = "SapServiceLayer";

    public string BaseUrl { get; init; } = string.Empty;
    public string CompanyDB { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public bool IgnoreSslCertificateErrors { get; init; } = false;
    public int TimeoutSeconds { get; init; } = 60;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new InvalidOperationException("SapServiceLayer:BaseUrl is required.");
        if (string.IsNullOrWhiteSpace(CompanyDB))
            throw new InvalidOperationException("SapServiceLayer:CompanyDB is required.");
        if (string.IsNullOrWhiteSpace(UserName))
            throw new InvalidOperationException("SapServiceLayer:UserName is required.");
        if (string.IsNullOrWhiteSpace(Password))
            throw new InvalidOperationException("SapServiceLayer:Password is required.");
    }
}
