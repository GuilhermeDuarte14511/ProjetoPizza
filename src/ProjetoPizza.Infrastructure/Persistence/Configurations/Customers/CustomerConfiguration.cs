using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Customers;
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
        builder.HasIndex(entity => new { entity.UnitId, entity.Phone }).IsUnique();
        builder.HasIndex(entity => new { entity.UnitId, entity.Name });
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}
