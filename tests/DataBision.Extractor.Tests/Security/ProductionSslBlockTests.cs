using DataBision.Extractor.DataBision;
using Xunit;

namespace DataBision.Extractor.Tests.Security;

public sealed class ProductionSslBlockTests : IDisposable
{
    private readonly string? _originalEnv;

    public ProductionSslBlockTests()
    {
        _originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _originalEnv);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("production")]
    [InlineData("PRODUCTION")]
    public void IsIgnoreSslBlockedInProduction_IgnoreSslTrue_ProductionEnv_ReturnsTrue(string envName)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", envName);
        Assert.True(ApiConnectionProfileResolver.IsIgnoreSslBlockedInProduction(true));
    }

    [Fact]
    public void IsIgnoreSslBlockedInProduction_IgnoreSslFalse_ProductionEnv_ReturnsFalse()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        Assert.False(ApiConnectionProfileResolver.IsIgnoreSslBlockedInProduction(false));
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("TST")]
    [InlineData("DEV")]
    [InlineData("Staging")]
    public void IsIgnoreSslBlockedInProduction_IgnoreSslTrue_NonProductionEnv_ReturnsFalse(string envName)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", envName);
        Assert.False(ApiConnectionProfileResolver.IsIgnoreSslBlockedInProduction(true));
    }

    [Fact]
    public void IsIgnoreSslBlockedInProduction_NoEnvVar_DefaultsToProductionBlock()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Assert.True(ApiConnectionProfileResolver.IsIgnoreSslBlockedInProduction(true));
    }
}
