using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Devices;
using ProjetoPizza.Domain.Dining;
using ProjetoPizza.Domain.Identity;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Infrastructure.Persistence.Configurations.Dining;

internal sealed class DiningAreaConfiguration : IEntityTypeConfiguration<DiningArea>
{
    public void Configure(EntityTypeBuilder<DiningArea> builder)
    {
        builder.ToTable("dining_areas", "dining");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new DiningAreaId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.Name).HasMaxLength(100);
        builder.Property(entity => entity.Description).HasMaxLength(500);
        builder.HasIndex(entity => new { entity.UnitId, entity.Name }).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RestaurantTableConfiguration : IEntityTypeConfiguration<RestaurantTable>
{
    public void Configure(EntityTypeBuilder<RestaurantTable> builder)
    {
        builder.ToTable("restaurant_tables", "dining");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new RestaurantTableId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.DiningAreaId).HasConversion(id => id.Value, value => new DiningAreaId(value));
        builder.Property(entity => entity.Name).HasMaxLength(80);
        builder.HasIndex(entity => new { entity.UnitId, entity.Number }).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DiningArea>().WithMany().HasForeignKey(entity => entity.DiningAreaId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

internal sealed class TableSessionConfiguration : IEntityTypeConfiguration<TableSession>
{
    public void Configure(EntityTypeBuilder<TableSession> builder)
    {
        builder.ToTable("table_sessions", "dining");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new TableSessionId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.PrimaryWaiterId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new EmployeeId(value.Value) : null);
        builder.Property(entity => entity.OpenedByEmployeeId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new EmployeeId(value.Value) : null);
        builder.Property(entity => entity.OpenedByDeviceId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new DeviceId(value.Value) : null);
        builder.Property(entity => entity.ClosedByEmployeeId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new EmployeeId(value.Value) : null);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.ServiceFeePercentageSnapshot).HasPercentageConversion();
        builder.Property(entity => entity.Notes).HasMaxLength(1000);
        builder.HasIndex(entity => new { entity.UnitId, entity.Status });
        builder.HasIndex(entity => new { entity.UnitId, entity.SessionNumber }).IsUnique();
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.OpenedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Device>().WithMany().HasForeignKey(entity => entity.OpenedByDeviceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.PrimaryWaiterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.ClosedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(entity => entity.Tables).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

internal sealed class TableSessionTableConfiguration : IEntityTypeConfiguration<TableSessionTable>
{
    public void Configure(EntityTypeBuilder<TableSessionTable> builder)
    {
        builder.ToTable("table_session_tables", "dining");
        builder.HasKey(entity => new { entity.TableSessionId, entity.RestaurantTableId, entity.LinkedAt });
        builder.Property(entity => entity.TableSessionId).HasConversion(id => id.Value, value => new TableSessionId(value));
        builder.Property(entity => entity.RestaurantTableId).HasConversion(id => id.Value, value => new RestaurantTableId(value));
        builder.Property(entity => entity.LinkedByEmployeeId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new EmployeeId(value.Value) : null);
        builder.Property(entity => entity.LinkedByDeviceId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new DeviceId(value.Value) : null);
        builder.HasIndex(entity => new { entity.RestaurantTableId, entity.UnlinkedAt });
        builder.HasOne<TableSession>().WithMany(entity => entity.Tables).HasForeignKey(entity => entity.TableSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RestaurantTable>().WithMany().HasForeignKey(entity => entity.RestaurantTableId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.LinkedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Device>().WithMany().HasForeignKey(entity => entity.LinkedByDeviceId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WaiterAssignmentConfiguration : IEntityTypeConfiguration<WaiterAssignment>
{
    public void Configure(EntityTypeBuilder<WaiterAssignment> builder)
    {
        builder.ToTable("waiter_assignments", "dining");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new WaiterAssignmentId(value));
        builder.Property(entity => entity.TableSessionId).HasConversion(id => id.Value, value => new TableSessionId(value));
        builder.Property(entity => entity.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.Property(entity => entity.AssignedByEmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.HasOne<TableSession>().WithMany().HasForeignKey(entity => entity.TableSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.AssignedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ServiceCallTypeConfiguration : IEntityTypeConfiguration<ServiceCallType>
{
    public void Configure(EntityTypeBuilder<ServiceCallType> builder)
    {
        builder.ToTable("service_call_types", "dining");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new ServiceCallTypeId(value));
        builder.Property(entity => entity.Code).HasMaxLength(50);
        builder.Property(entity => entity.Name).HasMaxLength(100);
        builder.HasIndex(entity => entity.Code).IsUnique();
    }
}

internal sealed class ServiceCallConfiguration : IEntityTypeConfiguration<ServiceCall>
{
    public void Configure(EntityTypeBuilder<ServiceCall> builder)
    {
        builder.ToTable("service_calls", "dining");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new ServiceCallId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.TableSessionId).HasConversion(id => id.Value, value => new TableSessionId(value));
        builder.Property(entity => entity.ServiceCallTypeId).HasConversion(id => id.Value, value => new ServiceCallTypeId(value));
        builder.Property(entity => entity.RequestedByDeviceId).HasConversion(id => id.Value, value => new DeviceId(value));
        builder.Property(entity => entity.AssignedEmployeeId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new EmployeeId(value.Value) : null);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(entity => entity.Details).HasMaxLength(1000);
        builder.HasIndex(entity => new { entity.UnitId, entity.Status, entity.CreatedAt });
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TableSession>().WithMany().HasForeignKey(entity => entity.TableSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ServiceCallType>().WithMany().HasForeignKey(entity => entity.ServiceCallTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Device>().WithMany().HasForeignKey(entity => entity.RequestedByDeviceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.AssignedEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
