using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjetoPizza.Domain.Cashier;
using ProjetoPizza.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ProjetoPizza.IntegrationTests.Persistence;

public sealed class PostgreSqlMigrationTests
{
    [Fact]
    public void CashShiftModel_ShouldEnforceOnlyOneActiveShift()
    {
        var options = new DbContextOptionsBuilder<ProjetoPizzaDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=model_validation;Password=model_validation")
            .UseSnakeCaseNamingConvention()
            .Options;
        using var context = new ProjetoPizzaDbContext(options);

        var entity = context.Model.FindEntityType(typeof(CashShift));
        var activeSlot = entity!.FindProperty("ActiveSlot");
        var uniqueIndex = entity.GetIndexes()
            .Single(index => index.GetDatabaseName() == "ix_cash_shifts_single_active");

        activeSlot.Should().NotBeNull();
        activeSlot!.GetComputedColumnSql().Should().Contain("Open").And.Contain("Closing");
        uniqueIndex.IsUnique.Should().BeTrue();
    }

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
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        appliedMigrations.Should().Contain(migration => migration.EndsWith("InitialCreate"));
        appliedMigrations.Should().Contain(migration => migration.EndsWith("AddCashShiftOpeningGuard"));
    }
}
