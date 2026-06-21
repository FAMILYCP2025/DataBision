namespace DataBision.Application.DTOs.Admin;

public sealed class NativeBiConnectionProfileDto
{
    public int    Id              { get; init; }
    public int    CompanyId       { get; init; }
    public string ProfileName     { get; init; } = string.Empty;
    public string EnvironmentName { get; init; } = string.Empty;
    public string ServiceLayerBaseUrl { get; init; } = string.Empty;
    public string CompanyDb       { get; init; } = string.Empty;
    public string SapUserName     { get; init; } = string.Empty;
    public string SecretRefHint   { get; init; } = string.Empty; // redacted: shows prefix only e.g. "env:***"
    public bool   IsActive        { get; init; }
    public bool   IgnoreSslErrors { get; init; }
    public int    TimeoutSeconds  { get; init; }
    public int    FetchConcurrency { get; init; }
    public DateTime CreatedAt     { get; init; }
    public DateTime UpdatedAt     { get; init; }
}

public sealed class CreateNativeBiConnectionProfileRequest
{
    public string ProfileName         { get; init; } = string.Empty;
    public string EnvironmentName     { get; init; } = "Production";
    public string ServiceLayerBaseUrl { get; init; } = string.Empty;
    public string CompanyDb           { get; init; } = string.Empty;
    public string SapUserName         { get; init; } = string.Empty;
    public string SecretRef           { get; init; } = string.Empty;
    public bool   IsActive            { get; init; } = true;
    public bool   IgnoreSslErrors     { get; init; } = false;
    public int    TimeoutSeconds      { get; init; } = 60;
    public int    FetchConcurrency    { get; init; } = 3;
}

public sealed class UpdateNativeBiConnectionProfileRequest
{
    public string ProfileName         { get; init; } = string.Empty;
    public string EnvironmentName     { get; init; } = "Production";
    public string ServiceLayerBaseUrl { get; init; } = string.Empty;
    public string CompanyDb           { get; init; } = string.Empty;
    public string SapUserName         { get; init; } = string.Empty;
    public string? SecretRef          { get; init; } // null = keep existing
    public bool   IsActive            { get; init; } = true;
    public bool   IgnoreSslErrors     { get; init; } = false;
    public int    TimeoutSeconds      { get; init; } = 60;
    public int    FetchConcurrency    { get; init; } = 3;
}

public sealed class TestNativeBiConnectionProfileResult
{
    public bool   Success      { get; init; }
    public long   LatencyMs    { get; init; }
    public DateTime CheckedAt  { get; init; }
    public string ServiceLayerBaseUrlMasked { get; init; } = string.Empty;
    public string CompanyDb    { get; init; } = string.Empty;
    public string Message      { get; init; } = string.Empty;
    public TestCapabilities Capabilities { get; init; } = new();
}

public sealed class TestCapabilities
{
    public bool LoginOk           { get; init; }
    public bool ChartOfAccountsOk { get; init; }
    public bool JournalEntriesOk  { get; init; }
}

/// <summary>
/// Returned by the internal resolve endpoint to the extractor.
/// Contains the resolved SAP password — only served over HTTPS to API-key-authenticated callers.
/// </summary>
public sealed class ResolveNativeBiConnectionProfileResponse
{
    public int    ProfileId           { get; init; }
    public string ProfileName         { get; init; } = string.Empty;
    public string ServiceLayerBaseUrl { get; init; } = string.Empty;
    public string CompanyDb           { get; init; } = string.Empty;
    public string SapUserName         { get; init; } = string.Empty;
    public string SapPassword         { get; init; } = string.Empty;
    public bool   IgnoreSslErrors     { get; init; }
    public int    TimeoutSeconds      { get; init; }
    public int    FetchConcurrency    { get; init; }
}
