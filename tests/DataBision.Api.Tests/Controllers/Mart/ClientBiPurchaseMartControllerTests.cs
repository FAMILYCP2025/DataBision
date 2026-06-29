using DataBision.Api.Contracts;
using DataBision.Api.Controllers;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace DataBision.Api.Tests.Controllers.Mart;

public sealed class ClientBiPurchaseMartControllerTests
{
    private const string CompanyId = "company-test";

    private static IConfiguration DevConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:PublicKey"] = null })
            .Build();

    private static ClientBiPurchaseController Build(
        IPurchaseMartRepository? purchaseMart,
        string? queryCompanyId = CompanyId)
    {
        var httpContext = new DefaultHttpContext();
        if (queryCompanyId is not null)
            httpContext.Request.QueryString = new QueryString($"?companyId={queryCompanyId}");

        var ctrl = new ClientBiPurchaseController(purchaseMart, DevConfig());
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return ctrl;
    }

    [Fact]
    public async Task GetMartKpi_WhenRepoNull_ReturnsBadRequest()
    {
        var ctrl = Build(purchaseMart: null);

        var result = await ctrl.GetMartKpi(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMartKpi_WhenNoCompanyId_ReturnsBadRequest()
    {
        var ctrl = Build(new Mock<IPurchaseMartRepository>().Object, queryCompanyId: null);

        var result = await ctrl.GetMartKpi(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMartKpi_WhenKpiNotNull_ReturnsData()
    {
        var kpi = new PurchaseMartKpiSummaryDto(
            800_000m, 700_000m, 14.3m, 400m, 8, 2, 80_000m, 0);
        var mock = new Mock<IPurchaseMartRepository>();
        mock.Setup(r => r.GetKpiSummaryAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(kpi);
        var ctrl = Build(mock.Object);

        var result = await ctrl.GetMartKpi(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<ApiResponse<PurchaseMartKpiSummaryDto>>()
            .Which.Data.Should().Be(kpi);
    }

    [Fact]
    public async Task GetMartKpi_WhenKpiNull_ReturnsOkWithHasDataFalse()
    {
        var mock = new Mock<IPurchaseMartRepository>();
        mock.Setup(r => r.GetKpiSummaryAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseMartKpiSummaryDto?)null);
        var ctrl = Build(mock.Object);

        var result = await ctrl.GetMartKpi(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().NotBeOfType<ApiResponse<PurchaseMartKpiSummaryDto>>();
    }

    [Fact]
    public async Task GetMartTopSuppliers_LimitTooHigh_ReturnsBadRequest()
    {
        var ctrl = Build(new Mock<IPurchaseMartRepository>().Object);

        var result = await ctrl.GetMartTopSuppliers(limit: 101);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
