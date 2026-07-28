using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.Production;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Infrastructure.Persistence.Configurations.Production;

internal sealed class ProductionStationConfiguration : IEntityTypeConfiguration<ProductionStation>
{
    public void Configure(EntityTypeBuilder<ProductionStation> builder)
    {
        builder.ToTable("production_stations", "production");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new ProductionStationId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.Name).HasMaxLength(100);
        builder.Property(entity => entity.Code).HasMaxLength(30);
        builder.HasIndex(entity => new { entity.UnitId, entity.Code }).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class KitchenTicketConfiguration : IEntityTypeConfiguration<KitchenTicket>
{
    public void Configure(EntityTypeBuilder<KitchenTicket> builder)
    {
        builder.ToTable("kitchen_tickets", "production");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new KitchenTicketId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.OrderId).HasConversion(id => id.Value, value => new OrderId(value));
        builder.Property(entity => entity.ProductionStationId).HasConversion(id => id.Value, value => new ProductionStationId(value));
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(entity => new { entity.ProductionStationId, entity.Status });
        builder.HasIndex(entity => new { entity.UnitId, entity.TicketNumber }).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Order>().WithMany().HasForeignKey(entity => entity.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductionStation>().WithMany().HasForeignKey(entity => entity.ProductionStationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

internal sealed class KitchenTicketItemConfiguration : IEntityTypeConfiguration<KitchenTicketItem>
{
    public void Configure(EntityTypeBuilder<KitchenTicketItem> builder)
    {
        builder.ToTable("kitchen_ticket_items", "production");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new KitchenTicketItemId(value));
        builder.Property(entity => entity.KitchenTicketId).HasConversion(id => id.Value, value => new KitchenTicketId(value));
        builder.Property(entity => entity.OrderItemId).HasConversion(id => id.Value, value => new OrderItemId(value));
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasOne<KitchenTicket>().WithMany().HasForeignKey(entity => entity.KitchenTicketId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OrderItem>().WithMany().HasForeignKey(entity => entity.OrderItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
