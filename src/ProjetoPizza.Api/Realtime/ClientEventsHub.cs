using Microsoft.AspNetCore.SignalR;
using ProjetoPizza.Application.Client;

namespace ProjetoPizza.Api.Realtime;

public sealed class ClientEventsHub(IClientSessionService sessionService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var token = Context.GetHttpContext()?.Request.Query["device_token"].ToString();
        var session = string.IsNullOrWhiteSpace(token)
            ? null
            : await sessionService.ValidateSessionAsync(token, Context.ConnectionAborted);

        if (session is null)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            ClientEventPublisher.UnitGroup(session.RestaurantUnitId),
            Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }
}
