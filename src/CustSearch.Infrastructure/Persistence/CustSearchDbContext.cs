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
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<TenantUsageSnapshot> TenantUsageSnapshots => Set<TenantUsageSnapshot>();
    public DbSet<TenantQuotaOverride> TenantQuotaOverrides => Set<TenantQuotaOverride>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>Phase 5C — tenant store master.</summary>
    public DbSet<Store> Stores => Set<Store>();
    /// <summary>Phase 5B — authoritative user/store access grants.</summary>
    public DbSet<UserStoreAssignment> UserStoreAssignments => Set<UserStoreAssignment>();
    /// <summary>Phase 5D — staff profiles linked to tenant users.</summary>
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
    /// <summary>Phase 5D — operational staff shifts.</summary>
    public DbSet<StaffShift> StaffShifts => Set<StaffShift>();
    /// <summary>Phase 5D — optional staff presence signals.</summary>
    public DbSet<StaffPresenceSession> StaffPresenceSessions => Set<StaffPresenceSession>();
    /// <summary>Phase 5E — tenant/store product-category taxonomy.</summary>
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    /// <summary>Phase 5F — dynamic per-store voice-command configuration.</summary>
    public DbSet<StoreVoiceCommandSetting> StoreVoiceCommandSettings => Set<StoreVoiceCommandSetting>();
    /// <summary>Phase 5F — optional trigger aliases.</summary>
    public DbSet<StoreVoiceCommandAlias> StoreVoiceCommandAliases => Set<StoreVoiceCommandAlias>();

    /// <summary>Phase 6A — tenant-owned shopper customers.</summary>
    public DbSet<Customer> Customers => Set<Customer>();
    /// <summary>Phase 6G — authoritative customer-to-store visibility assignments.</summary>
    public DbSet<CustomerStoreAssignment> CustomerStoreAssignments => Set<CustomerStoreAssignment>();
    /// <summary>Phase 6B — store-bound anonymous visitors that remain unidentified until explicit conversion.</summary>
    public DbSet<AnonymousVisitor> AnonymousVisitors => Set<AnonymousVisitor>();

    /// <summary>Phase 7A — tenant-owned verified households.</summary>
    public DbSet<Household> Households => Set<Household>();
    /// <summary>Phase 7B — explicit verified customer-to-household relationships.</summary>
    public DbSet<HouseholdMember> HouseholdMembers => Set<HouseholdMember>();
    /// <summary>Phase 7C — store-bound co-visit parties that do not imply family.</summary>
    public DbSet<VisitParty> VisitParties => Set<VisitParty>();
    /// <summary>Phase 7C — separately identified customer/visitor participants in a co-visit party.</summary>
    public DbSet<VisitPartyMember> VisitPartyMembers => Set<VisitPartyMember>();
    /// <summary>Phase 7D — factual customer visit history.</summary>
    public DbSet<CustomerVisit> CustomerVisits => Set<CustomerVisit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustSearchDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}