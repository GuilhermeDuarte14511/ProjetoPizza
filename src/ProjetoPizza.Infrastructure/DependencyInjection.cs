using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjetoPizza.Application.Abstractions.Persistence;
using ProjetoPizza.Application.Identity;
using ProjetoPizza.Infrastructure.Identity;
using ProjetoPizza.Infrastructure.Persistence;

namespace ProjetoPizza.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSql")
            ?? throw new InvalidOperationException("ConnectionStrings:PostgreSql is required.");

        services.AddDbContext<ProjetoPizzaDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsAssembly(typeof(ProjetoPizzaDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention());
        services.AddScoped<IProjetoPizzaDbContext>(provider => provider.GetRequiredService<ProjetoPizzaDbContext>());
        services.AddIdentityCore<IdentityUser<Guid>>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ProjetoPizzaDbContext>();
        services.AddScoped<IIdentityAccessService, IdentityAccessService>();
        services.AddScoped<DevelopmentDataSeeder>();
        return services;
    }
}
