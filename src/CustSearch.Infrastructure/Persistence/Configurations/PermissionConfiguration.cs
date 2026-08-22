using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the global permission catalog with one unique, stable name per capability.
/// </summary>
internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Permissions_Scope", "[Scope] IN (1, 2)");
        });
        builder.HasKey(permission => permission.Id);
        builder.Property(permission => permission.Id).ValueGeneratedOnAdd();
        builder.Property(permission => permission.Scope).HasConversion<byte>().IsRequired();
        builder.Property(permission => permission.Name).HasMaxLength(150).IsRequired();
        builder.Property(permission => permission.Description).HasMaxLength(300).IsRequired();
        builder.Property(permission => permission.CreatedUtc).HasPrecision(7).IsRequired();
        builder.HasIndex(permission => permission.Name).IsUnique();
        builder.HasIndex(permission => new { permission.Scope, permission.IsActive });
    }
}
