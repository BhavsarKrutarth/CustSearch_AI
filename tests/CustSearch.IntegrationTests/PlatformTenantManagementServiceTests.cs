using CustSearch.Application.Authorization;
using CustSearch.Application.PlatformTenancy;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using CustSearch.Infrastructure.PlatformTenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.IntegrationTests;

/// <summary>Verifies transactional tenant provisioning, concurrency and lifecycle security rules.</summary>
public sealed class PlatformTenantManagementServiceTests
{
    private static readonly DateTime TestNow = new(2026, 8, 16, 2, 30, 0, DateTimeKind.Utc);
    private static readonly string[] ExpectedTenantRoles =
    [
        "Auditor", "BillingStaff", "CRMStaff", "CameraOperator", "IntegrationAdmin",
        "Manager", "StoreAdmin", "TenantAdmin",
    ];

    [Fact]
    public async Task CreateTenantProvisionEightTenantRolesWithoutPlatformPermissions()
    {
        await using var fixture = await Fixture.CreateAsync(seedPermissions: true);

        var tenant = await fixture.Service.CreateTenantAsync(CreateCommand(), fixture.Audit);

        Assert.StartsWith("TEN-", tenant.TenantCode, StringComparison.Ordinal);
        var roles = await fixture.Context.Roles.Where(role => role.TenantId == tenant.Id).ToListAsync();
        Assert.Equal(8, roles.Count);
        Assert.Equal(
            ExpectedTenantRoles,
            roles.Select(role => role.Name).Order(StringComparer.Ordinal));
        var grantedScopes = await fixture.Context.RolePermissions
            .Where(grant => roles.Select(role => role.Id).Contains(grant.RoleId))
            .Select(grant => grant.Permission.Scope)
            .Distinct()
            .ToListAsync();
        Assert.Equal([UserScope.Tenant], grantedScopes);
        var cameraOperatorPermissions = await fixture.Context.RolePermissions
            .Where(grant => grant.Role.TenantId == tenant.Id && grant.Role.Name == "CameraOperator")
            .Select(grant => grant.Permission.Name)
            .ToListAsync();
        Assert.Contains(PermissionCatalog.Tenant.DashboardView, cameraOperatorPermissions);
        Assert.Contains(PermissionCatalog.Operations.CamerasView, cameraOperatorPermissions);
        Assert.DoesNotContain(cameraOperatorPermissions, permission => permission.StartsWith("Platform", StringComparison.Ordinal));
        Assert.Contains(await fixture.Context.AuditLogs.ToListAsync(), audit => audit.Action == "TenantCreated");
    }

    [Fact]
    public async Task MissingPermissionCatalogRollsBackTenantCreation()
    {
        await using var fixture = await Fixture.CreateAsync(seedPermissions: false);

        await Assert.ThrowsAsync<PlatformBusinessRuleException>(() =>
            fixture.Service.CreateTenantAsync(CreateCommand(), fixture.Audit));

        Assert.Equal(0, await fixture.Context.Tenants.CountAsync());
        Assert.Equal(0, await fixture.Context.Roles.CountAsync());
    }

    [Fact]
    public async Task StaleTenantVersionIsRejected()
    {
        await using var fixture = await Fixture.CreateAsync(seedPermissions: true);
        var created = await fixture.Service.CreateTenantAsync(CreateCommand(), fixture.Audit);
        var firstUpdate = UpdateCommand(created.Version, "First Update");
        _ = await fixture.Service.UpdateTenantAsync(created.Id, firstUpdate, fixture.Audit);

        var exception = await Assert.ThrowsAsync<PlatformConcurrencyException>(() =>
            fixture.Service.UpdateTenantAsync(created.Id, UpdateCommand(created.Version, "Stale Update"), fixture.Audit));

        Assert.Equal("The resource changed. Reload it and retry.", exception.Message);
    }

    [Fact]
    public async Task SuspendRevokesAllTenantRefreshSessionsButDoesNotDisableUsers()
    {
        await using var fixture = await Fixture.CreateAsync(seedPermissions: true);
        var created = await fixture.Service.CreateTenantAsync(CreateCommand(), fixture.Audit);
        var user = UserAccount.CreateTenant(
            created.Id,
            "tenant.owner",
            "owner@example.test",
            "Tenant Owner",
            "hash",
            TestNow);
        fixture.Context.UserAccounts.Add(user);
        await fixture.Context.SaveChangesAsync();
        fixture.Context.RefreshTokens.Add(RefreshToken.Issue(
            user.Id,
            new string('A', 64),
            Guid.NewGuid(),
            user.SecurityStamp,
            TestNow,
            TestNow.AddDays(7),
            "127.0.0.1"));
        await fixture.Context.SaveChangesAsync();

        _ = await fixture.Service.SuspendTenantAsync(
            created.Id,
            "Compliance review",
            created.Version,
            fixture.Audit);

        Assert.Equal(0, await fixture.Context.RefreshTokens.CountAsync(token => token.RevokedUtc == null));
        Assert.True((await fixture.Context.UserAccounts.SingleAsync(user => user.Id > 0 && user.TenantId == created.Id)).IsActive);
        Assert.Contains(await fixture.Context.AuditLogs.ToListAsync(), audit => audit.Action == "TenantSuspended");
    }

