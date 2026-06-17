using DataBision.Application.Interfaces;
using DataBision.Application.Services;
using DataBision.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace DataBision.Application.Tests.Services;

public sealed class AnalyticsCompanyResolverTests
{
    private readonly Mock<ICompanyRepository> _repo = new();
    private readonly Mock<IHostEnvironment>   _env  = new();

    private AnalyticsCompanyResolver BuildResolver(IConfiguration config)
        => new(_repo.Object, config, _env.Object);

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private void SetDevelopment(bool isDev)
        => _env.Setup(e => e.EnvironmentName)
               .Returns(isDev ? Environments.Development : Environments.Production);

    // ── DB lookup (primary path) ──────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_CompanyHasAnalyticsId_ReturnsDatabaseValue()
    {
        SetDevelopment(false);
        _repo.Setup(r => r.GetBySlugAsync("acme", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Company { Slug = "acme", AnalyticsCompanyId = "analytics-acme-001" });

        var resolver = BuildResolver(BuildConfig([]));
        var result   = await resolver.ResolveAsync("acme");

        result.Should().Be("analytics-acme-001");
    }

    [Fact]
    public async Task ResolveAsync_CompanyHasAnalyticsId_PrefersDatabaseOverAppsettings()
    {
        SetDevelopment(true);
        _repo.Setup(r => r.GetBySlugAsync("acme", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Company { Slug = "acme", AnalyticsCompanyId = "db-value" });

        var config   = BuildConfig(new() { ["NativeBi:CompanySlugMap:acme"] = "config-value" });
        var resolver = BuildResolver(config);
        var result   = await resolver.ResolveAsync("acme");

        result.Should().Be("db-value");
    }

    [Fact]
    public async Task ResolveAsync_CompanyHasEmptyAnalyticsId_DoesNotReturnEmptyString()
    {
        SetDevelopment(false);
        _repo.Setup(r => r.GetBySlugAsync("acme", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Company { Slug = "acme", AnalyticsCompanyId = "" });

        var resolver = BuildResolver(BuildConfig([]));
        var act = () => resolver.ResolveAsync("acme");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── Appsettings fallback (Development only) ───────────────────────────────

    [Fact]
    public async Task ResolveAsync_NoDB_Development_SlugMapEntry_ReturnsConfigValue()
    {
        SetDevelopment(true);
        _repo.Setup(r => r.GetBySlugAsync("ksdepor", It.IsAny<CancellationToken>()))
             .ReturnsAsync((Company?)null);

        var config   = BuildConfig(new() { ["NativeBi:CompanySlugMap:ksdepor"] = "ksdepor-analytics" });
        var resolver = BuildResolver(config);
        var result   = await resolver.ResolveAsync("ksdepor");

        result.Should().Be("ksdepor-analytics");
    }

    [Fact]
    public async Task ResolveAsync_NoDB_Development_NoSlugMap_DefaultIdUsed()
    {
        SetDevelopment(true);
        _repo.Setup(r => r.GetBySlugAsync("acme", It.IsAny<CancellationToken>()))
             .ReturnsAsync((Company?)null);

        var config   = BuildConfig(new() { ["NativeBi:DefaultAnalyticsCompanyId"] = "default-analytics-001" });
        var resolver = BuildResolver(config);
        var result   = await resolver.ResolveAsync("acme");

        result.Should().Be("default-analytics-001");
    }

    [Fact]
    public async Task ResolveAsync_NoDB_Development_NoConfig_Throws()
    {
        SetDevelopment(true);
        _repo.Setup(r => r.GetBySlugAsync("acme", It.IsAny<CancellationToken>()))
             .ReturnsAsync((Company?)null);

        var resolver = BuildResolver(BuildConfig([]));
        var act = () => resolver.ResolveAsync("acme");

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*analytics_company_id*");
    }

    // ── Production: no fallback ───────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_NoDB_Production_Throws_EvenWithAppsettings()
    {
        SetDevelopment(false);
        _repo.Setup(r => r.GetBySlugAsync("acme", It.IsAny<CancellationToken>()))
             .ReturnsAsync((Company?)null);

        var config   = BuildConfig(new() { ["NativeBi:CompanySlugMap:acme"] = "should-not-be-used" });
        var resolver = BuildResolver(config);
        var act = () => resolver.ResolveAsync("acme");

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*analytics_company_id*");
    }

    [Fact]
    public async Task ResolveAsync_NoDB_Production_NoConfig_Throws()
    {
        SetDevelopment(false);
        _repo.Setup(r => r.GetBySlugAsync("any-slug", It.IsAny<CancellationToken>()))
             .ReturnsAsync((Company?)null);

        var resolver = BuildResolver(BuildConfig([]));
        var act = () => resolver.ResolveAsync("any-slug");

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*analytics_company_id*");
    }

    // ── CancellationToken propagation ─────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_PassesCancellationTokenToRepository()
    {
        SetDevelopment(false);
        var cts = new CancellationTokenSource();
        _repo.Setup(r => r.GetBySlugAsync("acme", cts.Token))
             .ReturnsAsync(new Company { Slug = "acme", AnalyticsCompanyId = "analytics-acme-001" });

        var resolver = BuildResolver(BuildConfig([]));
        var result   = await resolver.ResolveAsync("acme", cts.Token);

        result.Should().Be("analytics-acme-001");
        _repo.Verify(r => r.GetBySlugAsync("acme", cts.Token), Times.Once);
    }
}
