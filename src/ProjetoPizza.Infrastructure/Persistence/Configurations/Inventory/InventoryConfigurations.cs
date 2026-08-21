using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoPizza.Domain.Catalog;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Identity;
using ProjetoPizza.Domain.Inventory;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items", "inventory");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new InventoryItemId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.Name).HasMaxLength(120);
        builder.Property(entity => entity.Sku).HasMaxLength(50);
        builder.Property(entity => entity.UnitOfMeasure).HasMaxLength(20);
        builder.Property(entity => entity.MinimumStock).HasPrecision(18, 4);
        builder.Property(entity => entity.UnitCost).HasMoneyConversion();
        builder.HasIndex(entity => new { entity.UnitId, entity.Sku }).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.ToTable("stock_balances", "inventory");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new StockBalanceId(value));
        builder.Property(entity => entity.InventoryItemId).HasConversion(id => id.Value, value => new InventoryItemId(value));
        builder.Property(entity => entity.CurrentQuantity).HasPrecision(18, 4);
        builder.Property(entity => entity.ReservedQuantity).HasPrecision(18, 4);
        builder.Ignore(entity => entity.AvailableQuantity);
        builder.HasIndex(entity => entity.InventoryItemId).IsUnique();
        builder.HasOne<InventoryItem>().WithOne().HasForeignKey<StockBalance>(entity => entity.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements", "inventory");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new StockMovementId(value));
        builder.Property(entity => entity.InventoryItemId).HasConversion(id => id.Value, value => new InventoryItemId(value));
        builder.Property(entity => entity.OrderItemId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new OrderItemId(value.Value) : null);
        builder.Property(entity => entity.CreatedByEmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.Property(entity => entity.MovementType).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.Quantity).HasPrecision(18, 4);
        builder.Property(entity => entity.UnitCost).HasMoneyConversion();
        builder.Property(entity => entity.Reason).HasMaxLength(300);
        builder.HasIndex(entity => new { entity.InventoryItemId, entity.CreatedAt });
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(entity => entity.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OrderItem>().WithMany().HasForeignKey(entity => entity.OrderItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.CreatedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable("inventory_reservations", "inventory");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new InventoryReservationId(value));
        builder.Property(entity => entity.InventoryItemId).HasConversion(id => id.Value, value => new InventoryItemId(value));
        builder.Property(entity => entity.OrderItemId).HasConversion(id => id.Value, value => new OrderItemId(value));
        builder.Property(entity => entity.Quantity).HasPrecision(18, 4);
        builder.Property(entity => entity.UnitCost).HasMoneyConversion();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(entity => new { entity.OrderItemId, entity.InventoryItemId }).IsUnique();
        builder.HasIndex(entity => new { entity.InventoryItemId, entity.Status });
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(entity => entity.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OrderItem>().WithMany().HasForeignKey(entity => entity.OrderItemId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("recipes", "inventory");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new RecipeId(value));
        builder.Property(entity => entity.ProductId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new ProductId(value.Value) : null);
        builder.Property(entity => entity.ProductVariantId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new ProductVariantId(value.Value) : null);
        builder.Property(entity => entity.PizzaFlavorId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new PizzaFlavorId(value.Value) : null);
        builder.Property(entity => entity.PizzaSizeId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new PizzaSizeId(value.Value) : null);
        builder.Property(entity => entity.YieldQuantity).HasPrecision(18, 4);
        builder.HasOne<Product>().WithMany().HasForeignKey(entity => entity.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(entity => entity.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PizzaFlavor>().WithMany().HasForeignKey(entity => entity.PizzaFlavorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PizzaSize>().WithMany().HasForeignKey(entity => entity.PizzaSizeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RecipeItemConfiguration : IEntityTypeConfiguration<RecipeItem>
{
    public void Configure(EntityTypeBuilder<RecipeItem> builder)
    {
        builder.ToTable("recipe_items", "inventory");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new RecipeItemId(value));
        builder.Property(entity => entity.RecipeId).HasConversion(id => id.Value, value => new RecipeId(value));
        builder.Property(entity => entity.InventoryItemId).HasConversion(id => id.Value, value => new InventoryItemId(value));
        builder.Property(entity => entity.Quantity).HasPrecision(18, 4);
        builder.Property(entity => entity.UnitOfMeasure).HasMaxLength(20);
        builder.HasOne<Recipe>().WithMany().HasForeignKey(entity => entity.RecipeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(entity => entity.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
