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

public sealed class ClientBiSalesMartControllerTests
{
    private const string CompanyId = "company-test";

    private static IConfiguration DevConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:PublicKey"] = null })
            .Build();

    private static ClientBiSalesController Build(
        ISalesMartRepository? salesMart,
        string? queryCompanyId = CompanyId)
    {
        var httpContext = new DefaultHttpContext();
        if (queryCompanyId is not null)
            httpContext.Request.QueryString = new QueryString($"?companyId={queryCompanyId}");

        var ctrl = new ClientBiSalesController(
            new Mock<IProcessDashboardService>().Object,
            salesMart,
            DevConfig());
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return ctrl;
    }

    [Fact]
    public async Task GetMartKpi_WhenRepoNull_ReturnsBadRequest()
    {
        var ctrl = Build(salesMart: null);

        var result = await ctrl.GetMartKpi(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMartKpi_WhenNoCompanyId_ReturnsBadRequest()
    {
        var ctrl = Build(new Mock<ISalesMartRepository>().Object, queryCompanyId: null);

        var result = await ctrl.GetMartKpi(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMartKpi_WhenKpiNotNull_ReturnsData()
    {
        var kpi = new SalesMartKpiSummaryDto(
            1_000_000m, 900_000m, 11.1m, 500m, 0.5m, 10, 3, 150_000m, 1);
        var mock = new Mock<ISalesMartRepository>();
        mock.Setup(r => r.GetKpiSummaryAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(kpi);
        var ctrl = Build(mock.Object);

        var result = await ctrl.GetMartKpi(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<ApiResponse<SalesMartKpiSummaryDto>>()
            .Which.Data.Should().Be(kpi);
    }

    [Fact]
    public async Task GetMartKpi_WhenKpiNull_ReturnsOkWithHasDataFalse()
    {
        var mock = new Mock<ISalesMartRepository>();
        mock.Setup(r => r.GetKpiSummaryAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalesMartKpiSummaryDto?)null);
        var ctrl = Build(mock.Object);

        var result = await ctrl.GetMartKpi(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().NotBeOfType<ApiResponse<SalesMartKpiSummaryDto>>();
    }

    [Fact]
    public async Task GetMartTopCustomers_LimitTooHigh_ReturnsBadRequest()
    {
        var ctrl = Build(new Mock<ISalesMartRepository>().Object);

        var result = await ctrl.GetMartTopCustomers(limit: 101);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
