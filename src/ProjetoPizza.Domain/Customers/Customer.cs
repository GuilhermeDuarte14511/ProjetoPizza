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
    public DateTimeOffset? LoyaltyPointsExpireAt { get; private set; }
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
        LifetimeSpend += amount;
        OrderCount += 1;
        LastOrderAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ReversePurchase(Money amount)
    {
        if (amount.Amount <= 0) return;
        LifetimeSpend = new Money(Math.Max(0, LifetimeSpend.Amount - amount.Amount));
        OrderCount = Math.Max(0, OrderCount - 1);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public int ExpireLoyaltyPoints(DateTimeOffset now)
    {
        if (!LoyaltyPointsExpireAt.HasValue || LoyaltyPointsExpireAt > now || LoyaltyPoints == 0) return 0;
        var expired = LoyaltyPoints;
        LoyaltyPoints = 0;
        LoyaltyPointsExpireAt = null;
        UpdatedAt = now;
        return expired;
    }

    public void EarnLoyaltyPoints(int points, DateTimeOffset expiresAt)
    {
        if (points <= 0) return;
        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new BusinessRuleException("loyalty.expiration", "Loyalty point expiration must be in the future.");
        LoyaltyPoints = checked(LoyaltyPoints + points);
        LoyaltyPointsExpireAt = expiresAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RedeemLoyaltyPoints(int points)
    {
        if (points <= 0) throw new BusinessRuleException("loyalty.points", "Redeemed points must be positive.");
        if (points > LoyaltyPoints) throw new BusinessRuleException("loyalty.balance", "Insufficient loyalty point balance.");
        LoyaltyPoints -= points;
        if (LoyaltyPoints == 0) LoyaltyPointsExpireAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RestoreLoyaltyPoints(int points, DateTimeOffset expiresAt)
    {
        if (points <= 0) return;
        LoyaltyPoints = checked(LoyaltyPoints + points);
        LoyaltyPointsExpireAt = !LoyaltyPointsExpireAt.HasValue || expiresAt > LoyaltyPointsExpireAt ? expiresAt : LoyaltyPointsExpireAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AdjustLoyaltyPoints(int points, DateTimeOffset expiresAt)
    {
        if (points is < -1_000_000 or > 1_000_000 || points == 0)
            throw new BusinessRuleException("loyalty.adjustment", "Loyalty point adjustment must be between -1,000,000 and 1,000,000 and cannot be zero.");

        if (points > 0)
        {
            EarnLoyaltyPoints(points, expiresAt);
            return;
        }

        RedeemLoyaltyPoints(-points);
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
