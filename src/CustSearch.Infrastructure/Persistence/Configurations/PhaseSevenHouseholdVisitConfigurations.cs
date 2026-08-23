using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

internal sealed class HouseholdConfiguration : IEntityTypeConfiguration<Household>
{
    public void Configure(EntityTypeBuilder<Household> b)
    {
        b.ToTable("Households","dbo"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).ValueGeneratedOnAdd();
        b.Property(x=>x.HouseholdCode).HasMaxLength(50).IsRequired(); b.Property(x=>x.Name).HasMaxLength(150).IsRequired(); b.Property(x=>x.Notes).HasMaxLength(1000);
        b.Property(x=>x.CreatedUtc).HasPrecision(7); b.Property(x=>x.UpdatedUtc).HasPrecision(7);
        b.HasOne(x=>x.Tenant).WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.TenantId,x.HouseholdCode}).IsUnique(); b.HasIndex(x=>new{x.TenantId,x.IsActive,x.UpdatedUtc});
    }
}

internal sealed class HouseholdMemberConfiguration : IEntityTypeConfiguration<HouseholdMember>
{
    public void Configure(EntityTypeBuilder<HouseholdMember> b)
    {
        b.ToTable("HouseholdMembers","dbo"); b.HasKey(x=>new{x.HouseholdId,x.CustomerId});
        b.Property(x=>x.RelationshipType).HasMaxLength(50).IsRequired(); b.Property(x=>x.RelationshipSource).HasConversion<byte>();
        b.Property(x=>x.VerifiedUtc).HasPrecision(7); b.Property(x=>x.CreatedUtc).HasPrecision(7); b.Property(x=>x.UpdatedUtc).HasPrecision(7);
        b.HasOne(x=>x.Household).WithMany().HasForeignKey(x=>x.HouseholdId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x=>x.Customer).WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x=>x.VerifiedByUser).WithMany().HasForeignKey(x=>x.VerifiedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.TenantId,x.CustomerId,x.IsActive}); b.HasIndex(x=>new{x.TenantId,x.HouseholdId,x.IsActive});
    }
}

internal sealed class VisitPartyConfiguration : IEntityTypeConfiguration<VisitParty>
{
    public void Configure(EntityTypeBuilder<VisitParty> b)
    {
        b.ToTable("VisitParties","dbo"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).ValueGeneratedOnAdd();
        b.Property(x=>x.PartyCode).HasMaxLength(50).IsRequired(); b.Property(x=>x.Source).HasConversion<byte>(); b.Property(x=>x.Status).HasConversion<byte>();
        b.Property(x=>x.StartedUtc).HasPrecision(7); b.Property(x=>x.EndedUtc).HasPrecision(7); b.Property(x=>x.CreatedUtc).HasPrecision(7); b.Property(x=>x.UpdatedUtc).HasPrecision(7);
        b.HasOne(x=>x.Tenant).WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x=>x.Store).WithMany().HasForeignKey(x=>x.StoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.TenantId,x.StoreId,x.PartyCode}).IsUnique(); b.HasIndex(x=>new{x.TenantId,x.StoreId,x.Status,x.StartedUtc});
    }
}

internal sealed class VisitPartyMemberConfiguration : IEntityTypeConfiguration<VisitPartyMember>
{
    public void Configure(EntityTypeBuilder<VisitPartyMember> b)
    {
        b.ToTable("VisitPartyMembers","dbo"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).ValueGeneratedOnAdd(); b.Property(x=>x.IdentityType).HasConversion<byte>(); b.Property(x=>x.JoinedUtc).HasPrecision(7);
        b.HasOne(x=>x.VisitParty).WithMany().HasForeignKey(x=>x.VisitPartyId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x=>x.Customer).WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x=>x.AnonymousVisitor).WithMany().HasForeignKey(x=>x.AnonymousVisitorId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.TenantId,x.StoreId,x.VisitPartyId}); b.HasIndex(x=>new{x.TenantId,x.CustomerId}); b.HasIndex(x=>new{x.TenantId,x.AnonymousVisitorId});
    }
}

internal sealed class CustomerVisitConfiguration : IEntityTypeConfiguration<CustomerVisit>
{
    public void Configure(EntityTypeBuilder<CustomerVisit> b)
    {
        b.ToTable("CustomerVisits","dbo"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).ValueGeneratedOnAdd(); b.Property(x=>x.VisitCode).HasMaxLength(50).IsRequired();
        b.Property(x=>x.Source).HasConversion<byte>(); b.Property(x=>x.Status).HasConversion<byte>(); b.Property(x=>x.EnteredUtc).HasPrecision(7); b.Property(x=>x.ExitedUtc).HasPrecision(7); b.Property(x=>x.CreatedUtc).HasPrecision(7); b.Property(x=>x.UpdatedUtc).HasPrecision(7);
        b.HasOne(x=>x.Tenant).WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x=>x.Store).WithMany().HasForeignKey(x=>x.StoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x=>x.Customer).WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x=>x.VisitParty).WithMany().HasForeignKey(x=>x.VisitPartyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.TenantId,x.VisitCode}).IsUnique(); b.HasIndex(x=>new{x.TenantId,x.StoreId,x.EnteredUtc}); b.HasIndex(x=>new{x.TenantId,x.CustomerId,x.EnteredUtc});
    }
}