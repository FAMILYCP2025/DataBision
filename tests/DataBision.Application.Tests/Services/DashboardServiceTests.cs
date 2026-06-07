using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using DataBision.Application.Services.Dashboard;
using FluentAssertions;
using Moq;
using Xunit;

namespace DataBision.Application.Tests.Services;

public sealed class DashboardServiceTests
{
    private readonly Mock<IDashboardRepository> _repo = new();
    private DashboardService NewService() => new(_repo.Object);

    // ── GetSummary ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSummary_WhenDataExists_ReturnsSummaryDto()
    {
        var expected = new DashboardSummaryDto
        {
            CompanyId        = "company-dev-001",
            GrossSalesAmount = 1_000_000m,
            InvoiceCount     = 72,
            TransformedAtUtc = DateTime.UtcNow,
        };
        _repo.Setup(r => r.GetSummaryAsync("company-dev-001", It.IsAny<CancellationToken>()))
             .ReturnsAsync(expected);

        var result = await NewService().GetSummaryAsync("company-dev-001");

        result.Should().NotBeNull();
        result!.GrossSalesAmount.Should().Be(1_000_000m);
        result.InvoiceCount.Should().Be(72);
    }

    [Fact]
    public async Task GetSummary_WhenNoData_ReturnsNull()
    {
        _repo.Setup(r => r.GetSummaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((DashboardSummaryDto?)null);

        var result = await NewService().GetSummaryAsync("company-dev-001");

        result.Should().BeNull();
    }

    // ── GetSalesDaily ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSalesDaily_ClampsDaysToMax365()
    {
        _repo.Setup(r => r.GetSalesDailyLastNDaysAsync("c1", 365, It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetSalesDailyAsync("c1", 9999);

        _repo.Verify(r => r.GetSalesDailyLastNDaysAsync("c1", 365, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSalesDaily_ClampsDaysToMin1()
    {
        _repo.Setup(r => r.GetSalesDailyLastNDaysAsync("c1", 1, It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetSalesDailyAsync("c1", -5);

        _repo.Verify(r => r.GetSalesDailyLastNDaysAsync("c1", 1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSalesDaily_WhenNoData_ReturnsEmptyList()
    {
        _repo.Setup(r => r.GetSalesDailyLastNDaysAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        var result = await NewService().GetSalesDailyAsync("company-dev-001", 30);

        result.Should().BeEmpty();
    }

    // ── GetSalesMonthly ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetSalesMonthly_ClampsMonthsToMax36()
    {
        _repo.Setup(r => r.GetSalesMonthlyLastNMonthsAsync("c1", 36, It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetSalesMonthlyAsync("c1", 999);

        _repo.Verify(r => r.GetSalesMonthlyLastNMonthsAsync("c1", 36, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── GetTopCustomers ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetTopCustomers_ClampsLimitToMax100()
    {
        _repo.Setup(r => r.GetCustomersAsync("c1", 100, It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetTopCustomersAsync("c1", 999);

        _repo.Verify(r => r.GetCustomersAsync("c1", 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTopCustomers_ClampsLimitToMin1()
    {
        _repo.Setup(r => r.GetCustomersAsync("c1", 1, It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetTopCustomersAsync("c1", 0);

        _repo.Verify(r => r.GetCustomersAsync("c1", 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTopCustomers_ReturnsListOrdered()
    {
        var expected = new List<CustomerSalesDto>
        {
            new() { CardCode = "C001", NetSalesAmount = 500_000m },
            new() { CardCode = "C002", NetSalesAmount = 300_000m },
        };
        _repo.Setup(r => r.GetCustomersAsync("c1", 10, It.IsAny<CancellationToken>()))
             .ReturnsAsync(expected);

        var result = await NewService().GetTopCustomersAsync("c1", 10);

        result.Should().HaveCount(2);
        result[0].CardCode.Should().Be("C001");
    }

    // ── GetTopItems ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTopItems_ClampsLimitToMax100()
    {
        _repo.Setup(r => r.GetItemsAsync("c1", 100, It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetTopItemsAsync("c1", 500);

        _repo.Verify(r => r.GetItemsAsync("c1", 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetSalespersons ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetSalespersons_ClampsLimitToMax100()
    {
        _repo.Setup(r => r.GetSalespersonsAsync("c1", 100, It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetSalespersonsAsync("c1", 200);

        _repo.Verify(r => r.GetSalespersonsAsync("c1", 100, It.IsAny<CancellationToken>()), Times.Once);
    }
}
