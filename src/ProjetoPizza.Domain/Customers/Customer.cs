using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Customers;

public sealed class Customer : AggregateRoot<CustomerId>
{
    private Customer() : base(default) { }

    public Customer(
        CustomerId id,
        RestaurantUnitId unitId,
        string name,
        string phone,
        DateOnly birthDate) : base(id)
    {
        UnitId = unitId;
        Name = Guard.Required(name, nameof(name), 120);
        Phone = NormalizePhone(phone);
        BirthDate = ValidateBirthDate(birthDate);
        IsActive = true;
        LoyaltyPoints = 0;
        LifetimeSpend = Money.Zero();
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public RestaurantUnitId UnitId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public DateOnly BirthDate { get; private set; }
    public bool IsActive { get; private set; }
    public int LoyaltyPoints { get; private set; }
    public Money LifetimeSpend { get; private set; }
    public int OrderCount { get; private set; }
    public DateTimeOffset? LastOrderAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(string name, string phone, DateOnly birthDate, bool isActive)
    {
        Name = Guard.Required(name, nameof(name), 120);
        Phone = NormalizePhone(phone);
        BirthDate = ValidateBirthDate(birthDate);
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RegisterPurchase(Money amount)
    {
        if (amount.Amount <= 0) return;
        LoyaltyPoints += decimal.ToInt32(decimal.Floor(amount.Amount));
        LifetimeSpend += amount;
        OrderCount += 1;
        LastOrderAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ReversePurchase(Money amount)
    {
        if (amount.Amount <= 0) return;
        LoyaltyPoints = Math.Max(0, LoyaltyPoints - decimal.ToInt32(decimal.Floor(amount.Amount)));
        LifetimeSpend = new Money(Math.Max(0, LifetimeSpend.Amount - amount.Amount));
        OrderCount = Math.Max(0, OrderCount - 1);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static string NormalizePhone(string phone)
    {
        var digits = new string(Guard.Required(phone, nameof(phone), 30).Where(char.IsDigit).ToArray());
        if (digits.Length is < 8 or > 15)
        {
            throw new BusinessRuleException("customer.phone", "Phone must contain between eight and fifteen digits.");
        }

        return digits;
    }

    private static DateOnly ValidateBirthDate(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (birthDate > today || birthDate < today.AddYears(-120))
        {
            throw new BusinessRuleException("customer.birth_date", "Birth date is outside the supported range.");
        }

        return birthDate;
    }
}
