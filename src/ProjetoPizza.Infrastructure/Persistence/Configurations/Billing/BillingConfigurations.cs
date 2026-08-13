using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoPizza.Domain.Billing;
using ProjetoPizza.Domain.Cashier;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Dining;
using ProjetoPizza.Domain.Identity;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Infrastructure.Persistence.Configurations.Billing;

internal sealed class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.ToTable("bills", "billing");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new BillId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.TableSessionId).HasConversion<Guid?>(
            id => id.HasValue ? id.Value.Value : null,
            value => value.HasValue ? new TableSessionId(value.Value) : null);
        builder.Property(entity => entity.OrderId).HasConversion<Guid?>(
            id => id.HasValue ? id.Value.Value : null,
            value => value.HasValue ? new OrderId(value.Value) : null);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.Subtotal).HasMoneyConversion();
        builder.Property(entity => entity.ServiceFeePercentage).HasPercentageConversion();
        builder.Property(entity => entity.ServiceFeeAmount).HasMoneyConversion();
        builder.Property(entity => entity.DiscountAmount).HasMoneyConversion();
        builder.Property(entity => entity.TotalAmount).HasMoneyConversion();
        builder.Property(entity => entity.PaidAmount).HasMoneyConversion();
        builder.Property(entity => entity.RemainingAmount).HasMoneyConversion();
        builder.Property(entity => entity.RequestedSplitCount);
        builder.HasIndex(entity => new { entity.TableSessionId, entity.Status });
        builder.HasIndex(entity => entity.OrderId).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TableSession>().WithMany().HasForeignKey(entity => entity.TableSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Order>().WithMany().HasForeignKey(entity => entity.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

internal sealed class BillItemConfiguration : IEntityTypeConfiguration<BillItem>
{
    public void Configure(EntityTypeBuilder<BillItem> builder)
    {
        builder.ToTable("bill_items", "billing");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new BillItemId(value));
        builder.Property(entity => entity.BillId).HasConversion(id => id.Value, value => new BillId(value));
        builder.Property(entity => entity.OrderItemId).HasConversion(id => id.Value, value => new OrderItemId(value));
        builder.Property(entity => entity.Quantity).HasPrecision(18, 4);
        builder.Property(entity => entity.GrossAmount).HasMoneyConversion();
        builder.Property(entity => entity.ServiceFeeAmount).HasMoneyConversion();
        builder.Property(entity => entity.DiscountAmount).HasMoneyConversion();
        builder.Property(entity => entity.NetAmount).HasMoneyConversion();
        builder.HasOne<Bill>().WithMany().HasForeignKey(entity => entity.BillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OrderItem>().WithMany().HasForeignKey(entity => entity.OrderItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BillSplitConfiguration : IEntityTypeConfiguration<BillSplit>
{
    public void Configure(EntityTypeBuilder<BillSplit> builder)
    {
        builder.ToTable("bill_splits", "billing");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new BillSplitId(value));
        builder.Property(entity => entity.BillId).HasConversion(id => id.Value, value => new BillId(value));
        builder.Property(entity => entity.Name).HasMaxLength(100);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.TotalAmount).HasMoneyConversion();
        builder.Property(entity => entity.PaidAmount).HasMoneyConversion();
        builder.Property(entity => entity.RemainingAmount).HasMoneyConversion();
        builder.HasIndex(entity => new { entity.BillId, entity.SplitNumber }).IsUnique();
        builder.HasOne<Bill>().WithMany().HasForeignKey(entity => entity.BillId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BillSplitItemConfiguration : IEntityTypeConfiguration<BillSplitItem>
{
    public void Configure(EntityTypeBuilder<BillSplitItem> builder)
    {
        builder.ToTable("bill_split_items", "billing");
        builder.HasKey(entity => new { entity.BillSplitId, entity.BillItemId });
        builder.Property(entity => entity.BillSplitId).HasConversion(id => id.Value, value => new BillSplitId(value));
        builder.Property(entity => entity.BillItemId).HasConversion(id => id.Value, value => new BillItemId(value));
        builder.Property(entity => entity.Quantity).HasPrecision(18, 4);
        builder.Property(entity => entity.AllocatedAmount).HasMoneyConversion();
        builder.HasOne<BillSplit>().WithMany().HasForeignKey(entity => entity.BillSplitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BillItem>().WithMany().HasForeignKey(entity => entity.BillItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("payment_methods", "billing");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new PaymentMethodId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.Code).HasMaxLength(40);
        builder.Property(entity => entity.Name).HasMaxLength(100);
        builder.HasIndex(entity => new { entity.UnitId, entity.Code }).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", "billing");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new PaymentId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.BillId).HasConversion(id => id.Value, value => new BillId(value));
        builder.Property(entity => entity.BillSplitId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new BillSplitId(value.Value) : null);
        builder.Property(entity => entity.CashShiftId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new CashShiftId(value.Value) : null);
        builder.Property(entity => entity.PaymentMethodId).HasConversion(id => id.Value, value => new PaymentMethodId(value));
        builder.Property(entity => entity.ReceivedByEmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.Amount).HasMoneyConversion();
        builder.Property(entity => entity.ReceivedAmount).HasMoneyConversion();
        builder.Property(entity => entity.ChangeAmount).HasMoneyConversion();
        builder.Property(entity => entity.RefundedAmount).HasMoneyConversion();
        builder.Property(entity => entity.ExternalReference).HasMaxLength(200);
        builder.Property(entity => entity.AuthorizationCode).HasMaxLength(100);
        builder.Property(entity => entity.CancellationReason).HasMaxLength(500);
        builder.Property(entity => entity.RefundReason).HasMaxLength(500);
        builder.HasIndex(entity => new { entity.BillId, entity.Status });
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Bill>().WithMany().HasForeignKey(entity => entity.BillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BillSplit>().WithMany().HasForeignKey(entity => entity.BillSplitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CashShift>().WithMany().HasForeignKey(entity => entity.CashShiftId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentMethod>().WithMany().HasForeignKey(entity => entity.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.ReceivedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}
