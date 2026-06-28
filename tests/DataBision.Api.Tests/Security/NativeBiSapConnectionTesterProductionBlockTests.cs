using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;
using DataBision.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DataBision.Api.Tests.Security;

public sealed class NativeBiSapConnectionTesterProductionBlockTests : IDisposable
{
    private readonly string? _originalEnv;
    private readonly NativeBiSapConnectionTester _tester;

    public NativeBiSapConnectionTesterProductionBlockTests()
    {
        _originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        var secretResolver = new Mock<ISecretRefResolver>();
        secretResolver.Setup(r => r.Resolve(It.IsAny<string>())).Returns("test-password");

        _tester = new NativeBiSapConnectionTester(
            secretResolver.Object,
            NullLogger<NativeBiSapConnectionTester>.Instance);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _originalEnv);
    }

    private static NativeBiConnectionProfile MakeProfile(bool ignoreSsl) => new()
    {
        Id                  = 99,
        CompanyId           = 1,
        ProfileName         = "test-profile",
        EnvironmentName     = "TST",
        ServiceLayerBaseUrl = "https://sap-server-unreachable:50000/b1s/v1",
        CompanyDb           = "TESTDB",
        SapUserName         = "DATABISION_RO",
        SecretRef           = "env:TEST_SAP_PW",
        IsActive            = true,
        IgnoreSslErrors     = ignoreSsl,
        TimeoutSeconds      = 5,
        FetchConcurrency    = 1
    };

    [Fact]
    public async Task TestAsync_IgnoreSslTrue_ProductionEnv_ReturnsFailureWithoutNetworkAttempt()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

        var result = await _tester.TestAsync(MakeProfile(ignoreSsl: true));

        Assert.False(result.Success);
        Assert.Contains("IgnoreSslErrors=true", result.Message);
        Assert.Contains("Production", result.Message);
    }

    [Fact]
    public async Task TestAsync_IgnoreSslTrue_TstEnv_DoesNotBlockOnSslPolicyMessage()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "TST");

        var result = await _tester.TestAsync(MakeProfile(ignoreSsl: true));

        // Fails on network (unreachable host), but NOT because of the production SSL block
        Assert.False(result.Success);
        Assert.DoesNotContain("no está permitido en entornos Production", result.Message);
    }

    [Fact]
    public async Task TestAsync_IgnoreSslFalse_ProductionEnv_DoesNotBlockOnSslPolicy()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

        var result = await _tester.TestAsync(MakeProfile(ignoreSsl: false));

        // Fails on network (unreachable host), but NOT because of the production SSL block
        Assert.False(result.Success);
        Assert.DoesNotContain("no está permitido en entornos Production", result.Message);
    }
}
