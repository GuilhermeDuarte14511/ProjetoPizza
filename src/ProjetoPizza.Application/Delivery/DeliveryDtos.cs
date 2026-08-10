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
    IReadOnlyList<SubmitClientOrderItemCommand> Items);

public sealed record DeliveryOrderPlacedDto(
    Guid Id,
    long Number,
    string TrackingToken,
    string Status,
    decimal Total);

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
