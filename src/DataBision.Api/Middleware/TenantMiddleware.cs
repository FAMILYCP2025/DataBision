using DataBision.Application.Interfaces;
using DataBision.Domain.Enums;

namespace DataBision.Api.Middleware;

public class TenantMiddleware(RequestDelegate next, IConfiguration config)
{
    private const string TenantKey = "CompanySlug";
    private const string CompanyIdKey = "TenantCompanyId";

    public async Task InvokeAsync(HttpContext ctx, ITenantService tenantService)
    {
        // Ingest API uses ApiKey auth — tenant is resolved from the key, not the Host header
        if (ctx.Request.Path.StartsWithSegments("/api/ingest"))
        {
            await next(ctx);
            return;
        }

        var slug = ResolveSlug(ctx, config);

        if (!string.IsNullOrEmpty(slug))
        {
            var company = await tenantService.GetCompanyBySlugAsync(slug);
            if (company is not null && company.Status == CompanyStatus.Active)
            {
                ctx.Items[TenantKey] = slug;
                ctx.Items[CompanyIdKey] = company.Id;
            }
        }

        await next(ctx);
    }

    public static string? ResolveSlug(HttpContext ctx, IConfiguration config)
    {
        var host = ctx.Request.Host.Host;
        var baseDomain = config["App:BaseDomain"] ?? "databision.com";
        var adminSubdomain = config["App:AdminSubdomain"] ?? "admin";

        if (host == "localhost" || host == "127.0.0.1")
        {
            // Dev: ?tenant=slug simulates subdomain
            return ctx.Request.Query["tenant"].FirstOrDefault();
        }

        if (!host.EndsWith($".{baseDomain}")) return null;

        var subdomain = host[..^(baseDomain.Length + 1)];
        return subdomain == adminSubdomain ? null : subdomain;
    }
}

public static class TenantHttpContextExtensions
{
    public static string? GetTenantSlug(this HttpContext ctx)
        => ctx.Items["CompanySlug"] as string;

    public static int? GetTenantCompanyId(this HttpContext ctx)
        => ctx.Items["TenantCompanyId"] as int?;
}
