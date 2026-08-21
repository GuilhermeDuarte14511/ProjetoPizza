using ProjetoPizza.Application.Abstractions.Persistence;
using ProjetoPizza.Domain.Customers;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Application.Customers;

public static class LoyaltyProgramService
{
    public static LoyaltySettings GetOrCreateSettings(IProjetoPizzaDbContext context, RestaurantUnitId unitId)
    {
        var settings = context.LoyaltySettings.SingleOrDefault(candidate => candidate.UnitId == unitId);
        if (settings is not null) return settings;
        settings = new LoyaltySettings(LoyaltySettingsId.New(), unitId);
        context.Add(settings);
        return settings;
    }

    public static void ApplyBenefits(IProjetoPizzaDbContext context, Order order, Customer? customer,
        Money manualDiscount, string? couponCode, int loyaltyPoints)
    {
        if (loyaltyPoints < 0) throw new BusinessRuleException("loyalty.points", "Redeemed points cannot be negative.");
        var eligible = order.Subtotal + order.ServiceFee + order.DeliveryFee;
        PromotionCoupon? coupon = null;
        var couponDiscount = Money.Zero();
        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var normalized = couponCode.Trim().ToUpperInvariant();
            coupon = context.PromotionCoupons.SingleOrDefault(candidate => candidate.UnitId == order.UnitId && candidate.Code == normalized)
                ?? throw new BusinessRuleException("coupon.not_found", "Coupon was not found.");
            couponDiscount = coupon.Redeem(eligible, DateTimeOffset.UtcNow);
        }

        var loyaltyDiscount = Money.Zero();
        if (loyaltyPoints > 0)
        {
            if (customer is null) throw new BusinessRuleException("loyalty.customer", "A verified customer is required to redeem points.");
            ExpirePoints(context, customer);
            var settings = GetOrCreateSettings(context, order.UnitId);
            var remaining = eligible - couponDiscount;
            loyaltyDiscount = settings.CalculateRedemption(loyaltyPoints, remaining);
            customer.RedeemLoyaltyPoints(loyaltyPoints);
            context.Add(new LoyaltyTransaction(LoyaltyTransactionId.New(), order.UnitId, customer.Id, order.Id,
                LoyaltyTransactionType.Redeemed, -loyaltyPoints, customer.LoyaltyPoints, loyaltyDiscount, "Pontos resgatados no pedido"));
        }

        order.ApplyLoyaltyBenefits(coupon?.Id, coupon?.Code, couponDiscount, loyaltyPoints, loyaltyDiscount, manualDiscount);
    }

    public static void AwardCompletedOrder(IProjetoPizzaDbContext context, Order order)
    {
        if (!order.CustomerId.HasValue || context.LoyaltyTransactions.Any(candidate =>
                candidate.OrderId == order.Id && candidate.Type == LoyaltyTransactionType.Earned)) return;
        var customer = context.Customers.Single(candidate => candidate.Id == order.CustomerId.Value);
        if (order.LoyaltyPointsRedeemed == 0) ExpirePoints(context, customer);
        var settings = GetOrCreateSettings(context, order.UnitId);
        var points = settings.CalculateEarnedPoints(order.Total);
        customer.RegisterPurchase(order.Total);
        if (points <= 0) return;
        customer.EarnLoyaltyPoints(points, DateTimeOffset.UtcNow.AddDays(settings.PointsValidityDays));
        context.Add(new LoyaltyTransaction(LoyaltyTransactionId.New(), order.UnitId, customer.Id, order.Id,
            LoyaltyTransactionType.Earned, points, customer.LoyaltyPoints, Money.Zero(), "Pontos ganhos no pedido concluído"));
    }

    public static void RestoreCancelledOrder(IProjetoPizzaDbContext context, Order order)
    {
        if (order.LoyaltyPointsRedeemed > 0 && order.CustomerId.HasValue && !context.LoyaltyTransactions.Any(candidate =>
                candidate.OrderId == order.Id && candidate.Type == LoyaltyTransactionType.Restored))
        {
            var customer = context.Customers.Single(candidate => candidate.Id == order.CustomerId.Value);
            var settings = GetOrCreateSettings(context, order.UnitId);
            customer.RestoreLoyaltyPoints(order.LoyaltyPointsRedeemed, DateTimeOffset.UtcNow.AddDays(settings.PointsValidityDays));
            context.Add(new LoyaltyTransaction(LoyaltyTransactionId.New(), order.UnitId, customer.Id, order.Id,
                LoyaltyTransactionType.Restored, order.LoyaltyPointsRedeemed, customer.LoyaltyPoints,
                order.LoyaltyDiscount, "Pontos restaurados após cancelamento"));
        }
        if (order.PromotionCouponId.HasValue)
            context.PromotionCoupons.Single(candidate => candidate.Id == order.PromotionCouponId.Value).ReleaseRedemption();
    }

    public static void ExpirePoints(IProjetoPizzaDbContext context, Customer customer)
    {
        if (customer.LoyaltyPoints > 0 && !context.LoyaltyTransactions.Any(candidate => candidate.CustomerId == customer.Id))
            context.Add(new LoyaltyTransaction(LoyaltyTransactionId.New(), customer.UnitId, customer.Id, null,
                LoyaltyTransactionType.OpeningBalance, customer.LoyaltyPoints, customer.LoyaltyPoints, Money.Zero(), "Saldo anterior à implantação do razão"));
        var expired = customer.ExpireLoyaltyPoints(DateTimeOffset.UtcNow);
        if (expired <= 0) return;
        context.Add(new LoyaltyTransaction(LoyaltyTransactionId.New(), customer.UnitId, customer.Id, null,
            LoyaltyTransactionType.Expired, -expired, 0, Money.Zero(), "Pontos expirados"));
    }
}
