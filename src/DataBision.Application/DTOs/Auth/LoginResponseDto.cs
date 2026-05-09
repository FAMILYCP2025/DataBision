namespace DataBision.Application.DTOs.Auth;

public record LoginResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserInfoDto User);

public record UserInfoDto(
    int Id,
    string Name,
    string Email,
    string Role,
    int? CompanyId);
