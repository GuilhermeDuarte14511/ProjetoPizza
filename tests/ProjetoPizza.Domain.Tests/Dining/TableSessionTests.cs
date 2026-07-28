using FluentAssertions;
using ProjetoPizza.Domain.Dining;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Dining;

public sealed class TableSessionTests
{
    private readonly RestaurantUnitId _unitId = RestaurantUnitId.New();
    private readonly EmployeeId _employeeId = EmployeeId.New();

    [Fact]
    public void Open_WithTable_ShouldCreateOpenSession()
    {
        var session = OpenSession();

        session.Status.Should().Be(TableSessionStatus.Open);
        session.Tables.Should().ContainSingle();
    }

    [Fact]
    public void Open_WithoutTable_ShouldReject()
    {
        var act = () => TableSession.Open(
            TableSessionId.New(), _unitId, 1, 2, _employeeId, new Percentage(10), []);

        act.Should().Throw<BusinessRuleException>()
            .Which.Rule.Should().Be("table_session.tables_required");
    }

    [Fact]
    public void RequestBill_FromOpenSession_ShouldChangeStatus()
    {
        var session = OpenSession();

        session.RequestBill();

        session.Status.Should().Be(TableSessionStatus.BillRequested);
        session.BillRequestedAt.Should().NotBeNull();
    }

    [Fact]
    public void Close_ShouldPreventNewOrders()
    {
        var session = OpenSession();
        session.Close(_employeeId);

        var act = session.EnsureCanReceiveOrders;

        session.Status.Should().Be(TableSessionStatus.Closed);
        act.Should().Throw<BusinessRuleException>();
    }

    private TableSession OpenSession()
    {
        var table = new RestaurantTable(RestaurantTableId.New(), _unitId, DiningAreaId.New(), 1, 4);
        return TableSession.Open(TableSessionId.New(), _unitId, 1, 2, _employeeId, new Percentage(10), [table]);
    }
}
