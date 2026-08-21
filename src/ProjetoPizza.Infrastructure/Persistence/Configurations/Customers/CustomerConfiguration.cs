using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Customers;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Infrastructure.Persistence.Configurations.Customers;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers", "customers");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new CustomerId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.Name).HasMaxLength(120);
        builder.Property(entity => entity.Phone).HasMaxLength(15);
        builder.Property(entity => entity.BirthDate).HasColumnType("date");
        builder.Property(entity => entity.LifetimeSpend).HasMoneyConversion();
        builder.HasIndex(entity => new { entity.UnitId, entity.Phone }).IsUnique();
        builder.HasIndex(entity => new { entity.UnitId, entity.Name });
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

internal sealed class LoyaltySettingsConfiguration : IEntityTypeConfiguration<LoyaltySettings>
{
    public void Configure(EntityTypeBuilder<LoyaltySettings> builder)
    {
        builder.ToTable("loyalty_settings", "customers");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new LoyaltySettingsId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.PointsPerCurrencyUnit).HasPrecision(10, 4);
        builder.Property(entity => entity.MaximumRedemptionPercentage).HasPrecision(5, 2);
        builder.Property(entity => entity.RedemptionValuePerPoint).HasMoneyConversion();
        builder.HasIndex(entity => entity.UnitId).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

internal sealed class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.ToTable("loyalty_transactions", "customers");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new LoyaltyTransactionId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.CustomerId).HasConversion(id => id.Value, value => new CustomerId(value));
        builder.Property(entity => entity.OrderId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new OrderId(value.Value) : null);
        builder.Property(entity => entity.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.Discount).HasMoneyConversion();
        builder.Property(entity => entity.Description).HasMaxLength(200);
        builder.HasIndex(entity => new { entity.CustomerId, entity.OccurredAt });
        builder.HasIndex(entity => new { entity.OrderId, entity.Type }).IsUnique();
        builder.HasOne<Customer>().WithMany().HasForeignKey(entity => entity.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Order>().WithMany().HasForeignKey(entity => entity.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PromotionCouponConfiguration : IEntityTypeConfiguration<PromotionCoupon>
{
    public void Configure(EntityTypeBuilder<PromotionCoupon> builder)
    {
        builder.ToTable("promotion_coupons", "customers");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new PromotionCouponId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.Code).HasMaxLength(40);
        builder.Property(entity => entity.Name).HasMaxLength(120);
        builder.Property(entity => entity.DiscountType).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.Value).HasPrecision(12, 2);
        builder.Property(entity => entity.MinimumOrderAmount).HasMoneyConversion();
        builder.Property(entity => entity.MaximumDiscountAmount)
            .HasConversion<decimal?>(money => money.HasValue ? money.Value.Amount : null,
                value => value.HasValue ? new Money(value.Value) : null)
            .HasPrecision(12, 2);
        builder.HasIndex(entity => new { entity.UnitId, entity.Code }).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}
