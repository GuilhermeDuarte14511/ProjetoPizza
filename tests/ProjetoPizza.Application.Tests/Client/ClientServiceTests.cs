using FluentAssertions;
using System.Security.Cryptography;
using System.Text;
using ProjetoPizza.Application.Abstractions.Persistence;
using ProjetoPizza.Application.Client;
using ProjetoPizza.Domain.Audit;
using ProjetoPizza.Domain.Billing;
using ProjetoPizza.Domain.Cashier;
using ProjetoPizza.Domain.Catalog;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Customers;
using ProjetoPizza.Domain.Devices;
using ProjetoPizza.Domain.Dining;
using ProjetoPizza.Domain.Identity;
using ProjetoPizza.Domain.Inventory;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.Production;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Application.Tests.Client;

public sealed class ClientServiceTests
{
    [Fact]
    public async Task Activate_ShouldCreatePersistentHashedAccessForLinkedTablet()
    {
        var fixture = CreateFixture();
        var service = new ClientService(fixture.Context);

        var result = await service.ActivateAsync(
            new ActivateClientSessionCommand(fixture.Device.SerialNumber),
            CancellationToken.None);

        result.Token.Should().NotBeNullOrWhiteSpace();
        result.Bootstrap.Session.TableNumber.Should().Be(2);
        fixture.Context.DeviceSessionItems.Should().ContainSingle();
        fixture.Context.DeviceSessionItems[0].SessionTokenHash.Should().NotBe(result.Token);
        fixture.Context.DeviceSessionItems[0].SessionTokenHash.Should().HaveLength(64);
        fixture.Context.DeviceSessionItems[0].ExpiresAt.Should().BeNull();
        fixture.Context.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Activate_WithoutOpenTableSession_ShouldKeepTabletInStandby()
    {
        var fixture = CreateFixture(withOpenTableSession: false);
        var service = new ClientService(fixture.Context);

        var result = await service.ActivateAsync(
            new ActivateClientSessionCommand(fixture.Device.SerialNumber),
            CancellationToken.None);

        result.Bootstrap.Session.Status.Should().Be("Idle");
        result.Bootstrap.Session.TableSessionId.Should().BeNull();
        result.Bootstrap.Session.TableNumber.Should().Be(2);
        fixture.Context.DeviceSessionItems.Should().ContainSingle()
            .Which.TableSessionId.Should().BeNull();
    }

    [Fact]
    public async Task StartTableSession_FromStandby_ShouldOpenComandaAndBindCredential()
    {
        var fixture = CreateFixture(withOpenTableSession: false);
        var service = new ClientService(fixture.Context);
        var activation = await service.ActivateAsync(
            new ActivateClientSessionCommand(fixture.Device.SerialNumber),
            CancellationToken.None);
        var session = await service.ValidateSessionAsync(activation.Token, CancellationToken.None);

        var result = await service.StartTableSessionAsync(
            session!,
            new StartClientTableSessionCommand(3),
            CancellationToken.None);

        result.Session.Status.Should().Be("Open");
        result.Session.GuestCount.Should().Be(3);
        var tableSession = fixture.Context.TableSessionItems.Should().ContainSingle().Which;
        tableSession.OpenedByDeviceId.Should().Be(fixture.Device.Id);
        tableSession.OpenedByEmployeeId.Should().BeNull();
        fixture.Context.DeviceSessionItems.Single().TableSessionId.Should().Be(tableSession.Id);
    }

    [Fact]
    public async Task CompleteTableSession_ShouldReturnToStandbyWithoutEndingDeviceAccess()
    {
        var fixture = CreateFixture();
        var service = new ClientService(fixture.Context);
        var activation = await service.ActivateAsync(
            new ActivateClientSessionCommand(fixture.Device.SerialNumber),
            CancellationToken.None);
        var session = await service.ValidateSessionAsync(activation.Token, CancellationToken.None);
        var tableSession = fixture.Context.TableSessionItems.Single();
        var bill = new Bill(
            BillId.New(),
            fixture.Unit.Id,
            tableSession.Id,
            new Money(100m),
            tableSession.ServiceFeePercentageSnapshot);
        bill.RegisterPayment(bill.TotalAmount);
        tableSession.Close(fixture.Employee.Id);
        fixture.Context.BillItems.Add(bill);

        var result = await service.CompleteTableSessionAsync(session!, CancellationToken.None);

        result.Session.Status.Should().Be("Idle");
        result.Session.TableSessionId.Should().BeNull();
        var deviceSession = fixture.Context.DeviceSessionItems.Single();
        deviceSession.TableSessionId.Should().BeNull();
        deviceSession.EndedAt.Should().BeNull();
        (await service.ValidateSessionAsync(activation.Token, CancellationToken.None))
            .Should().NotBeNull();
    }

    [Fact]
    public async Task Logout_ShouldEndDeviceAccess()
    {
        var fixture = CreateFixture();
        var service = new ClientService(fixture.Context);
        var activation = await service.ActivateAsync(
            new ActivateClientSessionCommand(fixture.Device.SerialNumber),
            CancellationToken.None);
        var session = await service.ValidateSessionAsync(activation.Token, CancellationToken.None);

        await service.LogoutAsync(session!, CancellationToken.None);

        fixture.Context.DeviceSessionItems.Single().EndedAt.Should().NotBeNull();
        (await service.ValidateSessionAsync(activation.Token, CancellationToken.None))
            .Should().BeNull();
    }

    [Fact]
    public async Task UpdateTelemetry_ShouldPersistAuthenticatedTabletStatus()
    {
        var fixture = CreateFixture();
        var service = new ClientService(fixture.Context);

        await service.UpdateTelemetryAsync(
            fixture.SessionContext,
            new UpdateClientTelemetryCommand(
                37,
                true,
                "Wi-Fi",
                "Web 1.0.0",
                "192.168.15.25"),
            CancellationToken.None);

        fixture.Device.Status.Should().Be(DeviceStatus.Online);
        fixture.Device.BatteryPercentage.Should().Be(37);
        fixture.Device.IsCharging.Should().BeTrue();
        fixture.Device.NetworkStatus.Should().Be("Wi-Fi");
        fixture.Device.IpAddress.Should().Be("192.168.15.25");
        fixture.Device.AppVersion.Should().Be("Web 1.0.0");
        fixture.Device.LastSeenAt.Should().NotBeNull();
        fixture.Context.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Activate_WithUnknownDevice_ShouldBeRejected()
    {
        var fixture = CreateFixture();
        var service = new ClientService(fixture.Context);

        var action = () => service.ActivateAsync(
            new ActivateClientSessionCommand("UNKNOWN"),
            CancellationToken.None);

        (await action.Should().ThrowAsync<BusinessRuleException>())
            .Which.Rule.Should().Be("client.device_unavailable");
    }

    [Fact]
    public async Task Activate_WithProvisioningToken_ShouldConsumeCredentialOnlyOnce()
    {
        var fixture = CreateFixture();
        var rawToken = "temporary-tablet-activation-token";
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var provisioning = new DeviceProvisioning(
            DeviceProvisioningId.New(),
            fixture.Device.Id,
            tokenHash,
            DateTimeOffset.UtcNow.AddMinutes(30));
        fixture.Context.DeviceProvisioningItems.Add(provisioning);
        var service = new ClientService(fixture.Context);

        var result = await service.ActivateAsync(
            new ActivateClientSessionCommand(ProvisioningToken: rawToken),
            CancellationToken.None);

        result.Bootstrap.Session.TableNumber.Should().Be(2);
        provisioning.ConsumedAt.Should().NotBeNull();

        var secondActivation = () => service.ActivateAsync(
            new ActivateClientSessionCommand(ProvisioningToken: rawToken),
            CancellationToken.None);
        (await secondActivation.Should().ThrowAsync<BusinessRuleException>())
            .Which.Rule.Should().Be("client.device_unavailable");
    }

    [Fact]
    public async Task SubmitOrder_ShouldUseServerPriceAndCreateKitchenTicket()
    {
        var fixture = CreateFixture();
        var product = new Product(
            ProductId.New(),
            fixture.Unit.Id,
            CategoryId.New(),
            "BEB-001",
            "Refrigerante",
            ProductType.Beverage,
            new Money(14m));
        fixture.Context.ProductItems.Add(product);
        fixture.Context.ProductionStationItems.Add(new ProductionStation(
            ProductionStationId.New(),
            fixture.Unit.Id,
            "Bar",
            "BAR",
            5));
        fixture.Context.CashShiftItems.Add(new CashShift(
            CashShiftId.New(),
            CashRegisterId.New(),
            fixture.Employee.Id,
            Money.Zero()));
        var service = new ClientService(fixture.Context);
        var requestId = Guid.NewGuid();

        var result = await service.SubmitOrderAsync(
            fixture.SessionContext,
            new SubmitClientOrderCommand(
                requestId,
                [new SubmitClientOrderItemCommand(product.Id.Value, 2, null, null)],
                null),
            CancellationToken.None);

        result.Id.Should().Be(requestId);
        result.Status.Should().Be("Submitted");
        result.Total.Should().Be(28m);
        fixture.Context.OrderItemsData.Should().ContainSingle();
        fixture.Context.KitchenTicketItemsData.Should().ContainSingle();
        fixture.Context.KitchenTicketLineItems.Should().ContainSingle()
            .Which.Quantity.Should().Be(2);
        fixture.Context.AuditLogItems.Should().ContainSingle(log =>
            log.Action == "SubmitFromTablet");
    }

    [Fact]
    public async Task SubmitPizza_WithSplitCrust_ShouldSumBothHalfPricesAndPersistComposition()
    {
        var fixture = CreateFixture();
        var categoryId = CategoryId.New();
        var product = new Product(
            ProductId.New(),
            fixture.Unit.Id,
            categoryId,
            "PIZ-SPLIT",
            "Pizza",
            ProductType.Pizza,
            Money.Zero());
        var size = new PizzaSize(
            PizzaSizeId.New(),
            fixture.Unit.Id,
            "Grande",
            "G",
            8,
            35,
            new Money(50m),
            2);
        var flavor = new PizzaFlavor(
            PizzaFlavorId.New(),
            fixture.Unit.Id,
            categoryId,
            "Calabresa",
            PizzaFlavorType.Savory);
        var catupiry = new PizzaCrust(
            PizzaCrustId.New(),
            fixture.Unit.Id,
            "Catupiry");
        var cheddar = new PizzaCrust(
            PizzaCrustId.New(),
            fixture.Unit.Id,
            "Cheddar");
        fixture.Context.ProductItems.Add(product);
        fixture.Context.PizzaSizeItems.Add(size);
        fixture.Context.PizzaFlavorItems.Add(flavor);
        fixture.Context.PizzaFlavorPriceItems.Add(new PizzaFlavorPrice(
            PizzaFlavorPriceId.New(),
            flavor.Id,
            size.Id,
            new Money(50m),
            Money.Zero()));
        fixture.Context.PizzaCrustItems.AddRange([catupiry, cheddar]);
        fixture.Context.PizzaCrustPriceItems.Add(new PizzaCrustPrice(
            PizzaCrustPriceId.New(),
            catupiry.Id,
            size.Id,
            new Money(12m),
            new Money(6m)));
        fixture.Context.PizzaCrustPriceItems.Add(new PizzaCrustPrice(
            PizzaCrustPriceId.New(),
            cheddar.Id,
            size.Id,
            new Money(14m),
            new Money(7m)));
        fixture.Context.ProductionStationItems.Add(new ProductionStation(
            ProductionStationId.New(),
            fixture.Unit.Id,
            "Pizzaria",
            "PIZZA",
            20));
        fixture.Context.CashShiftItems.Add(new CashShift(
            CashShiftId.New(),
            CashRegisterId.New(),
            fixture.Employee.Id,
            Money.Zero()));
        var service = new ClientService(fixture.Context);

        var result = await service.SubmitOrderAsync(
            fixture.SessionContext,
            new SubmitClientOrderCommand(
                Guid.NewGuid(),
                [new SubmitClientOrderItemCommand(
                    product.Id.Value,
                    1,
                    null,
                    new SubmitClientPizzaCommand(
                        size.Id.Value,
                        [flavor.Id.Value],
                        catupiry.Id.Value,
                        cheddar.Id.Value,
                        [],
                        []))],
                null),
            CancellationToken.None);

        result.Total.Should().Be(63m);
        var pizza = fixture.Context.OrderItemPizzaItems.Should().ContainSingle().Which;
        pizza.CrustSelectionMode.Should().Be(CrustSelectionMode.Split);
        pizza.CrustPrice.Amount.Should().Be(13m);
        pizza.CrustNameSnapshot.Should().Be("Catupiry");
        pizza.SecondCrustNameSnapshot.Should().Be("Cheddar");
    }

    [Fact]
    public async Task SubmitPizza_WithMultipleFlavors_ShouldPriceExtrasForEachTargetFlavor()
    {
        var fixture = CreateFixture();
        var categoryId = CategoryId.New();
        var product = new Product(
            ProductId.New(),
            fixture.Unit.Id,
            categoryId,
            "PIZ-001",
            "Pizza",
            ProductType.Pizza,
            Money.Zero());
        var size = new PizzaSize(
            PizzaSizeId.New(),
            fixture.Unit.Id,
            "Grande",
            "G",
            8,
            35,
            new Money(50m),
            2);
        var flavor = new PizzaFlavor(
            PizzaFlavorId.New(),
            fixture.Unit.Id,
            categoryId,
            "Calabresa",
            PizzaFlavorType.Savory);
        var ingredient = new Ingredient(
            IngredientId.New(),
            fixture.Unit.Id,
            "Bacon");
        ingredient.Update("Bacon", null, true, false, null, true, new Money(8m), 3);
        var secondFlavor = new PizzaFlavor(
            PizzaFlavorId.New(),
            fixture.Unit.Id,
            categoryId,
            "Margherita",
            PizzaFlavorType.Savory);
        fixture.Context.ProductItems.Add(product);
        fixture.Context.PizzaSizeItems.Add(size);
        fixture.Context.PizzaFlavorItems.Add(flavor);
        fixture.Context.PizzaFlavorItems.Add(secondFlavor);
        fixture.Context.PizzaFlavorPriceItems.Add(new PizzaFlavorPrice(
            PizzaFlavorPriceId.New(),
            flavor.Id,
            size.Id,
            new Money(50m),
            Money.Zero()));
        fixture.Context.PizzaFlavorPriceItems.Add(new PizzaFlavorPrice(
            PizzaFlavorPriceId.New(),
            secondFlavor.Id,
            size.Id,
            new Money(50m),
            Money.Zero()));
        fixture.Context.IngredientItems.Add(ingredient);
        fixture.Context.PizzaFlavorExtraItems.Add(new PizzaFlavorExtra(
            flavor.Id,
            ingredient.Id,
            new Money(8m),
            3));
        fixture.Context.PizzaFlavorExtraItems.Add(new PizzaFlavorExtra(
            secondFlavor.Id,
            ingredient.Id,
            new Money(5m),
            2));
        fixture.Context.ProductionStationItems.Add(new ProductionStation(
            ProductionStationId.New(),
            fixture.Unit.Id,
            "Pizzaria",
            "PIZZA",
            20));
        fixture.Context.CashShiftItems.Add(new CashShift(
            CashShiftId.New(),
            CashRegisterId.New(),
            fixture.Employee.Id,
            Money.Zero()));
        var service = new ClientService(fixture.Context);

        var result = await service.SubmitOrderAsync(
            fixture.SessionContext,
            new SubmitClientOrderCommand(
                Guid.NewGuid(),
                [new SubmitClientOrderItemCommand(
                    product.Id.Value,
                    1,
                    null,
                    new SubmitClientPizzaCommand(
                        size.Id.Value,
                        [flavor.Id.Value, secondFlavor.Id.Value],
                        null,
                        null,
                        [],
                        [
                            new SubmitClientPizzaExtraCommand(ingredient.Id.Value, flavor.Id.Value, 1),
                            new SubmitClientPizzaExtraCommand(ingredient.Id.Value, secondFlavor.Id.Value, 1)
                        ]))],
                null),
            CancellationToken.None);

        result.Total.Should().Be(63m);
        fixture.Context.OrderItemPizzaItems.Should().ContainSingle()
            .Which.ExtrasPrice.Amount.Should().Be(13m);
        fixture.Context.OrderItemModifierItems.Should().HaveCount(2);
        fixture.Context.OrderItemModifierItems.Should().Contain(modifier =>
                modifier.ModifierType == ModifierType.Extra &&
                modifier.IngredientId == ingredient.Id &&
                modifier.PizzaFlavorId == flavor.Id &&
                modifier.Quantity == 1 &&
                modifier.TotalPrice.Amount == 8m);
        fixture.Context.OrderItemModifierItems.Should().Contain(modifier =>
                modifier.ModifierType == ModifierType.Extra &&
                modifier.IngredientId == ingredient.Id &&
                modifier.PizzaFlavorId == secondFlavor.Id &&
                modifier.Quantity == 1 &&
                modifier.TotalPrice.Amount == 5m);

        product.ConfigureCustomExtras(true);
        fixture.Context.ProductExtraItems.Add(new ProductExtra(
            product.Id,
            ingredient.Id,
            new Money(4m),
            2));

        var customProductResult = await service.SubmitOrderAsync(
            fixture.SessionContext,
            new SubmitClientOrderCommand(
                Guid.NewGuid(),
                [new SubmitClientOrderItemCommand(
                    product.Id.Value,
                    1,
                    null,
                    new SubmitClientPizzaCommand(
                        size.Id.Value,
                        [flavor.Id.Value, secondFlavor.Id.Value],
                        null,
                        null,
                        [],
                        [
                            new SubmitClientPizzaExtraCommand(ingredient.Id.Value, flavor.Id.Value, 1),
                            new SubmitClientPizzaExtraCommand(ingredient.Id.Value, secondFlavor.Id.Value, 1)
                        ]))],
                null),
            CancellationToken.None);

        customProductResult.Total.Should().Be(58m);
        fixture.Context.OrderItemModifierItems.TakeLast(2)
            .Should().OnlyContain(modifier => modifier.UnitPrice.Amount == 4m);
    }

    [Fact]
    public async Task RequestBill_ShouldPersistSplitPreferenceAndReturnItToTablet()
    {
        var fixture = CreateFixture();
        var tableSession = fixture.Context.TableSessionItems.Single();
        var bill = new Bill(
            BillId.New(),
            fixture.Unit.Id,
            tableSession.Id,
            new Money(100m),
            tableSession.ServiceFeePercentageSnapshot);
        fixture.Context.BillItems.Add(bill);
        var service = new ClientService(fixture.Context);

        var result = await service.RequestBillAsync(
            fixture.SessionContext,
            new RequestClientBillCommand(4),
            CancellationToken.None);

        result.Status.Should().Be("Requested");
        result.RequestedSplitCount.Should().Be(4);
        bill.RequestedSplitCount.Should().Be(4);
        tableSession.Status.Should().Be(TableSessionStatus.BillRequested);
    }

    [Fact]
    public async Task CreateServiceCall_ShouldRejectDuplicateOpenReason()
    {
        var fixture = CreateFixture();
        var callType = new ServiceCallType(ServiceCallTypeId.New(), "WAITER", "Chamar garçom");
        fixture.Context.ServiceCallTypeItems.Add(callType);
        var service = new ClientService(fixture.Context);
        var command = new CreateClientServiceCallCommand(callType.Id.Value, "Guardanapos");

        await service.CreateServiceCallAsync(fixture.SessionContext, command, CancellationToken.None);
        var duplicate = () => service.CreateServiceCallAsync(fixture.SessionContext, command, CancellationToken.None);

        (await duplicate.Should().ThrowAsync<BusinessRuleException>())
            .Which.Rule.Should().Be("client.service_call_duplicate");
    }

    [Fact]
    public async Task GetState_ShouldNotRequireCatalogProjection()
    {
        var fixture = CreateFixture();
        var service = new ClientService(fixture.Context);

        var state = await service.GetStateAsync(fixture.SessionContext, CancellationToken.None);

        state.Session.TableNumber.Should().Be(2);
        state.Session.ClearTabletAfterTableClose.Should().BeTrue();
        state.Orders.Should().BeEmpty();
    }

    private static Fixture CreateFixture(bool withOpenTableSession = true)
    {
        var unit = new RestaurantUnit(
            RestaurantUnitId.New(),
            "Unidade Principal",
            "Projeto Pizza LTDA",
            "Forno 27",
            "00.000.000/0001-00");
        var operationSettings = new OperationSettings(unit.Id);
        var pizzaSettings = new PizzaSettings(unit.Id);
        var identityUserId = Guid.NewGuid();
        var employee = new Employee(
            EmployeeId.New(),
            unit.Id,
            identityUserId,
            "Carlos",
            "carlos@local.test",
            "GARCOM");
        var area = new DiningArea(DiningAreaId.New(), unit.Id, "Salão");
        var table = new RestaurantTable(RestaurantTableId.New(), unit.Id, area.Id, 2, 4);
        var tableSession = withOpenTableSession
            ? TableSession.Open(
                TableSessionId.New(),
                unit.Id,
                1002,
                2,
                employee.Id,
                new Percentage(10),
                [table])
            : null;
        tableSession?.AssignWaiter(employee.Id);
        var device = new Device(
            DeviceId.New(),
            unit.Id,
            "Tablet Mesa 02",
            "DEV-TABLET-002",
            DeviceType.CustomerTablet,
            "Web");
        device.LinkToTable(table.Id);
        var context = new FakeContext
        {
            RestaurantUnitItems = [unit],
            OperationSettingItems = [operationSettings],
            PizzaSettingItems = [pizzaSettings],
            EmployeeItems = [employee],
            DiningAreaItems = [area],
            RestaurantTableItems = [table],
            TableSessionItems = tableSession is null ? [] : [tableSession],
            DeviceItems = [device],
        };
        return new Fixture(
            context,
            unit,
            employee,
            device,
            new ClientSessionContext(
                Guid.NewGuid(),
                device.Id.Value,
                tableSession?.Id.Value,
                unit.Id.Value,
                table.Id.Value,
                table.Number));
    }

    private sealed record Fixture(
        FakeContext Context,
        RestaurantUnit Unit,
        Employee Employee,
        Device Device,
        ClientSessionContext SessionContext);

    private sealed class FakeContext : IProjetoPizzaDbContext
    {
        public RestaurantUnit[] RestaurantUnitItems { get; init; } = [];
        public OperationSettings[] OperationSettingItems { get; init; } = [];
        public PizzaSettings[] PizzaSettingItems { get; init; } = [];
        public Employee[] EmployeeItems { get; init; } = [];
        public List<Product> ProductItems { get; } = [];
        public List<PizzaSize> PizzaSizeItems { get; } = [];
        public List<PizzaFlavor> PizzaFlavorItems { get; } = [];
        public List<PizzaFlavorPrice> PizzaFlavorPriceItems { get; } = [];
        public List<PizzaCrust> PizzaCrustItems { get; } = [];
        public List<PizzaCrustPrice> PizzaCrustPriceItems { get; } = [];
        public List<Ingredient> IngredientItems { get; } = [];
        public List<PizzaFlavorExtra> PizzaFlavorExtraItems { get; } = [];
        public List<OrderItemPizza> OrderItemPizzaItems { get; } = [];
        public List<OrderItemModifier> OrderItemModifierItems { get; } = [];
        public DiningArea[] DiningAreaItems { get; init; } = [];
        public RestaurantTable[] RestaurantTableItems { get; init; } = [];
        public List<TableSession> TableSessionItems { get; init; } = [];
        public List<Order> OrderItemsData { get; } = [];
        public List<ProductionStation> ProductionStationItems { get; } = [];
        public List<KitchenTicket> KitchenTicketItemsData { get; } = [];
        public List<KitchenTicketItem> KitchenTicketLineItems { get; } = [];
        public List<CashShift> CashShiftItems { get; } = [];
        public Device[] DeviceItems { get; init; } = [];
        public List<DeviceSession> DeviceSessionItems { get; } = [];
        public List<ServiceCallType> ServiceCallTypeItems { get; } = [];
        public List<ServiceCall> ServiceCallItems { get; } = [];
        public List<Bill> BillItems { get; } = [];
        public List<AuditLog> AuditLogItems { get; } = [];
        public int SaveChangesCalls { get; private set; }

        public IQueryable<RestaurantUnit> RestaurantUnits => RestaurantUnitItems.AsQueryable();
        public IQueryable<OperationSettings> OperationSettings => OperationSettingItems.AsQueryable();
        public IQueryable<PizzaSettings> PizzaSettings => PizzaSettingItems.AsQueryable();
        public IQueryable<Employee> Employees => EmployeeItems.AsQueryable();
        public IQueryable<Customer> Customers => Array.Empty<Customer>().AsQueryable();
        public IQueryable<Category> Categories => Array.Empty<Category>().AsQueryable();
        public IQueryable<Product> Products => ProductItems.AsQueryable();
        public List<ProductExtra> ProductExtraItems { get; } = [];
        public IQueryable<ProductExtra> ProductExtras => ProductExtraItems.AsQueryable();
        public IQueryable<ProductImage> ProductImages => Array.Empty<ProductImage>().AsQueryable();
        public IQueryable<PizzaSize> PizzaSizes => PizzaSizeItems.AsQueryable();
        public IQueryable<PizzaFlavor> PizzaFlavors => PizzaFlavorItems.AsQueryable();
        public IQueryable<PizzaFlavorPrice> PizzaFlavorPrices => PizzaFlavorPriceItems.AsQueryable();
        public IQueryable<PizzaCrust> PizzaCrusts => PizzaCrustItems.AsQueryable();
        public IQueryable<PizzaCrustPrice> PizzaCrustPrices => PizzaCrustPriceItems.AsQueryable();
        public IQueryable<Ingredient> Ingredients => IngredientItems.AsQueryable();
        public IQueryable<PizzaFlavorIngredient> PizzaFlavorIngredients => Array.Empty<PizzaFlavorIngredient>().AsQueryable();
        public IQueryable<PizzaFlavorExtra> PizzaFlavorExtras => PizzaFlavorExtraItems.AsQueryable();
        public IQueryable<InventoryItem> InventoryItems => Array.Empty<InventoryItem>().AsQueryable();
        public IQueryable<StockBalance> StockBalances => Array.Empty<StockBalance>().AsQueryable();
        public IQueryable<DiningArea> DiningAreas => DiningAreaItems.AsQueryable();
        public IQueryable<RestaurantTable> RestaurantTables => RestaurantTableItems.AsQueryable();
        public IQueryable<TableSession> TableSessions => TableSessionItems.AsQueryable();
        public IQueryable<TableSessionTable> TableSessionTables =>
            TableSessionItems.SelectMany(session => session.Tables).AsQueryable();
        public IQueryable<ServiceCallType> ServiceCallTypes => ServiceCallTypeItems.AsQueryable();
        public IQueryable<ServiceCall> ServiceCalls => ServiceCallItems.AsQueryable();
        public IQueryable<Order> Orders => OrderItemsData.AsQueryable();
        public IQueryable<OrderItem> OrderItems => OrderItemsData.SelectMany(order => order.Items).AsQueryable();
        public IQueryable<OrderItemPizza> OrderItemPizzas => OrderItemPizzaItems.AsQueryable();
        public IQueryable<OrderItemPizzaFlavor> OrderItemPizzaFlavors => OrderItemPizzaItems.SelectMany(pizza => pizza.Flavors).AsQueryable();
        public IQueryable<OrderItemModifier> OrderItemModifiers => OrderItemModifierItems.AsQueryable();
        public IQueryable<ProductionStation> ProductionStations => ProductionStationItems.AsQueryable();
        public IQueryable<KitchenTicket> KitchenTickets => KitchenTicketItemsData.AsQueryable();
        public IQueryable<KitchenTicketItem> KitchenTicketItems => KitchenTicketLineItems.AsQueryable();
        public IQueryable<Bill> Bills => BillItems.AsQueryable();
        public IQueryable<BillSplit> BillSplits => Array.Empty<BillSplit>().AsQueryable();
        public IQueryable<PaymentMethod> PaymentMethods => Array.Empty<PaymentMethod>().AsQueryable();
        public IQueryable<Payment> Payments => Array.Empty<Payment>().AsQueryable();
        public IQueryable<CashRegister> CashRegisters => Array.Empty<CashRegister>().AsQueryable();
        public IQueryable<CashShift> CashShifts => CashShiftItems.AsQueryable();
        public IQueryable<CashMovement> CashMovements => Array.Empty<CashMovement>().AsQueryable();
        public IQueryable<Device> Devices => DeviceItems.AsQueryable();
        public IQueryable<DeviceSession> DeviceSessions => DeviceSessionItems.AsQueryable();
        public List<DeviceProvisioning> DeviceProvisioningItems { get; } = [];
        public IQueryable<DeviceProvisioning> DeviceProvisionings => DeviceProvisioningItems.AsQueryable();
        public IQueryable<PrintJob> PrintJobs => Array.Empty<PrintJob>().AsQueryable();
        public IQueryable<AuditLog> AuditLogs => AuditLogItems.AsQueryable();

        public void Add<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity is DeviceSession deviceSession) DeviceSessionItems.Add(deviceSession);
            if (entity is TableSession tableSession) TableSessionItems.Add(tableSession);
            if (entity is ProductExtra productExtra) ProductExtraItems.Add(productExtra);
            if (entity is DeviceProvisioning provisioning) DeviceProvisioningItems.Add(provisioning);
            if (entity is ServiceCall serviceCall) ServiceCallItems.Add(serviceCall);
            if (entity is Bill bill) BillItems.Add(bill);
            if (entity is Order order) OrderItemsData.Add(order);
            if (entity is OrderItemPizza pizza) OrderItemPizzaItems.Add(pizza);
            if (entity is OrderItemModifier modifier) OrderItemModifierItems.Add(modifier);
            if (entity is KitchenTicket ticket) KitchenTicketItemsData.Add(ticket);
            if (entity is KitchenTicketItem ticketItem) KitchenTicketLineItems.Add(ticketItem);
            if (entity is AuditLog auditLog) AuditLogItems.Add(auditLog);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls += 1;
            return Task.FromResult(1);
        }
    }
}
