using Microsoft.AspNetCore.RateLimiting;
using ProjetoPizza.Application.Identity;

namespace ProjetoPizza.Api.Endpoints;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/auth/login", async (
                LoginCommand command,
                IIdentityAccessService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.AuthenticateAsync(command, cancellationToken);
                return result is null ? Results.Unauthorized() : Results.Ok(result);
            })
            .AllowAnonymous()
            .RequireRateLimiting("Login")
            .WithTags("Authentication")
            .WithName("Login");
        return endpoints;
    }
}
