using DataBision.Domain.Entities;

namespace DataBision.Application.Interfaces;

public interface IBrandingService
{
    Task<CompanyBranding> UpdateAsync(int companyId, CompanyBranding branding);
    Task<string> UploadLogoAsync(int companyId, Stream fileStream, string fileName, string contentType);
}
