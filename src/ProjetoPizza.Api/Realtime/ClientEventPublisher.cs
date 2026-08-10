using Microsoft.AspNetCore.SignalR;

namespace ProjetoPizza.Api.Realtime;

public sealed record ClientResourceChanged(
    string Resource,
    string Action,
    DateTimeOffset OccurredAt);

public interface IClientEventPublisher
{
    Task PublishAsync(ClientResourceChanged notification, CancellationToken cancellationToken);
}

public sealed class ClientEventPublisher(IHubContext<ClientEventsHub> hubContext) : IClientEventPublisher
{
    // Today the installation has one restaurant unit. Keeping the group naming here
    // makes the boundary explicit for multi-unit routing without exposing payload data.
    public static string UnitGroup(Guid unitId) => $"unit:{unitId:N}";

    public Task PublishAsync(ClientResourceChanged notification, CancellationToken cancellationToken) =>
        hubContext.Clients.All.SendAsync("client:changed", notification, cancellationToken);
}
