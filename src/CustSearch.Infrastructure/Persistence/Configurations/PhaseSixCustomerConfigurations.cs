using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>Phase 6A — maps tenant-owned shopper customers and tenant-specific business-key uniqueness.</summary>
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("Customers", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.CustomerCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(100);
        b.Property(x => x.Mobile).HasMaxLength(30);
        b.Property(x => x.Email).HasMaxLength(254);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.CreatedUtc).HasPrecision(7);
        b.Property(x => x.UpdatedUtc).HasPrecision(7);
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.CustomerCode }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.IsActive });
        b.HasIndex(x => new { x.TenantId, x.Mobile });
        b.HasIndex(x => new { x.TenantId, x.Email });
    }
}

/// <summary>Phase 6G — maps the authoritative customer/store visibility relation used by store-scoped authorization.</summary>
internal sealed class CustomerStoreAssignmentConfiguration : IEntityTypeConfiguration<CustomerStoreAssignment>
{
    public void Configure(EntityTypeBuilder<CustomerStoreAssignment> b)
    {
        b.ToTable("CustomerStoreAssignments", "dbo");
        b.HasKey(x => new { x.CustomerId, x.StoreId });
        b.Property(x => x.AssignedUtc).HasPrecision(7);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.AssignedByUser).WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.StoreId });
        b.HasIndex(x => new { x.CustomerId, x.IsPrimary });
    }
}

/// <summary>Phase 6B — maps anonymous visitors without biometric identity data; each record is scoped to one tenant store.</summary>
internal sealed class AnonymousVisitorConfiguration : IEntityTypeConfiguration<AnonymousVisitor>
{
    public void Configure(EntityTypeBuilder<AnonymousVisitor> b)
    {
        b.ToTable("AnonymousVisitors", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.VisitorCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.FirstSeenUtc).HasPrecision(7);
        b.Property(x => x.LastSeenUtc).HasPrecision(7);
        b.Property(x => x.ConvertedUtc).HasPrecision(7);
        b.Property(x => x.CreatedUtc).HasPrecision(7);
        b.Property(x => x.UpdatedUtc).HasPrecision(7);
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ConvertedCustomer).WithMany().HasForeignKey(x => x.ConvertedCustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.StoreId, x.VisitorCode }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.StoreId, x.IsActive, x.LastSeenUtc });
        b.HasIndex(x => new { x.TenantId, x.ConvertedCustomerId });
    }
}
