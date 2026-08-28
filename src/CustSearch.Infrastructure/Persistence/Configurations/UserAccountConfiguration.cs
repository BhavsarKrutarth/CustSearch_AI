using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps platform and tenant identities while preserving their ownership separation.
/// </summary>
internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("Users", "dbo", table =>
        {
            table.HasCheckConstraint(
                "CK_Users_ScopeTenant",
                "([Scope] = 1 AND [TenantId] IS NULL) OR ([Scope] = 2 AND [TenantId] IS NOT NULL)");
        });
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedOnAdd();
        builder.Property(user => user.Scope).HasConversion<byte>().IsRequired();
        builder.Property(user => user.UserName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.NormalizedUserName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(254).IsRequired();
        builder.Property(user => user.NormalizedEmail).HasMaxLength(254).IsRequired();
        builder.Property(user => user.DisplayName).HasMaxLength(150).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(user => user.DisplayPassword).HasMaxLength(500);
        builder.Property(user => user.SecurityStamp).HasMaxLength(64).IsRequired();
        builder.Property(user => user.CreatedUtc).HasPrecision(7).IsRequired();
        builder.Property(user => user.LastLoginUtc).HasPrecision(7);
        builder.HasOne(user => user.Tenant)
            .WithMany()
            .HasForeignKey(user => user.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(user => new { user.TenantId, user.NormalizedUserName }).IsUnique();
        builder.HasIndex(user => new { user.TenantId, user.NormalizedEmail }).IsUnique();
        builder.HasIndex(user => user.Scope);
    }
}
