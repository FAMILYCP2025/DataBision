using DataBision.Application.DTOs.Dashboard;
using FluentAssertions;
using Xunit;

namespace DataBision.Application.Tests.Services;

public sealed class FinanceMartDtoTests
{
    [Fact]
    public void FinanceMartSummaryDto_ConstructsWithAllProperties()
    {
        var dto = new FinanceMartSummaryDto(
            TotalOpenAr:     3_000_000m,
            TotalOverdueAr:  500_000m,
            ArCustomerCount: 30,
            DsoDays:         45.5m,
            TotalOpenAp:     2_000_000m,
            TotalOverdueAp:  200_000m,
            ApSupplierCount: 18,
            DpoDays:         32.0m);

        dto.TotalOpenAr.Should().Be(3_000_000m);
        dto.ArCustomerCount.Should().Be(30);
        dto.DsoDays.Should().Be(45.5m);
        dto.DpoDays.Should().Be(32.0m);
    }

    [Fact]
    public void FinanceMartSummaryDto_NullableDsoDpo_AcceptsNull()
    {
        var dto = new FinanceMartSummaryDto(
            0m, 0m, 0, null, 0m, 0m, 0, null);

        dto.DsoDays.Should().BeNull();
        dto.DpoDays.Should().BeNull();
    }

    [Fact]
    public void FinancePeriodKpiDto_EqualWhenSameValues()
    {
        var a = new FinancePeriodKpiDto(2026, 1, 100m, 5m, 95m, 10, 80m, 3m, 77m, 8);
        var b = new FinancePeriodKpiDto(2026, 1, 100m, 5m, 95m, 10, 80m, 3m, 77m, 8);

        a.Should().Be(b);
    }
}
