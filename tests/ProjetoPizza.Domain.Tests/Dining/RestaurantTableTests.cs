using FluentAssertions;
using ProjetoPizza.Domain.Dining;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Dining;

public sealed class RestaurantTableTests
{
    private static readonly RestaurantUnitId UnitId = RestaurantUnitId.New();
    private static readonly DiningAreaId AreaId = DiningAreaId.New();

    [Fact]
    public void Create_WithValidData_ShouldBeActive()
    {
        var table = CreateTable();

        table.Number.Should().Be(1);
        table.Capacity.Should().Be(4);
        table.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidNumber_ShouldReject(int number)
    {
        var act = () => new RestaurantTable(RestaurantTableId.New(), UnitId, AreaId, number, 4);

        act.Should().Throw<BusinessRuleException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidCapacity_ShouldReject(int capacity)
    {
        var act = () => new RestaurantTable(RestaurantTableId.New(), UnitId, AreaId, 1, capacity);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Deactivate_ShouldPreventOpeningSession()
    {
        var table = CreateTable();
        table.Deactivate();

        var act = table.EnsureCanOpenSession;

        table.IsActive.Should().BeFalse();
        act.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("restaurant_table.inactive");
    }

    private static RestaurantTable CreateTable() =>
        new(RestaurantTableId.New(), UnitId, AreaId, 1, 4);
}
