using System.Security.Claims;
using DataBision.Api.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DataBision.Api.Tests.Security;

public sealed class CompanyContextResolverTests
{
    // ── Authenticated path ────────────────────────────────────────────────────

    [Fact]
    public void Resolve_AuthenticatedWithSlug_ReturnsSlug()
    {
        var (ctx, config) = Build(isAuthenticated: true,
            claims: [new Claim("company_slug", "acme-corp")],
            jwtConfigured: true);

        var (companyId, err) = CompanyContextResolver.TryResolve(ctx, config);

        err.Should().BeNull();
        companyId.Should().Be("acme-corp");
    }

    [Fact]
    public void Resolve_AuthenticatedWithoutSlug_Returns403()
    {
        var (ctx, config) = Build(isAuthenticated: true,
            claims: [],
            jwtConfigured: true);

        var (companyId, err) = CompanyContextResolver.TryResolve(ctx, config);

        companyId.Should().BeNull();
        err.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
    }

    // ── Dev fallback (JWT not configured) ─────────────────────────────────────

    [Fact]
    public void Resolve_NoJwt_QueryParamPresent_ReturnsQueryParam()
    {
        var (ctx, config) = Build(isAuthenticated: false,
            jwtConfigured: false,
            queryCompanyId: "company-dev-001");

        var (companyId, err) = CompanyContextResolver.TryResolve(ctx, config);

        err.Should().BeNull();
        companyId.Should().Be("company-dev-001");
    }

    [Fact]
    public void Resolve_NoJwt_NoQueryParam_Returns400()
    {
        var (ctx, config) = Build(isAuthenticated: false,
            jwtConfigured: false,
            queryCompanyId: null);

        var (companyId, err) = CompanyContextResolver.TryResolve(ctx, config);

        companyId.Should().BeNull();
        err.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Production unauthenticated ────────────────────────────────────────────

    [Fact]
    public void Resolve_JwtConfigured_NotAuthenticated_Returns401()
    {
        // Query param is present but must be ignored when JWT is configured
        var (ctx, config) = Build(isAuthenticated: false,
            jwtConfigured: true,
            queryCompanyId: "company-dev-001");

        var (companyId, err) = CompanyContextResolver.TryResolve(ctx, config);

        companyId.Should().BeNull();
        err.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (HttpContext ctx, IConfiguration config) Build(
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
