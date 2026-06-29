using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DataBision.Api.Middleware;
using DataBision.Application.DTOs.Auth;
using DataBision.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, IAuditService auditService) : ControllerBase
{
    private const string RefreshCookie = "databision_refresh";

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var slug = HttpContext.GetTenantSlug() ?? "admin";
        var result = await authService.LoginAsync(request, slug);

        if (result is null)
        {
            await auditService.LogAsync("LOGIN_FAILED",
                metadata: System.Text.Json.JsonSerializer.Serialize(new { email = request.Email, tenant = slug }));
            return Unauthorized(new { error = "invalid_credentials", message = "Invalid email or password." });
        }

        await auditService.LogAsync("LOGIN_SUCCESS", userId: result.User.Id, companyId: result.User.CompanyId,
            metadata: System.Text.Json.JsonSerializer.Serialize(new { email = request.Email, tenant = slug }));

        SetRefreshCookie(result.RefreshToken);

        return Ok(new { data = new { result.AccessToken, result.ExpiresIn, result.User } });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var token = Request.Cookies[RefreshCookie];
        if (string.IsNullOrEmpty(token))
            return Unauthorized(new { error = "missing_refresh_token" });

        var slug = HttpContext.GetTenantSlug() ?? "admin";
        var result = await authService.RefreshAsync(token, slug);

        if (result is null)
            return Unauthorized(new { error = "invalid_refresh_token", message = "Token expired or revoked." });

        SetRefreshCookie(result.RefreshToken);

        return Ok(new { data = new { result.AccessToken, result.ExpiresIn, result.User } });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdStr = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var (success, error) = await authService.ChangePasswordAsync(userId, dto);
        if (!success)
            return BadRequest(new
            {
                error,
                message = error == "wrong_current_password"
                    ? "La contraseña actual no es correcta."
                    : "No se pudo actualizar la contraseña.",
            });

        await auditService.LogAsync("CHANGE_PASSWORD", userId: userId);
        return Ok(new { data = "password_changed" });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies[RefreshCookie];
        if (!string.IsNullOrEmpty(token))
            await authService.RevokeRefreshTokenAsync(token);

        Response.Cookies.Delete(RefreshCookie);
        return Ok(new { data = "logged_out" });
    }

    private void SetRefreshCookie(string token)
    {
        var isHttps = Request.IsHttps;
        Response.Cookies.Append(RefreshCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure   = isHttps,                                      // false on localhost HTTP
            SameSite = isHttps ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires  = DateTimeOffset.UtcNow.AddDays(7)
        });
    }
}
