using CustSearch.Application.Authentication;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Entities;
using CustSearch.Infrastructure.Persistence;
using CustSearch.Infrastructure.TenantOperations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.IntegrationTests;

/// <summary>
/// Phase 5 security regression tests exercise the real application decorator against a relational SQLite database.
/// They specifically protect tenant/store isolation, privilege boundaries and MaxUsers reactivation enforcement.
/// </summary>
public sealed class PhaseFiveSecurityDecoratorTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 23, 8, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ReactivatingInactiveUserAtMaxUsersQuotaIsRejected()
    {
        await using var fixture = await Fixture.CreateAsync(maxUsers: 1);
        var actor = await fixture.AddUserAsync("owner", "owner@example.test", active: true);
        var inactive = await fixture.AddUserAsync("inactive", "inactive@example.test", active: false);
        fixture.SetCurrentUser(actor.Id, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "TenantAdmin" }, new HashSet<long>());

        var service = fixture.CreateService();
        var exception = await Assert.ThrowsAsync<TenantBusinessRuleException>(() => service.UpdateUserAsync(
            inactive.Id,
            new UpdateTenantUserCommand(inactive.Email, inactive.DisplayName, true),
            Fixture.Audit(actor.Id)));

        Assert.Contains("quota", exception.Message, StringComparison.OrdinalIgnoreCase);
        fixture.Db.ChangeTracker.Clear();
        var unchanged = await fixture.Db.UserAccounts.AsNoTracking().SingleAsync(x => x.Id == inactive.Id);
        Assert.False(unchanged.IsActive);
    }

    [Fact]
    public async Task StoreScopedAdministratorCannotReadUserAssignedOnlyToAnotherStore()
    {
        await using var fixture = await Fixture.CreateAsync();
        var actor = await fixture.AddUserAsync("storeadmin", "storeadmin@example.test", active: true);
        var target = await fixture.AddUserAsync("otherstore", "otherstore@example.test", active: true);
        var allowedStore = await fixture.AddStoreAsync("SURAT-01", "Surat Store");
        var forbiddenStore = await fixture.AddStoreAsync("AMD-01", "Ahmedabad Store");
        await fixture.AssignStoreAsync(actor.Id, allowedStore.Id, actor.Id);
        await fixture.AssignStoreAsync(target.Id, forbiddenStore.Id, actor.Id);
        fixture.SetCurrentUser(actor.Id, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "StoreAdmin" }, new HashSet<long> { allowedStore.Id });

        var service = fixture.CreateService();
        await Assert.ThrowsAsync<TenantResourceNotFoundException>(() => service.GetUserAsync(target.Id));
    }

    [Fact]
    public async Task StoreScopedAdministratorCannotAssignTenantWideRole()
    {
        await using var fixture = await Fixture.CreateAsync();
        var actor = await fixture.AddUserAsync("storeadmin", "storeadmin@example.test", active: true);
        var target = await fixture.AddUserAsync("sales", "sales@example.test", active: true);
        var store = await fixture.AddStoreAsync("SURAT-01", "Surat Store");
        await fixture.AssignStoreAsync(actor.Id, store.Id, actor.Id);
        await fixture.AssignStoreAsync(target.Id, store.Id, actor.Id);
        fixture.SetCurrentUser(actor.Id, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "StoreAdmin" }, new HashSet<long> { store.Id });

        var service = fixture.CreateService();
        var exception = await Assert.ThrowsAsync<TenantBusinessRuleException>(() => service.SetUserRolesAsync(
            target.Id,
            new SetTenantUserRolesCommand(["TenantAdmin"]),
            Fixture.Audit(actor.Id)));

        Assert.Contains("cannot assign", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StoreScopedAdministratorCannotAssignUserToStoreOutsideOwnScope()
    {
        await using var fixture = await Fixture.CreateAsync();
        var actor = await fixture.AddUserAsync("storeadmin", "storeadmin@example.test", active: true);
        var target = await fixture.AddUserAsync("sales", "sales@example.test", active: true);
        var allowedStore = await fixture.AddStoreAsync("SURAT-01", "Surat Store");
        var forbiddenStore = await fixture.AddStoreAsync("AMD-01", "Ahmedabad Store");
        await fixture.AssignStoreAsync(actor.Id, allowedStore.Id, actor.Id);
        await fixture.AssignStoreAsync(target.Id, allowedStore.Id, actor.Id);
        fixture.SetCurrentUser(actor.Id, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "StoreAdmin" }, new HashSet<long> { allowedStore.Id });

        var service = fixture.CreateService();
        var exception = await Assert.ThrowsAsync<TenantBusinessRuleException>(() => service.SetUserStoresAsync(
            target.Id,
            new SetTenantUserStoresCommand([forbiddenStore.Id], forbiddenStore.Id),
            Fixture.Audit(actor.Id)));

        Assert.Contains("own stores", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TenantAdministratorPasswordResetRehashesAndAuditsWithoutCredentialMaterial()
    {
        await using var fixture = await Fixture.CreateAsync();
        var actor = await fixture.AddUserAsync("owner", "owner@example.test", active: true);
        var target = await fixture.AddUserAsync("cashier", "cashier@example.test", active: true);
        fixture.SetCurrentUser(
            actor.Id,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "TenantAdmin" },
            new HashSet<long>());
        var before = target.PasswordHash;

        await fixture.CreateService().ResetUserPasswordAsync(
            target.Id,
            new ResetTenantUserPasswordCommand("ResetPassword123"),
            Fixture.Audit(actor.Id));

        fixture.Db.ChangeTracker.Clear();
        var changed = await fixture.Db.UserAccounts.AsNoTracking().SingleAsync(x => x.Id == target.Id);
        Assert.NotEqual(before, changed.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<UserAccount>().VerifyHashedPassword(changed, changed.PasswordHash, "ResetPassword123"));
        var audit = await fixture.Db.AuditLogs.AsNoTracking()
            .SingleAsync(x => x.Action == "TenantUserPasswordReset"
                && x.EntityId == target.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.DoesNotContain("ResetPassword123", audit.AfterJson ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("SessionsRevoked", audit.AfterJson ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StoreScopedAdministratorCannotResetPasswordOutsideOwnStore()
    {
        await using var fixture = await Fixture.CreateAsync();
        var actor = await fixture.AddUserAsync("storeadmin", "storeadmin@example.test", active: true);
        var target = await fixture.AddUserAsync("otherstore", "otherstore@example.test", active: true);
        var allowedStore = await fixture.AddStoreAsync("SURAT-02", "Surat Store Two");
        var forbiddenStore = await fixture.AddStoreAsync("AMD-02", "Ahmedabad Store Two");
        await fixture.AssignStoreAsync(actor.Id, allowedStore.Id, actor.Id);
        await fixture.AssignStoreAsync(target.Id, forbiddenStore.Id, actor.Id);
        fixture.SetCurrentUser(
            actor.Id,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "StoreAdmin" },
            new HashSet<long> { allowedStore.Id });

        await Assert.ThrowsAsync<TenantResourceNotFoundException>(() =>
            fixture.CreateService().ResetUserPasswordAsync(
                target.Id,
                new ResetTenantUserPasswordCommand("ResetPassword123"),
                Fixture.Audit(actor.Id)));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly MutableCurrentUserContext currentUser = new();
        private readonly FixedTimeProvider timeProvider = new(UtcNow);

        private Fixture(SqliteConnection connection, CustSearchDbContext db, Tenant tenant)
        {
            this.connection = connection;
            Db = db;
            Tenant = tenant;
        }

        public CustSearchDbContext Db { get; }
        public Tenant Tenant { get; }

        public static async Task<Fixture> CreateAsync(int maxUsers = 10)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<CustSearchDbContext>()
                .UseSqlite(connection)
                .Options;
            var db = new CustSearchDbContext(options);
            await db.Database.EnsureCreatedAsync();

            var tenant = Tenant.Create(
                "T-PHASE5",
                "Phase Five Retail Private Limited",
                "Phase Five Retail",
                "Owner",
                "owner@phase5.test",
                "9000000000",
                "IN",
                "India Standard Time",
                "INR",
                10,
                maxUsers,
                10,
                UtcNow);
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
            return new Fixture(connection, db, tenant);
        }

        public async Task<UserAccount> AddUserAsync(string userName, string email, bool active)
        {
            var user = UserAccount.CreateTenant(Tenant.Id, userName, email, userName, "hash", UtcNow);
            if (!active) user.Deactivate();
            Db.UserAccounts.Add(user);
            await Db.SaveChangesAsync();
            return user;
        }

        public async Task<Store> AddStoreAsync(string code, string name)
        {
            // Security tests do not need geospatial data. Keeping coordinates null avoids SQLite's provider-specific
            // decimal CHECK comparison behavior while the SQL Server coordinate rules remain covered elsewhere.
            var store = Store.Create(
                Tenant.Id,
                code,
                name,
                "Retail Road",
                null,
                null,
                "Surat",
                "Surat",
                "Gujarat",
                "395007",
                "IN",
                null,
                null,
                null,
                null,
                CustSearch.Domain.Enums.StoreLocationSource.Manual,
                "India Standard Time",
                null,
                null,
                UtcNow);
            Db.Stores.Add(store);
            await Db.SaveChangesAsync();
            return store;
        }

        public async Task AssignStoreAsync(long userId, long storeId, long assignedByUserId)
        {
            Db.UserStoreAssignments.Add(UserStoreAssignment.Assign(Tenant.Id, userId, storeId, true, UtcNow, assignedByUserId));
            await Db.SaveChangesAsync();
        }

        public void SetCurrentUser(long userId, IReadOnlySet<string> roles, IReadOnlySet<long> storeIds)
        {
            currentUser.UserIdValue = userId;
            currentUser.TenantIdValue = Tenant.Id;
            currentUser.RolesValue = roles;
            currentUser.StoreIdsValue = storeIds;
        }

        public TenantOperationsSecurityDecorator CreateService()
        {
            var repository = new TenantOperationsRepository(Db);
            var inner = new TenantOperationsService(Db, repository, currentUser, new PasswordHasher<UserAccount>(), timeProvider);
            return new TenantOperationsSecurityDecorator(inner, Db, currentUser);
        }

        public static TenantAuditContext Audit(long actorUserId) => new(actorUserId, "127.0.0.1", "Phase5IntegrationTest", "phase5-test-correlation");

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class MutableCurrentUserContext : ICurrentUserContext
    {
        public long UserIdValue { get; set; }
        public long? TenantIdValue { get; set; }
        public IReadOnlySet<string> RolesValue { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlySet<long> StoreIdsValue { get; set; } = new HashSet<long>();

        public bool IsAuthenticated => true;
        public long UserId => UserIdValue;
        public long? TenantId => TenantIdValue;
        public bool IsPlatformAdmin => false;
        public string SecurityStamp => "phase5-test-stamp";
        public IReadOnlySet<string> Roles => RolesValue;
        public IReadOnlySet<string> Permissions => new HashSet<string>();
        public IReadOnlySet<long> StoreIds => StoreIdsValue;
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        private readonly DateTimeOffset fixedUtcNow = new(utcNow);
        public override DateTimeOffset GetUtcNow() => fixedUtcNow;
    }
}
