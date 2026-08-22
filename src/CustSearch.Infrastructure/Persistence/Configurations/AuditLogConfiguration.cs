using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps append-only platform and tenant audit evidence for efficient investigations.
/// </summary>
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", "dbo");
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Id).ValueGeneratedOnAdd();
        builder.Property(audit => audit.ActorType).HasMaxLength(50).IsRequired();
        builder.Property(audit => audit.Action).HasMaxLength(100).IsRequired();
        builder.Property(audit => audit.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(audit => audit.EntityId).HasMaxLength(100);
        builder.Property(audit => audit.BeforeJson).HasMaxLength(4000);
        builder.Property(audit => audit.AfterJson).HasMaxLength(4000);
        builder.Property(audit => audit.IpAddress).HasMaxLength(64).IsUnicode(false);
        builder.Property(audit => audit.UserAgent).HasMaxLength(500);
        builder.Property(audit => audit.CorrelationId).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(audit => audit.CreatedUtc).HasPrecision(7).IsRequired();
        builder.HasOne(audit => audit.Tenant).WithMany().HasForeignKey(audit => audit.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(audit => audit.User).WithMany().HasForeignKey(audit => audit.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(audit => new { audit.TenantId, audit.CreatedUtc });
        builder.HasIndex(audit => new { audit.Action, audit.CreatedUtc });
        builder.HasIndex(audit => audit.CorrelationId);
    }
}
