using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps subscription pricing and quota defaults with optimistic concurrency protection.
/// </summary>
internal sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans", "dbo", table =>
        {
            table.HasCheckConstraint("CK_SubscriptionPlans_Prices", "[MonthlyPrice] >= 0 AND ([AnnualPrice] IS NULL OR [AnnualPrice] >= 0)");
            table.HasCheckConstraint("CK_SubscriptionPlans_Limits", "[MaxStores] > 0 AND [MaxUsers] > 0 AND [MaxCameras] > 0 AND ([MaxMonthlyRecognitions] IS NULL OR [MaxMonthlyRecognitions] > 0) AND ([MaxMonthlyApiCalls] IS NULL OR [MaxMonthlyApiCalls] > 0)");
        });
        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Id).ValueGeneratedOnAdd();
        builder.Property(plan => plan.PlanCode).HasMaxLength(30).IsRequired();
        builder.Property(plan => plan.PlanName).HasMaxLength(100).IsRequired();
        builder.Property(plan => plan.MonthlyPrice).HasPrecision(19, 4).IsRequired();
        builder.Property(plan => plan.AnnualPrice).HasPrecision(19, 4);
        builder.Property(plan => plan.CreatedUtc).HasPrecision(7).IsRequired();
        builder.Property(plan => plan.UpdatedUtc).HasPrecision(7).IsRequired().IsConcurrencyToken();
        builder.Property(plan => plan.RowVersion).HasMaxLength(16).IsFixedLength().IsRequired().IsConcurrencyToken();
        builder.HasIndex(plan => plan.PlanCode).IsUnique();
        builder.HasIndex(plan => new { plan.IsActive, plan.PlanName });
    }
}
