using DataBision.Application.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DataBision.Application.Tests.Services;

public sealed class MartAdminRefreshServiceTests
{
    [Fact]
    public async Task RefreshAllMartAsync_WhenModuleReturnsRows_ContainsResult()
    {
        var mock = new Mock<IMartAdminRefreshService>();
        mock.Setup(s => s.RefreshAllMartAsync("c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([("sales", "mart.sales_period_kpi", 42)]);

        var results = await mock.Object.RefreshAllMartAsync("c1");

        results.Should().HaveCount(1);
        results[0].Module.Should().Be("sales");
        results[0].RowsAffected.Should().Be(42);
    }

    [Fact]
    public async Task RefreshAllMartAsync_WhenAllModulesRun_ReturnsFourModules()
    {
        var mock = new Mock<IMartAdminRefreshService>();
        mock.Setup(s => s.RefreshAllMartAsync("c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                ("sales",     "mart.sales_period_kpi",    10),
                ("purchases", "mart.purchase_period_kpi", 20),
                ("inventory", "mart.inventory_snapshot",  30),
                ("finance",   "mart.finance_ar_aging",    40),
            ]);

        var results = await mock.Object.RefreshAllMartAsync("c1");

        results.Should().HaveCount(4);
        results.Select(r => r.Module).Should()
            .BeEquivalentTo(["sales", "purchases", "inventory", "finance"]);
    }

    [Fact]
    public async Task RefreshAllMartAsync_WhenEmptyResult_ReturnsEmptyList()
    {
        var mock = new Mock<IMartAdminRefreshService>();
        mock.Setup(s => s.RefreshAllMartAsync("c-empty", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var results = await mock.Object.RefreshAllMartAsync("c-empty");

        results.Should().BeEmpty();
    }
}
