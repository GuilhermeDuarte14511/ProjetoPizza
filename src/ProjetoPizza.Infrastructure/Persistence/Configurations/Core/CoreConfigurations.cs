using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Identity;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Infrastructure.Persistence.Configurations.Core;

internal sealed class RestaurantUnitConfiguration : IEntityTypeConfiguration<RestaurantUnit>
{
    public void Configure(EntityTypeBuilder<RestaurantUnit> builder)
    {
        builder.ToTable("restaurant_units", "core");
        builder.HasKey(unit => unit.Id);
        builder.Property(unit => unit.Id).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(unit => unit.Name).HasMaxLength(120);
        builder.Property(unit => unit.LegalName).HasMaxLength(160);
        builder.Property(unit => unit.TradeName).HasMaxLength(160);
        builder.Property(unit => unit.Cnpj).HasMaxLength(18);
        builder.Property(unit => unit.Phone).HasMaxLength(24);
        builder.Property(unit => unit.AdministrativeEmail).HasMaxLength(254);
        builder.Property(unit => unit.Timezone).HasMaxLength(80);
        builder.Property(unit => unit.CurrencyCode).HasMaxLength(3);
        builder.HasIndex(unit => unit.Cnpj).IsUnique();
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

internal sealed class OperationSettingsConfiguration : IEntityTypeConfiguration<OperationSettings>
{
    public void Configure(EntityTypeBuilder<OperationSettings> builder)
    {
        builder.ToTable("operation_settings", "core");
        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.Id).HasColumnName("unit_id").HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Ignore(settings => settings.UnitId);
        builder.Property(settings => settings.ServiceFeePercentage).HasPercentageConversion();
        builder.Property(settings => settings.DefaultDeliveryFee).HasMoneyConversion();
        builder.HasOne<RestaurantUnit>().WithOne().HasForeignKey<OperationSettings>(settings => settings.Id).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PizzaSettingsConfiguration : IEntityTypeConfiguration<PizzaSettings>
{
    public void Configure(EntityTypeBuilder<PizzaSettings> builder)
    {
        builder.ToTable("pizza_settings", "core");
        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.Id).HasColumnName("unit_id").HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Ignore(settings => settings.UnitId);
        builder.Property(settings => settings.PricingPolicy).HasConversion<string>().HasMaxLength(40);
        builder.HasOne<RestaurantUnit>().WithOne().HasForeignKey<PizzaSettings>(settings => settings.Id).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees", "identity");
        builder.HasKey(employee => employee.Id);
        builder.Property(employee => employee.Id).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.Property(employee => employee.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(employee => employee.Name).HasMaxLength(120);
        builder.Property(employee => employee.DisplayName).HasMaxLength(80);
        builder.Property(employee => employee.Email).HasMaxLength(254);
        builder.Property(employee => employee.Phone).HasMaxLength(24);
        builder.Property(employee => employee.EmployeeCode).HasMaxLength(30);
        builder.HasIndex(employee => new { employee.UnitId, employee.EmployeeCode }).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(employee => employee.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}
