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

public sealed class ClientBiInventoryMartControllerTests
{
    private const string CompanyId = "company-test";

    private static IConfiguration DevConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:PublicKey"] = null })
            .Build();

    private static ClientBiInventoryController Build(
        IInventoryMartRepository? inventoryMart,
        string? queryCompanyId = CompanyId)
    {
        var httpContext = new DefaultHttpContext();
        if (queryCompanyId is not null)
            httpContext.Request.QueryString = new QueryString($"?companyId={queryCompanyId}");

        var ctrl = new ClientBiInventoryController(
            new Mock<IProcessDashboardService>().Object,
            inventoryMart,
            DevConfig());
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return ctrl;
    }

    [Fact]
    public async Task GetMartKpi_WhenRepoNull_ReturnsBadRequest()
    {
        var ctrl = Build(inventoryMart: null);

        var result = await ctrl.GetMartKpi(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMartKpi_WhenNoCompanyId_ReturnsBadRequest()
    {
        var ctrl = Build(new Mock<IInventoryMartRepository>().Object, queryCompanyId: null);

        var result = await ctrl.GetMartKpi(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMartKpi_WhenKpiNotNull_ReturnsData()
    {
        var kpi = new InventoryMartKpiSummaryDto(
            5_000_000m, 1200, 45, 300_000m, 12, 3);
        var mock = new Mock<IInventoryMartRepository>();
        mock.Setup(r => r.GetKpiSummaryAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(kpi);
        var ctrl = Build(mock.Object);

        var result = await ctrl.GetMartKpi(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<ApiResponse<InventoryMartKpiSummaryDto>>()
            .Which.Data.Should().Be(kpi);
    }

    [Fact]
    public async Task GetMartKpi_WhenKpiNull_ReturnsOkWithHasDataFalse()
    {
        var mock = new Mock<IInventoryMartRepository>();
        mock.Setup(r => r.GetKpiSummaryAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryMartKpiSummaryDto?)null);
        var ctrl = Build(mock.Object);

        var result = await ctrl.GetMartKpi(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().NotBeOfType<ApiResponse<InventoryMartKpiSummaryDto>>();
    }

    [Fact]
    public async Task GetMartSnapshot_LimitTooHigh_ReturnsBadRequest()
    {
        var ctrl = Build(new Mock<IInventoryMartRepository>().Object);

        var result = await ctrl.GetMartSnapshot(limit: 501);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
