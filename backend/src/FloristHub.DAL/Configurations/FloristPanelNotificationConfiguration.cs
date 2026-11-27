using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class FloristPanelNotificationConfiguration : IEntityTypeConfiguration<FloristPanelNotification>
{
    public void Configure(EntityTypeBuilder<FloristPanelNotification> builder)
    {
        builder.HasIndex(x => new { x.TenantId, x.ReadAt, x.CreatedAt });
        builder.HasIndex(x => new { x.TenantId, x.OrderId, x.Kind });
        builder.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
