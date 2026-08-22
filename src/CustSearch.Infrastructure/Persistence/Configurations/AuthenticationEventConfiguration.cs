using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps immutable authentication audit events without credential/token payloads.
/// </summary>
internal sealed class AuthenticationEventConfiguration : IEntityTypeConfiguration<AuthenticationEvent>
{
    public void Configure(EntityTypeBuilder<AuthenticationEvent> builder)
    {
        builder.ToTable("AuthenticationEvents", "dbo");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedOnAdd();
        builder.Property(item => item.EventType).HasMaxLength(60).IsRequired();
        builder.Property(item => item.FailureCode).HasMaxLength(60);
        builder.Property(item => item.OccurredUtc).HasPrecision(7).IsRequired();
        builder.Property(item => item.IpAddress).HasMaxLength(64).IsUnicode(false);
        builder.Property(item => item.CorrelationId).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.OccurredUtc });
        builder.HasIndex(item => new { item.UserId, item.OccurredUtc });
    }
}
