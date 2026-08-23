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

    public DbSet<Store> Stores => Set<Store>();
    public DbSet<UserStoreAssignment> UserStoreAssignments => Set<UserStoreAssignment>();
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
    public DbSet<StaffShift> StaffShifts => Set<StaffShift>();
    public DbSet<StaffPresenceSession> StaffPresenceSessions => Set<StaffPresenceSession>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<StoreVoiceCommandSetting> StoreVoiceCommandSettings => Set<StoreVoiceCommandSetting>();
    public DbSet<StoreVoiceCommandAlias> StoreVoiceCommandAliases => Set<StoreVoiceCommandAlias>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerStoreAssignment> CustomerStoreAssignments => Set<CustomerStoreAssignment>();
    public DbSet<AnonymousVisitor> AnonymousVisitors => Set<AnonymousVisitor>();

    public DbSet<Household> Households => Set<Household>();
    public DbSet<HouseholdMember> HouseholdMembers => Set<HouseholdMember>();
    public DbSet<VisitParty> VisitParties => Set<VisitParty>();
    public DbSet<VisitPartyMember> VisitPartyMembers => Set<VisitPartyMember>();
    public DbSet<CustomerVisit> CustomerVisits => Set<CustomerVisit>();

    /// <summary>Phase 8A — tenant product master.</summary>
    public DbSet<Product> Products => Set<Product>();
    /// <summary>Phase 8A — optional explicit product/store availability.</summary>
    public DbSet<ProductStoreAvailability> ProductStoreAvailabilities => Set<ProductStoreAvailability>();
    /// <summary>Phase 8B — factual retail invoice header.</summary>
    public DbSet<RetailInvoice> RetailInvoices => Set<RetailInvoice>();
    /// <summary>Phase 8C — immutable invoice line snapshots.</summary>
    public DbSet<RetailInvoiceItem> RetailInvoiceItems => Set<RetailInvoiceItem>();
    /// <summary>Phase 8D — append-only payment facts.</summary>
    public DbSet<RetailInvoicePayment> RetailInvoicePayments => Set<RetailInvoicePayment>();
    /// <summary>Phase 8E — explicit known-customer invoice participation.</summary>
    public DbSet<RetailInvoiceParticipant> RetailInvoiceParticipants => Set<RetailInvoiceParticipant>();
    /// <summary>Phase 8F — explicit auditable spend attribution.</summary>
    public DbSet<RetailInvoiceItemAttribution> RetailInvoiceItemAttributions => Set<RetailInvoiceItemAttribution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustSearchDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
