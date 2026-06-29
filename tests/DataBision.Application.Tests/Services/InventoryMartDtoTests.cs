using DataBision.Application.DTOs.Dashboard;
using FluentAssertions;
using Xunit;

namespace DataBision.Application.Tests.Services;

public sealed class InventoryMartDtoTests
{
    [Fact]
    public void InventoryMartKpiSummaryDto_ConstructsWithAllProperties()
    {
        var dto = new InventoryMartKpiSummaryDto(
            TotalStockValue:      8_000_000m,
            TotalItems:           2500,
            SlowMovingItemsCount: 80,
            SlowMovingStockValue: 400_000m,
            ItemsBelowMin:        15,
            WarehouseCount:       4);

        dto.TotalStockValue.Should().Be(8_000_000m);
        dto.TotalItems.Should().Be(2500);
        dto.ItemsBelowMin.Should().Be(15);
        dto.WarehouseCount.Should().Be(4);
    }

    [Fact]
    public void InventoryMartKpiSummaryDto_EqualWhenSameValues()
    {
        var a = new InventoryMartKpiSummaryDto(100m, 10, 2, 10m, 1, 1);
        var b = new InventoryMartKpiSummaryDto(100m, 10, 2, 10m, 1, 1);

        a.Should().Be(b);
    }

    [Fact]
    public void SlowMovingItemDto_ConstructsWithNullableDates()
    {
        var dto = new SlowMovingItemDto(
            ItemCode:             "I001",
            ItemName:             "Tornillo M6",
            ItemGroupName:        "Ferretería",
            OnHand:               500m,
            StockValue:           2_500m,
            LastMovementDate:     null,
            DaysWithoutMovement:  180);

        dto.ItemCode.Should().Be("I001");
        dto.LastMovementDate.Should().BeNull();
        dto.DaysWithoutMovement.Should().Be(180);
    }
}
