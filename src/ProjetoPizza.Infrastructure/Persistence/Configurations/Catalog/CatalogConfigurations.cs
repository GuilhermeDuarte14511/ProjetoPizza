using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoPizza.Domain.Catalog;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Inventory;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories", "catalog");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new CategoryId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.ParentCategoryId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new CategoryId(value.Value) : null);
        builder.Property(entity => entity.Name).HasMaxLength(100);
        builder.Property(entity => entity.Description).HasMaxLength(500);
        builder.Property(entity => entity.Slug).HasMaxLength(120);
        builder.Property(entity => entity.Icon).HasMaxLength(80);
        builder.HasIndex(entity => new { entity.UnitId, entity.Slug }).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>().WithMany().HasForeignKey(entity => entity.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", "catalog");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new ProductId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.CategoryId).HasConversion(id => id.Value, value => new CategoryId(value));
        builder.Property(entity => entity.Sku).HasMaxLength(50);
        builder.Property(entity => entity.Name).HasMaxLength(140);
        builder.Property(entity => entity.Description).HasMaxLength(1000);
        builder.Property(entity => entity.ProductType).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.BasePrice).HasMoneyConversion();
        builder.HasIndex(entity => new { entity.UnitId, entity.CategoryId, entity.IsActive });
        builder.HasIndex(entity => new { entity.UnitId, entity.Sku }).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>().WithMany().HasForeignKey(entity => entity.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

internal sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants", "catalog");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new ProductVariantId(value));
        builder.Property(entity => entity.ProductId).HasConversion(id => id.Value, value => new ProductId(value));
        builder.Property(entity => entity.Name).HasMaxLength(100);
        builder.Property(entity => entity.Sku).HasMaxLength(50);
        builder.Property(entity => entity.Price).HasMoneyConversion();
        builder.HasIndex(entity => entity.Sku).IsUnique();
        builder.HasOne<Product>().WithMany().HasForeignKey(entity => entity.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProductExtraConfiguration : IEntityTypeConfiguration<ProductExtra>
{
    public void Configure(EntityTypeBuilder<ProductExtra> builder)
    {
        builder.ToTable("product_extras", "catalog");
        builder.HasKey(entity => new { entity.ProductId, entity.IngredientId });
        builder.Property(entity => entity.ProductId).HasConversion(id => id.Value, value => new ProductId(value));
        builder.Property(entity => entity.IngredientId).HasConversion(id => id.Value, value => new IngredientId(value));
        builder.Property(entity => entity.Price).HasMoneyConversion();
        builder.Property(entity => entity.MaxQuantity);
        builder.Property(entity => entity.IsActive);
        builder.HasIndex(entity => new { entity.ProductId, entity.IsActive });
        builder.HasOne<Product>().WithMany().HasForeignKey(entity => entity.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Ingredient>().WithMany().HasForeignKey(entity => entity.IngredientId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images", "catalog");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new ProductImageId(value));
        builder.Property(entity => entity.ProductId).HasConversion(id => id.Value, value => new ProductId(value));
        builder.Property(entity => entity.Url).HasMaxLength(1000);
        builder.Property(entity => entity.AltText).HasMaxLength(160);
        builder.HasOne<Product>().WithMany().HasForeignKey(entity => entity.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PizzaSizeConfiguration : IEntityTypeConfiguration<PizzaSize>
{
    public void Configure(EntityTypeBuilder<PizzaSize> builder)
    {
        builder.ToTable("pizza_sizes", "catalog");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new PizzaSizeId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.Name).HasMaxLength(80);
        builder.Property(entity => entity.ShortName).HasMaxLength(12);
        builder.Property(entity => entity.DiameterCm).HasPrecision(6, 2);
        builder.Property(entity => entity.BasePrice).HasMoneyConversion();
        builder.HasIndex(entity => new { entity.UnitId, entity.Name }).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PizzaFlavorConfiguration : IEntityTypeConfiguration<PizzaFlavor>
{
    public void Configure(EntityTypeBuilder<PizzaFlavor> builder)
    {
        builder.ToTable("pizza_flavors", "catalog");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new PizzaFlavorId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.CategoryId).HasConversion(id => id.Value, value => new CategoryId(value));
        builder.Property(entity => entity.Name).HasMaxLength(120);
        builder.Property(entity => entity.Description).HasMaxLength(1000);
        builder.Property(entity => entity.FlavorType).HasConversion<string>().HasMaxLength(20);
        builder.Property(entity => entity.SoldOutReason).HasMaxLength(300);
        builder.Property(entity => entity.ImageUrl).HasMaxLength(1000);
        builder.HasIndex(entity => new { entity.UnitId, entity.Name }).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>().WithMany().HasForeignKey(entity => entity.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PizzaFlavorPriceConfiguration : IEntityTypeConfiguration<PizzaFlavorPrice>
{
    public void Configure(EntityTypeBuilder<PizzaFlavorPrice> builder)
    {
        builder.ToTable("pizza_flavor_prices", "catalog");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new PizzaFlavorPriceId(value));
        builder.Property(entity => entity.PizzaFlavorId).HasConversion(id => id.Value, value => new PizzaFlavorId(value));
        builder.Property(entity => entity.PizzaSizeId).HasConversion(id => id.Value, value => new PizzaSizeId(value));
        builder.Property(entity => entity.Price).HasMoneyConversion();
        builder.Property(entity => entity.AdditionalPrice).HasMoneyConversion();
        builder.HasIndex(entity => new { entity.PizzaFlavorId, entity.PizzaSizeId }).IsUnique();
        builder.HasOne<PizzaFlavor>().WithMany().HasForeignKey(entity => entity.PizzaFlavorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PizzaSize>().WithMany().HasForeignKey(entity => entity.PizzaSizeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PizzaCrustConfiguration : IEntityTypeConfiguration<PizzaCrust>
{
    public void Configure(EntityTypeBuilder<PizzaCrust> builder)
    {
        builder.ToTable("pizza_crusts", "catalog");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new PizzaCrustId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.Name).HasMaxLength(100);
        builder.Property(entity => entity.Description).HasMaxLength(500);
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PizzaCrustPriceConfiguration : IEntityTypeConfiguration<PizzaCrustPrice>
{
    public void Configure(EntityTypeBuilder<PizzaCrustPrice> builder)
    {
        builder.ToTable("pizza_crust_prices", "catalog");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new PizzaCrustPriceId(value));
        builder.Property(entity => entity.PizzaCrustId).HasConversion(id => id.Value, value => new PizzaCrustId(value));
        builder.Property(entity => entity.PizzaSizeId).HasConversion(id => id.Value, value => new PizzaSizeId(value));
        builder.Property(entity => entity.AdditionalPrice).HasMoneyConversion();
        builder.Property(entity => entity.HalfAdditionalPrice).HasMoneyConversion();
        builder.HasIndex(entity => new { entity.PizzaCrustId, entity.PizzaSizeId }).IsUnique();
        builder.HasOne<PizzaCrust>().WithMany().HasForeignKey(entity => entity.PizzaCrustId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PizzaSize>().WithMany().HasForeignKey(entity => entity.PizzaSizeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> builder)
    {
        builder.ToTable("ingredients", "catalog");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new IngredientId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.InventoryItemId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new InventoryItemId(value.Value) : null);
        builder.Property(entity => entity.Name).HasMaxLength(120);
        builder.Property(entity => entity.Description).HasMaxLength(500);
        builder.Property(entity => entity.AllergenDescription).HasMaxLength(300);
        builder.Property(entity => entity.ExtraPrice).HasMoneyConversion();
        builder.HasIndex(entity => new { entity.UnitId, entity.IsActive, entity.IsAvailableAsExtra });
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(entity => entity.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PizzaFlavorIngredientConfiguration : IEntityTypeConfiguration<PizzaFlavorIngredient>
{
    public void Configure(EntityTypeBuilder<PizzaFlavorIngredient> builder)
    {
        builder.ToTable("pizza_flavor_ingredients", "catalog");
        builder.HasKey(entity => new { entity.PizzaFlavorId, entity.IngredientId });
        builder.Property(entity => entity.PizzaFlavorId).HasConversion(id => id.Value, value => new PizzaFlavorId(value));
        builder.Property(entity => entity.IngredientId).HasConversion(id => id.Value, value => new IngredientId(value));
        builder.Property(entity => entity.Quantity).HasPrecision(18, 4);
        builder.Property(entity => entity.UnitOfMeasure).HasMaxLength(20);
        builder.HasOne<PizzaFlavor>().WithMany().HasForeignKey(entity => entity.PizzaFlavorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Ingredient>().WithMany().HasForeignKey(entity => entity.IngredientId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PizzaFlavorExtraConfiguration : IEntityTypeConfiguration<PizzaFlavorExtra>
{
    public void Configure(EntityTypeBuilder<PizzaFlavorExtra> builder)
    {
        builder.ToTable("pizza_flavor_extras", "catalog");
        builder.HasKey(entity => new { entity.PizzaFlavorId, entity.IngredientId });
        builder.Property(entity => entity.PizzaFlavorId)
            .HasConversion(id => id.Value, value => new PizzaFlavorId(value));
        builder.Property(entity => entity.IngredientId)
            .HasConversion(id => id.Value, value => new IngredientId(value));
        builder.Property(entity => entity.Price).HasMoneyConversion();
        builder.HasIndex(entity => new { entity.PizzaFlavorId, entity.IsActive });
        builder.HasOne<PizzaFlavor>()
            .WithMany()
            .HasForeignKey(entity => entity.PizzaFlavorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Ingredient>()
            .WithMany()
            .HasForeignKey(entity => entity.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
