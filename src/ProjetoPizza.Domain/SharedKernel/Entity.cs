namespace ProjetoPizza.Domain.SharedKernel;

public abstract class Entity<TId> where TId : notnull
{
    protected Entity(TId id) => Id = id;

    public TId Id { get; protected init; }
}

public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    protected AggregateRoot(TId id) : base(id)
    {
    }
}
