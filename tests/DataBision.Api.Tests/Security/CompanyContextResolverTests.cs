using System.Security.Claims;
using DataBision.Api.Contracts;
using DataBision.Api.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DataBision.Api.Tests.Security;

public sealed class CompanyContextResolverTests
{
    // ── Sprint 6I: company claim priority ─────────────────────────────────────

    [Fact]
    public void Resolve_AuthenticatedWithCompanySlugClaim_ReturnsSlug()
    {
        var (ctx, config) = Build(isAuthenticated: true,
            claims: [new Claim("company_slug", "acme-corp")],
            jwtConfigured: true);

        var result = CompanyContextResolver.TryResolve(ctx, config);

        result.IsSuccess.Should().BeTrue();
        result.CompanyId.Should().Be("acme-corp");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Resolve_AuthenticatedWithCompanyIdClaim_ReturnsId()
    {
        // company_id claim is tried second (after company_slug)
        var (ctx, config) = Build(isAuthenticated: true,
            claims: [new Claim("company_id", "tenant-beta")],
            jwtConfigured: true);

        var result = CompanyContextResolver.TryResolve(ctx, config);

        result.IsSuccess.Should().BeTrue();
        result.CompanyId.Should().Be("tenant-beta");
    }

    [Fact]
    public void Resolve_AuthenticatedWithCompanyIdAlternateClaim_ReturnsId()
    {
        // companyId claim is tried third
        var (ctx, config) = Build(isAuthenticated: true,
            claims: [new Claim("companyId", "tenant-gamma")],
            jwtConfigured: true);

        var result = CompanyContextResolver.TryResolve(ctx, config);

        result.IsSuccess.Should().BeTrue();
        result.CompanyId.Should().Be("tenant-gamma");
    }

    [Fact]
    public void Resolve_AuthenticatedWithSlugClaim_PrioritizedOverCompanyId()
    {
        // company_slug takes priority over company_id
        var (ctx, config) = Build(isAuthenticated: true,
            claims:
            [
                new Claim("company_id", "42"),
                new Claim("company_slug", "acme-corp"),
            ],
            jwtConfigured: true);

        var result = CompanyContextResolver.TryResolve(ctx, config);

        result.IsSuccess.Should().BeTrue();
        result.CompanyId.Should().Be("acme-corp");
    }

    [Fact]
    public void Resolve_AuthenticatedWithoutCompanyClaim_Returns403()
    {
        var (ctx, config) = Build(isAuthenticated: true,
            claims: [],
            jwtConfigured: true);

        var result = CompanyContextResolver.TryResolve(ctx, config);

        result.IsSuccess.Should().BeFalse();
        result.CompanyId.Should().BeNull();
        result.Error.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public void Resolve_SuperAdminWithoutCompanyClaim_Returns403()
    {
        // SuperAdmin tokens without a company claim are rejected — no cross-tenant access yet.
        var (ctx, config) = Build(isAuthenticated: true,
            claims: [new Claim("role", "SuperAdmin")],
            jwtConfigured: true);

        var result = CompanyContextResolver.TryResolve(ctx, config);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
    }

    // ── Sprint 6I: role resolution ────────────────────────────────────────────

    [Fact]
    public void Resolve_ReturnsRoleFromClaim()
    {
        var (ctx, config) = Build(isAuthenticated: true,
            claims:
            [
                new Claim("company_slug", "acme-corp"),
                new Claim("role", "Admin"),
            ],
            jwtConfigured: true);

        var result = CompanyContextResolver.TryResolve(ctx, config);

        result.IsSuccess.Should().BeTrue();
        result.Role.Should().Be("Admin");
    }

    // ── Dev fallback (JWT not configured) ─────────────────────────────────────

    [Fact]
    public void Resolve_NoJwt_QueryParamPresent_ReturnsQueryParam()
    {
        var (ctx, config) = Build(isAuthenticated: false,
            jwtConfigured: false,
            queryCompanyId: "company-dev-001");

        var result = CompanyContextResolver.TryResolve(ctx, config);

        result.IsSuccess.Should().BeTrue();
        result.CompanyId.Should().Be("company-dev-001");
        result.IsDevelopmentFallback.Should().BeTrue();
    }

    [Fact]
    public void Resolve_NoJwt_NoQueryParam_Returns400()
    {
        var (ctx, config) = Build(isAuthenticated: false,
            jwtConfigured: false,
            queryCompanyId: null);

        var result = CompanyContextResolver.TryResolve(ctx, config);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Production unauthenticated ────────────────────────────────────────────

    [Fact]
    public void Resolve_JwtConfigured_NotAuthenticated_Returns401()
    {
        // Query param present but must be ignored when JWT is configured
        var (ctx, config) = Build(isAuthenticated: false,
            jwtConfigured: true,
            queryCompanyId: "company-dev-001");

        var result = CompanyContextResolver.TryResolve(ctx, config);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // ── Sprint 6K: ApiErrorResponse contract in error bodies ─────────────────

    [Fact]
    public void Resolve_Unauthorized_ErrorBodyIsApiErrorResponse()
    {
        var (ctx, config) = Build(isAuthenticated: false, jwtConfigured: true);
        ctx.TraceIdentifier = "trace-401-test";

        var result = CompanyContextResolver.TryResolve(ctx, config);

        var body = result.Error.Should().BeOfType<UnauthorizedObjectResult>()
                               .Which.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        body.Error.Should().Be("unauthorized");
        body.TraceId.Should().Be("trace-401-test");
    }

    [Fact]
    public void Resolve_Forbidden_ErrorBodyIsApiErrorResponse()
    {
        var (ctx, config) = Build(isAuthenticated: true, claims: [], jwtConfigured: true);
        ctx.TraceIdentifier = "trace-403-test";

        var result = CompanyContextResolver.TryResolve(ctx, config);

        var body = result.Error.Should().BeOfType<ObjectResult>()
                               .Which.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        body.Error.Should().Be("forbidden_no_company");
        body.TraceId.Should().Be("trace-403-test");
    }

    [Fact]
    public void Resolve_BadRequest_ErrorBodyIsApiErrorResponse()
    {
        var (ctx, config) = Build(isAuthenticated: false, jwtConfigured: false, queryCompanyId: null);
        ctx.TraceIdentifier = "trace-400-test";

        var result = CompanyContextResolver.TryResolve(ctx, config);

        var body = result.Error.Should().BeOfType<BadRequestObjectResult>()
                               .Which.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        body.Error.Should().Be("missing_company_id");
        body.TraceId.Should().Be("trace-400-test");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (DefaultHttpContext ctx, IConfiguration config) Build(
        bool isAuthenticated,
        IEnumerable<Claim>? claims = null,
        bool jwtConfigured = true,
        string? queryCompanyId = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:PublicKey"] = jwtConfigured
                    ? "-----BEGIN PUBLIC KEY-----\nfake\n-----END PUBLIC KEY-----"
                    : null,
            })
            .Build();

        var httpContext = new DefaultHttpContext();

        if (queryCompanyId is not null)
            httpContext.Request.QueryString = new QueryString($"?companyId={queryCompanyId}");

        if (isAuthenticated)
        {
            var identity = new ClaimsIdentity(claims ?? [], "Bearer");
            httpContext.User = new ClaimsPrincipal(identity);
        }
        else
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        }

        return (httpContext, config);
    }
}
