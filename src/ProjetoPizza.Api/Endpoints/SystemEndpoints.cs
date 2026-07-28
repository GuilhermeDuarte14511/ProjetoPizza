using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace ProjetoPizza.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/api/v1/health", new HealthCheckOptions
        {
            AllowCachingResponses = false
        }).AllowAnonymous().WithTags("System");

        endpoints.MapGet("/api/v1/system/info", (IHostEnvironment environment) =>
        {
            var assembly = Assembly.GetExecutingAssembly().GetName();
            return Results.Ok(new
            {
                service = "ProjetoPizza.Api",
                version = assembly.Version?.ToString() ?? "unknown",
                environment = environment.EnvironmentName,
                utcNow = DateTimeOffset.UtcNow
            });
        }).AllowAnonymous().WithName("GetSystemInfo").WithTags("System");

        return endpoints;
    }
}
