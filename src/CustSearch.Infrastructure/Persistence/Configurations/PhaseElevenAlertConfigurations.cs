using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>Maps authoritative tenant/store alerts and their tenant-wide deduplication key.</summary>
internal sealed class AlertConfiguration:IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert>b)
    {
        b.ToTable("Alerts","dbo",t=>{t.HasCheckConstraint("CK_Alerts_Severity","[Severity] BETWEEN 1 AND 3");t.HasCheckConstraint("CK_Alerts_Status","[Status] BETWEEN 1 AND 5");t.HasCheckConstraint("CK_Alerts_Acknowledgement","([AcknowledgedUtc] IS NULL AND [AcknowledgedByUserId] IS NULL) OR ([AcknowledgedUtc] IS NOT NULL AND [AcknowledgedByUserId] IS NOT NULL)");});
        b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.AlertType).HasMaxLength(100).IsRequired();b.Property(x=>x.Title).HasMaxLength(200).IsRequired();b.Property(x=>x.Message).HasMaxLength(2000).IsRequired();b.Property(x=>x.EntityType).HasMaxLength(100).IsRequired();b.Property(x=>x.EntityId).HasMaxLength(100);b.Property(x=>x.CorrelationId).HasMaxLength(64).IsRequired();b.Property(x=>x.DeduplicationKey).HasMaxLength(200).IsRequired();b.Property(x=>x.CreatedUtc).HasPrecision(7);b.Property(x=>x.AcknowledgedUtc).HasPrecision(7);b.Property(x=>x.ResolvedUtc).HasPrecision(7);
        b.HasIndex(x=>new{x.TenantId,x.DeduplicationKey}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.StoreId,x.Status,x.CreatedUtc});b.HasIndex(x=>new{x.TenantId,x.EntityType,x.EntityId});
    }
}

/// <summary>Maps the durable ordered replay cursor for reliable reconnect recovery.</summary>
internal sealed class RealtimeEventConfiguration:IEntityTypeConfiguration<RealtimeEvent>
{
    public void Configure(EntityTypeBuilder<RealtimeEvent>b)
    {
        b.ToTable("RealtimeEvents","dbo");b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.EventName).HasMaxLength(100).IsRequired();b.Property(x=>x.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();b.Property(x=>x.CorrelationId).HasMaxLength(64).IsRequired();b.Property(x=>x.DeduplicationKey).HasMaxLength(200).IsRequired();b.Property(x=>x.OccurredUtc).HasPrecision(7);b.HasIndex(x=>new{x.TenantId,x.DeduplicationKey}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.StoreId,x.Id});b.HasIndex(x=>new{x.TenantId,x.OccurredUtc});
    }
}

/// <summary>Maps reliable channel delivery state with unique idempotency and optimistic claims.</summary>
internal sealed class NotificationOutboxMessageConfiguration:IEntityTypeConfiguration<NotificationOutboxMessage>
{
    public void Configure(EntityTypeBuilder<NotificationOutboxMessage>b)
    {
        b.ToTable("NotificationOutbox","dbo",t=>{t.HasCheckConstraint("CK_NotificationOutbox_Status","[Status] BETWEEN 1 AND 6");t.HasCheckConstraint("CK_NotificationOutbox_AttemptCount","[AttemptCount]>=0");});b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.Channel).HasMaxLength(30).IsRequired();b.Property(x=>x.EventType).HasMaxLength(100).IsRequired();b.Property(x=>x.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();b.Property(x=>x.LastError).HasMaxLength(2000);b.Property(x=>x.CorrelationId).HasMaxLength(64).IsRequired();b.Property(x=>x.IdempotencyKey).HasMaxLength(200).IsRequired();b.Property(x=>x.CreatedUtc).HasPrecision(7);b.Property(x=>x.NextAttemptUtc).HasPrecision(7);b.Property(x=>x.ProcessedUtc).HasPrecision(7);b.Property(x=>x.RowVersion).IsRowVersion();b.HasIndex(x=>x.IdempotencyKey).IsUnique();b.HasIndex(x=>new{x.Status,x.NextAttemptUtc,x.Id});b.HasIndex(x=>new{x.TenantId,x.Status,x.CreatedUtc});
    }
}
