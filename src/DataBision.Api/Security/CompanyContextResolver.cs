using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Security;

/// <summary>
/// Resolves the MART company_id (= company_slug) for incoming Native BI requests.
///
/// Priority order:
///   1. JWT company_slug claim — production path when Jwt:PublicKey is configured.
///   2. ?companyId query param — dev fallback when Jwt:PublicKey is absent.
///
/// When Jwt:PublicKey is configured, unauthenticated requests receive 401.
/// When Jwt:PublicKey is absent (local dev), the query param is used.
/// </summary>
public static class CompanyContextResolver
{
    public static (string? companyId, IActionResult? error) TryResolve(
        HttpContext context, IConfiguration config)
    {
        var user = context.User;
        var jwtConfigured = !string.IsNullOrEmpty(config["Jwt:PublicKey"]);

        // Authenticated path: JWT validated → use company_slug claim
        if (user.Identity?.IsAuthenticated == true)
        {
            var slug = user.FindFirstValue("company_slug");
            if (string.IsNullOrWhiteSpace(slug))
                return (null, Forbidden("forbidden_no_company",
                    "Authenticated user has no company context."));
            return (slug, null);
        }

        // Dev fallback: JWT not configured → accept ?companyId query param
        if (!jwtConfigured)
        {
            var qp = context.Request.Query["companyId"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(qp))
                return (qp, null);
            return (null, BadRequestResult("missing_company_id",
                "companyId query parameter is required."));
        }

        // Production: JWT configured but request is not authenticated
        return (null, UnauthorizedResult("unauthorized", "Authentication required."));
    }

    private static IActionResult UnauthorizedResult(string code, string message) =>
        new UnauthorizedObjectResult(new { error = code, message });

    private static IActionResult Forbidden(string code, string message) =>
        new ObjectResult(new { error = code, message }) { StatusCode = 403 };

    private static IActionResult BadRequestResult(string code, string message) =>
        new BadRequestObjectResult(new { error = code, message });
}
