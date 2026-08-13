namespace ProjetoPizza.Application.Admin;

public sealed record DashboardDto(
    decimal SalesToday,
    int OrdersToday,
    decimal AverageTicket,
    int OccupiedTables,
    int TotalTables,
    int OrdersInProduction,
    int PendingServiceCalls,
    IReadOnlyCollection<DashboardOrderDto> RecentOrders,
    DashboardTableStatusDto TableStatus,
    IReadOnlyCollection<DashboardProductDto> TopProducts,
    IReadOnlyCollection<DashboardPaymentMethodDto> PaymentMethods,
    IReadOnlyCollection<DashboardStockAlertDto> StockAlerts);

public sealed record DashboardOrderDto(long Number, string Channel, string Status, decimal Total, DateTimeOffset? PlacedAt);
public sealed record DashboardTableStatusDto(int Free, int Occupied, int Calling, int AwaitingPayment);
public sealed record DashboardProductDto(string Name, int Quantity);
public sealed record DashboardPaymentMethodDto(string Name, decimal Total, decimal Percentage);
public sealed record DashboardStockAlertDto(Guid InventoryItemId, string Name, decimal AvailableQuantity, decimal MinimumStock, string UnitOfMeasure);

public sealed record TableSummaryDto(
    Guid Id,
    int Number,
    string Name,
    int Capacity,
    string Area,
    string Status,
    int? GuestCount,
    DateTimeOffset? OpenedAt,
    decimal CurrentTotal,
    bool HasPendingCall);

public sealed record TableDetailDto(
    TableSummaryDto Table,
    Guid? SessionId,
    long? SessionNumber,
    string? Waiter,
    IReadOnlyCollection<TableOrderDto> Orders,
    Guid? BillId,
    decimal SubtotalAmount,
    decimal ServiceFeePercentage,
    decimal ServiceFeeAmount,
    decimal TotalAmount,
    decimal RemainingAmount,
    int? RequestedSplitCount,
    IReadOnlyCollection<TableBillItemDto> BillItems,
    IReadOnlyCollection<TableReferenceDto> LinkedTables,
    IReadOnlyCollection<TableOperatorDto> Waiters);

public sealed record TableOrderDto(
    Guid Id,
    long Number,
    string Channel,
    string Status,
    decimal Subtotal,
    decimal Discount,
    decimal ServiceFee,
    decimal Total,
    DateTimeOffset? PlacedAt,
    string? Notes,
    IReadOnlyCollection<TableOrderItemDto> Items);
public sealed record TableOrderItemDto(
    Guid Id,
    string Name,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string? Notes,
    IReadOnlyCollection<string> Details);
public sealed record TableReferenceDto(Guid Id, string Name, bool IsPrimary);
public sealed record TableOperatorDto(Guid Id, string Name);
public sealed record TableBillItemDto(Guid Id, string Name, int Quantity, decimal Total);
public sealed record CategoryDto(Guid Id, string Name, string Slug, string? Description, bool IsActive, bool IsVisibleOnTablet);
public sealed record ProductDto(
    Guid Id,
    Guid CategoryId,
    string Sku,
    string Name,
    string? Description,
    string Type,
    decimal BasePrice,
    int PreparationTimeMinutes,
    string? ImageUrl,
    bool IsActive,
    bool IsAvailable,
    bool IsFeatured,
    bool UsesCustomExtras,
    IReadOnlyCollection<ProductExtraDto> Complements);
public sealed record ProductExtraDto(
    Guid IngredientId,
    string Name,
    decimal Price,
    int MaxQuantity);
public sealed record PizzaSizeDto(Guid Id, string Name, string ShortName, int Slices, decimal DiameterCm, decimal BasePrice, int MaxFlavors, bool IsActive);
public sealed record PizzaFlavorDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    string? Description,
    string Type,
    bool IsPremium,
    bool IsVegetarian,
    bool IsActive,
    bool IsAvailable,
    string? SoldOutReason,
    string? ImageUrl,
    IReadOnlyCollection<PizzaFlavorExtraDto> Extras);
public sealed record PizzaFlavorExtraDto(
    Guid IngredientId,
    string IngredientName,
    decimal Price,
    int MaxQuantity);
public sealed record ServiceCallDto(
    Guid Id,
    Guid TableSessionId,
    Guid TableId,
    int TableNumber,
    string TableName,
    string TypeCode,
    string TypeName,
    string Status,
    string? Details,
    string? AssignedEmployee,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AcknowledgedAt);
public sealed record KitchenTicketDto(
    Guid Id,
    long TicketNumber,
    long OrderNumber,
    string Station,
    string StationCode,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    int TargetPreparationMinutes,
    int ItemCount,
    string Summary);
