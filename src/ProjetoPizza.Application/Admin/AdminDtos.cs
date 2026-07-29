namespace ProjetoPizza.Application.Admin;

public sealed record DashboardDto(
    decimal SalesToday,
    int OrdersToday,
    decimal AverageTicket,
    int OccupiedTables,
    int TotalTables,
    int OrdersInProduction,
    int PendingServiceCalls,
    IReadOnlyCollection<DashboardOrderDto> RecentOrders);

public sealed record DashboardOrderDto(long Number, string Channel, string Status, decimal Total, DateTimeOffset? PlacedAt);

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
    int? RequestedSplitCount);

public sealed record TableOrderDto(long Number, string Channel, string Status, decimal Total, DateTimeOffset? PlacedAt);
public sealed record CategoryDto(Guid Id, string Name, string Slug, string? Description, bool IsActive, bool IsVisibleOnTablet);
public sealed record ProductDto(
    Guid Id,
    Guid CategoryId,
    string Sku,
    string Name,
    string Type,
    decimal BasePrice,
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
    string Status,
    DateTimeOffset CreatedAt,
    int ItemCount,
    string Summary);
