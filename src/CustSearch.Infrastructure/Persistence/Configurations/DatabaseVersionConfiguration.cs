using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the SQL-script-owned DatabaseVersions table for read-only diagnostics.
/// </summary>
internal sealed class DatabaseVersionConfiguration : IEntityTypeConfiguration<DatabaseVersion>
{
    public void Configure(EntityTypeBuilder<DatabaseVersion> builder)
    {
        builder.ToTable("DatabaseVersions", "dbo");
        builder.HasKey(version => version.VersionId);
        builder.Property(version => version.VersionId).ValueGeneratedOnAdd();
        builder.Property(version => version.VersionNumber).HasMaxLength(50).IsRequired();
        builder.Property(version => version.Description).HasMaxLength(250).IsRequired();
        builder.Property(version => version.AppliedUtc).HasPrecision(7).IsRequired();
        builder.Property(version => version.AppliedBy).HasMaxLength(100).IsRequired();
        builder.HasIndex(version => version.VersionNumber).IsUnique();
    }
}
