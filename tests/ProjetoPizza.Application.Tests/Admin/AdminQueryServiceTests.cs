using FluentAssertions;
using ProjetoPizza.Application.Abstractions.Persistence;
using ProjetoPizza.Application.Admin;
using ProjetoPizza.Domain.Billing;
using ProjetoPizza.Domain.Audit;
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

public sealed class AdminQueryServiceTests
{
    [Fact]
    public async Task ListTables_WithoutActiveSession_ShouldReturnFreeStatus()
    {
        var unitId = RestaurantUnitId.New();
        var area = new DiningArea(DiningAreaId.New(), unitId, "Salão Principal");
        var table = new RestaurantTable(RestaurantTableId.New(), unitId, area.Id, 1, 4);
        var context = new FakeContext
        {
            DiningAreaItems = [area],
            RestaurantTableItems = [table]
        };
        var service = new AdminQueryService(context);

        var result = await service.ListTablesAsync(CancellationToken.None);

        result.Should().ContainSingle().Which.Status.Should().Be("Livre");
    }

    [Fact]
    public async Task GetDashboard_WithStockAtMinimum_ShouldReturnRealAlertAndTableBreakdown()
    {
        var unit = new RestaurantUnit(
            RestaurantUnitId.New(),
            "Unidade Principal",
            "Projeto Pizza LTDA",
            "Forno 27",
            "00.000.000/0001-00");
        var area = new DiningArea(DiningAreaId.New(), unit.Id, "Salão Principal");
        var table = new RestaurantTable(RestaurantTableId.New(), unit.Id, area.Id, 1, 4);
        var inventoryItem = new InventoryItem(InventoryItemId.New(), unit.Id, "Mussarela", "INS-MUS", "kg", 5m);
        var balance = new StockBalance(StockBalanceId.New(), inventoryItem.Id);
        balance.ApplyAdjustment(2.5m);
        var context = new FakeContext
        {
            RestaurantUnitItems = [unit],
            DiningAreaItems = [area],
            RestaurantTableItems = [table],
            InventoryItemItems = [inventoryItem],
            StockBalanceItems = [balance]
        };
        var service = new AdminQueryService(context);

        var result = await service.GetDashboardAsync(CancellationToken.None);

        result.TableStatus.Free.Should().Be(1);
        result.TableStatus.Occupied.Should().Be(0);
        result.StockAlerts.Should().ContainSingle().Which.Name.Should().Be("Mussarela");
        result.StockAlerts.Single().AvailableQuantity.Should().Be(2.5m);
    }

