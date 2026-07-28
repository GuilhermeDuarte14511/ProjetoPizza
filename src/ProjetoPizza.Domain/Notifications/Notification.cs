using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Notifications;

public sealed class Notification : AggregateRoot<NotificationId>
{
    private Notification() : base(default) { }

    public Notification(NotificationId id, RestaurantUnitId unitId, string type, string title, string message) : base(id)
    {
        UnitId = unitId;
        Type = Guard.Required(type, nameof(type), 60);
        Title = Guard.Required(title, nameof(title), 120);
        Message = Guard.Required(message, nameof(message), 1000);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
}
