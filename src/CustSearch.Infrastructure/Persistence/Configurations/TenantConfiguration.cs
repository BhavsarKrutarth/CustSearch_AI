using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the root tenant/client organization boundary.
/// </summary>
internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Tenants_ActiveSuspended", "NOT ([IsActive] = 0 AND [IsSuspended] = 1)");
            table.HasCheckConstraint("CK_Tenants_SubscriptionStatus", "[SubscriptionStatus] BETWEEN 1 AND 6");
            table.HasCheckConstraint("CK_Tenants_Quotas", "[MaxStores] > 0 AND [MaxUsers] > 0 AND [MaxCameras] > 0");
            table.HasCheckConstraint("CK_Tenants_TrialPeriod", "[TrialEndsUtc] IS NULL OR [TrialStartsUtc] IS NULL OR [TrialEndsUtc] > [TrialStartsUtc]");
            table.HasCheckConstraint("CK_Tenants_SubscriptionPeriod", "[SubscriptionEndsUtc] IS NULL OR [SubscriptionStartsUtc] IS NULL OR [SubscriptionEndsUtc] > [SubscriptionStartsUtc]");
        });
        builder.HasKey(tenant => tenant.Id);
        builder.Property(tenant => tenant.Id).ValueGeneratedOnAdd();
        builder.Property(tenant => tenant.TenantCode).HasMaxLength(30).IsRequired();
        builder.Property(tenant => tenant.LegalName).HasMaxLength(200).IsRequired();
        builder.Property(tenant => tenant.DisplayName).HasMaxLength(150).IsRequired();
        builder.Property(tenant => tenant.PrimaryContactName).HasMaxLength(150).IsRequired();
        builder.Property(tenant => tenant.PrimaryEmail).HasMaxLength(254).IsRequired();
        builder.Property(tenant => tenant.PrimaryMobile).HasMaxLength(30).IsRequired();
        builder.Property(tenant => tenant.CountryCode).HasMaxLength(2).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(tenant => tenant.TimeZone).HasMaxLength(100).IsRequired();
        builder.Property(tenant => tenant.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(tenant => tenant.SubscriptionStatus).HasConversion<byte>().IsRequired();
        builder.Property(tenant => tenant.TrialStartsUtc).HasPrecision(7);
        builder.Property(tenant => tenant.TrialEndsUtc).HasPrecision(7);
        builder.Property(tenant => tenant.SubscriptionStartsUtc).HasPrecision(7);
        builder.Property(tenant => tenant.SubscriptionEndsUtc).HasPrecision(7);
        builder.Property(tenant => tenant.SuspensionReason).HasMaxLength(500);
        builder.Property(tenant => tenant.CreatedUtc).HasPrecision(7).IsRequired();
        builder.Property(tenant => tenant.UpdatedUtc).HasPrecision(7).IsRequired().IsConcurrencyToken();
        builder.Property(tenant => tenant.RowVersion).HasMaxLength(16).IsFixedLength().IsRequired().IsConcurrencyToken();
        builder.HasOne(tenant => tenant.SubscriptionPlan)
            .WithMany()
            .HasForeignKey(tenant => tenant.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(tenant => tenant.TenantCode).IsUnique();
        builder.HasIndex(tenant => new { tenant.IsActive, tenant.IsSuspended, tenant.SubscriptionStatus });
    }
}
