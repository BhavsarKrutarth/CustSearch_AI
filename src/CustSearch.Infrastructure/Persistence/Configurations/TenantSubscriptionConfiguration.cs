using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>Maps historical tenant plan assignments and Phase 9 server-authoritative billing periods.</summary>
internal sealed class TenantSubscriptionConfiguration:IEntityTypeConfiguration<TenantSubscription>
{
    public void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        builder.ToTable("TenantSubscriptions","dbo",table=>
        {
            table.HasCheckConstraint("CK_TenantSubscriptions_BillingCycle","[BillingCycle] IN (1,2)");
            table.HasCheckConstraint("CK_TenantSubscriptions_Status","[Status] BETWEEN 1 AND 6");
            table.HasCheckConstraint("CK_TenantSubscriptions_Period","[EndsUtc] IS NULL OR [EndsUtc] > [StartsUtc]");
            table.HasCheckConstraint("CK_TenantSubscriptions_CurrentPeriod","[CurrentPeriodEndUtc] IS NULL OR [CurrentPeriodStartUtc] IS NOT NULL AND [CurrentPeriodEndUtc] > [CurrentPeriodStartUtc]");
        });
        builder.HasKey(x=>x.Id);builder.Property(x=>x.Id).ValueGeneratedOnAdd();builder.Property(x=>x.BillingCycle).HasConversion<byte>().IsRequired();builder.Property(x=>x.Status).HasConversion<byte>().IsRequired();builder.Property(x=>x.StartsUtc).HasPrecision(7).IsRequired();builder.Property(x=>x.EndsUtc).HasPrecision(7);builder.Property(x=>x.TrialEndUtc).HasPrecision(7);builder.Property(x=>x.CurrentPeriodStartUtc).HasPrecision(7);builder.Property(x=>x.CurrentPeriodEndUtc).HasPrecision(7);builder.Property(x=>x.CancelledUtc).HasPrecision(7);builder.Property(x=>x.CreatedUtc).HasPrecision(7).IsRequired();builder.Property(x=>x.UpdatedUtc).HasPrecision(7).IsRequired().IsConcurrencyToken();builder.Property(x=>x.RowVersion).HasMaxLength(16).IsFixedLength().IsRequired().IsConcurrencyToken();
        builder.HasOne(x=>x.Tenant).WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Restrict);builder.HasOne(x=>x.SubscriptionPlan).WithMany().HasForeignKey(x=>x.SubscriptionPlanId).OnDelete(DeleteBehavior.Restrict);builder.HasIndex(x=>new{x.TenantId,x.Status,x.StartsUtc});
    }
}
