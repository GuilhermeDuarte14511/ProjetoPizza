using System.Data;
using Microsoft.EntityFrameworkCore;
using ProjetoPizza.Application.Abstractions.Persistence;

namespace ProjetoPizza.Infrastructure.Persistence;

internal sealed class PostgresOperationNumberGenerator(ProjetoPizzaDbContext context)
    : IOperationNumberGenerator
{
    public Task<long> NextOrderNumberAsync(CancellationToken cancellationToken) =>
        NextValueAsync("ordering.order_number_sequence", cancellationToken);

    public Task<long> NextKitchenTicketNumberAsync(CancellationToken cancellationToken) =>
        NextValueAsync("production.kitchen_ticket_number_sequence", cancellationToken);

    public Task<long> NextTableSessionNumberAsync(CancellationToken cancellationToken) =>
        NextValueAsync("dining.table_session_number_sequence", cancellationToken);

    private async Task<long> NextValueAsync(string sequence, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT nextval('{sequence}')";
            var value = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
