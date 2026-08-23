using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>Maps the root tenant/client organization boundary and effective plan quotas.</summary>
internal sealed class TenantConfiguration:IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants","dbo",table=>
        {
            table.HasCheckConstraint("CK_Tenants_ActiveSuspended","NOT ([IsActive] = 0 AND [IsSuspended] = 1)");
            table.HasCheckConstraint("CK_Tenants_SubscriptionStatus","[SubscriptionStatus] BETWEEN 1 AND 6");
            table.HasCheckConstraint("CK_Tenants_Quotas","[MaxStores] > 0 AND [MaxUsers] > 0 AND [MaxStaff] > 0 AND [MaxCameras] > 0");
            table.HasCheckConstraint("CK_Tenants_TrialPeriod","[TrialEndsUtc] IS NULL OR [TrialStartsUtc] IS NULL OR [TrialEndsUtc] > [TrialStartsUtc]");
            table.HasCheckConstraint("CK_Tenants_SubscriptionPeriod","[SubscriptionEndsUtc] IS NULL OR [SubscriptionStartsUtc] IS NULL OR [SubscriptionEndsUtc] > [SubscriptionStartsUtc]");
        });
        builder.HasKey(x=>x.Id);builder.Property(x=>x.Id).ValueGeneratedOnAdd();builder.Property(x=>x.TenantCode).HasMaxLength(30).IsRequired();builder.Property(x=>x.LegalName).HasMaxLength(200).IsRequired();builder.Property(x=>x.DisplayName).HasMaxLength(150).IsRequired();builder.Property(x=>x.PrimaryContactName).HasMaxLength(150).IsRequired();builder.Property(x=>x.PrimaryEmail).HasMaxLength(254).IsRequired();builder.Property(x=>x.PrimaryMobile).HasMaxLength(30).IsRequired();builder.Property(x=>x.CountryCode).HasMaxLength(2).IsUnicode(false).IsFixedLength().IsRequired();builder.Property(x=>x.TimeZone).HasMaxLength(100).IsRequired();builder.Property(x=>x.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsFixedLength().IsRequired();builder.Property(x=>x.SubscriptionStatus).HasConversion<byte>().IsRequired();builder.Property(x=>x.TrialStartsUtc).HasPrecision(7);builder.Property(x=>x.TrialEndsUtc).HasPrecision(7);builder.Property(x=>x.SubscriptionStartsUtc).HasPrecision(7);builder.Property(x=>x.SubscriptionEndsUtc).HasPrecision(7);builder.Property(x=>x.SuspensionReason).HasMaxLength(500);builder.Property(x=>x.CreatedUtc).HasPrecision(7).IsRequired();builder.Property(x=>x.UpdatedUtc).HasPrecision(7).IsRequired().IsConcurrencyToken();builder.Property(x=>x.RowVersion).HasMaxLength(16).IsFixedLength().IsRequired().IsConcurrencyToken();builder.HasOne(x=>x.SubscriptionPlan).WithMany().HasForeignKey(x=>x.SubscriptionPlanId).OnDelete(DeleteBehavior.Restrict);builder.HasIndex(x=>x.TenantCode).IsUnique();builder.HasIndex(x=>new{x.IsActive,x.IsSuspended,x.SubscriptionStatus});
    }
}
