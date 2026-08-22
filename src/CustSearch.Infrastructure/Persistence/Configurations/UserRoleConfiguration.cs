using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps user-role assignments and keeps deletion behavior explicit for audit safety.
/// </summary>
internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles", "dbo");
        builder.HasKey(userRole => new { userRole.UserId, userRole.RoleId });
        builder.Property(userRole => userRole.AssignedUtc).HasPrecision(7).IsRequired();
        builder.HasOne(userRole => userRole.User)
            .WithMany()
            .HasForeignKey(userRole => userRole.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(userRole => userRole.Role)
            .WithMany(role => role.UserRoles)
            .HasForeignKey(userRole => userRole.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(userRole => userRole.AssignedByUser)
            .WithMany()
            .HasForeignKey(userRole => userRole.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(userRole => userRole.RoleId);
        builder.HasIndex(userRole => userRole.AssignedByUserId);
    }
}
