using DataBision.Application.DTOs.Admin;
using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;
using DataBision.Domain.Enums;

namespace DataBision.Application.Services;

public class CompanyService(ICompanyRepository companies, IUserRepository users) : ICompanyService
{
    public async Task<IEnumerable<CompanyDto>> GetAllAsync()
    {
        var list = await companies.GetAllAsync();
        var userCounts = await users.GetActiveUserCountsAsync();
        return list.Select(c => new CompanyDto(c.Id, c.Name, c.Slug, c.Status.ToString(), c.PlanName, c.UserLimit, userCounts.GetValueOrDefault(c.Id, 0), c.CreatedAt));
    }

    public async Task<(CompanyDto? result, string? error)> CreateAsync(CreateCompanyDto dto)
    {
        if (await companies.SlugExistsAsync(dto.Slug))
            return (null, "slug_taken");

        var company = new Company
        {
            Name = dto.Name,
            Slug = dto.Slug,
            PlanName = dto.PlanName,
            UserLimit = dto.UserLimit,
            Branding = new CompanyBranding
            {
                PrimaryColor    = "#2563EB",
                SecondaryColor  = "#64748B",
                AccentColor     = "#2563EB",
                BackgroundColor = "#F8FAFC",
                SidebarColor    = "#0F172A",
                CompanyDisplayName = dto.Name
            }
        };
        var created = await companies.AddAsync(company);

        return (new CompanyDto(created.Id, created.Name, created.Slug, created.Status.ToString(), created.PlanName, created.UserLimit, 0, created.CreatedAt), null);
    }

    public async Task<(CompanyDto? result, string? error)> UpdateAsync(int id, UpdateCompanyDto dto)
    {
        var company = await companies.GetByIdAsync(id);
        if (company is null) return (null, "company_not_found");

        company.Name = dto.Name;
        company.Status = Enum.Parse<CompanyStatus>(dto.Status, ignoreCase: true);
        company.PlanName = dto.PlanName;
        company.UserLimit = dto.UserLimit;
        company.UpdatedAt = DateTime.UtcNow;
        await companies.SaveAsync();

        var activeUsers = await users.GetActiveUserCountAsync(company.Id);

        return (new CompanyDto(company.Id, company.Name, company.Slug, company.Status.ToString(), company.PlanName, company.UserLimit, activeUsers, company.CreatedAt), null);
    }

    public async Task<IEnumerable<UserWithCompanyDto>> GetUsersAsync(int companyId)
    {
        var rows = await users.GetForCompanyWithCompanyAsync(companyId);
        return rows.Select(r => ToUserWithCompanyDto(r.User, r.Company));
    }

    public async Task<(UserWithCompanyDto? result, string? error)> CreateUserAsync(int companyId, CreateUserDto dto)
    {
        var company = await companies.GetByIdAsync(companyId);
        if (company is null) return (null, "company_not_found");

        if (await users.EmailExistsAsync(dto.Email))
            return (null, "email_taken");

        var activeUsers = await users.GetActiveUserCountAsync(companyId);
        if (activeUsers >= company.UserLimit)
            return (null, "user_limit_reached");

        var user = new User
        {
            Email        = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName    = dto.FirstName,
            LastName     = dto.LastName,
            Role         = Enum.Parse<UserRole>(dto.Role, ignoreCase: true),
            IsActive     = true
        };

        var created = await users.AddAsync(user, companyId);
        return (ToUserWithCompanyDto(created, company), null);
    }

    private static UserWithCompanyDto ToUserWithCompanyDto(User u, Company c) =>
        new(u.Id, u.Email, u.FirstName, u.LastName, u.Role.ToString(),
            u.IsActive, u.CreatedAt, u.LastLoginAt,
            c.Id, c.Name, c.Slug);
}
