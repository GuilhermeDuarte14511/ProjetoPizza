using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Infrastructure.Persistence.Configurations;

internal static class ValueObjectPropertyExtensions
{
    public static PropertyBuilder<Money> HasMoneyConversion(this PropertyBuilder<Money> property) =>
        property.HasConversion(value => value.Amount, value => new Money(value)).HasPrecision(18, 2);

    public static PropertyBuilder<Money?> HasNullableMoneyConversion(this PropertyBuilder<Money?> property) =>
        property.HasConversion<decimal?>(
                value => value.HasValue ? value.Value.Amount : null,
                value => value.HasValue ? new Money(value.Value) : null)
            .HasPrecision(18, 2);

    public static PropertyBuilder<Percentage> HasPercentageConversion(this PropertyBuilder<Percentage> property) =>
        property.HasConversion(value => value.Value, value => new Percentage(value)).HasPrecision(5, 2);
}
