using DataBision.Application.DTOs.Reports;
using DataBision.Application.Interfaces;

namespace DataBision.Infrastructure.PowerBI;

// Placeholder — implementar con Microsoft.PowerBI.Api + Microsoft.Identity.Client
// cuando el Service Principal esté configurado en Azure AD.
public class PowerBIService : IEmbedTokenService
{
    public Task<EmbedTokenResponseDto> GenerateAsync(int reportId, string companySlug)
    {
        throw new NotImplementedException(
            "Power BI Service Principal not yet configured. " +
            "Set PowerBI__TenantId, ClientId, ClientSecret, WorkspaceId in app config.");
    }
}
