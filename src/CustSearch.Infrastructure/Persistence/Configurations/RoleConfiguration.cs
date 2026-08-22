using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps roles and enforces that platform roles have no tenant while tenant roles always have one.
/// </summary>
internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "dbo", table =>
        {
            table.HasCheckConstraint(
                "CK_Roles_ScopeTenant",
                "([Scope] = 1 AND [TenantId] IS NULL) OR ([Scope] = 2 AND [TenantId] IS NOT NULL)");
        });
        builder.HasKey(role => role.Id);
        builder.Property(role => role.Id).ValueGeneratedOnAdd();
        builder.Property(role => role.Scope).HasConversion<byte>().IsRequired();
        builder.Property(role => role.Name).HasMaxLength(100).IsRequired();
        builder.Property(role => role.NormalizedName).HasMaxLength(100).IsRequired();
        builder.Property(role => role.Description).HasMaxLength(300).IsRequired();
        builder.Property(role => role.CreatedUtc).HasPrecision(7).IsRequired();
        builder.HasOne(role => role.Tenant)
            .WithMany()
            .HasForeignKey(role => role.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(role => new { role.TenantId, role.NormalizedName }).IsUnique();
        builder.HasIndex(role => new { role.Scope, role.IsActive });
    }
}
