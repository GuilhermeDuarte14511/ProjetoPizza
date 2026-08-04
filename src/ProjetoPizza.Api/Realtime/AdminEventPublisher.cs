using Microsoft.AspNetCore.SignalR;

namespace ProjetoPizza.Api.Realtime;

public sealed record AdminResourceChanged(
    string Resource,
    string Action,
    string Source,
    DateTimeOffset OccurredAt);

public interface IAdminEventPublisher
{
    Task PublishAsync(AdminResourceChanged notification, CancellationToken cancellationToken);
}

public sealed class AdminEventPublisher(IHubContext<AdminEventsHub> hubContext) : IAdminEventPublisher
{
    public Task PublishAsync(AdminResourceChanged notification, CancellationToken cancellationToken) =>
        hubContext.Clients.All.SendAsync("admin:changed", notification, cancellationToken);
}
