using DataBision.Application.DTOs.Admin;

namespace DataBision.Application.Interfaces;

public interface ICompanyService
{
    Task<IEnumerable<CompanyDto>> GetAllAsync();
    Task<(CompanyDto? result, string? error)> CreateAsync(CreateCompanyDto dto);
    Task<(CompanyDto? result, string? error)> UpdateAsync(int id, UpdateCompanyDto dto);
    Task<IEnumerable<UserWithCompanyDto>> GetUsersAsync(int companyId);
    Task<(UserWithCompanyDto? result, string? error)> CreateUserAsync(int companyId, CreateUserDto dto);
}
