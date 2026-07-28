using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjetoPizza.Api.Endpoints;
using ProjetoPizza.Api.ErrorHandling;
using ProjetoPizza.Api.Health;
using ProjetoPizza.Api.Realtime;
using ProjetoPizza.Application.Admin;
using ProjetoPizza.Infrastructure;
using ProjetoPizza.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IAdminEventPublisher, AdminEventPublisher>();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("Login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgresql");
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAdminQueryService, AdminQueryService>();
builder.Services.AddScoped<IAdminManagementService, AdminManagementService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var authority = builder.Configuration["Authentication:Authority"];
        if (!string.IsNullOrWhiteSpace(authority))
        {
            options.Authority = authority;
        }

        var audience = builder.Configuration["Authentication:Audience"] ?? "projeto-pizza-web";
        options.Audience = audience;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        var signingKey = builder.Configuration["Authentication:SigningKey"];
        if (!string.IsNullOrWhiteSpace(signingKey))
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Authentication:Issuer"] ?? "ProjetoPizza.Api",
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        }

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs/admin"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminAccess", policy => policy.RequireAuthenticatedUser().RequireClaim("permission", "admin:read"));
    options.AddPolicy("AdminWrite", policy => policy.RequireAuthenticatedUser().RequireClaim("permission", "admin:write"));
    options.AddPolicy("OperationsAccess", policy => policy.RequireAuthenticatedUser().RequireClaim("permission", "operations:read"));
    options.AddPolicy("OperationsWrite", policy => policy.RequireAuthenticatedUser().RequireClaim("permission", "operations:write"));
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentWeb", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseCors("DevelopmentWeb");
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapSystemEndpoints();
app.MapAuthenticationEndpoints();
app.MapAdminEndpoints();
app.MapHub<AdminEventsHub>("/hubs/admin");

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase) ||
    args.Contains("--seed", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ProjetoPizzaDbContext>();
    await dbContext.Database.MigrateAsync();

    if (args.Contains("--seed", StringComparer.OrdinalIgnoreCase))
    {
        await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
    }

    return;
}

app.Run();

public partial class Program;
