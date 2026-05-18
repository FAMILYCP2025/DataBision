namespace DataBision.Application.Options;

public class PowerBISettingsOptions
{
    public const string SectionName = "PowerBI";

    public bool Enabled { get; set; } = false;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;

    // "SingleWorkspace" — all reports in one workspace (current model)
    // "PerCompany" — one workspace per tenant (future enterprise mode)
    public string WorkspaceMode { get; set; } = "SingleWorkspace";
}
