using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;
using DataBision.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DataBision.Infrastructure.Repositories;

public class AuthRepository(AppDbContext db) : IAuthRepository
{
    public Task<User?> FindUserByEmailAsync(string email)
        => db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> FindUserByIdAsync(int id)
        => db.Users.FindAsync(id).AsTask();

    public Task<Company?> FindCompanyBySlugAsync(string slug)
        => db.Companies.FirstOrDefaultAsync(c => c.Slug == slug);

    public Task<Company?> FindCompanyByIdAsync(int id)
        => db.Companies.FindAsync(id).AsTask();

    public Task<bool> UserBelongsToCompanyAsync(int userId, int companyId)
        => db.UserCompanies.AnyAsync(uc => uc.UserId == userId && uc.CompanyId == companyId);

    public async Task<int[]> GetUserModuleIdsAsync(int userId, int companyId)
        => await db.UserPermissions
            .Where(p => p.UserId == userId && p.CompanyId == companyId && p.CanView)
            .Select(p => p.ModuleId)
            .Distinct()
            .ToArrayAsync();

    public async Task SaveRefreshTokenAsync(RefreshToken token)
    {
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();
    }

    public Task<RefreshToken?> FindRefreshTokenAsync(string hash)
        => db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);

    public async Task RevokeRefreshTokenAsync(int tokenId)
    {
        var token = await db.RefreshTokens.FindAsync(tokenId);
        if (token is not null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is not null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdateUserPasswordHashAsync(int userId, string passwordHash)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is not null)
        {
            user.PasswordHash = passwordHash;
            await db.SaveChangesAsync();
        }
    }
}
