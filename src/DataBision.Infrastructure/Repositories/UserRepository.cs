using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;
using DataBision.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DataBision.Infrastructure.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<bool> EmailExistsAsync(string email)
        => db.Users.AnyAsync(u => u.Email == email);

    public async Task<User?> GetByIdForCompanyAsync(int userId, int companyId)
    {
        var belongs = await db.UserCompanies.AnyAsync(uc => uc.UserId == userId && uc.CompanyId == companyId);
        if (!belongs) return null;
        return await db.Users.FindAsync(userId);
    }

    public async Task<IEnumerable<User>> GetForCompanyAsync(int companyId)
        => await db.UserCompanies
            .Where(uc => uc.CompanyId == companyId)
            .Select(uc => uc.User)
            .ToListAsync();

    public async Task<IEnumerable<(User User, Company Company)>> GetForCompanyWithCompanyAsync(int companyId)
    {
        var rows = await db.UserCompanies
            .Where(uc => uc.CompanyId == companyId)
            .Include(uc => uc.User)
            .Include(uc => uc.Company)
            .ToListAsync();
        return rows.Select(uc => (uc.User, uc.Company));
    }

    public async Task<User> AddAsync(User user, int companyId)
    {
        // Single transaction so a failure linking UserCompany rolls back the User row,
        // preventing orphan users without company association.
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
            db.UserCompanies.Add(new UserCompany { UserId = user.Id, CompanyId = companyId });
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return user;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public Task<int> GetActiveUserCountAsync(int companyId)
        => db.UserCompanies
            .Where(uc => uc.CompanyId == companyId && uc.User.IsActive)
            .CountAsync();

    public async Task<Dictionary<int, int>> GetActiveUserCountsAsync()
    {
        var counts = await db.UserCompanies
            .Where(uc => uc.User.IsActive)
            .GroupBy(uc => uc.CompanyId)
            .Select(g => new { CompanyId = g.Key, Count = g.Count() })
            .ToListAsync();
        return counts.ToDictionary(x => x.CompanyId, x => x.Count);
    }

    public Task SaveAsync()
        => db.SaveChangesAsync();
}
