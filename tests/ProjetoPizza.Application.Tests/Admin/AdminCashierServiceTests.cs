using FluentAssertions;
using ProjetoPizza.Application.Abstractions.Persistence;
using ProjetoPizza.Application.Admin;
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

namespace ProjetoPizza.Application.Tests.Admin;

public sealed class AdminCashierServiceTests
{
    [Fact]
    public async Task OpenCashShift_ShouldUseAuthenticatedEmployeeAndCreateAudit()
    {
        var fixture = CreateFixture();
        var service = new AdminManagementService(fixture.Context);

        var result = await service.OpenCashShiftAsync(
            new OpenCashShiftCommand(fixture.Register.Id.Value, 250m),
            fixture.IdentityUserId,
            CancellationToken.None);

        result.Status.Should().Be("Open");
        result.Register.Should().Be("Caixa Principal");
        result.Operator.Should().Be("Administrador");
        result.OpeningAmount.Should().Be(250m);
        fixture.Context.CashShiftItems.Should().ContainSingle()
            .Which.OperatorEmployeeId.Should().Be(fixture.Employee.Id);
        fixture.Context.AuditLogItems.Should().ContainSingle(log =>
            log.Module == "Cashier" && log.Action == "Open");
        fixture.Context.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task OpenCashShift_WithActiveShift_ShouldBeRejected()
    {
        var fixture = CreateFixture();
        fixture.Context.CashShiftItems.Add(new CashShift(
            CashShiftId.New(),
            fixture.Register.Id,
            fixture.Employee.Id,
            new Money(100m)));
        var service = new AdminManagementService(fixture.Context);

        var action = () => service.OpenCashShiftAsync(
            new OpenCashShiftCommand(fixture.Register.Id.Value, 200m),
            fixture.IdentityUserId,
            CancellationToken.None);

        (await action.Should().ThrowAsync<BusinessRuleException>())
            .Which.Rule.Should().Be("cash_shift.already_open");
        fixture.Context.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task OpenCashShift_WithUnknownRegister_ShouldBeRejected()
    {
        var fixture = CreateFixture();
        var service = new AdminManagementService(fixture.Context);

        var action = () => service.OpenCashShiftAsync(
            new OpenCashShiftCommand(Guid.NewGuid(), 200m),
            fixture.IdentityUserId,
            CancellationToken.None);

        (await action.Should().ThrowAsync<BusinessRuleException>())
            .Which.Rule.Should().Be("cash_register.unavailable");
    }

    [Fact]
    public async Task CreateOrder_Delivery_ShouldUseServerPriceFeeDiscountAndCreateTicket()
    {
        var fixture = CreateFixture();
        var settings = new OperationSettings(fixture.Context.RestaurantUnitItems.Single().Id);
        settings.Update(false, true, true, new Percentage(10), new Money(8), true, true, 5);
        var customer = new Customer(
            CustomerId.New(),
            fixture.Context.RestaurantUnitItems.Single().Id,
            "Ana Souza",
            "(11) 99999-8877",
            new DateOnly(1992, 5, 18));
        var product = new Product(
            ProductId.New(),
            fixture.Context.RestaurantUnitItems.Single().Id,
            CategoryId.New(),
            "REFRI-01",
            "Refrigerante",
            ProductType.Standard,
            new Money(15));
        var station = new ProductionStation(
            ProductionStationId.New(),
            fixture.Context.RestaurantUnitItems.Single().Id,
            "Cozinha quente",
            "HOT",
            15);
        fixture.Context.OperationSettingItems = [settings];
        fixture.Context.CustomerItems = [customer];
        fixture.Context.ProductItems = [product];
        fixture.Context.ProductionStationItems = [station];
        var service = new AdminManagementService(fixture.Context);
        var requestId = Guid.NewGuid();

        var result = await service.CreateOrderAsync(
            new CreateAdministrativeOrderCommand(
                requestId,
                customer.Id.Value,
                "Delivery",
                "Rua das Flores, 27 - Centro",
                3,
                "Entregar na portaria.",
                [new SubmitClientOrderItemCommand(product.Id.Value, 2, "Sem gelo.", null)]),
            fixture.IdentityUserId,
            CancellationToken.None);

        result.Id.Should().Be(requestId);
        result.Total.Should().Be(35);
        result.Receipt.Subtotal.Should().Be(30);
        result.Receipt.DeliveryFee.Should().Be(8);
        result.Receipt.Discount.Should().Be(3);
        result.Receipt.CustomerPhone.Should().Be("11999998877");
        result.Receipt.DeliveryAddress.Should().Be("Rua das Flores, 27 - Centro");
        result.Receipt.Items.Should().ContainSingle().Which.Notes.Should().Be("Sem gelo.");
        fixture.Context.OrderEntities.Should().ContainSingle();
        fixture.Context.KitchenTicketEntities.Should().ContainSingle();
        fixture.Context.KitchenTicketItemEntities.Should().ContainSingle();
        fixture.Context.AuditLogItems.Should().ContainSingle(log => log.Action == "CreateAdministrative");
    }

    private static CashierFixture CreateFixture()
    {
        var identityUserId = Guid.NewGuid();
        var unit = new RestaurantUnit(
            RestaurantUnitId.New(),
            "Unidade Principal",
            "Projeto Pizza LTDA",
            "Forno 27",
            "00.000.000/0001-00");
        var employee = new Employee(
            EmployeeId.New(),
            unit.Id,
            identityUserId,
            "Administrador",
            "admin@local.test",
            "ADMIN");
        var register = new CashRegister(CashRegisterId.New(), unit.Id, "Caixa Principal", "CX-01");
        var context = new FakeContext
        {
            RestaurantUnitItems = [unit],
            EmployeeItems = [employee],
            CashRegisterItems = [register],
        };
        return new CashierFixture(context, identityUserId, employee, register);
    }

    private sealed record CashierFixture(
        FakeContext Context,
        Guid IdentityUserId,
        Employee Employee,
        CashRegister Register);

    private sealed class FakeContext : IProjetoPizzaDbContext
    {
        public RestaurantUnit[] RestaurantUnitItems { get; init; } = [];
        public Employee[] EmployeeItems { get; init; } = [];
        public CashRegister[] CashRegisterItems { get; init; } = [];
        public OperationSettings[] OperationSettingItems { get; set; } = [];
        public Customer[] CustomerItems { get; set; } = [];
        public Product[] ProductItems { get; set; } = [];
        public ProductionStation[] ProductionStationItems { get; set; } = [];
        public List<CashShift> CashShiftItems { get; } = [];
        public List<Order> OrderEntities { get; } = [];
        public List<KitchenTicket> KitchenTicketEntities { get; } = [];
        public List<KitchenTicketItem> KitchenTicketItemEntities { get; } = [];
        public List<AuditLog> AuditLogItems { get; } = [];
        public int SaveChangesCalls { get; private set; }

        public IQueryable<RestaurantUnit> RestaurantUnits => RestaurantUnitItems.AsQueryable();
        public IQueryable<OperationSettings> OperationSettings => OperationSettingItems.AsQueryable();
        public IQueryable<PizzaSettings> PizzaSettings => Array.Empty<PizzaSettings>().AsQueryable();
        public IQueryable<Employee> Employees => EmployeeItems.AsQueryable();
        public IQueryable<Customer> Customers => CustomerItems.AsQueryable();
        public IQueryable<Category> Categories => Array.Empty<Category>().AsQueryable();
        public IQueryable<Product> Products => ProductItems.AsQueryable();
        public IQueryable<ProductExtra> ProductExtras => Array.Empty<ProductExtra>().AsQueryable();
        public IQueryable<ProductImage> ProductImages => Array.Empty<ProductImage>().AsQueryable();
        public IQueryable<PizzaSize> PizzaSizes => Array.Empty<PizzaSize>().AsQueryable();
        public IQueryable<PizzaFlavor> PizzaFlavors => Array.Empty<PizzaFlavor>().AsQueryable();
        public IQueryable<PizzaFlavorPrice> PizzaFlavorPrices => Array.Empty<PizzaFlavorPrice>().AsQueryable();
        public IQueryable<PizzaCrust> PizzaCrusts => Array.Empty<PizzaCrust>().AsQueryable();
        public IQueryable<PizzaCrustPrice> PizzaCrustPrices => Array.Empty<PizzaCrustPrice>().AsQueryable();
        public IQueryable<Ingredient> Ingredients => Array.Empty<Ingredient>().AsQueryable();
        public IQueryable<PizzaFlavorIngredient> PizzaFlavorIngredients => Array.Empty<PizzaFlavorIngredient>().AsQueryable();
        public IQueryable<PizzaFlavorExtra> PizzaFlavorExtras => Array.Empty<PizzaFlavorExtra>().AsQueryable();
        public IQueryable<InventoryItem> InventoryItems => Array.Empty<InventoryItem>().AsQueryable();
        public IQueryable<StockBalance> StockBalances => Array.Empty<StockBalance>().AsQueryable();
        public IQueryable<DiningArea> DiningAreas => Array.Empty<DiningArea>().AsQueryable();
        public IQueryable<RestaurantTable> RestaurantTables => Array.Empty<RestaurantTable>().AsQueryable();
        public IQueryable<TableSession> TableSessions => Array.Empty<TableSession>().AsQueryable();
        public IQueryable<TableSessionTable> TableSessionTables => Array.Empty<TableSessionTable>().AsQueryable();
        public IQueryable<ServiceCallType> ServiceCallTypes => Array.Empty<ServiceCallType>().AsQueryable();
        public IQueryable<ServiceCall> ServiceCalls => Array.Empty<ServiceCall>().AsQueryable();
        public IQueryable<Order> Orders => OrderEntities.AsQueryable();
        public IQueryable<OrderItem> OrderItems => Array.Empty<OrderItem>().AsQueryable();
        public IQueryable<OrderItemPizza> OrderItemPizzas => Array.Empty<OrderItemPizza>().AsQueryable();
        public IQueryable<OrderItemPizzaFlavor> OrderItemPizzaFlavors => Array.Empty<OrderItemPizzaFlavor>().AsQueryable();
        public IQueryable<OrderItemModifier> OrderItemModifiers => Array.Empty<OrderItemModifier>().AsQueryable();
        public IQueryable<ProductionStation> ProductionStations => ProductionStationItems.AsQueryable();
        public IQueryable<KitchenTicket> KitchenTickets => KitchenTicketEntities.AsQueryable();
        public IQueryable<KitchenTicketItem> KitchenTicketItems => KitchenTicketItemEntities.AsQueryable();
        public IQueryable<Bill> Bills => Array.Empty<Bill>().AsQueryable();
        public IQueryable<BillSplit> BillSplits => Array.Empty<BillSplit>().AsQueryable();
        public IQueryable<PaymentMethod> PaymentMethods => Array.Empty<PaymentMethod>().AsQueryable();
        public IQueryable<Payment> Payments => Array.Empty<Payment>().AsQueryable();
        public IQueryable<CashRegister> CashRegisters => CashRegisterItems.AsQueryable();
        public IQueryable<CashShift> CashShifts => CashShiftItems.AsQueryable();
        public IQueryable<CashMovement> CashMovements => Array.Empty<CashMovement>().AsQueryable();
        public IQueryable<Device> Devices => Array.Empty<Device>().AsQueryable();
        public IQueryable<DeviceSession> DeviceSessions => Array.Empty<DeviceSession>().AsQueryable();
        public IQueryable<DeviceProvisioning> DeviceProvisionings => Array.Empty<DeviceProvisioning>().AsQueryable();
        public IQueryable<AuditLog> AuditLogs => AuditLogItems.AsQueryable();

        public void Add<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity is CashShift shift) CashShiftItems.Add(shift);
            if (entity is Order order) OrderEntities.Add(order);
            if (entity is KitchenTicket kitchenTicket) KitchenTicketEntities.Add(kitchenTicket);
            if (entity is KitchenTicketItem kitchenTicketItem) KitchenTicketItemEntities.Add(kitchenTicketItem);
            if (entity is AuditLog auditLog) AuditLogItems.Add(auditLog);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls += 1;
            return Task.FromResult(1);
        }
    }
}
