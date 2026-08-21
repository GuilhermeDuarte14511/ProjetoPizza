using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoPizza.Domain.Catalog;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Customers;
using ProjetoPizza.Domain.Devices;
using ProjetoPizza.Domain.Dining;
using ProjetoPizza.Domain.Identity;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.Production;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Infrastructure.Persistence.Configurations.Ordering;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", "ordering");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new OrderId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.TableSessionId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new TableSessionId(value.Value) : null);
        builder.Property(entity => entity.CreatedByEmployeeId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new EmployeeId(value.Value) : null);
        builder.Property(entity => entity.CreatedByDeviceId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new DeviceId(value.Value) : null);
        builder.Property(entity => entity.CustomerId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new CustomerId(value.Value) : null);
        builder.Property(entity => entity.CustomerNameSnapshot).HasMaxLength(120);
        builder.Property(entity => entity.SalesChannel).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.FulfillmentType).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.PaymentStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.Subtotal).HasMoneyConversion();
        builder.Property(entity => entity.ServiceFee).HasMoneyConversion();
        builder.Property(entity => entity.DeliveryFee).HasMoneyConversion();
        builder.Property(entity => entity.Discount).HasMoneyConversion();
        builder.Property(entity => entity.ManualDiscount).HasMoneyConversion();
        builder.Property(entity => entity.CouponDiscount).HasMoneyConversion();
        builder.Property(entity => entity.LoyaltyDiscount).HasMoneyConversion();
        builder.Property(entity => entity.CouponCode).HasMaxLength(40);
        builder.Property(entity => entity.PromotionCouponId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new PromotionCouponId(value.Value) : null);
        builder.Property(entity => entity.Total).HasMoneyConversion();
        builder.Property(entity => entity.Notes).HasMaxLength(1000);
        builder.Property(entity => entity.DeliveryAddressSnapshot).HasMaxLength(500);
        builder.Property(entity => entity.DeliveryStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.DeliveryDriverName).HasMaxLength(120);
        builder.Property(entity => entity.DeliveryTrackingTokenHash).HasMaxLength(128);
        builder.Property(entity => entity.DeliveryFailureReason).HasMaxLength(500);
        builder.Property(entity => entity.CancellationReason).HasMaxLength(500);
        builder.HasIndex(entity => new { entity.UnitId, entity.Status, entity.PlacedAt });
        builder.HasIndex(entity => entity.DeliveryTrackingTokenHash).IsUnique();
        builder.HasIndex(entity => new { entity.UnitId, entity.OrderNumber }).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TableSession>().WithMany().HasForeignKey(entity => entity.TableSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.CreatedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Device>().WithMany().HasForeignKey(entity => entity.CreatedByDeviceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(entity => entity.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PromotionCoupon>().WithMany().HasForeignKey(entity => entity.PromotionCouponId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(entity => entity.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items", "ordering");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new OrderItemId(value));
        builder.Property(entity => entity.OrderId).HasConversion(id => id.Value, value => new OrderId(value));
        builder.Property(entity => entity.ProductId).HasConversion(id => id.Value, value => new ProductId(value));
        builder.Property(entity => entity.ProductVariantId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new ProductVariantId(value.Value) : null);
        builder.Property(entity => entity.ProductionStationId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new ProductionStationId(value.Value) : null);
        builder.Property(entity => entity.ProductNameSnapshot).HasMaxLength(140);
        builder.Property(entity => entity.VariantNameSnapshot).HasMaxLength(100);
        builder.Property(entity => entity.UnitPrice).HasMoneyConversion();
        builder.Property(entity => entity.TotalPrice).HasMoneyConversion();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.Notes).HasMaxLength(1000);
        builder.Property(entity => entity.CancellationReason).HasMaxLength(500);
        builder.HasIndex(entity => new { entity.OrderId, entity.Status });
        builder.HasOne<Order>().WithMany(entity => entity.Items).HasForeignKey(entity => entity.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany().HasForeignKey(entity => entity.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(entity => entity.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductionStation>().WithMany().HasForeignKey(entity => entity.ProductionStationId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OrderItemPizzaConfiguration : IEntityTypeConfiguration<OrderItemPizza>
{
    public void Configure(EntityTypeBuilder<OrderItemPizza> builder)
    {
        builder.ToTable("order_item_pizzas", "ordering");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("order_item_id").HasConversion(id => id.Value, value => new OrderItemId(value));
        builder.Ignore(entity => entity.OrderItemId);
        builder.Property(entity => entity.PizzaSizeId).HasConversion(id => id.Value, value => new PizzaSizeId(value));
        builder.Property(entity => entity.PizzaCrustId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new PizzaCrustId(value.Value) : null);
        builder.Property(entity => entity.SecondPizzaCrustId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new PizzaCrustId(value.Value) : null);
        builder.Property(entity => entity.SizeNameSnapshot).HasMaxLength(80);
        builder.Property(entity => entity.CrustNameSnapshot).HasMaxLength(100);
        builder.Property(entity => entity.SecondCrustNameSnapshot).HasMaxLength(100);
        builder.Property(entity => entity.CrustSelectionMode).HasConversion<string>().HasMaxLength(20);
        builder.Property(entity => entity.PricingPolicySnapshot).HasConversion<string>().HasMaxLength(40);
        builder.Property(entity => entity.BasePrice).HasMoneyConversion();
        builder.Property(entity => entity.CrustPrice).HasMoneyConversion();
        builder.Property(entity => entity.ExtrasPrice).HasMoneyConversion();
        builder.HasOne<OrderItem>().WithOne().HasForeignKey<OrderItemPizza>(entity => entity.Id).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PizzaSize>().WithMany().HasForeignKey(entity => entity.PizzaSizeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PizzaCrust>().WithMany().HasForeignKey(entity => entity.PizzaCrustId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PizzaCrust>().WithMany().HasForeignKey(entity => entity.SecondPizzaCrustId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(entity => entity.Flavors).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class OrderItemPizzaFlavorConfiguration : IEntityTypeConfiguration<OrderItemPizzaFlavor>
{
    public void Configure(EntityTypeBuilder<OrderItemPizzaFlavor> builder)
    {
        builder.ToTable("order_item_pizza_flavors", "ordering");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new OrderItemPizzaFlavorId(value));
        builder.Property(entity => entity.OrderItemId).HasConversion(id => id.Value, value => new OrderItemId(value));
        builder.Property(entity => entity.PizzaFlavorId).HasConversion(id => id.Value, value => new PizzaFlavorId(value));
        builder.Property(entity => entity.FlavorNameSnapshot).HasMaxLength(120);
        builder.Property(entity => entity.CalculatedPrice).HasMoneyConversion();
        builder.HasOne<OrderItemPizza>().WithMany(entity => entity.Flavors).HasForeignKey(entity => entity.OrderItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PizzaFlavor>().WithMany().HasForeignKey(entity => entity.PizzaFlavorId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OrderItemModifierConfiguration : IEntityTypeConfiguration<OrderItemModifier>
{
    public void Configure(EntityTypeBuilder<OrderItemModifier> builder)
    {
        builder.ToTable("order_item_modifiers", "ordering");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new OrderItemModifierId(value));
        builder.Property(entity => entity.OrderItemId).HasConversion(id => id.Value, value => new OrderItemId(value));
        builder.Property(entity => entity.PizzaFlavorId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new PizzaFlavorId(value.Value) : null);
        builder.Property(entity => entity.IngredientId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new IngredientId(value.Value) : null);
        builder.Property(entity => entity.ModifierType).HasConversion<string>().HasMaxLength(20);
        builder.Property(entity => entity.NameSnapshot).HasMaxLength(120);
        builder.Property(entity => entity.Quantity).HasPrecision(18, 4);
        builder.Property(entity => entity.UnitPrice).HasMoneyConversion();
        builder.Property(entity => entity.TotalPrice).HasMoneyConversion();
        builder.HasOne<OrderItem>().WithMany().HasForeignKey(entity => entity.OrderItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PizzaFlavor>().WithMany().HasForeignKey(entity => entity.PizzaFlavorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Ingredient>().WithMany().HasForeignKey(entity => entity.IngredientId).OnDelete(DeleteBehavior.Restrict);
    }
}
