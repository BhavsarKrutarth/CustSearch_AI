using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>Maps factual customer preference signals separately from calculated preference scores.</summary>
internal sealed class CustomerPreferenceSignalConfiguration:IEntityTypeConfiguration<CustomerPreferenceSignal>
{
    public void Configure(EntityTypeBuilder<CustomerPreferenceSignal> b){b.ToTable("CustomerPreferenceSignals","dbo");b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.Value).HasMaxLength(200);b.Property(x=>x.SignalScore).HasPrecision(6,2);b.Property(x=>x.Confidence).HasPrecision(6,2);b.Property(x=>x.Reason).HasMaxLength(500);b.Property(x=>x.FirstObservedUtc).HasPrecision(7);b.Property(x=>x.LastObservedUtc).HasPrecision(7);b.Property(x=>x.CreatedUtc).HasPrecision(7);b.Property(x=>x.UpdatedUtc).HasPrecision(7);b.HasIndex(x=>new{x.TenantId,x.CustomerId,x.IsActive});b.HasIndex(x=>new{x.TenantId,x.StoreId,x.CustomerId});b.HasIndex(x=>new{x.TenantId,x.CustomerId,x.PreferenceType,x.ReferenceId});}
}

/// <summary>Maps deterministic derived scores with the exact weight version used for calculation.</summary>
internal sealed class CustomerPreferenceScoreConfiguration:IEntityTypeConfiguration<CustomerPreferenceScore>
{
    public void Configure(EntityTypeBuilder<CustomerPreferenceScore> b){b.ToTable("CustomerPreferenceScores","dbo");b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.Value).HasMaxLength(200);b.Property(x=>x.Score).HasPrecision(6,2);b.Property(x=>x.CalculatedUtc).HasPrecision(7);b.HasIndex(x=>new{x.TenantId,x.CustomerId,x.PreferenceType});b.HasIndex(x=>new{x.TenantId,x.CustomerId,x.WeightVersionId});}
}

/// <summary>Maps only explicit shared Household tags; member aggregation is computed from verified HouseholdMembers.</summary>
internal sealed class HouseholdPreferenceTagConfiguration:IEntityTypeConfiguration<HouseholdPreferenceTag>
{
    public void Configure(EntityTypeBuilder<HouseholdPreferenceTag> b){b.ToTable("HouseholdPreferenceTags","dbo");b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.Value).HasMaxLength(200).IsRequired();b.Property(x=>x.Reason).HasMaxLength(500);b.Property(x=>x.CreatedUtc).HasPrecision(7);b.Property(x=>x.UpdatedUtc).HasPrecision(7);b.HasIndex(x=>new{x.TenantId,x.HouseholdId,x.IsActive});}
}

/// <summary>Maps versioned tenant preference weights so recalculation can be reproduced.</summary>
internal sealed class PreferenceWeightVersionConfiguration:IEntityTypeConfiguration<PreferenceWeightVersion>
{
    public void Configure(EntityTypeBuilder<PreferenceWeightVersion> b){b.ToTable("PreferenceWeightVersions","dbo");b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.VersionCode).HasMaxLength(50).IsRequired();b.Property(x=>x.ManualStaffWeight).HasPrecision(6,3);b.Property(x=>x.PurchaseWeight).HasPrecision(6,3);b.Property(x=>x.CategoryInteractionWeight).HasPrecision(6,3);b.Property(x=>x.VoiceConfirmedWeight).HasPrecision(6,3);b.Property(x=>x.CreatedUtc).HasPrecision(7);b.HasIndex(x=>new{x.TenantId,x.VersionCode}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.IsActive});}
}

/// <summary>Maps one-to-one Phase 10 runtime voice controls while Phase 5 keeps the trigger/alias master.</summary>
internal sealed class StoreVoiceCommandRuntimeSettingConfiguration:IEntityTypeConfiguration<StoreVoiceCommandRuntimeSetting>
{
    public void Configure(EntityTypeBuilder<StoreVoiceCommandRuntimeSetting> b){b.ToTable("StoreVoiceCommandRuntimeSettings","dbo");b.HasKey(x=>x.StoreId);b.Property(x=>x.StoreId).ValueGeneratedNever();b.Property(x=>x.LanguageCode).HasMaxLength(20).IsRequired();b.Property(x=>x.MinimumRecognitionConfidence).HasPrecision(6,2);b.Property(x=>x.CreatedUtc).HasPrecision(7);b.Property(x=>x.UpdatedUtc).HasPrecision(7);b.HasIndex(x=>new{x.TenantId,x.StoreId}).IsUnique();}
}

/// <summary>Maps voice interaction confirmation state; a confirmed session can create a separate factual preference signal.</summary>
internal sealed class VoiceCommandSessionConfiguration:IEntityTypeConfiguration<VoiceCommandSession>
{
    public void Configure(EntityTypeBuilder<VoiceCommandSession> b){b.ToTable("VoiceCommandSessions","dbo");b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.MatchedTrigger).HasMaxLength(100).IsRequired();b.Property(x=>x.RecognizedText).HasMaxLength(250);b.Property(x=>x.RecognitionConfidence).HasPrecision(6,2);b.Property(x=>x.ProposedValue).HasMaxLength(200);b.Property(x=>x.ExpiresUtc).HasPrecision(7);b.Property(x=>x.ResolvedUtc).HasPrecision(7);b.Property(x=>x.CreatedUtc).HasPrecision(7);b.Property(x=>x.UpdatedUtc).HasPrecision(7);b.HasIndex(x=>new{x.TenantId,x.StoreId,x.CustomerId,x.Status});b.HasIndex(x=>new{x.TenantId,x.StaffUserId,x.CreatedUtc});}
}

/// <summary>Maps store/tenant category aliases used by the server-side voice parser. Aliases only resolve to existing ProductCategories.</summary>
internal sealed class ProductCategoryAliasConfiguration:IEntityTypeConfiguration<ProductCategoryAlias>
{
    public void Configure(EntityTypeBuilder<ProductCategoryAlias> b)
    {
        b.ToTable("ProductCategoryAliases","dbo");b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.AliasText).HasMaxLength(150).IsRequired();b.Property(x=>x.NormalizedAliasText).HasMaxLength(150).IsRequired();b.Property(x=>x.LanguageCode).HasMaxLength(20).IsRequired();b.Property(x=>x.CreatedUtc).HasPrecision(7);b.Property(x=>x.UpdatedUtc).HasPrecision(7);
        b.HasIndex(x=>new{x.TenantId,x.StoreId,x.NormalizedAliasText,x.ProductCategoryId}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.ProductCategoryId,x.IsActive});
    }
}
