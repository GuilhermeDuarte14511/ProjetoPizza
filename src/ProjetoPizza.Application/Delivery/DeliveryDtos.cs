using ProjetoPizza.Application.Client;

namespace ProjetoPizza.Application.Delivery;

public sealed record DeliveryCatalogDto(ClientCatalogDto Catalog, decimal DeliveryFee);

public sealed record PlaceDeliveryOrderCommand(
    Guid RequestId,
    string CustomerName,
    string Phone,
    DateOnly BirthDate,
    string Address,
    string? Notes,
    IReadOnlyList<SubmitClientOrderItemCommand> Items,
    string? CouponCode = null,
    int LoyaltyPoints = 0);

public sealed record DeliveryOrderPlacedDto(
    Guid Id,
    long Number,
    string TrackingToken,
    string Status,
    decimal Total);

public sealed record LoyaltyLookupCommand(string Phone, DateOnly BirthDate, decimal OrderAmount = 0, string? CouponCode = null, int LoyaltyPoints = 0);
public sealed record LoyaltyLookupDto(string CustomerName, int Points, DateTimeOffset? ExpiresAt,
    decimal CouponDiscount, decimal LoyaltyDiscount, decimal TotalBenefits);

public sealed record DeliveryTrackingDto(
    long Number,
    string OrderStatus,
    string DeliveryStatus,
    string CustomerName,
    string Address,
    string? DriverName,
    DateTimeOffset PlacedAt,
    DateTimeOffset? DispatchedAt,
    DateTimeOffset? DeliveredAt,
    decimal Total,
    IReadOnlyList<DeliveryTrackingItemDto> Items);

public sealed record DeliveryTrackingItemDto(string Name, int Quantity, string Status);
