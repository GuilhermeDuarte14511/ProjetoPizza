using ProjetoPizza.Domain.Audit;
using ProjetoPizza.Domain.Billing;
using ProjetoPizza.Domain.Cashier;
using ProjetoPizza.Domain.Catalog;
using ProjetoPizza.Domain.Core;
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
    IQueryable<Category> Categories { get; }
    IQueryable<Product> Products { get; }
    IQueryable<PizzaSize> PizzaSizes { get; }
    IQueryable<PizzaFlavor> PizzaFlavors { get; }
    IQueryable<PizzaCrust> PizzaCrusts { get; }
    IQueryable<InventoryItem> InventoryItems { get; }
    IQueryable<StockBalance> StockBalances { get; }
    IQueryable<DiningArea> DiningAreas { get; }
    IQueryable<RestaurantTable> RestaurantTables { get; }
    IQueryable<TableSession> TableSessions { get; }
    IQueryable<TableSessionTable> TableSessionTables { get; }
    IQueryable<ServiceCallType> ServiceCallTypes { get; }
    IQueryable<ServiceCall> ServiceCalls { get; }
    IQueryable<Order> Orders { get; }
    IQueryable<OrderItem> OrderItems { get; }
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
    IQueryable<AuditLog> AuditLogs { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
