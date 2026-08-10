namespace ProjetoPizza.Api.Realtime;

public sealed class AdminRealtimeFilter(
    IAdminEventPublisher adminPublisher,
    IClientEventPublisher clientPublisher) : IEndpointFilter
{
    private static readonly HashSet<string> MutatingMethods =
        [HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete];

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);
        var request = context.HttpContext.Request;

        if (!MutatingMethods.Contains(request.Method) ||
            context.HttpContext.Response.StatusCode >= StatusCodes.Status400BadRequest)
        {
            return result;
        }

        var segments = request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var resource = segments.Length >= 4
            ? segments[3]
            : segments.LastOrDefault() ?? "admin";
        var source = segments.Length >= 3
            ? segments[2]
            : "unknown";

        await adminPublisher.PublishAsync(
            new AdminResourceChanged(resource, request.Method, source, DateTimeOffset.UtcNow),
            context.HttpContext.RequestAborted);

        await clientPublisher.PublishAsync(
            new ClientResourceChanged(resource, request.Method, DateTimeOffset.UtcNow),
            context.HttpContext.RequestAborted);

        return result;
    }
}
