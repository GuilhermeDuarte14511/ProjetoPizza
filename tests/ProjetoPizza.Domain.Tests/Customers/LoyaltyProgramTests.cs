using FluentAssertions;
using ProjetoPizza.Domain.Customers;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Customers;

public sealed class LoyaltyProgramTests
{
    [Fact]
    public void Customer_should_redeem_and_restore_points_without_changing_purchase_statistics()
    {
        var customer = new Customer(CustomerId.New(), RestaurantUnitId.New(), "Ana", "11999998877", new DateOnly(1990, 1, 1));
        var expiration = DateTimeOffset.UtcNow.AddDays(365);
        customer.EarnLoyaltyPoints(200, expiration);

        customer.RedeemLoyaltyPoints(120);
        customer.RestoreLoyaltyPoints(120, expiration);

        customer.LoyaltyPoints.Should().Be(200);
        customer.OrderCount.Should().Be(0);
        customer.LifetimeSpend.Should().Be(Money.Zero());
    }

    [Fact]
    public void Settings_should_limit_redemption_percentage()
    {
        var settings = new LoyaltySettings(LoyaltySettingsId.New(), RestaurantUnitId.New());

        var action = () => settings.CalculateRedemption(700, new Money(100));

        action.Should().Throw<BusinessRuleException>().Which.Rule.Should().Be("loyalty.maximum");
    }

    [Fact]
    public void Percentage_coupon_should_honor_maximum_discount_and_usage_limit()
    {
        var now = DateTimeOffset.UtcNow;
        var coupon = new PromotionCoupon(PromotionCouponId.New(), RestaurantUnitId.New(), "VOLTE10", "Volte sempre",
            CouponDiscountType.Percentage, 10, 50, 8, now.AddDays(-1), now.AddDays(1), 1);

        coupon.Redeem(new Money(100), now).Should().Be(new Money(8));
        var action = () => coupon.Redeem(new Money(100), now);

        action.Should().Throw<BusinessRuleException>().Which.Rule.Should().Be("coupon.limit");
    }

    [Fact]
    public void Expired_balance_should_be_cleared_as_one_account_wide_expiration()
    {
        var customer = new Customer(CustomerId.New(), RestaurantUnitId.New(), "Ana", "11999998877", new DateOnly(1990, 1, 1));
        var expiration = DateTimeOffset.UtcNow.AddDays(1);
        customer.EarnLoyaltyPoints(50, expiration);

        customer.ExpireLoyaltyPoints(expiration.AddSeconds(1)).Should().Be(50);
        customer.LoyaltyPoints.Should().Be(0);
        customer.LoyaltyPointsExpireAt.Should().BeNull();
    }
}
