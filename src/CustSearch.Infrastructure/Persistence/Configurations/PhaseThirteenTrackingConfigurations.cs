using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

internal sealed class CameraConfiguration:IEntityTypeConfiguration<Camera>
{
    public void Configure(EntityTypeBuilder<Camera>b){b.ToTable("Cameras","dbo",t=>{t.HasCheckConstraint("CK_Cameras_Status","[Status] BETWEEN 1 AND 4");t.HasCheckConstraint("CK_Cameras_Direction","[Direction] BETWEEN 1 AND 4");});b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.CameraCode).HasMaxLength(50).IsRequired();b.Property(x=>x.Name).HasMaxLength(150).IsRequired();b.Property(x=>x.RtspConfigurationReference).HasMaxLength(200).IsRequired();b.Property(x=>x.Location).HasMaxLength(250);b.Property(x=>x.CreatedUtc).HasPrecision(7);b.Property(x=>x.UpdatedUtc).HasPrecision(7);b.Property(x=>x.LastHeartbeatUtc).HasPrecision(7);b.Property(x=>x.RowVersion).IsRowVersion();b.HasIndex(x=>new{x.TenantId,x.StoreId,x.CameraCode}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.StoreId,x.IsActive,x.Status});}
}
internal sealed class CameraZoneConfigurationConfiguration:IEntityTypeConfiguration<CameraZoneConfiguration>
{
    public void Configure(EntityTypeBuilder<CameraZoneConfiguration>b){b.ToTable("CameraZoneConfigurations","dbo",t=>{t.HasCheckConstraint("CK_CameraZones_Type","[ZoneType] BETWEEN 1 AND 7");t.HasCheckConstraint("CK_CameraZones_Version","[Version]>=1");});b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.ZoneCode).HasMaxLength(50).IsRequired();b.Property(x=>x.Name).HasMaxLength(150).IsRequired();b.Property(x=>x.GeometryJson).HasMaxLength(8000).IsRequired();b.Property(x=>x.EffectiveUtc).HasPrecision(7);b.Property(x=>x.SupersededUtc).HasPrecision(7);b.HasIndex(x=>new{x.TenantId,x.CameraId,x.ZoneCode,x.Version}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.StoreId,x.CameraId,x.IsActive});}
}
internal sealed class PersonTrackSessionConfiguration:IEntityTypeConfiguration<PersonTrackSession>
{
    public void Configure(EntityTypeBuilder<PersonTrackSession>b){b.ToTable("PersonTrackSessions","dbo",t=>{t.HasCheckConstraint("CK_PersonTracks_Confidence","[Confidence] BETWEEN 0 AND 1");t.HasCheckConstraint("CK_PersonTracks_State","[TrackingState] BETWEEN 1 AND 4");t.HasCheckConstraint("CK_PersonTracks_Subject","([SubjectKind]=1 AND [CustomerId] IS NULL AND [StaffProfileId] IS NULL) OR ([SubjectKind]=2 AND [CustomerId] IS NOT NULL AND [StaffProfileId] IS NULL) OR ([SubjectKind]=3 AND [CustomerId] IS NULL AND [StaffProfileId] IS NOT NULL)");});b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.PersonTrackId).HasMaxLength(100).IsRequired();b.Property(x=>x.Confidence).HasPrecision(5,4);b.Property(x=>x.StartUtc).HasPrecision(7);b.Property(x=>x.EndUtc).HasPrecision(7);b.Property(x=>x.UpdatedUtc).HasPrecision(7);b.Property(x=>x.RowVersion).IsRowVersion();b.HasIndex(x=>new{x.TenantId,x.StoreId,x.PersonTrackId}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.StoreId,x.TrackingState,x.UpdatedUtc});}
}
internal sealed class CameraTrackHandoffConfiguration:IEntityTypeConfiguration<CameraTrackHandoff>
{
    public void Configure(EntityTypeBuilder<CameraTrackHandoff>b){b.ToTable("CameraTrackHandoffs","dbo",t=>{t.HasCheckConstraint("CK_CameraHandoffs_Confidence","[Confidence] BETWEEN 0 AND 1");t.HasCheckConstraint("CK_CameraHandoffs_Gap","[GapMilliseconds]>=0");});b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.Confidence).HasPrecision(5,4);b.Property(x=>x.OccurredUtc).HasPrecision(7);b.HasIndex(x=>new{x.TenantId,x.StoreId,x.PersonTrackSessionId,x.OccurredUtc});}
}
internal sealed class CameraOperationalEventConfiguration:IEntityTypeConfiguration<CameraOperationalEvent>
{
    public void Configure(EntityTypeBuilder<CameraOperationalEvent>b){b.ToTable("CameraOperationalEvents","dbo",t=>t.HasCheckConstraint("CK_CameraEvents_Status","[Status] BETWEEN 1 AND 4"));b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.ServiceId).HasMaxLength(100).IsRequired();b.Property(x=>x.EventId).HasMaxLength(150).IsRequired();b.Property(x=>x.IdempotencyKey).HasMaxLength(150).IsRequired();b.Property(x=>x.EventType).HasMaxLength(100).IsRequired();b.Property(x=>x.PayloadHash).HasMaxLength(64).IsRequired();b.Property(x=>x.CorrelationId).HasMaxLength(64).IsRequired();b.Property(x=>x.OccurredUtc).HasPrecision(7);b.Property(x=>x.ReceivedUtc).HasPrecision(7);b.HasIndex(x=>new{x.ServiceId,x.EventId}).IsUnique();b.HasIndex(x=>new{x.ServiceId,x.IdempotencyKey}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.StoreId,x.ReceivedUtc});}
}
