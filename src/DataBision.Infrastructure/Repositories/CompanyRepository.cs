using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;
using DataBision.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DataBision.Infrastructure.Repositories;

public class CompanyRepository(AppDbContext db) : ICompanyRepository
{
    public async Task<IEnumerable<Company>> GetAllAsync()
        => await db.Companies.OrderBy(c => c.Name).ToListAsync();

    public Task<Company?> GetByIdAsync(int id)
        => db.Companies.FindAsync(id).AsTask();

    public Task<bool> SlugExistsAsync(string slug)
        => db.Companies.AnyAsync(c => c.Slug == slug);

    public async Task<Company> AddAsync(Company company)
    {
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return company;
    }

    public Task SaveAsync()
        => db.SaveChangesAsync();
}
