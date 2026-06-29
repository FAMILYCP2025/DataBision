using DataBision.Api.Contracts;
using DataBision.Api.Controllers;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces;
using DataBision.Application.Interfaces.Dashboard;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace DataBision.Api.Tests.Controllers.Mart;

public sealed class ClientBiFinanceMartControllerTests
{
    private const string CompanyId = "company-test";

    private static IConfiguration DevConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:PublicKey"] = null })
            .Build();

    private static ClientBiFinanceController Build(
        IFinanceMartRepository? financeMart,
        string? queryCompanyId = CompanyId)
    {
        var httpContext = new DefaultHttpContext();
        if (queryCompanyId is not null)
            httpContext.Request.QueryString = new QueryString($"?companyId={queryCompanyId}");

        var ctrl = new ClientBiFinanceController(
            new Mock<IProcessDashboardService>().Object,
            financeMart,
            DevConfig());
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return ctrl;
    }

    [Fact]
    public async Task GetMartSummary_WhenRepoNull_ReturnsBadRequest()
    {
        var ctrl = Build(financeMart: null);

        var result = await ctrl.GetMartSummary(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMartSummary_WhenNoCompanyId_ReturnsBadRequest()
    {
        var ctrl = Build(new Mock<IFinanceMartRepository>().Object, queryCompanyId: null);

        var result = await ctrl.GetMartSummary(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMartSummary_WhenSummaryNotNull_ReturnsData()
    {
        var summary = new FinanceMartSummaryDto(
            2_000_000m, 300_000m, 25, 45.5m, 1_500_000m, 100_000m, 12, 30.2m);
        var mock = new Mock<IFinanceMartRepository>();
        mock.Setup(r => r.GetSummaryAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        var ctrl = Build(mock.Object);

        var result = await ctrl.GetMartSummary(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<ApiResponse<FinanceMartSummaryDto>>()
            .Which.Data.Should().Be(summary);
    }

    [Fact]
    public async Task GetMartSummary_WhenSummaryNull_ReturnsOkWithHasDataFalse()
    {
        var mock = new Mock<IFinanceMartRepository>();
        mock.Setup(r => r.GetSummaryAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinanceMartSummaryDto?)null);
        var ctrl = Build(mock.Object);

        var result = await ctrl.GetMartSummary(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().NotBeOfType<ApiResponse<FinanceMartSummaryDto>>();
    }

    [Fact]
    public async Task GetMartArAging_LimitTooHigh_ReturnsBadRequest()
    {
        var ctrl = Build(new Mock<IFinanceMartRepository>().Object);

        var result = await ctrl.GetMartArAging(limit: 501);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
