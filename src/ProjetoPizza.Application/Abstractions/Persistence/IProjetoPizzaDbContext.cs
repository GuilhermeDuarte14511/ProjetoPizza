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

namespace ProjetoPizza.Application.Abstractions.Persistence;

public interface IProjetoPizzaDbContext
{
    IQueryable<RestaurantUnit> RestaurantUnits { get; }
    IQueryable<OperationSettings> OperationSettings { get; }
    IQueryable<PizzaSettings> PizzaSettings { get; }
    IQueryable<Employee> Employees { get; }
    IQueryable<Customer> Customers { get; }
    IQueryable<Category> Categories { get; }
    IQueryable<Product> Products { get; }
    IQueryable<ProductExtra> ProductExtras { get; }
    IQueryable<ProductImage> ProductImages { get; }
    IQueryable<PizzaSize> PizzaSizes { get; }
    IQueryable<PizzaFlavor> PizzaFlavors { get; }
    IQueryable<PizzaFlavorPrice> PizzaFlavorPrices { get; }
    IQueryable<PizzaCrust> PizzaCrusts { get; }
    IQueryable<PizzaCrustPrice> PizzaCrustPrices { get; }
    IQueryable<Ingredient> Ingredients { get; }
    IQueryable<PizzaFlavorIngredient> PizzaFlavorIngredients { get; }
    IQueryable<PizzaFlavorExtra> PizzaFlavorExtras { get; }
    IQueryable<InventoryItem> InventoryItems { get; }
    IQueryable<StockBalance> StockBalances { get; }
    IQueryable<StockMovement> StockMovements { get; }
    IQueryable<Recipe> Recipes { get; }
    IQueryable<RecipeItem> RecipeItems { get; }
    IQueryable<DiningArea> DiningAreas { get; }
    IQueryable<RestaurantTable> RestaurantTables { get; }
    IQueryable<TableSession> TableSessions { get; }
    IQueryable<TableSessionTable> TableSessionTables { get; }
    IQueryable<Reservation> Reservations { get; }
    IQueryable<WaitlistEntry> WaitlistEntries { get; }
    IQueryable<ServiceCallType> ServiceCallTypes { get; }
    IQueryable<ServiceCall> ServiceCalls { get; }
    IQueryable<Order> Orders { get; }
    IQueryable<OrderItem> OrderItems { get; }
    IQueryable<OrderItemPizza> OrderItemPizzas { get; }
    IQueryable<OrderItemPizzaFlavor> OrderItemPizzaFlavors { get; }
    IQueryable<OrderItemModifier> OrderItemModifiers { get; }
    IQueryable<ProductionStation> ProductionStations { get; }
    IQueryable<KitchenTicket> KitchenTickets { get; }
    IQueryable<KitchenTicketItem> KitchenTicketItems { get; }
    IQueryable<Bill> Bills { get; }
    IQueryable<BillSplit> BillSplits { get; }
    IQueryable<PaymentMethod> PaymentMethods { get; }
    IQueryable<Payment> Payments { get; }
    IQueryable<CashRegister> CashRegisters { get; }
    IQueryable<CashShift> CashShifts { get; }
    IQueryable<CashMovement> CashMovements { get; }
    IQueryable<Device> Devices { get; }
    IQueryable<DeviceSession> DeviceSessions { get; }
    IQueryable<DeviceProvisioning> DeviceProvisionings { get; }
    IQueryable<PrintJob> PrintJobs { get; }
    IQueryable<AuditLog> AuditLogs { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
