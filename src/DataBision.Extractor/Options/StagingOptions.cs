namespace DataBision.Extractor.Options;

public sealed class StagingOptions
{
    public const string Section = "Staging";

    /// <summary>
    /// PostgreSQL connection string for the staging database (Supabase).
    /// Required only for --transform. Set in appsettings.Development.json or env var.
    /// Never log this value.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException(
                "Staging:ConnectionString is required for --transform. Set it in appsettings.Development.json.");
    }
}
