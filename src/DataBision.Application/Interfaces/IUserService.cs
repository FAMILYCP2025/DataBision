using DataBision.Application.DTOs.Admin;

namespace DataBision.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetForCompanyAsync(int companyId);
    Task<(UserDto? result, string? error)> CreateForCompanyAsync(int companyId, CreateUserDto dto);
    Task<(UserDto? result, string? error)> UpdateAsync(int userId, int companyId, UpdateUserDto dto);
    Task<(UserDto? result, string? error)> UpdateStatusAsync(int userId, int companyId, UpdateUserStatusDto dto);
}
