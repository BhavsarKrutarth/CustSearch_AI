using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

internal sealed class IntegrationConfigurationConfiguration:IEntityTypeConfiguration<IntegrationConfiguration>
{
    public void Configure(EntityTypeBuilder<IntegrationConfiguration>b)
    {
        b.ToTable("IntegrationConfigurations","dbo",t=>{t.HasCheckConstraint("CK_IntegrationConfigurations_Type","[IntegrationType] BETWEEN 1 AND 4");t.HasCheckConstraint("CK_IntegrationConfigurations_Timeout","[TimeoutSeconds] BETWEEN 1 AND 120");t.HasCheckConstraint("CK_IntegrationConfigurations_Retry","[RetryMaxAttempts] BETWEEN 1 AND 10 AND [RetryBaseDelaySeconds] BETWEEN 1 AND 300");});b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.Provider).HasMaxLength(100).IsRequired();b.Property(x=>x.EndpointBaseUrl).HasMaxLength(500).IsRequired();b.Property(x=>x.CredentialReference).HasMaxLength(200);b.Property(x=>x.WebhookSigningSecretReference).HasMaxLength(200);b.Property(x=>x.PreviousWebhookSigningSecretReference).HasMaxLength(200);b.Property(x=>x.CreatedUtc).HasPrecision(7);b.Property(x=>x.UpdatedUtc).HasPrecision(7);b.Property(x=>x.PreviousSigningSecretValidUntilUtc).HasPrecision(7);b.Property(x=>x.RowVersion).IsRowVersion();b.HasIndex(x=>new{x.TenantId,x.Provider,x.IntegrationType}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.Enabled,x.UpdatedUtc});
    }
}

internal sealed class IntegrationInboundEventConfiguration:IEntityTypeConfiguration<IntegrationInboundEvent>
{
    public void Configure(EntityTypeBuilder<IntegrationInboundEvent>b)
    {
        b.ToTable("IntegrationInboundEvents","dbo",t=>t.HasCheckConstraint("CK_IntegrationInboundEvents_Status","[Status] BETWEEN 1 AND 3"));b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.ProviderEventId).HasMaxLength(200).IsRequired();b.Property(x=>x.IdempotencyKey).HasMaxLength(200).IsRequired();b.Property(x=>x.EventType).HasMaxLength(100).IsRequired();b.Property(x=>x.PayloadHash).HasMaxLength(64).IsRequired();b.Property(x=>x.CorrelationId).HasMaxLength(64).IsRequired();b.Property(x=>x.ProviderTimestampUtc).HasPrecision(7);b.Property(x=>x.ReceivedUtc).HasPrecision(7);b.Property(x=>x.ProcessedUtc).HasPrecision(7);b.HasIndex(x=>new{x.TenantId,x.IntegrationConfigurationId,x.ProviderEventId}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.IntegrationConfigurationId,x.IdempotencyKey}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.ReceivedUtc});
    }
}

internal sealed class IntegrationOutboxMessageConfiguration:IEntityTypeConfiguration<IntegrationOutboxMessage>
{
    public void Configure(EntityTypeBuilder<IntegrationOutboxMessage>b)
    {
        b.ToTable("IntegrationOutbox","dbo",t=>{t.HasCheckConstraint("CK_IntegrationOutbox_Status","[Status] BETWEEN 1 AND 6");t.HasCheckConstraint("CK_IntegrationOutbox_Attempts","[AttemptCount]>=0 AND [MaxAttempts] BETWEEN 1 AND 10");});b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.Provider).HasMaxLength(100).IsRequired();b.Property(x=>x.Destination).HasMaxLength(500).IsRequired();b.Property(x=>x.EventType).HasMaxLength(100).IsRequired();b.Property(x=>x.PayloadJson).IsRequired();b.Property(x=>x.PayloadHash).HasMaxLength(64).IsRequired();b.Property(x=>x.LastError).HasMaxLength(2000);b.Property(x=>x.CorrelationId).HasMaxLength(64).IsRequired();b.Property(x=>x.IdempotencyKey).HasMaxLength(200).IsRequired();b.Property(x=>x.NextAttemptUtc).HasPrecision(7);b.Property(x=>x.CreatedUtc).HasPrecision(7);b.Property(x=>x.DeliveredUtc).HasPrecision(7);b.Property(x=>x.CompletedUtc).HasPrecision(7);b.Property(x=>x.RowVersion).IsRowVersion();b.HasIndex(x=>new{x.TenantId,x.IdempotencyKey}).IsUnique();b.HasIndex(x=>new{x.Status,x.NextAttemptUtc,x.Id});b.HasIndex(x=>new{x.TenantId,x.IntegrationConfigurationId,x.CreatedUtc});
    }
}

internal sealed class IntegrationDeliveryLogConfiguration:IEntityTypeConfiguration<IntegrationDeliveryLog>
{
    public void Configure(EntityTypeBuilder<IntegrationDeliveryLog>b)
    {
        b.ToTable("IntegrationDeliveryLogs","dbo",t=>{t.HasCheckConstraint("CK_IntegrationDeliveryLogs_Direction","[Direction] BETWEEN 1 AND 2");t.HasCheckConstraint("CK_IntegrationDeliveryLogs_Status","[Status] BETWEEN 1 AND 5");t.HasCheckConstraint("CK_IntegrationDeliveryLogs_Source","([InboundEventId] IS NULL AND [OutboxMessageId] IS NOT NULL) OR ([InboundEventId] IS NOT NULL AND [OutboxMessageId] IS NULL)");});b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.CorrelationId).HasMaxLength(64).IsRequired();b.Property(x=>x.Provider).HasMaxLength(100).IsRequired();b.Property(x=>x.ErrorCategory).HasMaxLength(100);b.Property(x=>x.CreatedUtc).HasPrecision(7);b.HasIndex(x=>new{x.TenantId,x.IntegrationConfigurationId,x.CreatedUtc});b.HasIndex(x=>new{x.TenantId,x.Direction,x.Status,x.CreatedUtc});
    }
}
