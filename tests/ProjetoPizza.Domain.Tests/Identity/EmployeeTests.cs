using FluentAssertions;
using ProjetoPizza.Domain.Identity;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Identity;

public sealed class EmployeeTests
{
    [Fact]
    public void UpdateProfile_ShouldPreserveAdministrativeFields()
    {
        var employee = new Employee(
            EmployeeId.New(),
            RestaurantUnitId.New(),
            Guid.NewGuid(),
            "Administrador",
            "admin@local.test",
            "ADMIN");

        employee.UpdateProfile(
            "Gerente",
            "Gerente",
            "gerente@local.test",
            "GER-01",
            "11999999999");

        employee.DisplayName.Should().Be("Gerente");
        employee.EmployeeCode.Should().Be("GER-01");
        employee.Phone.Should().Be("11999999999");
    }
}
