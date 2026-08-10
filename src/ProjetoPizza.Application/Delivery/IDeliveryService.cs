namespace ProjetoPizza.Application.Delivery;

public interface IDeliveryService
{
    Task<DeliveryCatalogDto> GetCatalogAsync(CancellationToken cancellationToken);
    Task<DeliveryOrderPlacedDto> PlaceOrderAsync(PlaceDeliveryOrderCommand command, CancellationToken cancellationToken);
    Task<DeliveryTrackingDto?> TrackAsync(string trackingToken, CancellationToken cancellationToken);
}