    [Fact]
    public async Task HistoricalQuotaOverrideExpiryReturnsSafeBusinessFailure()
    {
        await using var fixture = await Fixture.CreateAsync(seedPermissions: true);
        var tenant = await fixture.Service.CreateTenantAsync(CreateCommand(), fixture.Audit);
        var plan = await fixture.Service.CreatePlanAsync(
            new SaveSubscriptionPlanCommand(
                "GROWTH",
                "Growth",
                1000,
                10000,
                5,
                20,
                20,
                null,
                null,
                true,
                null),
            fixture.Audit);

        var failure = await Assert.ThrowsAsync<PlatformBusinessRuleException>(() =>
            fixture.Service.AssignSubscriptionAsync(
                tenant.Id,
                new AssignTenantSubscriptionCommand(
                    plan.Id,
                    "Monthly",
                    "Active",
                    TestNow.AddMonths(-2),
                    TestNow.AddMonths(-1),
                    false,
                    6,
                    null,
                    null,
                    null,
                    null,
                    tenant.Version,
                    "Temporary quota"),
                fixture.Audit));

        Assert.Contains("expiry", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await fixture.Context.TenantSubscriptions.CountAsync());
        Assert.Equal(0, await fixture.Context.TenantQuotaOverrides.CountAsync());
    }

    [Fact]
    public async Task ReassignSubscriptionTwiceClosesHistoryAndLeavesOneCurrentRow()
    {
        await using var fixture = await Fixture.CreateAsync(seedPermissions: true);
        var tenant = await fixture.Service.CreateTenantAsync(CreateCommand(), fixture.Audit);
        var plan = await fixture.Service.CreatePlanAsync(CreatePlanCommand(), fixture.Audit);

        var first = await fixture.Service.AssignSubscriptionAsync(
            tenant.Id,
            AssignCommand(plan.Id, tenant.Version, TestNow.AddDays(1), "Active", "Monthly"),
            fixture.Audit);
        var second = await fixture.Service.AssignSubscriptionAsync(
            tenant.Id,
            AssignCommand(plan.Id, first.Version, TestNow.AddDays(31), "Active", "Monthly"),
            fixture.Audit);
        _ = await fixture.Service.AssignSubscriptionAsync(
            tenant.Id,
            AssignCommand(plan.Id, second.Version, TestNow.AddDays(61), "Active", "Monthly"),
            fixture.Audit);

        var history = await fixture.Context.TenantSubscriptions
            .Where(subscription => subscription.TenantId == tenant.Id)
            .OrderBy(subscription => subscription.StartsUtc)
            .ToListAsync();
        Assert.Equal(3, history.Count);
        Assert.Equal([SubscriptionStatus.Cancelled, SubscriptionStatus.Cancelled, SubscriptionStatus.Active],
            history.Select(subscription => subscription.Status));
        Assert.Equal(history[1].StartsUtc, history[0].EndsUtc);
        Assert.Equal(history[2].StartsUtc, history[1].EndsUtc);
        Assert.Single(history, subscription => subscription.Status is SubscriptionStatus.Trial
            or SubscriptionStatus.Active or SubscriptionStatus.PastDue or SubscriptionStatus.Suspended);
    }

    [Fact]
    public async Task DashboardMrrUsesBillableStatusesAndNormalizesAnnualBilling()
    {
        await using var fixture = await Fixture.CreateAsync(seedPermissions: true);
        var annualTenant = await fixture.Service.CreateTenantAsync(CreateCommand(), fixture.Audit);
        var trialTenant = await fixture.Service.CreateTenantAsync(CreateCommand(), fixture.Audit);
        var plan = await fixture.Service.CreatePlanAsync(CreatePlanCommand(), fixture.Audit);
        _ = await fixture.Service.AssignSubscriptionAsync(
            annualTenant.Id,
            AssignCommand(plan.Id, annualTenant.Version, TestNow.AddDays(-1), "Active", "Annual"),
            fixture.Audit);
        _ = await fixture.Service.AssignSubscriptionAsync(
            trialTenant.Id,
            AssignCommand(plan.Id, trialTenant.Version, TestNow.AddDays(-1), "Trial", "Monthly"),
            fixture.Audit);

        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Equal(100m, dashboard.MonthlyRecurringRevenue);
    }

