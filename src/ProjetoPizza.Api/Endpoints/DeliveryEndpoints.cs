using ProjetoPizza.Application.Delivery;

namespace ProjetoPizza.Api.Endpoints;

public static class DeliveryEndpoints
{
    public static IEndpointRouteBuilder MapDeliveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/delivery")
            .WithTags("Delivery")
            .AddEndpointFilter<ProjetoPizza.Api.Realtime.AdminRealtimeFilter>();

        group.MapGet("/catalog", (IDeliveryService service, CancellationToken cancellationToken) =>
            service.GetCatalogAsync(cancellationToken));
        group.MapPost("/orders", async (
            PlaceDeliveryOrderCommand command,
            IDeliveryService service,
            CancellationToken cancellationToken) =>
        {
            var order = await service.PlaceOrderAsync(command, cancellationToken);
            return Results.Created($"/api/v1/delivery/tracking/{order.TrackingToken}", order);
        }).RequireRateLimiting("PublicDelivery");
        group.MapGet("/tracking/{token}", async (
            string token,
            IDeliveryService service,
            CancellationToken cancellationToken) =>
        {
            var tracking = await service.TrackAsync(token, cancellationToken);
            return tracking is null ? Results.NotFound() : Results.Ok(tracking);
        }).RequireRateLimiting("PublicDelivery");
        return endpoints;
    }
}
