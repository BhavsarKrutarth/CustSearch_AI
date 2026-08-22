using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps tenant plan history and guarantees valid billing and date ranges.
/// </summary>
internal sealed class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
{
    public void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        builder.ToTable("TenantSubscriptions", "dbo", table =>
        {
            table.HasCheckConstraint("CK_TenantSubscriptions_BillingCycle", "[BillingCycle] IN (1, 2)");
            table.HasCheckConstraint("CK_TenantSubscriptions_Status", "[Status] BETWEEN 1 AND 6");
            table.HasCheckConstraint("CK_TenantSubscriptions_Period", "[EndsUtc] IS NULL OR [EndsUtc] > [StartsUtc]");
        });
        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.Id).ValueGeneratedOnAdd();
        builder.Property(subscription => subscription.BillingCycle).HasConversion<byte>().IsRequired();
        builder.Property(subscription => subscription.Status).HasConversion<byte>().IsRequired();
        builder.Property(subscription => subscription.StartsUtc).HasPrecision(7).IsRequired();
        builder.Property(subscription => subscription.EndsUtc).HasPrecision(7);
        builder.Property(subscription => subscription.CreatedUtc).HasPrecision(7).IsRequired();
        builder.Property(subscription => subscription.UpdatedUtc).HasPrecision(7).IsRequired().IsConcurrencyToken();
        builder.Property(subscription => subscription.RowVersion).HasMaxLength(16).IsFixedLength().IsRequired().IsConcurrencyToken();
        builder.HasOne(subscription => subscription.Tenant).WithMany().HasForeignKey(subscription => subscription.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(subscription => subscription.SubscriptionPlan).WithMany().HasForeignKey(subscription => subscription.SubscriptionPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(subscription => new { subscription.TenantId, subscription.Status, subscription.StartsUtc });
    }
}
