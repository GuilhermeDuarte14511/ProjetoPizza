using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Customers;

public enum LoyaltyTransactionType { OpeningBalance, Earned, Redeemed, Restored, Expired, ManualAdjustment }
public enum CouponDiscountType { FixedAmount, Percentage }

public sealed class LoyaltySettings : AggregateRoot<LoyaltySettingsId>
{
    private LoyaltySettings() : base(default) { }
    public LoyaltySettings(LoyaltySettingsId id, RestaurantUnitId unitId) : base(id)
    {
        UnitId = unitId;
        Update(true, 1, 0.05m, 100, 30, 365);
    }
    public RestaurantUnitId UnitId { get; private set; }
    public bool IsEnabled { get; private set; }
    public decimal PointsPerCurrencyUnit { get; private set; }
    public Money RedemptionValuePerPoint { get; private set; } = Money.Zero();
    public int MinimumRedemptionPoints { get; private set; }
    public decimal MaximumRedemptionPercentage { get; private set; }
    public int PointsValidityDays { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(bool isEnabled, decimal pointsPerCurrencyUnit, decimal redemptionValuePerPoint,
        int minimumRedemptionPoints, decimal maximumRedemptionPercentage, int pointsValidityDays)
    {
        if (pointsPerCurrencyUnit is < 0 or > 100) throw new BusinessRuleException("loyalty.earn_rate", "Point earning rate is invalid.");
        if (redemptionValuePerPoint is <= 0 or > 100) throw new BusinessRuleException("loyalty.redemption_value", "Point redemption value is invalid.");
        if (minimumRedemptionPoints < 1) throw new BusinessRuleException("loyalty.minimum", "Minimum redemption must be positive.");
        if (maximumRedemptionPercentage is <= 0 or > 100) throw new BusinessRuleException("loyalty.maximum", "Maximum redemption percentage is invalid.");
        if (pointsValidityDays is < 1 or > 3650) throw new BusinessRuleException("loyalty.validity", "Point validity is invalid.");
        IsEnabled = isEnabled;
        PointsPerCurrencyUnit = pointsPerCurrencyUnit;
        RedemptionValuePerPoint = new Money(redemptionValuePerPoint);
        MinimumRedemptionPoints = minimumRedemptionPoints;
        MaximumRedemptionPercentage = maximumRedemptionPercentage;
        PointsValidityDays = pointsValidityDays;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public int CalculateEarnedPoints(Money paid) => IsEnabled
        ? decimal.ToInt32(decimal.Floor(paid.Amount * PointsPerCurrencyUnit)) : 0;

    public Money CalculateRedemption(int points, Money eligibleAmount)
    {
        if (!IsEnabled) throw new BusinessRuleException("loyalty.disabled", "The loyalty program is disabled.");
        if (points < MinimumRedemptionPoints) throw new BusinessRuleException("loyalty.minimum", "The minimum point redemption was not reached.");
        var requested = points * RedemptionValuePerPoint.Amount;
        var maximum = decimal.Round(eligibleAmount.Amount * MaximumRedemptionPercentage / 100m, 2, MidpointRounding.ToZero);
        if (requested > maximum) throw new BusinessRuleException("loyalty.maximum", "Point redemption exceeds the allowed order percentage.");
        return new Money(requested);
    }
}

public sealed class LoyaltyTransaction : AggregateRoot<LoyaltyTransactionId>
{
    private LoyaltyTransaction() : base(default) { }
    public LoyaltyTransaction(LoyaltyTransactionId id, RestaurantUnitId unitId, CustomerId customerId,
        OrderId? orderId, LoyaltyTransactionType type, int points, int balanceAfter, Money discount, string description) : base(id)
    {
        if (points == 0) throw new BusinessRuleException("loyalty.transaction_points", "A loyalty transaction cannot have zero points.");
        UnitId = unitId; CustomerId = customerId; OrderId = orderId; Type = type; Points = points;
        BalanceAfter = balanceAfter; Discount = discount; Description = Guard.Required(description, nameof(description), 200);
        OccurredAt = DateTimeOffset.UtcNow;
    }
    public RestaurantUnitId UnitId { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public OrderId? OrderId { get; private set; }
    public LoyaltyTransactionType Type { get; private set; }
    public int Points { get; private set; }
    public int BalanceAfter { get; private set; }
    public Money Discount { get; private set; } = Money.Zero();
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
}

public sealed class PromotionCoupon : AggregateRoot<PromotionCouponId>
{
    private PromotionCoupon() : base(default) { }
    public PromotionCoupon(PromotionCouponId id, RestaurantUnitId unitId, string code, string name,
        CouponDiscountType discountType, decimal value, decimal minimumOrderAmount, decimal? maximumDiscountAmount,
        DateTimeOffset startsAt, DateTimeOffset endsAt, int? usageLimit, bool isActive = true) : base(id)
    { UnitId = unitId; Update(code, name, discountType, value, minimumOrderAmount, maximumDiscountAmount, startsAt, endsAt, usageLimit, isActive); }
    public RestaurantUnitId UnitId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public CouponDiscountType DiscountType { get; private set; }
    public decimal Value { get; private set; }
    public Money MinimumOrderAmount { get; private set; } = Money.Zero();
    public Money? MaximumDiscountAmount { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public int? UsageLimit { get; private set; }
    public int TimesRedeemed { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(string code, string name, CouponDiscountType discountType, decimal value, decimal minimumOrderAmount,
        decimal? maximumDiscountAmount, DateTimeOffset startsAt, DateTimeOffset endsAt, int? usageLimit, bool isActive)
    {
        var normalized = Guard.Required(code, nameof(code), 40).Trim().ToUpperInvariant();
        if (normalized.Any(ch => !char.IsLetterOrDigit(ch) && ch != '-')) throw new BusinessRuleException("coupon.code", "Coupon code contains unsupported characters.");
        if (value <= 0 || discountType == CouponDiscountType.Percentage && value > 100) throw new BusinessRuleException("coupon.value", "Coupon value is invalid.");
        if (minimumOrderAmount < 0 || maximumDiscountAmount is <= 0 || usageLimit is <= 0) throw new BusinessRuleException("coupon.limits", "Coupon limits are invalid.");
        if (endsAt <= startsAt) throw new BusinessRuleException("coupon.period", "Coupon end must be after its start.");
        Code = normalized; Name = Guard.Required(name, nameof(name), 120); DiscountType = discountType; Value = value;
        MinimumOrderAmount = new Money(minimumOrderAmount); MaximumDiscountAmount = maximumDiscountAmount.HasValue ? new Money(maximumDiscountAmount.Value) : null;
        StartsAt = startsAt; EndsAt = endsAt; UsageLimit = usageLimit; IsActive = isActive; UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Money Redeem(Money eligibleAmount, DateTimeOffset now)
    {
        var amount = CalculateDiscount(eligibleAmount, now);
        TimesRedeemed += 1; UpdatedAt = now;
        return amount;
    }

    public Money CalculateDiscount(Money eligibleAmount, DateTimeOffset now)
    {
        if (!IsActive || now < StartsAt || now > EndsAt) throw new BusinessRuleException("coupon.unavailable", "Coupon is not available.");
        if (UsageLimit.HasValue && TimesRedeemed >= UsageLimit) throw new BusinessRuleException("coupon.limit", "Coupon usage limit was reached.");
        if (eligibleAmount.Amount < MinimumOrderAmount.Amount) throw new BusinessRuleException("coupon.minimum", "Order does not reach the coupon minimum.");
        var amount = DiscountType == CouponDiscountType.FixedAmount ? Value : eligibleAmount.Amount * Value / 100m;
        if (MaximumDiscountAmount.HasValue) amount = Math.Min(amount, MaximumDiscountAmount.Value.Amount);
        amount = Math.Min(decimal.Round(amount, 2, MidpointRounding.ToZero), eligibleAmount.Amount);
        return new Money(amount);
    }

    public void ReleaseRedemption()
    {
        if (TimesRedeemed > 0) TimesRedeemed -= 1;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
