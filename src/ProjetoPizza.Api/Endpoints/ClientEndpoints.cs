using ProjetoPizza.Api.Realtime;
using ProjetoPizza.Application.Client;

namespace ProjetoPizza.Api.Endpoints;

public static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/client/sessions", async (
                ActivateClientSessionCommand command,
                IClientSessionService service,
                CancellationToken cancellationToken) =>
            {
                var activation = await service.ActivateAsync(command, cancellationToken);
                return Results.Created("/api/v1/client/bootstrap", activation);
            })
            .AllowAnonymous()
            .RequireRateLimiting("DeviceActivation")
            .WithName("ActivateClientSession")
            .WithTags("Client");

        var sessionGroup = endpoints.MapGroup("/api/v1/client")
            .WithTags("Client")
            .AddEndpointFilter<ClientSessionFilter>()
            .AddEndpointFilter<AdminRealtimeFilter>();

        sessionGroup.MapGet("/bootstrap", (
            HttpContext httpContext,
            IClientQueryService service,
            CancellationToken cancellationToken) =>
            service.GetBootstrapAsync(GetSession(httpContext), cancellationToken));

        sessionGroup.MapGet("/state", (
            HttpContext httpContext,
            IClientQueryService service,
            CancellationToken cancellationToken) =>
            service.GetStateAsync(GetSession(httpContext), cancellationToken));

        sessionGroup.MapPost("/table-sessions", (
            StartClientTableSessionCommand command,
            HttpContext httpContext,
            IClientSessionService service,
            CancellationToken cancellationToken) =>
            service.StartTableSessionAsync(GetSession(httpContext), command, cancellationToken));

        sessionGroup.MapPost("/table-sessions/complete", (
            HttpContext httpContext,
            IClientSessionService service,
            CancellationToken cancellationToken) =>
            service.CompleteTableSessionAsync(GetSession(httpContext), cancellationToken));

        sessionGroup.MapPost("/logout", async (
            HttpContext httpContext,
            IClientSessionService service,
            CancellationToken cancellationToken) =>
        {
            await service.LogoutAsync(GetSession(httpContext), cancellationToken);
            return Results.NoContent();
        });

        sessionGroup.MapPost("/orders", async (
            SubmitClientOrderCommand command,
            HttpContext httpContext,
            IClientOrderingService service,
            CancellationToken cancellationToken) =>
        {
            var order = await service.SubmitOrderAsync(
                GetSession(httpContext),
                command,
                cancellationToken);
            return Results.Created($"/api/v1/client/orders/{order.Id}", order);
        });

        sessionGroup.MapPost("/service-calls", async (
            CreateClientServiceCallCommand command,
            HttpContext httpContext,
            IClientAssistanceService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CreateServiceCallAsync(
                GetSession(httpContext),
                command,
                cancellationToken);
            return Results.Created($"/api/v1/client/service-calls/{result.Id}", result);
        });

        sessionGroup.MapPost("/bill-requests", (
            RequestClientBillCommand command,
            HttpContext httpContext,
            IClientAssistanceService service,
            CancellationToken cancellationToken) =>
            service.RequestBillAsync(GetSession(httpContext), command, cancellationToken));

        return endpoints;
    }

    private static ClientSessionContext GetSession(HttpContext httpContext) =>
        httpContext.Items[ClientSessionFilter.HttpContextKey] as ClientSessionContext
        ?? throw new UnauthorizedAccessException("Tablet session is missing.");
}
