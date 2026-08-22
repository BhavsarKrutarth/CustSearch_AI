using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps hashed rotating refresh tokens and their revocation families.
/// </summary>
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", "dbo");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedOnAdd();
        builder.Property(token => token.TokenHash).HasMaxLength(64).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(token => token.IssuedSecurityStamp).HasMaxLength(64).IsRequired();
        builder.Property(token => token.CreatedUtc).HasPrecision(7).IsRequired();
        builder.Property(token => token.ExpiresUtc).HasPrecision(7).IsRequired();
        builder.Property(token => token.RevokedUtc).HasPrecision(7);
        builder.Property(token => token.RevokedReason).HasMaxLength(100);
        builder.Property(token => token.ReplacedByTokenHash).HasMaxLength(64).IsUnicode(false).IsFixedLength();
        builder.Property(token => token.CreatedByIp).HasMaxLength(64).IsUnicode(false);
        builder.Property(token => token.RevokedByIp).HasMaxLength(64).IsUnicode(false);
        builder.HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.UserId, token.FamilyId });
        builder.HasIndex(token => token.ExpiresUtc);
    }
}
