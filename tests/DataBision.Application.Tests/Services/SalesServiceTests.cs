using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces;
using DataBision.Application.Interfaces.Dashboard;
using DataBision.Application.Services.Dashboard;
using FluentAssertions;
using Moq;
using Xunit;

namespace DataBision.Application.Tests.Services;

public sealed class SalesServiceTests
{
    private readonly Mock<IDashboardRepository> _repo = new();
    private readonly Mock<IAnalyticsCompanyResolver> _resolver = new();

    private SalesService NewService()
    {
        // Pass-through resolver: companyId unchanged so repo assertions stay exact.
        _resolver.Setup(r => r.Resolve(It.IsAny<string>())).Returns<string>(id => id);
        return new(_repo.Object, _resolver.Object);
    }

    // ── GetOverview ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOverview_PassesDateRangeToRepository()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to   = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);
        var expected = new SalesOverviewDto
        {
            GrossSalesAmount = 2_500_000m,
            InvoiceCount     = 55,
            DateFrom         = new DateOnly(2026, 1, 1),
            DateTo           = new DateOnly(2026, 3, 31),
        };
        _repo.Setup(r => r.GetSalesOverviewByRangeAsync("c1", from, to, It.IsAny<CancellationToken>()))
             .ReturnsAsync(expected);

        var result = await NewService().GetOverviewAsync("c1", from, to);

        result.GrossSalesAmount.Should().Be(2_500_000m);
        result.InvoiceCount.Should().Be(55);
        _repo.Verify(r => r.GetSalesOverviewByRangeAsync("c1", from, to, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── GetDaily ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDaily_PassesDateRangeToRepository()
    {
        var from = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var to   = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc);
        _repo.Setup(r => r.GetSalesDailyByRangeAsync("c1", from, to, It.IsAny<CancellationToken>()))
             .ReturnsAsync([new SalesDailyDto { InvoiceCount = 10 }]);

        var result = await NewService().GetDailyAsync("c1", from, to);

        result.Should().HaveCount(1);
        result[0].InvoiceCount.Should().Be(10);
    }

    // ── GetMonthly ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMonthly_WhenNoData_ReturnsEmpty()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to   = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        _repo.Setup(r => r.GetSalesMonthlyByRangeAsync("c1", from, to, It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        var result = await NewService().GetMonthlyAsync("c1", from, to);

        result.Should().BeEmpty();
    }

    // ── Customers ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCustomers_ClampsLimitToMax100()
    {
        _repo.Setup(r => r.GetCustomersAsync("c1",
                         It.Is<PaginationOptions>(p => p.Limit == 101),
                         It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetCustomersAsync("c1", new PaginationOptions(999));

        _repo.Verify(r => r.GetCustomersAsync("c1",
                         It.Is<PaginationOptions>(p => p.Limit == 101),
                         It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCustomers_ReturnsPagedResult()
    {
        _repo.Setup(r => r.GetCustomersAsync("c1",
                         It.Is<PaginationOptions>(p => p.Limit == 51),
                         It.IsAny<CancellationToken>()))
             .ReturnsAsync([new CustomerSalesDto { CardCode = "C001" }]);

        var result = await NewService().GetCustomersAsync("c1", new PaginationOptions(50));

        result.Data.Should().HaveCount(1);
        result.Meta.HasMore.Should().BeFalse();
        result.Meta.Limit.Should().Be(50);
    }

    // ── Items ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetItems_ClampsLimitToMax100()
    {
        _repo.Setup(r => r.GetItemsAsync("c1",
                         It.Is<PaginationOptions>(p => p.Limit == 101),
                         It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetItemsAsync("c1", new PaginationOptions(999));

        _repo.Verify(r => r.GetItemsAsync("c1",
                         It.Is<PaginationOptions>(p => p.Limit == 101),
                         It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Salespersons ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSalespersons_ClampsLimitToMax100()
    {
        _repo.Setup(r => r.GetSalespersonsAsync("c1",
                         It.Is<PaginationOptions>(p => p.Limit == 101),
                         It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetSalespersonsAsync("c1", new PaginationOptions(999));

        _repo.Verify(r => r.GetSalespersonsAsync("c1",
                         It.Is<PaginationOptions>(p => p.Limit == 101),
                         It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── DefaultDateRange ──────────────────────────────────────────────────────

    [Fact]
    public void DefaultDateRange_ReturnsLast30Days()
    {
        var (from, to) = SalesService.DefaultDateRange();

        (to - from).TotalDays.Should().BeApproximately(30, 1.0);
        to.Date.Should().Be(DateTime.UtcNow.Date);
    }
}
