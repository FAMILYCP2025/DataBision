using DataBision.Application.DTOs.Dashboard;
using FluentAssertions;
using Xunit;

namespace DataBision.Application.Tests.Services;

public sealed class SalesMartDtoTests
{
    [Fact]
    public void SalesMartKpiSummaryDto_ConstructsWithAllProperties()
    {
        var dto = new SalesMartKpiSummaryDto(
            NetSalesLtm:        1_200_000m,
            NetSalesPrevLtm:    1_000_000m,
            GrowthPct:          20.0m,
            AvgTicketLtm:       600m,
            ReturnRatePct:      1.5m,
            ActiveCustomersLtm: 15,
            OpenOrdersCount:    4,
            OpenOrdersAmount:   200_000m,
            OverdueOrdersCount: 1);

        dto.NetSalesLtm.Should().Be(1_200_000m);
        dto.GrowthPct.Should().Be(20.0m);
        dto.ActiveCustomersLtm.Should().Be(15);
        dto.OverdueOrdersCount.Should().Be(1);
    }

    [Fact]
    public void SalesMartKpiSummaryDto_EqualWhenSameValues()
    {
        var a = new SalesMartKpiSummaryDto(500m, 400m, 25m, 250m, 0m, 5, 1, 50m, 0);
        var b = new SalesMartKpiSummaryDto(500m, 400m, 25m, 250m, 0m, 5, 1, 50m, 0);

        a.Should().Be(b);
    }

    [Fact]
    public void SalesMartKpiSummaryDto_NotEqualWhenValuesDiffer()
    {
        var a = new SalesMartKpiSummaryDto(500m, 400m, 25m, 250m, 0m, 5, 1, 50m, 0);
        var b = new SalesMartKpiSummaryDto(500m, 400m, 25m, 250m, 0m, 5, 1, 50m, 2);

        a.Should().NotBe(b);
    }
}
