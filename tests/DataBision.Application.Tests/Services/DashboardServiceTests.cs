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
        // Service clamps 999 → 100, then requests limit+1=101 from repo for hasMore detection
        _repo.Setup(r => r.GetCustomersAsync("c1",
                         It.Is<PaginationOptions>(p => p.Limit == 101 && p.Offset == 0),
                         It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        var result = await NewService().GetTopCustomersAsync("c1", new PaginationOptions(999));

        result.Meta.Limit.Should().Be(100);
        result.Data.Should().BeEmpty();
        _repo.Verify(r => r.GetCustomersAsync("c1",
                         It.Is<PaginationOptions>(p => p.Limit == 101),
                         It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTopCustomers_ClampsLimitToMin1()
    {
        _repo.Setup(r => r.GetCustomersAsync("c1",
                         It.Is<PaginationOptions>(p => p.Limit == 2),
                         It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        var result = await NewService().GetTopCustomersAsync("c1", new PaginationOptions(0));

        result.Meta.Limit.Should().Be(1);
    }

    [Fact]
    public async Task GetTopCustomers_HasMore_WhenRepoReturnsLimitPlusOne()
    {
        var items = Enumerable.Range(1, 11)
            .Select(i => new CustomerSalesDto { CardCode = $"C{i:D3}", NetSalesAmount = 1000m - i })
            .ToList();

        _repo.Setup(r => r.GetCustomersAsync("c1",
                         It.Is<PaginationOptions>(p => p.Limit == 11),
                         It.IsAny<CancellationToken>()))
             .ReturnsAsync(items);

        var result = await NewService().GetTopCustomersAsync("c1", new PaginationOptions(10));

        result.Meta.HasMore.Should().BeTrue();
        result.Meta.Count.Should().Be(10);
        result.Data.Should().HaveCount(10);
        result.Data[0].CardCode.Should().Be("C001");
    }

    [Fact]
    public async Task GetTopCustomers_PassesOffsetAndSort()
    {
        _repo.Setup(r => r.GetCustomersAsync("c1",
                         It.Is<PaginationOptions>(p => p.Offset == 20 && p.SortBy == "cardCode" && p.SortDir == "asc"),
                         It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetTopCustomersAsync("c1", new PaginationOptions(10, 20, "cardCode", "asc"));

        _repo.Verify(r => r.GetCustomersAsync("c1",
                         It.Is<PaginationOptions>(p => p.Offset == 20),
                         It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetTopItems ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTopItems_ClampsLimitToMax100()
    {
        _repo.Setup(r => r.GetItemsAsync("c1",
                         It.Is<PaginationOptions>(p => p.Limit == 101),
                         It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetTopItemsAsync("c1", new PaginationOptions(500));

        _repo.Verify(r => r.GetItemsAsync("c1",
                         It.Is<PaginationOptions>(p => p.Limit == 101),
                         It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetSalespersons ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetSalespersons_ClampsLimitToMax100()
    {
        _repo.Setup(r => r.GetSalespersonsAsync("c1",
                         It.Is<PaginationOptions>(p => p.Limit == 101),
                         It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetSalespersonsAsync("c1", new PaginationOptions(200));

        _repo.Verify(r => r.GetSalespersonsAsync("c1",
                         It.Is<PaginationOptions>(p => p.Limit == 101),
                         It.IsAny<CancellationToken>()), Times.Once);
    }
}
