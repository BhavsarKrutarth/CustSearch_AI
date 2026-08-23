using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>Phase 5C — SQL mapping and tenant uniqueness rules for stores.</summary>
internal sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> b)
    {
        b.ToTable("Stores", "dbo", t =>
        {
            t.HasCheckConstraint("CK_Stores_Latitude", "[Latitude] IS NULL OR ([Latitude] >= -90 AND [Latitude] <= 90)");
            t.HasCheckConstraint("CK_Stores_Longitude", "[Longitude] IS NULL OR ([Longitude] >= -180 AND [Longitude] <= 180)");
            t.HasCheckConstraint("CK_Stores_CoordinatesPair", "([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL)");
            t.HasCheckConstraint("CK_Stores_GeoFence", "[GeoFenceRadiusMeters] IS NULL OR [GeoFenceRadiusMeters] > 0");
        });
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.StoreCode).HasMaxLength(30).IsRequired(); b.Property(x => x.StoreName).HasMaxLength(150).IsRequired();
        b.Property(x => x.AddressLine1).HasMaxLength(250).IsRequired(); b.Property(x => x.AddressLine2).HasMaxLength(250); b.Property(x => x.Landmark).HasMaxLength(150);
        b.Property(x => x.City).HasMaxLength(100).IsRequired(); b.Property(x => x.District).HasMaxLength(100); b.Property(x => x.StateOrProvince).HasMaxLength(100).IsRequired();
        b.Property(x => x.PostalCode).HasMaxLength(20).IsRequired(); b.Property(x => x.CountryCode).HasMaxLength(2).IsUnicode(false).IsRequired();
        b.Property(x => x.Latitude).HasPrecision(9,6); b.Property(x => x.Longitude).HasPrecision(9,6); b.Property(x => x.GeoFenceRadiusMeters).HasPrecision(10,2);
        b.Property(x => x.ExternalPlaceId).HasMaxLength(200); b.Property(x => x.LocationSource).HasConversion<byte>(); b.Property(x => x.TimeZone).HasMaxLength(100).IsRequired();
        b.Property(x => x.ContactEmail).HasMaxLength(254); b.Property(x => x.ContactMobile).HasMaxLength(30); b.Property(x => x.CreatedUtc).HasPrecision(7); b.Property(x => x.UpdatedUtc).HasPrecision(7); b.Property(x => x.LocationVerifiedUtc).HasPrecision(7);
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.StoreCode }).IsUnique(); b.HasIndex(x => new { x.TenantId, x.IsActive }); b.HasIndex(x => new { x.TenantId, x.City });
    }
}

/// <summary>Phase 5B — maps authoritative user/store grants used by authentication StoreId claims.</summary>
internal sealed class UserStoreAssignmentConfiguration : IEntityTypeConfiguration<UserStoreAssignment>
{
    public void Configure(EntityTypeBuilder<UserStoreAssignment> b)
    {
        b.ToTable("UserStoreAssignments", "dbo"); b.HasKey(x => new { x.UserId, x.StoreId });
        b.Property(x => x.AssignedUtc).HasPrecision(7);
        b.HasOne(x=>x.Tenant).WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x=>x.User).WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x=>x.Store).WithMany().HasForeignKey(x=>x.StoreId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x=>x.AssignedByUser).WithMany().HasForeignKey(x=>x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.TenantId,x.StoreId}); b.HasIndex(x=>new{x.UserId,x.IsPrimary});
    }
}

/// <summary>Phase 5D — staff profile mapping.</summary>
internal sealed class StaffProfileConfiguration : IEntityTypeConfiguration<StaffProfile>
{
    public void Configure(EntityTypeBuilder<StaffProfile> b)
    {
        b.ToTable("StaffProfiles","dbo"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).ValueGeneratedOnAdd();
        b.Property(x=>x.EmployeeCode).HasMaxLength(50).IsRequired(); b.Property(x=>x.FirstName).HasMaxLength(100).IsRequired(); b.Property(x=>x.LastName).HasMaxLength(100).IsRequired(); b.Property(x=>x.Mobile).HasMaxLength(30);
        b.Property(x=>x.CreatedUtc).HasPrecision(7); b.Property(x=>x.UpdatedUtc).HasPrecision(7);
        b.HasOne(x=>x.Tenant).WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x=>x.User).WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.TenantId,x.EmployeeCode}).IsUnique(); b.HasIndex(x=>x.UserId).IsUnique(); b.HasIndex(x=>new{x.TenantId,x.IsActive});
    }
}

