using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.Infrastructure.Persistence;

/// <summary>EF Core unit-of-work for normal CustSearch CRUD operations. Database deployment remains script-based.</summary>
public sealed class CustSearchDbContext(DbContextOptions<CustSearchDbContext> options):DbContext(options)
{
    public DbSet<DatabaseVersion> DatabaseVersions=>Set<DatabaseVersion>(); public DbSet<Tenant> Tenants=>Set<Tenant>(); public DbSet<UserAccount> UserAccounts=>Set<UserAccount>(); public DbSet<RefreshToken> RefreshTokens=>Set<RefreshToken>(); public DbSet<AuthenticationEvent> AuthenticationEvents=>Set<AuthenticationEvent>(); public DbSet<Role> Roles=>Set<Role>(); public DbSet<Permission> Permissions=>Set<Permission>(); public DbSet<UserRole> UserRoles=>Set<UserRole>(); public DbSet<RolePermission> RolePermissions=>Set<RolePermission>(); public DbSet<SubscriptionPlan> SubscriptionPlans=>Set<SubscriptionPlan>(); public DbSet<TenantSubscription> TenantSubscriptions=>Set<TenantSubscription>(); public DbSet<TenantUsageSnapshot> TenantUsageSnapshots=>Set<TenantUsageSnapshot>(); public DbSet<TenantQuotaOverride> TenantQuotaOverrides=>Set<TenantQuotaOverride>(); public DbSet<AuditLog> AuditLogs=>Set<AuditLog>();
    public DbSet<Store> Stores=>Set<Store>(); public DbSet<UserStoreAssignment> UserStoreAssignments=>Set<UserStoreAssignment>(); public DbSet<StaffProfile> StaffProfiles=>Set<StaffProfile>(); public DbSet<StaffShift> StaffShifts=>Set<StaffShift>(); public DbSet<StaffPresenceSession> StaffPresenceSessions=>Set<StaffPresenceSession>(); public DbSet<ProductCategory> ProductCategories=>Set<ProductCategory>(); public DbSet<StoreVoiceCommandSetting> StoreVoiceCommandSettings=>Set<StoreVoiceCommandSetting>(); public DbSet<StoreVoiceCommandAlias> StoreVoiceCommandAliases=>Set<StoreVoiceCommandAlias>();
    public DbSet<Customer> Customers=>Set<Customer>(); public DbSet<CustomerStoreAssignment> CustomerStoreAssignments=>Set<CustomerStoreAssignment>(); public DbSet<AnonymousVisitor> AnonymousVisitors=>Set<AnonymousVisitor>();
    public DbSet<Household> Households=>Set<Household>(); public DbSet<HouseholdMember> HouseholdMembers=>Set<HouseholdMember>(); public DbSet<VisitParty> VisitParties=>Set<VisitParty>(); public DbSet<VisitPartyMember> VisitPartyMembers=>Set<VisitPartyMember>(); public DbSet<CustomerVisit> CustomerVisits=>Set<CustomerVisit>();

    // Phase 8 Retail Billing — shop-customer purchase domain only.
    public DbSet<Product> Products=>Set<Product>(); public DbSet<ProductStoreAvailability> ProductStoreAvailabilities=>Set<ProductStoreAvailability>(); public DbSet<RetailInvoice> RetailInvoices=>Set<RetailInvoice>(); public DbSet<RetailInvoiceItem> RetailInvoiceItems=>Set<RetailInvoiceItem>(); public DbSet<RetailInvoicePayment> RetailInvoicePayments=>Set<RetailInvoicePayment>(); public DbSet<RetailInvoiceParticipant> RetailInvoiceParticipants=>Set<RetailInvoiceParticipant>(); public DbSet<RetailInvoiceItemAttribution> RetailInvoiceItemAttributions=>Set<RetailInvoiceItemAttribution>();

    // Phase 9 Platform Billing — CustSearch subscription domain, deliberately separate from Retail Billing.
    public DbSet<PlatformInvoice> PlatformInvoices=>Set<PlatformInvoice>(); public DbSet<PlatformInvoiceItem> PlatformInvoiceItems=>Set<PlatformInvoiceItem>(); public DbSet<PlatformPayment> PlatformPayments=>Set<PlatformPayment>();

    // Phase 10 Preferences & Voice — factual signals, derived scores, category aliases and confirmation-controlled voice sessions.
    public DbSet<CustomerPreferenceSignal> CustomerPreferenceSignals=>Set<CustomerPreferenceSignal>(); public DbSet<CustomerPreferenceScore> CustomerPreferenceScores=>Set<CustomerPreferenceScore>(); public DbSet<HouseholdPreferenceTag> HouseholdPreferenceTags=>Set<HouseholdPreferenceTag>(); public DbSet<PreferenceWeightVersion> PreferenceWeightVersions=>Set<PreferenceWeightVersion>(); public DbSet<StoreVoiceCommandRuntimeSetting> StoreVoiceCommandRuntimeSettings=>Set<StoreVoiceCommandRuntimeSetting>(); public DbSet<VoiceCommandSession> VoiceCommandSessions=>Set<VoiceCommandSession>(); public DbSet<ProductCategoryAlias> ProductCategoryAliases=>Set<ProductCategoryAlias>();

    protected override void OnModelCreating(ModelBuilder modelBuilder){ArgumentNullException.ThrowIfNull(modelBuilder);modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustSearchDbContext).Assembly);base.OnModelCreating(modelBuilder);}
}