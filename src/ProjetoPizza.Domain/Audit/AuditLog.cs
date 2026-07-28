using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Audit;

public sealed class AuditLog : AggregateRoot<AuditLogId>
{
    private AuditLog() : base(default) { }

    public AuditLog(
        AuditLogId id,
        RestaurantUnitId unitId,
        string module,
        string action,
        string entityType,
        string entityId,
        EmployeeId? employeeId = null) : base(id)
    {
        UnitId = unitId;
        Module = Guard.Required(module, nameof(module), 80);
        Action = Guard.Required(action, nameof(action), 80);
        EntityType = Guard.Required(entityType, nameof(entityType), 120);
        EntityId = Guard.Required(entityId, nameof(entityId), 100);
        EmployeeId = employeeId;
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public EmployeeId? EmployeeId { get; private set; }
    public string Module { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
}
