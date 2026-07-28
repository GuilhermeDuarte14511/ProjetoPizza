using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoPizza.Domain.Audit;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Identity;
using ProjetoPizza.Domain.Notifications;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Infrastructure.Persistence.Configurations.CrossCutting;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", "notifications");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new NotificationId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.Type).HasMaxLength(60);
        builder.Property(entity => entity.Title).HasMaxLength(120);
        builder.Property(entity => entity.Message).HasMaxLength(1000);
        builder.HasIndex(entity => new { entity.UnitId, entity.ReadAt, entity.CreatedAt });
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", "audit");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion(id => id.Value, value => new AuditLogId(value));
        builder.Property(entity => entity.UnitId).HasConversion(id => id.Value, value => new RestaurantUnitId(value));
        builder.Property(entity => entity.EmployeeId).HasConversion<Guid?>(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new EmployeeId(value.Value) : null);
        builder.Property(entity => entity.Module).HasMaxLength(80);
        builder.Property(entity => entity.Action).HasMaxLength(80);
        builder.Property(entity => entity.EntityType).HasMaxLength(120);
        builder.Property(entity => entity.EntityId).HasMaxLength(100);
        builder.HasIndex(entity => new { entity.UnitId, entity.OccurredAt });
        builder.HasOne<RestaurantUnit>().WithMany().HasForeignKey(entity => entity.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
