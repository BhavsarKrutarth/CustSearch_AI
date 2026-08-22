using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps immutable usage snapshots with one record per tenant reporting period.
/// </summary>
internal sealed class TenantUsageSnapshotConfiguration : IEntityTypeConfiguration<TenantUsageSnapshot>
{
    public void Configure(EntityTypeBuilder<TenantUsageSnapshot> builder)
    {
        builder.ToTable("TenantUsageSnapshots", "dbo", table =>
        {
            table.HasCheckConstraint("CK_TenantUsageSnapshots_Period", "[PeriodEndUtc] > [PeriodStartUtc]");
            table.HasCheckConstraint("CK_TenantUsageSnapshots_Counts", "[StoreCount] >= 0 AND [UserCount] >= 0 AND [CameraCount] >= 0 AND [RecognitionCount] >= 0 AND [ApiCallCount] >= 0");
        });
        builder.HasKey(snapshot => snapshot.Id);
        builder.Property(snapshot => snapshot.Id).ValueGeneratedOnAdd();
        builder.Property(snapshot => snapshot.PeriodStartUtc).HasPrecision(7).IsRequired();
        builder.Property(snapshot => snapshot.PeriodEndUtc).HasPrecision(7).IsRequired();
        builder.Property(snapshot => snapshot.CapturedUtc).HasPrecision(7).IsRequired();
        builder.HasOne(snapshot => snapshot.Tenant).WithMany().HasForeignKey(snapshot => snapshot.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(snapshot => new { snapshot.TenantId, snapshot.PeriodStartUtc, snapshot.PeriodEndUtc }).IsUnique();
    }
}
