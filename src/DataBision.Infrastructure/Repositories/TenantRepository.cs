using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;
using DataBision.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DataBision.Infrastructure.Repositories;

public class TenantRepository(AppDbContext db) : ITenantRepository
{
    public Task<Company?> GetCompanyWithBrandingAsync(string slug)
        => db.Companies
            .Include(c => c.Branding)
            .FirstOrDefaultAsync(c => c.Slug == slug);
}
