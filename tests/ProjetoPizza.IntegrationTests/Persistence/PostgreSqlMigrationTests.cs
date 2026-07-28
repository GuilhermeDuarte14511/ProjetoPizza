using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjetoPizza.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ProjetoPizza.IntegrationTests.Persistence;

public sealed class PostgreSqlMigrationTests
{
    [Fact(Skip = "Requer um daemon Docker. Execute com Docker disponível removendo temporariamente o Skip.")]
    public async Task InitialMigration_ShouldCreatePostgreSqlSchema()
    {
        await using var postgreSql = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("projeto_pizza_test")
            .WithUsername("projeto_pizza")
            .WithPassword("integration-test-only")
            .Build();
        await postgreSql.StartAsync();

        var options = new DbContextOptionsBuilder<ProjetoPizzaDbContext>()
            .UseNpgsql(postgreSql.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var context = new ProjetoPizzaDbContext(options);

        await context.Database.MigrateAsync();

        (await context.Database.CanConnectAsync()).Should().BeTrue();
        (await context.Database.GetAppliedMigrationsAsync()).Should().ContainSingle(migration => migration.EndsWith("InitialCreate"));
    }
}