    [Fact]
    public async Task NumericUndefinedSubscriptionEnumsAreRejected()
    {
        await using var fixture = await Fixture.CreateAsync(seedPermissions: true);
        var tenant = await fixture.Service.CreateTenantAsync(CreateCommand(), fixture.Audit);
        var plan = await fixture.Service.CreatePlanAsync(CreatePlanCommand(), fixture.Audit);

        var failure = await Assert.ThrowsAsync<PlatformBusinessRuleException>(() =>
            fixture.Service.AssignSubscriptionAsync(
                tenant.Id,
                AssignCommand(plan.Id, tenant.Version, TestNow.AddDays(1), "99", "99"),
                fixture.Audit));

        Assert.Contains("invalid", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await fixture.Context.TenantSubscriptions.CountAsync());
    }

    [Fact]
    public async Task CompetingSubscriptionAssignmentWithSameVersionCannotCreateSecondCurrentRow()
    {
        await using var fixture = await Fixture.CreateAsync(seedPermissions: true);
        var tenant = await fixture.Service.CreateTenantAsync(CreateCommand(), fixture.Audit);
        var plan = await fixture.Service.CreatePlanAsync(CreatePlanCommand(), fixture.Audit);
        var firstCommand = AssignCommand(plan.Id, tenant.Version, TestNow.AddDays(1), "Active", "Monthly");
        var competingCommand = AssignCommand(plan.Id, tenant.Version, TestNow.AddDays(2), "Active", "Monthly");

        _ = await fixture.Service.AssignSubscriptionAsync(tenant.Id, firstCommand, fixture.Audit);
        await Assert.ThrowsAsync<PlatformConcurrencyException>(() =>
            fixture.Service.AssignSubscriptionAsync(tenant.Id, competingCommand, fixture.Audit));

        var currentCount = await fixture.Context.TenantSubscriptions.CountAsync(subscription =>
            subscription.TenantId == tenant.Id
            && (subscription.Status == SubscriptionStatus.Trial
                || subscription.Status == SubscriptionStatus.Active
                || subscription.Status == SubscriptionStatus.PastDue
                || subscription.Status == SubscriptionStatus.Suspended));
        Assert.Equal(1, currentCount);
    }

    private static CreatePlatformTenantCommand CreateCommand() => new(
        "New Retail Pvt Ltd",
        "New Retail",
        "Asia/Kolkata",
        "Asha Owner",
        "asha@example.test",
        "+919999999999",
        "IN",
        "INR",
        null,
        null,
        null,
        null,
        null);

    private static UpdatePlatformTenantCommand UpdateCommand(string version, string displayName) => new(
        "New Retail Pvt Ltd",
        displayName,
        "Asia/Kolkata",
        "Asha Owner",
        "asha@example.test",
        "+919999999999",
        "IN",
        "INR",
        version);

    private static SaveSubscriptionPlanCommand CreatePlanCommand() => new(
        "ANNUAL-GROWTH",
        "Annual Growth",
        120m,
        1200m,
        10,
        50,
        50,
        null,
        null,
        true,
        null);

    private static AssignTenantSubscriptionCommand AssignCommand(
        long planId,
        string version,
        DateTime startsUtc,
        string status,
        string billingCycle) => new(
        planId,
        billingCycle,
        status,
        startsUtc,
        null,
        true,
        null,
        null,
        null,
        null,
        null,
        version,
        "Approved plan assignment");

    private sealed class Fixture : IAsyncDisposable
    {
        private Fixture(SqliteConnection connection, CustSearchDbContext context, long actorId)
        {
            Connection = connection;
            Context = context;
            Service = new PlatformTenantManagementService(context, new FixedTimeProvider(TestNow));
            Audit = new PlatformAuditContext(actorId, "127.0.0.1", "Phase4Tests/1.0", "phase4-test");
        }

        private SqliteConnection Connection { get; }
        public CustSearchDbContext Context { get; }
        public PlatformTenantManagementService Service { get; }
        public PlatformAuditContext Audit { get; }

        public static async Task<Fixture> CreateAsync(bool seedPermissions)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var context = new CustSearchDbContext(
                new DbContextOptionsBuilder<CustSearchDbContext>().UseSqlite(connection).Options);
            await context.Database.EnsureCreatedAsync();
            var actor = UserAccount.CreatePlatform(
                "platform.phase4",
                "platform.phase4@example.test",
                "Platform Phase 4",
                "hash",
                TestNow);
            context.UserAccounts.Add(actor);
            if (seedPermissions)
            {
                var platformNames = typeof(PermissionCatalog.Platform).GetFields()
                    .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                    .Select(field => (string)field.GetRawConstantValue()!)
                    .ToHashSet(StringComparer.Ordinal);
                var permissions = PermissionCatalog.All.Select(name => Permission.Create(
                    platformNames.Contains(name) ? UserScope.Platform : UserScope.Tenant,
                    name,
                    $"Test permission {name}.",
                    TestNow));
                context.Permissions.AddRange(permissions);
            }

            await context.SaveChangesAsync();
            return new Fixture(connection, context, actor.Id);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
