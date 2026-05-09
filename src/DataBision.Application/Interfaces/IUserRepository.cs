using DataBision.Domain.Entities;

namespace DataBision.Application.Interfaces;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email);
    Task<User?> GetByIdForCompanyAsync(int userId, int companyId);
    Task<IEnumerable<User>> GetForCompanyAsync(int companyId);
    Task<IEnumerable<(User User, Company Company)>> GetForCompanyWithCompanyAsync(int companyId);
    Task<User> AddAsync(User user, int companyId);
    Task<int> GetActiveUserCountAsync(int companyId);
    Task<Dictionary<int, int>> GetActiveUserCountsAsync();
    Task SaveAsync();
}
