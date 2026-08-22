using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.Infrastructure.Persistence;

/// <summary>
/// EF Core unit-of-work for normal CustSearch CRUD operations.
/// </summary>
/// <remarks>
/// Database schema deployment is intentionally external to this context. Do not call
/// Database.Migrate() or EnsureCreated(); use the versioned scripts under /database.
/// </remarks>
public sealed class CustSearchDbContext(DbContextOptions<CustSearchDbContext> options) : DbContext(options)
{
    public DbSet<DatabaseVersion> DatabaseVersions => Set<DatabaseVersion>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AuthenticationEvent> AuthenticationEvents => Set<AuthenticationEvent>();

    /// <summary>Provides role storage for authorization queries and administration.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Provides the shared permission catalog used by policies.</summary>
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>Provides user-to-role assignments for session authorization.</summary>
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    /// <summary>Provides permission grants attached to roles.</summary>
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    /// <summary>Provides reusable subscription plan definitions and default quotas.</summary>
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

    /// <summary>Provides tenant subscription history and current commercial state.</summary>
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();

    /// <summary>Provides time-bounded tenant usage totals for quota and reporting screens.</summary>
    public DbSet<TenantUsageSnapshot> TenantUsageSnapshots => Set<TenantUsageSnapshot>();

    /// <summary>Provides audited platform overrides to tenant quota defaults.</summary>
    public DbSet<TenantQuotaOverride> TenantQuotaOverrides => Set<TenantQuotaOverride>();

    /// <summary>Provides append-only evidence for platform and tenant administration actions.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustSearchDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
