namespace DataBision.Application.DTOs.Tenant;

public record TenantConfigDto(
    string CompanyDisplayName,
    string? LogoUrl,
    string? FaviconUrl,
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string BackgroundColor,
    string SidebarColor);
