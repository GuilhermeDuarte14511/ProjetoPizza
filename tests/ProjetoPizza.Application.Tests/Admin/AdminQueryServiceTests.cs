using FluentAssertions;
using ProjetoPizza.Application.Abstractions.Persistence;
using ProjetoPizza.Application.Admin;
using ProjetoPizza.Domain.Billing;
using ProjetoPizza.Domain.Audit;
using ProjetoPizza.Domain.Cashier;
using ProjetoPizza.Domain.Catalog;
using ProjetoPizza.Domain.Core;
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

        public IQueryable<RestaurantUnit> RestaurantUnits => Array.Empty<RestaurantUnit>().AsQueryable();
        public IQueryable<OperationSettings> OperationSettings => Array.Empty<OperationSettings>().AsQueryable();
        public IQueryable<PizzaSettings> PizzaSettings => Array.Empty<PizzaSettings>().AsQueryable();
        public IQueryable<Employee> Employees => Array.Empty<Employee>().AsQueryable();
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
        public IQueryable<InventoryItem> InventoryItems => Array.Empty<InventoryItem>().AsQueryable();
        public IQueryable<StockBalance> StockBalances => Array.Empty<StockBalance>().AsQueryable();
        public IQueryable<DiningArea> DiningAreas => DiningAreaItems.AsQueryable();
        public IQueryable<RestaurantTable> RestaurantTables => RestaurantTableItems.AsQueryable();
        public IQueryable<TableSession> TableSessions => TableSessionItems.AsQueryable();
        public IQueryable<TableSessionTable> TableSessionTables => TableSessionItems.SelectMany(session => session.Tables).AsQueryable();
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
        public IQueryable<AuditLog> AuditLogs => Array.Empty<AuditLog>().AsQueryable();

        public void Add<TEntity>(TEntity entity) where TEntity : class { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
