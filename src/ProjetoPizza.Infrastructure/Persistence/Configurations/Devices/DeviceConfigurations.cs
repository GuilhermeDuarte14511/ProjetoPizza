using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Devices;
using ProjetoPizza.Domain.Dining;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Infrastructure.Persistence.Configurations.Devices;

internal sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("devices", "devices");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new DeviceId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.LinkedTableId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new RestaurantTableId(value.Value) : null);
        builder.Property(entity => entity.Name).HasMaxLength(100);
        builder.Property(entity => entity.SerialNumber).HasMaxLength(100);
        builder.Property(entity => entity.DeviceType).HasConversion<string>().HasMaxLength(40);
        builder.Property(entity => entity.Platform).HasMaxLength(60);
        builder.Property(entity => entity.AppVersion).HasMaxLength(30);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.NetworkStatus).HasMaxLength(40);
        builder.Property(entity => entity.IpAddress).HasMaxLength(255);
        builder.HasIndex(entity => new { entity.UnitId, entity.Status });
        builder.HasIndex(entity => entity.SerialNumber).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RestaurantTable>().WithMany().HasForeignKey(entity => entity.LinkedTableId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

internal sealed class PrintJobConfiguration : IEntityTypeConfiguration<PrintJob>
{
    public void Configure(EntityTypeBuilder<PrintJob> builder)
    {
        builder.ToTable("print_jobs", "devices");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new PrintJobId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.PrinterId).HasConversion(id => id.Value, value => new DeviceId(value));
        builder.Property(entity => entity.DocumentType).HasConversion<string>().HasMaxLength(40);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.Payload).HasMaxLength(20000);
        builder.Property(entity => entity.LastError).HasMaxLength(1000);
        builder.HasIndex(entity => new { entity.Status, entity.NextAttemptAt });
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Device>().WithMany().HasForeignKey(entity => entity.PrinterId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

internal sealed class DeviceSessionConfiguration : IEntityTypeConfiguration<DeviceSession>
{
    public void Configure(EntityTypeBuilder<DeviceSession> builder)
    {
        builder.ToTable("device_sessions", "devices");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new DeviceSessionId(value));
        builder.Property(entity => entity.DeviceId).HasConversion(id => id.Value, value => new DeviceId(value));
        builder.Property(entity => entity.TableSessionId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new TableSessionId(value.Value) : null);
        builder.Property(entity => entity.SessionTokenHash).HasMaxLength(256);
        builder.Property(entity => entity.EndedReason).HasMaxLength(200);
        builder.HasIndex(entity => entity.SessionTokenHash).IsUnique();
        builder.HasIndex(entity => new { entity.DeviceId, entity.EndedAt, entity.ExpiresAt });
        builder.HasOne<Device>().WithMany().HasForeignKey(entity => entity.DeviceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TableSession>().WithMany().HasForeignKey(entity => entity.TableSessionId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DeviceProvisioningConfiguration : IEntityTypeConfiguration<DeviceProvisioning>
{
    public void Configure(EntityTypeBuilder<DeviceProvisioning> builder)
    {
        builder.ToTable("device_provisionings", "devices");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new DeviceProvisioningId(value));
        builder.Property(entity => entity.DeviceId).HasConversion(id => id.Value, value => new DeviceId(value));
        builder.Property(entity => entity.TokenHash).HasMaxLength(64);
        builder.HasIndex(entity => entity.TokenHash).IsUnique();
        builder.HasIndex(entity => new { entity.DeviceId, entity.ExpiresAt });
        builder.HasOne<Device>().WithMany().HasForeignKey(entity => entity.DeviceId).OnDelete(DeleteBehavior.Cascade);
    }
}
