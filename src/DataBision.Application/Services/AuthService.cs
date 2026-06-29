using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DataBision.Application.DTOs.Auth;
using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;
using DataBision.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DataBision.Application.Services;

public class AuthService(IAuthRepository repository, IConfiguration config) : IAuthService
{
    private const int AccessTokenMinutes = 15;
    private const int RefreshTokenDays = 7;

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, string companySlug)
    {
        var user = await repository.FindUserByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        int? companyId = null;
        if (user.Role != UserRole.SuperAdmin)
        {
            var company = await repository.FindCompanyBySlugAsync(companySlug);
            if (company is null || company.Status != Domain.Enums.CompanyStatus.Active)
                return null;

            var belongs = await repository.UserBelongsToCompanyAsync(user.Id, company.Id);
            if (!belongs) return null;

            companyId = company.Id;
        }

        var moduleIds = companyId.HasValue
            ? await repository.GetUserModuleIdsAsync(user.Id, companyId.Value)
            : [];

        var accessToken = GenerateAccessToken(user, companySlug, companyId, moduleIds);
        var rawRefresh = GenerateRawRefreshToken();
        var tokenHash = HashToken(rawRefresh);

        await repository.SaveRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            CompanyId = companyId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays)
        });

        await repository.UpdateLastLoginAsync(user.Id);

        return new LoginResponseDto(
            accessToken,
            rawRefresh,
            AccessTokenMinutes * 60,
            new UserInfoDto(user.Id, user.FullName, user.Email, user.Role.ToString(), companyId));
    }

    public async Task<LoginResponseDto?> RefreshAsync(string refreshToken, string companySlug)
    {
        var hash = HashToken(refreshToken);
        var stored = await repository.FindRefreshTokenAsync(hash);
        if (stored is null || !stored.IsActive) return null;

        // Rotate: revoke old token before issuing new one
        await repository.RevokeRefreshTokenAsync(stored.Id);

        var user = await repository.FindUserByIdAsync(stored.UserId);
        if (user is null || !user.IsActive) return null;

        // Verify the company context from the stored token is still active
        int? companyId = stored.CompanyId;
        if (companyId.HasValue)
        {
            var company = await repository.FindCompanyByIdAsync(companyId.Value);
            if (company is null || company.Status != Domain.Enums.CompanyStatus.Active)
                return null;
        }

        var moduleIds = companyId.HasValue
            ? await repository.GetUserModuleIdsAsync(user.Id, companyId.Value)
            : [];

        var newAccessToken = GenerateAccessToken(user, companySlug, companyId, moduleIds);
        var rawRefresh = GenerateRawRefreshToken();

        await repository.SaveRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            CompanyId = companyId,
            TokenHash = HashToken(rawRefresh),
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays)
        });

        return new LoginResponseDto(
            newAccessToken,
            rawRefresh,
            AccessTokenMinutes * 60,
            new UserInfoDto(user.Id, user.FullName, user.Email, user.Role.ToString(), companyId));
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);
        var stored = await repository.FindRefreshTokenAsync(hash);
        if (stored is not null)
            await repository.RevokeRefreshTokenAsync(stored.Id);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await repository.FindUserByIdAsync(userId);
        if (user is null || !user.IsActive)
            return (false, "user_not_found");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return (false, "wrong_current_password");

        var newHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await repository.UpdateUserPasswordHashAsync(userId, newHash);
        return (true, null);
    }

    private string GenerateAccessToken(User user, string companySlug, int? companyId, int[] moduleIds)
    {
        var privateKeyPem = config["Jwt:PrivateKey"]
            ?? throw new InvalidOperationException("Jwt:PrivateKey not configured");

        var rsa = RSA.Create();
        rsa.ImportFromPem(ResolvePemKey(privateKeyPem, "Jwt:PrivateKey"));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role", user.Role.ToString()),
            new("company_slug", companySlug),
        };

        if (companyId.HasValue)
            claims.Add(new Claim("company_id", companyId.Value.ToString()));

        foreach (var id in moduleIds)
            claims.Add(new Claim("module_ids", id.ToString()));

        var key = new RsaSecurityKey(rsa);
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"] ?? "databision-api",
            expires: DateTime.UtcNow.AddMinutes(AccessTokenMinutes),
            claims: claims,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Resolves a PEM key from: direct PEM content, escaped-newline PEM, or a file path.
    /// Never logs the resolved content.
    /// </summary>
    private static string ResolvePemKey(string pemOrPath, string configKey)
    {
        // Normalize escaped newlines (common when stored in JSON or env vars as \n literals)
        var candidate = pemOrPath.Replace("\\n", "\n");

        if (candidate.Contains("-----BEGIN", StringComparison.Ordinal))
            return candidate;

        if (File.Exists(candidate))
            return File.ReadAllText(candidate);

        throw new InvalidOperationException(
            $"Configuration key '{configKey}' must contain PEM content (starting with '-----BEGIN...') " +
            "or a valid file path to a PEM file. " +
            "Check that appsettings, user-secrets, or environment variable is set correctly.");
    }

    private static string GenerateRawRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
