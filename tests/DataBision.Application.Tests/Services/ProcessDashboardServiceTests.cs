using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using DataBision.Application.Services.Dashboard;
using FluentAssertions;
using Moq;
using Xunit;

namespace DataBision.Application.Tests.Services;

public sealed class ProcessDashboardServiceTests
{
    private readonly Mock<IProcessDashboardRepository> _repo = new();
    private ProcessDashboardService NewService() => new(_repo.Object);

    // ── Pagination clamping ───────────────────────────────────────────────────

    [Fact]
    public async Task GetSalesCustomers_ClampsLimitToMax200()
    {
        _repo.Setup(r => r.GetSalesCustomersAsync("c1",
                It.Is<PaginationOptions>(p => p.Limit == 201), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetSalesCustomersAsync("c1", new PaginationOptions(500, 0));

        _repo.Verify(r => r.GetSalesCustomersAsync("c1",
            It.Is<PaginationOptions>(p => p.Limit == 201), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSalesCustomers_ClampsLimitToMin1()
    {
        _repo.Setup(r => r.GetSalesCustomersAsync("c1",
                It.Is<PaginationOptions>(p => p.Limit == 2), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetSalesCustomersAsync("c1", new PaginationOptions(-5, 0));

        _repo.Verify(r => r.GetSalesCustomersAsync("c1",
            It.Is<PaginationOptions>(p => p.Limit == 2), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Empty tables return empty paged result (not error) ────────────────────

    [Fact]
    public async Task GetSalesCustomers_WhenEmpty_ReturnsEmptyPagedResult()
    {
        _repo.Setup(r => r.GetSalesCustomersAsync(It.IsAny<string>(),
                It.IsAny<PaginationOptions>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        var result = await NewService().GetSalesCustomersAsync("c1", new PaginationOptions(50, 0));

        result.Should().NotBeNull();
        result.Data.Should().BeEmpty();
        result.Meta.HasMore.Should().BeFalse();
        result.Meta.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetFinanceArAging_WhenEmpty_ReturnsEmptyPagedResult()
    {
        _repo.Setup(r => r.GetFinanceArAgingAsync(It.IsAny<string>(),
                It.IsAny<PaginationOptions>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        var result = await NewService().GetFinanceArAgingAsync("c1", new PaginationOptions(50, 0));

        result.Data.Should().BeEmpty();
        result.Meta.HasMore.Should().BeFalse();
    }

    // ── HasMore detection ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetSalesCustomers_HasMoreTrue_WhenMoreResultsAvailable()
    {
        var customers = Enumerable.Range(0, 51)
            .Select(i => new SalesCustomerDashboardDto { CardCode = $"C{i:000}" })
            .ToList();

        _repo.Setup(r => r.GetSalesCustomersAsync("c1",
                It.Is<PaginationOptions>(p => p.Limit == 51), It.IsAny<CancellationToken>()))
             .ReturnsAsync(customers);

        var result = await NewService().GetSalesCustomersAsync("c1", new PaginationOptions(50, 0));

        result.Meta.HasMore.Should().BeTrue();
        result.Meta.Count.Should().Be(50);
        result.Data.Should().HaveCount(50);
    }

    // ── Operations pipeline health returns null when no data ──────────────────

    [Fact]
    public async Task GetPipelineHealth_WhenNoPipelineData_ReturnsNull()
    {
        _repo.Setup(r => r.GetPipelineHealthAsync("c1", It.IsAny<CancellationToken>()))
             .ReturnsAsync((OperationHealthDto?)null);

        var result = await NewService().GetPipelineHealthAsync("c1");

        result.Should().BeNull();
    }

    // ── Days clamping ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetFinanceExecutive_ClampsDaysTo365()
    {
        _repo.Setup(r => r.GetFinanceExecutiveAsync("c1", 365, It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetFinanceExecutiveAsync("c1", 9999);

        _repo.Verify(r => r.GetFinanceExecutiveAsync("c1", 365, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetInventoryRotation_WhenEmpty_ReturnsEmptyPaged()
    {
        _repo.Setup(r => r.GetInventoryRotationAsync(It.IsAny<string>(),
                It.IsAny<PaginationOptions>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        var result = await NewService().GetInventoryRotationAsync("c1", new PaginationOptions(50, 0));

        result.Data.Should().BeEmpty();
        result.Meta.HasMore.Should().BeFalse();
    }

    // ── Company isolation: each call uses exact company_id passed ─────────────

    [Fact]
    public async Task GetSalesCustomers_UsesExactCompanyId()
    {
        _repo.Setup(r => r.GetSalesCustomersAsync("tenant-a",
                It.IsAny<PaginationOptions>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        await NewService().GetSalesCustomersAsync("tenant-a", new PaginationOptions(10, 0));

        _repo.Verify(r => r.GetSalesCustomersAsync("tenant-a",
            It.IsAny<PaginationOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _repo.Verify(r => r.GetSalesCustomersAsync(
            It.Is<string>(s => s != "tenant-a"),
            It.IsAny<PaginationOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
