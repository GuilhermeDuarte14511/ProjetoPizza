using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProjetoPizza.Application.Abstractions.Persistence;
using ProjetoPizza.Domain.Audit;
using ProjetoPizza.Domain.Billing;
using ProjetoPizza.Domain.Cashier;
using ProjetoPizza.Domain.Catalog;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Devices;
using ProjetoPizza.Domain.Dining;
using ProjetoPizza.Domain.Identity;
using ProjetoPizza.Domain.Inventory;
using ProjetoPizza.Domain.Notifications;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.Production;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Infrastructure.Persistence;

public sealed class ProjetoPizzaDbContext(DbContextOptions<ProjetoPizzaDbContext> options)
    : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>(options), IProjetoPizzaDbContext
{
    public DbSet<RestaurantUnit> RestaurantUnits => Set<RestaurantUnit>();
    public DbSet<OperationSettings> OperationSettings => Set<OperationSettings>();
    public DbSet<PizzaSettings> PizzaSettings => Set<PizzaSettings>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductExtra> ProductExtras => Set<ProductExtra>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<PizzaSize> PizzaSizes => Set<PizzaSize>();
    public DbSet<PizzaFlavor> PizzaFlavors => Set<PizzaFlavor>();
    public DbSet<PizzaFlavorPrice> PizzaFlavorPrices => Set<PizzaFlavorPrice>();
    public DbSet<PizzaCrust> PizzaCrusts => Set<PizzaCrust>();
    public DbSet<PizzaCrustPrice> PizzaCrustPrices => Set<PizzaCrustPrice>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<PizzaFlavorIngredient> PizzaFlavorIngredients => Set<PizzaFlavorIngredient>();
    public DbSet<PizzaFlavorExtra> PizzaFlavorExtras => Set<PizzaFlavorExtra>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();
    public DbSet<DiningArea> DiningAreas => Set<DiningArea>();
    public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();
    public DbSet<TableSession> TableSessions => Set<TableSession>();
    public DbSet<TableSessionTable> TableSessionTables => Set<TableSessionTable>();
    public DbSet<WaiterAssignment> WaiterAssignments => Set<WaiterAssignment>();
    public DbSet<ServiceCallType> ServiceCallTypes => Set<ServiceCallType>();
    public DbSet<ServiceCall> ServiceCalls => Set<ServiceCall>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemPizza> OrderItemPizzas => Set<OrderItemPizza>();
    public DbSet<OrderItemPizzaFlavor> OrderItemPizzaFlavors => Set<OrderItemPizzaFlavor>();
    public DbSet<OrderItemModifier> OrderItemModifiers => Set<OrderItemModifier>();
    public DbSet<ProductionStation> ProductionStations => Set<ProductionStation>();
    public DbSet<KitchenTicket> KitchenTickets => Set<KitchenTicket>();
    public DbSet<KitchenTicketItem> KitchenTicketItems => Set<KitchenTicketItem>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillItem> BillItems => Set<BillItem>();
    public DbSet<BillSplit> BillSplits => Set<BillSplit>();
    public DbSet<BillSplitItem> BillSplitItems => Set<BillSplitItem>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<CashShift> CashShifts => Set<CashShift>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceSession> DeviceSessions => Set<DeviceSession>();
    public DbSet<DeviceProvisioning> DeviceProvisionings => Set<DeviceProvisioning>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    IQueryable<Category> IProjetoPizzaDbContext.Categories => Categories;
    IQueryable<RestaurantUnit> IProjetoPizzaDbContext.RestaurantUnits => RestaurantUnits;
    IQueryable<OperationSettings> IProjetoPizzaDbContext.OperationSettings => OperationSettings;
    IQueryable<PizzaSettings> IProjetoPizzaDbContext.PizzaSettings => PizzaSettings;
    IQueryable<Employee> IProjetoPizzaDbContext.Employees => Employees;
    IQueryable<Product> IProjetoPizzaDbContext.Products => Products;
    IQueryable<ProductExtra> IProjetoPizzaDbContext.ProductExtras => ProductExtras;
    IQueryable<ProductImage> IProjetoPizzaDbContext.ProductImages => ProductImages;
    IQueryable<PizzaSize> IProjetoPizzaDbContext.PizzaSizes => PizzaSizes;
    IQueryable<PizzaFlavor> IProjetoPizzaDbContext.PizzaFlavors => PizzaFlavors;
    IQueryable<PizzaFlavorPrice> IProjetoPizzaDbContext.PizzaFlavorPrices => PizzaFlavorPrices;
    IQueryable<PizzaCrust> IProjetoPizzaDbContext.PizzaCrusts => PizzaCrusts;
    IQueryable<PizzaCrustPrice> IProjetoPizzaDbContext.PizzaCrustPrices => PizzaCrustPrices;
    IQueryable<Ingredient> IProjetoPizzaDbContext.Ingredients => Ingredients;
    IQueryable<PizzaFlavorIngredient> IProjetoPizzaDbContext.PizzaFlavorIngredients => PizzaFlavorIngredients;
    IQueryable<PizzaFlavorExtra> IProjetoPizzaDbContext.PizzaFlavorExtras => PizzaFlavorExtras;
    IQueryable<InventoryItem> IProjetoPizzaDbContext.InventoryItems => InventoryItems;
    IQueryable<StockBalance> IProjetoPizzaDbContext.StockBalances => StockBalances;
    IQueryable<DiningArea> IProjetoPizzaDbContext.DiningAreas => DiningAreas;
    IQueryable<RestaurantTable> IProjetoPizzaDbContext.RestaurantTables => RestaurantTables;
    IQueryable<TableSession> IProjetoPizzaDbContext.TableSessions => TableSessions;
    IQueryable<TableSessionTable> IProjetoPizzaDbContext.TableSessionTables => TableSessionTables;
    IQueryable<ServiceCallType> IProjetoPizzaDbContext.ServiceCallTypes => ServiceCallTypes;
    IQueryable<ServiceCall> IProjetoPizzaDbContext.ServiceCalls => ServiceCalls;
    IQueryable<Order> IProjetoPizzaDbContext.Orders => Orders;
    IQueryable<OrderItem> IProjetoPizzaDbContext.OrderItems => OrderItems;
    IQueryable<OrderItemPizza> IProjetoPizzaDbContext.OrderItemPizzas => OrderItemPizzas;
    IQueryable<OrderItemPizzaFlavor> IProjetoPizzaDbContext.OrderItemPizzaFlavors => OrderItemPizzaFlavors;
    IQueryable<OrderItemModifier> IProjetoPizzaDbContext.OrderItemModifiers => OrderItemModifiers;
    IQueryable<ProductionStation> IProjetoPizzaDbContext.ProductionStations => ProductionStations;
    IQueryable<KitchenTicket> IProjetoPizzaDbContext.KitchenTickets => KitchenTickets;
    IQueryable<KitchenTicketItem> IProjetoPizzaDbContext.KitchenTicketItems => KitchenTicketItems;
    IQueryable<Bill> IProjetoPizzaDbContext.Bills => Bills;
    IQueryable<BillSplit> IProjetoPizzaDbContext.BillSplits => BillSplits;
    IQueryable<PaymentMethod> IProjetoPizzaDbContext.PaymentMethods => PaymentMethods;
    IQueryable<Payment> IProjetoPizzaDbContext.Payments => Payments;
    IQueryable<CashRegister> IProjetoPizzaDbContext.CashRegisters => CashRegisters;
    IQueryable<CashShift> IProjetoPizzaDbContext.CashShifts => CashShifts;
    IQueryable<CashMovement> IProjetoPizzaDbContext.CashMovements => CashMovements;
    IQueryable<Device> IProjetoPizzaDbContext.Devices => Devices;
    IQueryable<DeviceSession> IProjetoPizzaDbContext.DeviceSessions => DeviceSessions;
    IQueryable<DeviceProvisioning> IProjetoPizzaDbContext.DeviceProvisionings => DeviceProvisionings;
    IQueryable<AuditLog> IProjetoPizzaDbContext.AuditLogs => AuditLogs;

    void IProjetoPizzaDbContext.Add<TEntity>(TEntity entity) => Add(entity);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { ConstraintName: "ix_cash_shifts_single_active" })
        {
            throw new BusinessRuleException("cash_shift.already_open", "An open cash shift already exists.");
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasSequence<long>("order_number_sequence", "ordering");
        builder.HasSequence<long>("kitchen_ticket_number_sequence", "production");
        builder.HasSequence<long>("table_session_number_sequence", "dining");
        builder.ApplyConfigurationsFromAssembly(typeof(ProjetoPizzaDbContext).Assembly);
        ConfigureIdentity(builder);
    }

    private static void ConfigureIdentity(ModelBuilder builder)
    {
        builder.Entity<IdentityUser<Guid>>().ToTable("users", "identity");
        builder.Entity<IdentityRole<Guid>>().ToTable("roles", "identity");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles", "identity");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims", "identity");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins", "identity");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims", "identity");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens", "identity");
    }
}
