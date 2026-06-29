using DataBision.Application.DTOs.Auth;

namespace DataBision.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, string companySlug);
    Task<LoginResponseDto?> RefreshAsync(string refreshToken, string companySlug);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, ChangePasswordDto dto);
}
