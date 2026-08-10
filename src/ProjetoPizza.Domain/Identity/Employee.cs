using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Identity;

public sealed class Employee : AggregateRoot<EmployeeId>
{
    private Employee() : base(default) { }

    public Employee(EmployeeId id, RestaurantUnitId unitId, Guid identityUserId, string name, string email, string employeeCode) : base(id)
    {
        UnitId = unitId;
        IdentityUserId = identityUserId;
        Name = DisplayName = Guard.Required(name, nameof(name), 120);
        Email = Guard.Required(email, nameof(email), 254);
        EmployeeCode = Guard.Required(employeeCode, nameof(employeeCode), 30);
        IsActive = true;
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public Guid IdentityUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string EmployeeCode { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset? LastAccessAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Activate() => ChangeActive(true);
    public void Deactivate() => ChangeActive(false);

    public void UpdateProfile(string name, string displayName, string email, string employeeCode, string? phone)
    {
        Name = Guard.Required(name, nameof(name), 120);
        DisplayName = Guard.Required(displayName, nameof(displayName), 80);
        Email = Guard.Required(email, nameof(email), 254);
        EmployeeCode = Guard.Required(employeeCode, nameof(employeeCode), 30);
        Phone = string.IsNullOrWhiteSpace(phone) ? null : Guard.Required(phone, nameof(phone), 24);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RegisterAccess() => LastAccessAt = DateTimeOffset.UtcNow;

    private void ChangeActive(bool value)
    {
        IsActive = value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
