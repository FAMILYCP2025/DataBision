using DataBision.Domain.Enums;

namespace DataBision.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    public ICollection<UserCompany> UserCompanies { get; set; } = [];
    public ICollection<UserPermission> Permissions { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
