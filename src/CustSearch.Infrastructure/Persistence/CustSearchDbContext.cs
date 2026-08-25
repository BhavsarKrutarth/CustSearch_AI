using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

    // Phase 11 Alerts & Real-Time — authoritative alerts, durable recovery cursor and transactional notification outbox.
    public DbSet<Alert> Alerts=>Set<Alert>(); public DbSet<RealtimeEvent> RealtimeEvents=>Set<RealtimeEvent>(); public DbSet<NotificationOutboxMessage> NotificationOutbox=>Set<NotificationOutboxMessage>();

    // Phase 12 Integrations — secret-reference configuration, inbound receipts, outbound outbox and payload-free delivery audit.
    public DbSet<IntegrationConfiguration> IntegrationConfigurations=>Set<IntegrationConfiguration>(); public DbSet<IntegrationInboundEvent> IntegrationInboundEvents=>Set<IntegrationInboundEvent>(); public DbSet<IntegrationOutboxMessage> IntegrationOutbox=>Set<IntegrationOutboxMessage>(); public DbSet<IntegrationDeliveryLog> IntegrationDeliveryLogs=>Set<IntegrationDeliveryLog>();

    // Phase 13 Cameras & Tracking — configuration references, versioned zones and anonymous-first operational tracks.
    public DbSet<Camera> Cameras=>Set<Camera>(); public DbSet<CameraZoneConfiguration> CameraZoneConfigurations=>Set<CameraZoneConfiguration>(); public DbSet<PersonTrackSession> PersonTrackSessions=>Set<PersonTrackSession>(); public DbSet<CameraTrackHandoff> CameraTrackHandoffs=>Set<CameraTrackHandoff>(); public DbSet<CameraOperationalEvent> CameraOperationalEvents=>Set<CameraOperationalEvent>();

    // Phase 14 Consent-Based Recognition — purpose consent, encrypted derived templates and human-reviewed candidates.
    public DbSet<CustomerRecognitionConsent> CustomerRecognitionConsents=>Set<CustomerRecognitionConsent>(); public DbSet<BiometricTemplate> BiometricTemplates=>Set<BiometricTemplate>(); public DbSet<RecognitionCandidate> RecognitionCandidates=>Set<RecognitionCandidate>();

    // Phase 15 Reports & Async Exports — durable authorized jobs; report rows are queried through Dapper stored procedures.
    public DbSet<ExportJob> ExportJobs=>Set<ExportJob>();

    // Phase 16 Operational Platform — hierarchy settings, separate secret references, controls, leases and auditable retention.
    public DbSet<OperationalSetting> OperationalSettings=>Set<OperationalSetting>(); public DbSet<OperationalSecretReference> OperationalSecretReferences=>Set<OperationalSecretReference>(); public DbSet<WorkerControl> WorkerControls=>Set<WorkerControl>(); public DbSet<WorkerLease> WorkerLeases=>Set<WorkerLease>(); public DbSet<WorkerHeartbeat> WorkerHeartbeats=>Set<WorkerHeartbeat>(); public DbSet<RetentionPolicy> RetentionPolicies=>Set<RetentionPolicy>(); public DbSet<RetentionRun> RetentionRuns=>Set<RetentionRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder){ArgumentNullException.ThrowIfNull(modelBuilder);modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustSearchDbContext).Assembly);base.OnModelCreating(modelBuilder);}
    public override int SaveChanges(bool acceptAllChangesOnSuccess){ValidateAuditWrites();return base.SaveChanges(acceptAllChangesOnSuccess);}
    public override Task<int>SaveChangesAsync(bool acceptAllChangesOnSuccess,CancellationToken cancellationToken=default){ValidateAuditWrites();return base.SaveChangesAsync(acceptAllChangesOnSuccess,cancellationToken);}
    private void ValidateAuditWrites(){foreach(var entry in ChangeTracker.Entries<AuditLog>()){if(entry.State is EntityState.Modified or EntityState.Deleted)throw new InvalidOperationException("Audit entries are immutable; retention must use the audited retention procedure.");if(entry.State==EntityState.Added&&(Unsafe(entry.Entity.BeforeJson)||Unsafe(entry.Entity.AfterJson)))throw new InvalidOperationException("Audit metadata contains a prohibited sensitive field.");}}
    private static bool Unsafe(string?json){if(string.IsNullOrWhiteSpace(json))return false;try{using var document=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=16});return Unsafe(document.RootElement);}catch(JsonException){throw new InvalidOperationException("Audit metadata must be valid JSON.");}}
    private static bool Unsafe(JsonElement element){if(element.ValueKind==JsonValueKind.Object){foreach(var property in element.EnumerateObject()){if(property.Name.Equals("password",StringComparison.OrdinalIgnoreCase)||property.Name.Equals("secretValue",StringComparison.OrdinalIgnoreCase)||property.Name.Equals("signingKey",StringComparison.OrdinalIgnoreCase)||property.Name.Equals("accessToken",StringComparison.OrdinalIgnoreCase)||property.Name.Equals("refreshToken",StringComparison.OrdinalIgnoreCase)||property.Name.Equals("biometricTemplate",StringComparison.OrdinalIgnoreCase)||property.Name.Equals("rawFrame",StringComparison.OrdinalIgnoreCase))return true;if(Unsafe(property.Value))return true;}}else if(element.ValueKind==JsonValueKind.Array){foreach(var item in element.EnumerateArray())if(Unsafe(item))return true;}return false;}
}
