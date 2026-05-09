using DataBision.Domain.Entities;

namespace DataBision.Application.Interfaces;

public interface ICompanyRepository
{
    Task<IEnumerable<Company>> GetAllAsync();
    Task<Company?> GetByIdAsync(int id);
    Task<bool> SlugExistsAsync(string slug);
    Task<Company> AddAsync(Company company);
    Task SaveAsync();
}
