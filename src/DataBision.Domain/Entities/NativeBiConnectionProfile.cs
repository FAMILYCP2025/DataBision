namespace DataBision.Domain.Entities;

public sealed class NativeBiConnectionProfile
{
    public int    Id              { get; set; }
    public int    CompanyId       { get; set; }

    public string ProfileName     { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = "Production";

    // SAP Service Layer connection
    public string ServiceLayerBaseUrl { get; set; } = string.Empty;
    public string CompanyDb           { get; set; } = string.Empty;
    public string SapUserName         { get; set; } = string.Empty;

    // Credential reference — NEVER store password in plain text.
    // Format: "env:VARIABLE_NAME" | "azure-kv://vault/secret" | "local-dev-only:value"
    // Resolved at runtime by SecretRefResolver. Never returned in API responses.
    public string SecretRef { get; set; } = string.Empty;

    public bool IsActive        { get; set; } = true;
    public bool IgnoreSslErrors { get; set; } = false;
    public int  TimeoutSeconds  { get; set; } = 60;
    public int  FetchConcurrency { get; set; } = 3;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Company Company { get; set; } = null!;
}
