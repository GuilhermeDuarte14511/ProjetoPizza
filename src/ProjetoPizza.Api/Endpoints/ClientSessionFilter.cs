using ProjetoPizza.Application.Client;

namespace ProjetoPizza.Api.Endpoints;

public sealed class ClientSessionFilter(IClientSessionService clientService) : IEndpointFilter
{
    public const string HttpContextKey = "ProjetoPizza.ClientSession";
    private const string HeaderName = "X-Device-Session";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var token = context.HttpContext.Request.Headers[HeaderName].ToString();
        var session = await clientService.ValidateSessionAsync(
            token,
            context.HttpContext.RequestAborted);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        context.HttpContext.Items[HttpContextKey] = session;
        return await next(context);
    }
}
