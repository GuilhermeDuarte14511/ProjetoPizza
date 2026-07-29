namespace ProjetoPizza.Application.Abstractions.Persistence;

public interface IOperationNumberGenerator
{
    Task<long> NextOrderNumberAsync(CancellationToken cancellationToken);
    Task<long> NextKitchenTicketNumberAsync(CancellationToken cancellationToken);
    Task<long> NextTableSessionNumberAsync(CancellationToken cancellationToken);
}