    [Fact]
    public async Task GetTable_WithLinkedTables_ShouldReturnNamesOrderedAfterMaterialization()
    {
        var unitId = RestaurantUnitId.New();
        var employeeId = EmployeeId.New();
        var area = new DiningArea(DiningAreaId.New(), unitId, "Salão Principal");
        var tableTwo = new RestaurantTable(RestaurantTableId.New(), unitId, area.Id, 2, 4);
        var tableOne = new RestaurantTable(RestaurantTableId.New(), unitId, area.Id, 1, 4);
        var session = TableSession.Open(
            TableSessionId.New(), unitId, 1, 4, employeeId, new Percentage(10), [tableTwo]);
        session.LinkTable(tableOne, employeeId);
        var order = new Order(
            OrderId.New(), unitId, 27, SalesChannel.DineIn, FulfillmentType.DineIn,
            tableSessionId: session.Id);
        order.SetNotes("Entregar pratos juntos.");
        order.AddItem(OrderItemId.New(), ProductId.New(), "Pizza Média · 2 sabores", 2, new Money(37.50m), notes: "Sem cebola.");
        order.Submit();
        var context = new FakeContext
        {
            DiningAreaItems = [area],
            RestaurantTableItems = [tableTwo, tableOne],
            TableSessionItems = [session],
            OrderItemsData = [order]
        };
        var service = new AdminQueryService(context);

        var result = await service.GetTableAsync(tableTwo.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.LinkedTables.Select(table => table.Name).Should().ContainInOrder("Mesa 01", "Mesa 02");
        result.Orders.Should().ContainSingle();
        result.Orders.Single().Number.Should().Be(27);
        result.Orders.Single().Notes.Should().Be("Entregar pratos juntos.");
        result.Orders.Single().Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Name = "Pizza Média · 2 sabores",
            Quantity = 2,
            UnitPrice = 37.50m,
            TotalPrice = 75m,
            Notes = "Sem cebola."
        });
    }

    private sealed class FakeContext : IProjetoPizzaDbContext
    {
        public Category[] CategoryItems { get; init; } = [];
        public Product[] ProductItems { get; init; } = [];
        public PizzaSize[] PizzaSizeItems { get; init; } = [];
        public PizzaFlavor[] PizzaFlavorItems { get; init; } = [];
        public DiningArea[] DiningAreaItems { get; init; } = [];
        public RestaurantTable[] RestaurantTableItems { get; init; } = [];
        public TableSession[] TableSessionItems { get; init; } = [];
        public ServiceCall[] ServiceCallItems { get; init; } = [];
        public Order[] OrderItemsData { get; init; } = [];
        public ProductionStation[] ProductionStationItems { get; init; } = [];
        public KitchenTicket[] KitchenTicketItemsData { get; init; } = [];
        public KitchenTicketItem[] KitchenTicketLineItems { get; init; } = [];
        public Bill[] BillItemsData { get; init; } = [];
        public RestaurantUnit[] RestaurantUnitItems { get; init; } = [];
        public InventoryItem[] InventoryItemItems { get; init; } = [];
        public StockBalance[] StockBalanceItems { get; init; } = [];

        public IQueryable<RestaurantUnit> RestaurantUnits => RestaurantUnitItems.AsQueryable();
        public IQueryable<OperationSettings> OperationSettings => Array.Empty<OperationSettings>().AsQueryable();
        public IQueryable<PizzaSettings> PizzaSettings => Array.Empty<PizzaSettings>().AsQueryable();
        public IQueryable<Employee> Employees => Array.Empty<Employee>().AsQueryable();
        public IQueryable<Customer> Customers => Array.Empty<Customer>().AsQueryable();
        public IQueryable<Category> Categories => CategoryItems.AsQueryable();
        public IQueryable<Product> Products => ProductItems.AsQueryable();
        public IQueryable<ProductExtra> ProductExtras => Array.Empty<ProductExtra>().AsQueryable();
        public IQueryable<ProductImage> ProductImages => Array.Empty<ProductImage>().AsQueryable();
        public IQueryable<PizzaSize> PizzaSizes => PizzaSizeItems.AsQueryable();
        public IQueryable<PizzaFlavor> PizzaFlavors => PizzaFlavorItems.AsQueryable();
        public IQueryable<PizzaFlavorPrice> PizzaFlavorPrices => Array.Empty<PizzaFlavorPrice>().AsQueryable();
        public IQueryable<PizzaCrust> PizzaCrusts => Array.Empty<PizzaCrust>().AsQueryable();
        public IQueryable<PizzaCrustPrice> PizzaCrustPrices => Array.Empty<PizzaCrustPrice>().AsQueryable();
        public IQueryable<Ingredient> Ingredients => Array.Empty<Ingredient>().AsQueryable();
        public IQueryable<PizzaFlavorIngredient> PizzaFlavorIngredients => Array.Empty<PizzaFlavorIngredient>().AsQueryable();
        public IQueryable<PizzaFlavorExtra> PizzaFlavorExtras => Array.Empty<PizzaFlavorExtra>().AsQueryable();
        public IQueryable<InventoryItem> InventoryItems => InventoryItemItems.AsQueryable();
        public IQueryable<StockBalance> StockBalances => StockBalanceItems.AsQueryable();
        public IQueryable<StockMovement> StockMovements => Array.Empty<StockMovement>().AsQueryable();
        public IQueryable<Recipe> Recipes => Array.Empty<Recipe>().AsQueryable();
        public IQueryable<RecipeItem> RecipeItems => Array.Empty<RecipeItem>().AsQueryable();
        public IQueryable<DiningArea> DiningAreas => DiningAreaItems.AsQueryable();
        public IQueryable<RestaurantTable> RestaurantTables => RestaurantTableItems.AsQueryable();
        public IQueryable<TableSession> TableSessions => TableSessionItems.AsQueryable();
        public IQueryable<TableSessionTable> TableSessionTables => TableSessionItems.SelectMany(session => session.Tables).AsQueryable();
        public IQueryable<Reservation> Reservations => Array.Empty<Reservation>().AsQueryable();
        public IQueryable<WaitlistEntry> WaitlistEntries => Array.Empty<WaitlistEntry>().AsQueryable();
        public IQueryable<ServiceCallType> ServiceCallTypes => Array.Empty<ServiceCallType>().AsQueryable();
        public IQueryable<ServiceCall> ServiceCalls => ServiceCallItems.AsQueryable();
        public IQueryable<Order> Orders => OrderItemsData.AsQueryable();
        public IQueryable<OrderItem> OrderItems => OrderItemsData.SelectMany(order => order.Items).AsQueryable();
        public IQueryable<OrderItemPizza> OrderItemPizzas => Array.Empty<OrderItemPizza>().AsQueryable();
        public IQueryable<OrderItemPizzaFlavor> OrderItemPizzaFlavors => Array.Empty<OrderItemPizzaFlavor>().AsQueryable();
        public IQueryable<OrderItemModifier> OrderItemModifiers => Array.Empty<OrderItemModifier>().AsQueryable();
        public IQueryable<ProductionStation> ProductionStations => ProductionStationItems.AsQueryable();
        public IQueryable<KitchenTicket> KitchenTickets => KitchenTicketItemsData.AsQueryable();
        public IQueryable<KitchenTicketItem> KitchenTicketItems => KitchenTicketLineItems.AsQueryable();
        public IQueryable<Bill> Bills => BillItemsData.AsQueryable();
        public IQueryable<BillSplit> BillSplits => Array.Empty<BillSplit>().AsQueryable();
        public IQueryable<PaymentMethod> PaymentMethods => Array.Empty<PaymentMethod>().AsQueryable();
        public IQueryable<Payment> Payments => Array.Empty<Payment>().AsQueryable();
        public IQueryable<CashRegister> CashRegisters => Array.Empty<CashRegister>().AsQueryable();
        public IQueryable<CashShift> CashShifts => Array.Empty<CashShift>().AsQueryable();
        public IQueryable<CashMovement> CashMovements => Array.Empty<CashMovement>().AsQueryable();
        public IQueryable<Device> Devices => Array.Empty<Device>().AsQueryable();
        public IQueryable<DeviceSession> DeviceSessions => Array.Empty<DeviceSession>().AsQueryable();
        public IQueryable<DeviceProvisioning> DeviceProvisionings => Array.Empty<DeviceProvisioning>().AsQueryable();
        public IQueryable<PrintJob> PrintJobs => Array.Empty<PrintJob>().AsQueryable();
        public IQueryable<AuditLog> AuditLogs => Array.Empty<AuditLog>().AsQueryable();

        public void Add<TEntity>(TEntity entity) where TEntity : class { }
        public void Remove<TEntity>(TEntity entity) where TEntity : class { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
