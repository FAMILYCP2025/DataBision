using System.Security.Claims;
using DataBision.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Security;

/// <summary>
/// Resolves the MART company_id and role for Native BI requests.
///
/// Claim priority (company): company_slug → company_id → companyId
/// Claim priority (role):    role → user_role → ClaimTypes.Role URI
///
/// Auth rules:
///   - Jwt:PublicKey configured + authenticated: company claim required, else 403
///   - Jwt:PublicKey configured + unauthenticated: 401
///   - Jwt:PublicKey absent (dev): ?companyId query param accepted
///
/// SuperAdmin cross-company access: not yet enabled. SuperAdmin must also have a
/// company claim. TODO (Sprint future): add explicit scope param + authorization check
/// to allow SuperAdmin to query any company without a matching JWT claim.
/// </summary>
public static class CompanyContextResolver
{
    private static readonly string[] CompanyClaims =
        ["company_slug", "company_id", "companyId"];

    private static readonly string[] RoleClaims =
        ["role", "user_role", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];

    public static CompanyContextResult TryResolve(HttpContext context, IConfiguration config)
    {
        var user = context.User;
        var jwtConfigured = !string.IsNullOrEmpty(config["Jwt:PublicKey"]);

        if (user.Identity?.IsAuthenticated == true)
        {
            var companyId = ResolveCompanyId(user);
            var role = ResolveRole(user);

            if (string.IsNullOrWhiteSpace(companyId))
                return Fail(Forbidden("forbidden_no_company",
                    "Authenticated user has no company context.", context));

            return new CompanyContextResult { CompanyId = companyId, Role = role };
        }

        if (!jwtConfigured)
        {
            var qp = context.Request.Query["companyId"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(qp))
                return new CompanyContextResult { CompanyId = qp, IsDevelopmentFallback = true };

            return Fail(BadRequest("missing_company_id",
                "companyId query parameter is required.", context));
        }

        return Fail(Unauthorized("unauthorized", "Authentication required.", context));
    }

    // ── Claim resolvers ───────────────────────────────────────────────────────

    private static string? ResolveCompanyId(ClaimsPrincipal user) =>
        CompanyClaims
            .Select(c => user.FindFirstValue(c))
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string? ResolveRole(ClaimsPrincipal user) =>
        RoleClaims
            .Select(c => user.FindFirstValue(c))
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    // ── Error builders ────────────────────────────────────────────────────────

    private static CompanyContextResult Fail(IActionResult err) =>
        new() { Error = err };

    private static ApiErrorResponse Body(string code, string message, HttpContext ctx) =>
        new() { Error = code, Message = message, TraceId = ctx.TraceIdentifier };

    private static IActionResult Unauthorized(string code, string message, HttpContext ctx) =>
        new UnauthorizedObjectResult(Body(code, message, ctx));

    private static IActionResult Forbidden(string code, string message, HttpContext ctx) =>
        new ObjectResult(Body(code, message, ctx)) { StatusCode = 403 };

    private static IActionResult BadRequest(string code, string message, HttpContext ctx) =>
        new BadRequestObjectResult(Body(code, message, ctx));
}
