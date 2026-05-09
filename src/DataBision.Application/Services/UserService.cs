using DataBision.Application.DTOs.Admin;
using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;
using DataBision.Domain.Enums;

namespace DataBision.Application.Services;

public class UserService(IUserRepository repository, ICompanyRepository companies) : IUserService
{
    public async Task<IEnumerable<UserDto>> GetForCompanyAsync(int companyId)
    {
        var list = await repository.GetForCompanyAsync(companyId);
        return list.Select(u => new UserDto(u.Id, u.Email, u.FirstName, u.LastName, u.Role.ToString(), u.IsActive, u.CreatedAt, u.LastLoginAt));
    }

    public async Task<(UserDto? result, string? error)> CreateForCompanyAsync(int companyId, CreateUserDto dto)
    {
        var company = await companies.GetByIdAsync(companyId);
        if (company is null) return (null, "company_not_found");

        if (await repository.EmailExistsAsync(dto.Email))
            return (null, "email_taken");

        var activeUsers = await repository.GetActiveUserCountAsync(companyId);
        if (activeUsers >= company.UserLimit)
            return (null, "user_limit_reached");

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = Enum.Parse<UserRole>(dto.Role, ignoreCase: true),
            IsActive = true
        };

        var created = await repository.AddAsync(user, companyId);
        return (new UserDto(created.Id, created.Email, created.FirstName, created.LastName, created.Role.ToString(), created.IsActive, created.CreatedAt, null), null);
    }

    public async Task<(UserDto? result, string? error)> UpdateAsync(int userId, int companyId, UpdateUserDto dto)
    {
        var user = await repository.GetByIdForCompanyAsync(userId, companyId);
        if (user is null) return (null, "user_not_found");

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        
        if (Enum.TryParse<UserRole>(dto.Role, true, out var role))
        {
            user.Role = role;
        }

        await repository.SaveAsync();

        return (new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString(), user.IsActive, user.CreatedAt, user.LastLoginAt), null);
    }

    public async Task<(UserDto? result, string? error)> UpdateStatusAsync(int userId, int companyId, UpdateUserStatusDto dto)
    {
        var user = await repository.GetByIdForCompanyAsync(userId, companyId);
        if (user is null) return (null, "user_not_found");

        if (dto.IsActive && !user.IsActive)
        {
            var company = await companies.GetByIdAsync(companyId);
            if (company is null) return (null, "company_not_found");

            var activeUsers = await repository.GetActiveUserCountAsync(companyId);
            if (activeUsers >= company.UserLimit)
                return (null, "user_limit_reached");
        }

        user.IsActive = dto.IsActive;
        await repository.SaveAsync();

        return (new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString(), user.IsActive, user.CreatedAt, user.LastLoginAt), null);
    }
}
