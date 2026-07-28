using FluentAssertions;
using ProjetoPizza.Domain.Billing;
using ProjetoPizza.Domain.Catalog;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Devices;
using ProjetoPizza.Domain.Production;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Administration;

public sealed class AdministrativeDomainTests
{
    [Fact]
    public void OperationSettings_Update_ShouldApplyValidatedValues()
    {
        var settings = new OperationSettings(RestaurantUnitId.New());

        settings.Update(true, true, false, new Percentage(12), new Money(8.5m), false, true, 10);

        settings.AllowTableWithoutWaiter.Should().BeTrue();
        settings.ServiceFeePercentage.Value.Should().Be(12);
        settings.DefaultDeliveryFee.Amount.Should().Be(8.5m);
        settings.TableCallToleranceMinutes.Should().Be(10);
    }

    [Fact]
    public void PizzaSettings_Update_WithMoreThanThreeFlavors_ShouldFail()
    {
        var settings = new PizzaSettings(RestaurantUnitId.New());

        var action = () => settings.Update(4, PizzaPricingPolicy.HighestFlavorPrice, false, true, false);

        action.Should().Throw<BusinessRuleException>().Which.Rule.Should().Be("pizza_settings.global_max_flavors");
    }

    [Fact]
    public void KitchenTicket_ShouldEnforceProductionSequence()
    {
        var ticket = new KitchenTicket(
            KitchenTicketId.New(),
            RestaurantUnitId.New(),
            OrderId.New(),
            ProductionStationId.New(),
            123);

        ticket.Confirm();
        ticket.StartPreparation();
        ticket.MarkReady();
        ticket.Dispatch();

        ticket.Status.Should().Be(KitchenTicketStatus.Dispatched);
        ticket.DispatchedAt.Should().NotBeNull();
    }

    [Fact]
    public void Bill_RegisterPayment_ShouldCloseWhenRemainingAmountIsPaid()
    {
        var bill = new Bill(
            BillId.New(),
            RestaurantUnitId.New(),
            TableSessionId.New(),
            new Money(100),
            new Percentage(10));
        bill.Request();

        bill.RegisterPayment(new Money(110));

        bill.Status.Should().Be(BillStatus.Paid);
        bill.RemainingAmount.Amount.Should().Be(0);
        bill.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public void Device_UpdateStatus_WithInvalidBattery_ShouldFail()
    {
        var device = new Device(
            DeviceId.New(),
            RestaurantUnitId.New(),
            "Tablet",
            "SERIAL-1",
            DeviceType.CustomerTablet,
            "Android");

        var action = () => device.UpdateStatus(DeviceStatus.Online, 101, false, "Wi-Fi", null, "1.0");

        action.Should().Throw<BusinessRuleException>().Which.Rule.Should().Be("device.battery");
    }

    [Fact]
    public void PizzaCrust_Update_ShouldChangeAvailabilityWithoutReplacingIdentity()
    {
        var id = PizzaCrustId.New();
        var crust = new PizzaCrust(id, RestaurantUnitId.New(), "Catupiry");

        crust.Update("Catupiry Original", "Borda recheada", true, false);

        crust.Id.Should().Be(id);
        crust.Name.Should().Be("Catupiry Original");
        crust.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void PizzaFlavor_UpdateUnavailable_ShouldRequireAndPreserveSoldOutReason()
    {
        var flavor = new PizzaFlavor(
            PizzaFlavorId.New(),
            RestaurantUnitId.New(),
            CategoryId.New(),
            "Calabresa",
            PizzaFlavorType.Savory);

        flavor.Update("Calabresa especial", "Receita da casa", PizzaFlavorType.Savory, true, false, true, false, "Ingrediente em falta");

        flavor.Name.Should().Be("Calabresa especial");
        flavor.Description.Should().Be("Receita da casa");
        flavor.IsAvailable.Should().BeFalse();
        flavor.SoldOutReason.Should().Be("Ingrediente em falta");
    }
}
