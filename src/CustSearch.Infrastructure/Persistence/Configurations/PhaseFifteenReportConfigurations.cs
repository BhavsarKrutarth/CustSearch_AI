using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

public sealed class ExportJobConfiguration:IEntityTypeConfiguration<ExportJob>
{
    public void Configure(EntityTypeBuilder<ExportJob>builder){builder.ToTable("ExportJobs","dbo",t=>{t.HasCheckConstraint("CK_ExportJobs_Status","[Status] BETWEEN 1 AND 5");t.HasCheckConstraint("CK_ExportJobs_Progress","[Progress] BETWEEN 0 AND 100");t.HasCheckConstraint("CK_ExportJobs_Scope","([TenantId] IS NULL AND [ReportType]=20) OR ([TenantId] IS NOT NULL AND [ReportType] BETWEEN 1 AND 10)");});builder.HasKey(x=>x.Id);builder.Property(x=>x.Id).ValueGeneratedOnAdd();builder.Property(x=>x.FilterJson).HasMaxLength(4000).IsRequired();builder.Property(x=>x.AuthorizedStoreIdsJson).HasMaxLength(4000).IsRequired();builder.Property(x=>x.Error).HasMaxLength(2000);builder.Property(x=>x.FilePath).HasMaxLength(1000);builder.Property(x=>x.FileName).HasMaxLength(260);builder.Property(x=>x.ContentType).HasMaxLength(150);builder.Property(x=>x.CreatedUtc).HasPrecision(7);builder.Property(x=>x.StartedUtc).HasPrecision(7);builder.Property(x=>x.CompletedUtc).HasPrecision(7);builder.Property(x=>x.ExpiresUtc).HasPrecision(7);builder.Property(x=>x.LeaseExpiresUtc).HasPrecision(7);builder.Property(x=>x.RowVersion).IsRowVersion();builder.HasIndex(x=>new{x.Status,x.CreatedUtc});builder.HasIndex(x=>new{x.TenantId,x.RequestedByUserId,x.CreatedUtc});}
}
