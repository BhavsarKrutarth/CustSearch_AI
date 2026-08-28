using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

internal sealed class TenantStoragePolicyConfiguration : IEntityTypeConfiguration<TenantStoragePolicy>
{
    public void Configure(EntityTypeBuilder<TenantStoragePolicy> builder)
    {
        builder.ToTable("TenantStoragePolicies", "dbo", table =>
        {
            table.HasCheckConstraint("CK_TenantStoragePolicies_Quota", "[StorageQuotaBytes] BETWEEN 1048576 AND 10995116277760");
            table.HasCheckConstraint("CK_TenantStoragePolicies_Warnings", "[WarningPercent] BETWEEN 1 AND 99 AND [CriticalPercent]>[WarningPercent] AND [CriticalPercent]<=100");
            table.HasCheckConstraint("CK_TenantStoragePolicies_Retention", "[DefaultRetentionDays] BETWEEN 1 AND 3650 AND [MotionSnapshotRetentionDays] BETWEEN 1 AND 3650 AND [MotionClipRetentionDays] BETWEEN 1 AND 3650 AND [FalsePositiveRetentionDays] BETWEEN 1 AND 3650 AND [UnreviewedEvidenceRetentionDays] BETWEEN 1 AND 3650 AND [ConfirmedIncidentRetentionDays] BETWEEN 1 AND 3650");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.QuotaPressurePolicy).HasConversion<byte>();
        builder.Property(x => x.CreatedUtc).HasPrecision(7);
        builder.Property(x => x.UpdatedUtc).HasPrecision(7);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.TenantId).IsUnique();
    }
}

internal sealed class TenantStorageUsageConfiguration : IEntityTypeConfiguration<TenantStorageUsage>
{
    public void Configure(EntityTypeBuilder<TenantStorageUsage> builder)
    {
        builder.ToTable("TenantStorageUsage", "dbo", table =>
            table.HasCheckConstraint("CK_TenantStorageUsage_Bytes", "[QuotaBytes]>=0 AND [UsedBytes]>=0 AND [SnapshotBytes]>=0 AND [MotionClipBytes]>=0 AND [SecurityEvidenceBytes]>=0 AND [OtherBytes]>=0 AND [UsedBytes]=[SnapshotBytes]+[MotionClipBytes]+[SecurityEvidenceBytes]+[OtherBytes] AND [UsedBytes]<=[QuotaBytes]"));
        builder.HasKey(x => x.TenantId);
        builder.Property(x => x.TenantId).ValueGeneratedNever();
        builder.Property(x => x.LastCalculatedUtc).HasPrecision(7);
        builder.Property(x => x.LastCleanupUtc).HasPrecision(7);
        builder.Property(x => x.LastReconciledUtc).HasPrecision(7);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.LastReconciledUtc, x.TenantId });
    }
}

internal sealed class CameraEvidenceConfiguration : IEntityTypeConfiguration<CameraEvidence>
{
    public void Configure(EntityTypeBuilder<CameraEvidence> builder)
    {
        builder.ToTable("CameraEvidence", "dbo", table =>
        {
            table.HasCheckConstraint("CK_CameraEvidence_Type", "[EvidenceType] BETWEEN 1 AND 6");
            table.HasCheckConstraint("CK_CameraEvidence_Size", "[FileSizeBytes]>0");
            table.HasCheckConstraint("CK_CameraEvidence_Retention", "[RetentionUntilUtc]>=[CapturedUtc]");
            // Keep the EF model portable for SQLite tests. SQL Server's versioned script adds
            // the stronger traversal checks; runtime keys are generated internally as well.
            table.HasCheckConstraint("CK_CameraEvidence_Key", "[StorageObjectKey]<>''");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.EvidenceType).HasConversion<byte>();
        builder.Property(x => x.StorageObjectKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ContentHash).HasMaxLength(32).IsFixedLength().IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SourceEventId).HasMaxLength(150).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(150).IsRequired();
        builder.Property(x => x.IngestionHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CapturedUtc).HasPrecision(7);
        builder.Property(x => x.RetentionUntilUtc).HasPrecision(7);
        builder.Property(x => x.DeletedUtc).HasPrecision(7);
        builder.Property(x => x.DeleteReason).HasMaxLength(200);
        builder.Property(x => x.CreatedUtc).HasPrecision(7);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.ServiceId, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.StoreId, x.CameraId, x.CapturedUtc });
        builder.HasIndex(x => new { x.RetentionUntilUtc, x.DeletedUtc, x.IsPinned, x.Id });
    }
}
