namespace DataBision.Application.DTOs.Admin;

public record UserDto(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

// Used by admin routes — includes company context for multi-tenant visibility
public record UserWithCompanyDto(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    int CompanyId,
    string CompanyName,
    string CompanySlug);

// CompanyId intentionally omitted — company is determined by route or tenant context, never by body
public record CreateUserDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role);

public record UpdateUserDto(
    string FirstName,
    string LastName,
    string Role);

public record UpdateUserStatusDto(bool IsActive);
