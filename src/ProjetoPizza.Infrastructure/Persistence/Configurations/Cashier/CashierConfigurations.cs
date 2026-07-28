using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoPizza.Domain.Billing;
using ProjetoPizza.Domain.Cashier;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Identity;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Infrastructure.Persistence.Configurations.Cashier;

internal sealed class CashRegisterConfiguration : IEntityTypeConfiguration<CashRegister>
{
    public void Configure(EntityTypeBuilder<CashRegister> builder)
    {
        builder.ToTable("cash_registers", "cashier");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new CashRegisterId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.Name).HasMaxLength(100);
        builder.Property(entity => entity.Code).HasMaxLength(30);
        builder.HasIndex(entity => new { entity.UnitId, entity.Code }).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CashShiftConfiguration : IEntityTypeConfiguration<CashShift>
{
    public void Configure(EntityTypeBuilder<CashShift> builder)
    {
        builder.ToTable("cash_shifts", "cashier");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new CashShiftId(value));
        builder.Property(entity => entity.CashRegisterId).HasConversion(id => id.Value, value => new CashRegisterId(value));
        builder.Property(entity => entity.OperatorEmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.Property(entity => entity.ClosedByEmployeeId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new EmployeeId(value.Value) : null);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.OpeningAmount).HasMoneyConversion();
        builder.Property(entity => entity.ExpectedCashAmount).HasMoneyConversion();
        builder.Property(entity => entity.CountedCashAmount).HasNullableMoneyConversion();
        builder.Property(entity => entity.DifferenceAmount).HasPrecision(18, 2);
        builder.Property(entity => entity.ClosingNotes).HasMaxLength(500);
        builder.HasIndex(entity => new { entity.CashRegisterId, entity.Status });
        builder.HasOne<CashRegister>().WithMany().HasForeignKey(entity => entity.CashRegisterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.OperatorEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.ClosedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(entity => entity.Movements).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

internal sealed class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.ToTable("cash_movements", "cashier");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new CashMovementId(value));
        builder.Property(entity => entity.CashShiftId).HasConversion(id => id.Value, value => new CashShiftId(value));
        builder.Property(entity => entity.PaymentId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new PaymentId(value.Value) : null);
        builder.Property(entity => entity.CreatedByEmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.Property(entity => entity.AuthorizedByEmployeeId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new EmployeeId(value.Value) : null);
        builder.Property(entity => entity.MovementType).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.Amount).HasMoneyConversion();
        builder.Property(entity => entity.Description).HasMaxLength(200);
        builder.Property(entity => entity.Reason).HasMaxLength(300);
        builder.HasOne<CashShift>().WithMany(entity => entity.Movements).HasForeignKey(entity => entity.CashShiftId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Payment>().WithMany().HasForeignKey(entity => entity.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.CreatedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.AuthorizedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