/// <summary>Phase 5D — staff shift mapping.</summary>
internal sealed class StaffShiftConfiguration : IEntityTypeConfiguration<StaffShift>
{
    public void Configure(EntityTypeBuilder<StaffShift> b)
    {
        b.ToTable("StaffShifts","dbo"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).ValueGeneratedOnAdd(); b.Property(x=>x.Status).HasConversion<byte>();
        b.Property(x=>x.StartsUtc).HasPrecision(7); b.Property(x=>x.ScheduledEndsUtc).HasPrecision(7); b.Property(x=>x.ActualEndsUtc).HasPrecision(7); b.Property(x=>x.CreatedUtc).HasPrecision(7); b.Property(x=>x.UpdatedUtc).HasPrecision(7);
        b.HasOne(x=>x.StaffProfile).WithMany().HasForeignKey(x=>x.StaffProfileId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x=>x.Store).WithMany().HasForeignKey(x=>x.StoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.TenantId,x.StoreId,x.StartsUtc}); b.HasIndex(x=>new{x.StaffProfileId,x.Status});
    }
}

/// <summary>Phase 5D — optional staff presence mapping.</summary>
internal sealed class StaffPresenceSessionConfiguration : IEntityTypeConfiguration<StaffPresenceSession>
{
    public void Configure(EntityTypeBuilder<StaffPresenceSession> b)
    {
        b.ToTable("StaffPresenceSessions","dbo",t=>t.HasCheckConstraint("CK_StaffPresence_Confidence","[Confidence] >= 0 AND [Confidence] <= 1")); b.HasKey(x=>x.Id); b.Property(x=>x.Id).ValueGeneratedOnAdd(); b.Property(x=>x.Source).HasConversion<byte>(); b.Property(x=>x.Confidence).HasPrecision(5,4); b.Property(x=>x.EnteredUtc).HasPrecision(7); b.Property(x=>x.ExitedUtc).HasPrecision(7);
        b.HasOne(x=>x.StaffProfile).WithMany().HasForeignKey(x=>x.StaffProfileId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x=>x.Store).WithMany().HasForeignKey(x=>x.StoreId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x=>new{x.TenantId,x.StoreId,x.EnteredUtc});
    }
}

/// <summary>Phase 5E — category taxonomy mapping.</summary>
internal sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> b)
    {
        b.ToTable("ProductCategories","dbo"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).ValueGeneratedOnAdd(); b.Property(x=>x.CategoryCode).HasMaxLength(50).IsRequired(); b.Property(x=>x.Name).HasMaxLength(150).IsRequired(); b.Property(x=>x.CreatedUtc).HasPrecision(7); b.Property(x=>x.UpdatedUtc).HasPrecision(7);
        b.HasOne(x=>x.Store).WithMany().HasForeignKey(x=>x.StoreId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x=>x.ParentCategory).WithMany().HasForeignKey(x=>x.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.TenantId,x.StoreId,x.CategoryCode}).IsUnique(); b.HasIndex(x=>new{x.TenantId,x.IsActive});
    }
}

/// <summary>Phase 5F — dynamic store voice configuration mapping.</summary>
internal sealed class StoreVoiceCommandSettingConfiguration : IEntityTypeConfiguration<StoreVoiceCommandSetting>
{
    public void Configure(EntityTypeBuilder<StoreVoiceCommandSetting> b)
    {
        b.ToTable("StoreVoiceCommandSettings","dbo"); b.HasKey(x=>x.StoreId); b.Property(x=>x.StoreId).ValueGeneratedNever(); b.Property(x=>x.TriggerKeyword).HasMaxLength(100).IsRequired(); b.Property(x=>x.ResponseMode).HasConversion<byte>(); b.Property(x=>x.CreatedUtc).HasPrecision(7); b.Property(x=>x.UpdatedUtc).HasPrecision(7);
        b.HasOne(x=>x.Store).WithOne().HasForeignKey<StoreVoiceCommandSetting>(x=>x.StoreId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x=>new{x.TenantId,x.IsEnabled});
    }
}

/// <summary>Phase 5F — alternative voice-trigger alias mapping.</summary>
internal sealed class StoreVoiceCommandAliasConfiguration : IEntityTypeConfiguration<StoreVoiceCommandAlias>
{
    public void Configure(EntityTypeBuilder<StoreVoiceCommandAlias> b)
    {
        b.ToTable("StoreVoiceCommandAliases","dbo"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).ValueGeneratedOnAdd(); b.Property(x=>x.Alias).HasMaxLength(100).IsRequired(); b.Property(x=>x.CreatedUtc).HasPrecision(7);
        b.HasOne(x=>x.Setting).WithMany().HasForeignKey(x=>x.StoreId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x=>new{x.TenantId,x.StoreId,x.Alias}).IsUnique();
    }
}
