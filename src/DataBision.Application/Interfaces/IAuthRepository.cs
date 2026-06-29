using DataBision.Domain.Entities;

namespace DataBision.Application.Interfaces;

public interface IAuthRepository
{
    Task<User?> FindUserByEmailAsync(string email);
    Task<User?> FindUserByIdAsync(int id);
    Task<Company?> FindCompanyBySlugAsync(string slug);
    Task<Company?> FindCompanyByIdAsync(int id);
    Task<bool> UserBelongsToCompanyAsync(int userId, int companyId);
    Task<int[]> GetUserModuleIdsAsync(int userId, int companyId);
    Task SaveRefreshTokenAsync(RefreshToken token);
    Task<RefreshToken?> FindRefreshTokenAsync(string hash);
    Task RevokeRefreshTokenAsync(int tokenId);
    Task UpdateLastLoginAsync(int userId);
    Task UpdateUserPasswordHashAsync(int userId, string passwordHash);
}
