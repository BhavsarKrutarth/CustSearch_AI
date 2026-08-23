using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>Maps the CustSearch platform subscription plan catalog and authoritative quota defaults.</summary>
internal sealed class SubscriptionPlanConfiguration:IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans","dbo",table=>
        {
            table.HasCheckConstraint("CK_SubscriptionPlans_Prices","[MonthlyPrice] >= 0 AND ([AnnualPrice] IS NULL OR [AnnualPrice] >= 0)");
            table.HasCheckConstraint("CK_SubscriptionPlans_Limits","[MaxStores] > 0 AND [MaxUsers] > 0 AND [MaxStaff] > 0 AND [MaxCameras] > 0 AND ([MaxMonthlyRecognitions] IS NULL OR [MaxMonthlyRecognitions] > 0) AND ([MaxMonthlyApiCalls] IS NULL OR [MaxMonthlyApiCalls] > 0)");
            table.HasCheckConstraint("CK_SubscriptionPlans_TrialDisplay","[TrialDays] >= 0 AND [DisplayOrder] >= 0");
        });
        builder.HasKey(x=>x.Id);builder.Property(x=>x.Id).ValueGeneratedOnAdd();builder.Property(x=>x.PlanCode).HasMaxLength(30).IsRequired();builder.Property(x=>x.PlanName).HasMaxLength(100).IsRequired();builder.Property(x=>x.Description).HasMaxLength(1000).IsRequired();builder.Property(x=>x.MonthlyPrice).HasPrecision(19,4).IsRequired();builder.Property(x=>x.AnnualPrice).HasPrecision(19,4);builder.Property(x=>x.Currency).HasMaxLength(3).IsFixedLength().IsRequired();builder.Property(x=>x.FeatureLimitsJson).HasMaxLength(4000);builder.Property(x=>x.CreatedUtc).HasPrecision(7).IsRequired();builder.Property(x=>x.UpdatedUtc).HasPrecision(7).IsRequired().IsConcurrencyToken();builder.Property(x=>x.RowVersion).HasMaxLength(16).IsFixedLength().IsRequired().IsConcurrencyToken();builder.HasIndex(x=>x.PlanCode).IsUnique();builder.HasIndex(x=>new{x.IsActive,x.DisplayOrder,x.PlanName});
    }
}
