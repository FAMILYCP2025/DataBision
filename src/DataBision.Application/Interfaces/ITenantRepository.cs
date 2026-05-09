using DataBision.Domain.Entities;

namespace DataBision.Application.Interfaces;

public interface ITenantRepository
{
    Task<Company?> GetCompanyWithBrandingAsync(string slug);
}
