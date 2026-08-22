using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps auditable quota overrides and keeps their actor and tenant references intact.
/// </summary>
internal sealed class TenantQuotaOverrideConfiguration : IEntityTypeConfiguration<TenantQuotaOverride>
{
    public void Configure(EntityTypeBuilder<TenantQuotaOverride> builder)
    {
        builder.ToTable("TenantQuotaOverrides", "dbo", table =>
        {
            table.HasCheckConstraint("CK_TenantQuotaOverrides_AnyLimit", "[MaxStores] IS NOT NULL OR [MaxUsers] IS NOT NULL OR [MaxCameras] IS NOT NULL OR [MaxMonthlyRecognitions] IS NOT NULL OR [MaxMonthlyApiCalls] IS NOT NULL");
            table.HasCheckConstraint("CK_TenantQuotaOverrides_Limits", "([MaxStores] IS NULL OR [MaxStores] > 0) AND ([MaxUsers] IS NULL OR [MaxUsers] > 0) AND ([MaxCameras] IS NULL OR [MaxCameras] > 0) AND ([MaxMonthlyRecognitions] IS NULL OR [MaxMonthlyRecognitions] > 0) AND ([MaxMonthlyApiCalls] IS NULL OR [MaxMonthlyApiCalls] > 0)");
            table.HasCheckConstraint("CK_TenantQuotaOverrides_Expiry", "[ExpiresUtc] IS NULL OR [ExpiresUtc] > [CreatedUtc]");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedOnAdd();
        builder.Property(item => item.Reason).HasMaxLength(500).IsRequired();
        builder.Property(item => item.CreatedUtc).HasPrecision(7).IsRequired();
        builder.Property(item => item.ExpiresUtc).HasPrecision(7);
        builder.HasOne(item => item.Tenant).WithMany().HasForeignKey(item => item.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.TenantId, item.CreatedUtc });
        builder.HasIndex(item => item.ExpiresUtc);
    }
}
