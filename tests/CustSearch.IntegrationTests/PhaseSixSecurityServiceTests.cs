using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.ShopperCustomers;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using CustSearch.Infrastructure.ShopperCustomers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.IntegrationTests;

/// <summary>Phase 6 security regressions exercise the real shopper service against relational SQLite state.</summary>
public sealed class PhaseSixSecurityServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 23, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task StoreScopedUserCannotReadCustomerAssignedOnlyToAnotherStore()
    {
        await using var f = await Fixture.CreateAsync();
        var actor = await f.AddUserAsync("storeadmin", "storeadmin@phase6.test");
        var allowed = await f.AddStoreAsync("SURAT-01", "Surat");
        var forbidden = await f.AddStoreAsync("AMD-01", "Ahmedabad");
        var customer = await f.AddCustomerAsync("CUST-OTHER", forbidden.Id, actor.Id);
        f.SetCurrentUser(actor.Id, ["StoreAdmin"], [PermissionCatalog.Operations.CustomersView], [allowed.Id]);

        await Assert.ThrowsAsync<TenantResourceNotFoundException>(() => f.CreateService().GetCustomerAsync(customer.Id));
    }

    [Fact]
    public async Task StoreScopedUserCannotReadAnonymousVisitorFromAnotherStore()
    {
        await using var f = await Fixture.CreateAsync();
        var actor = await f.AddUserAsync("crm", "crm@phase6.test");
        var allowed = await f.AddStoreAsync("SURAT-01", "Surat");
        var forbidden = await f.AddStoreAsync("AMD-01", "Ahmedabad");
        var visitor = await f.AddVisitorAsync(forbidden.Id, "VIS-OTHER");
        f.SetCurrentUser(actor.Id, ["CRMStaff"], [PermissionCatalog.Operations.VisitorsView], [allowed.Id]);

        await Assert.ThrowsAsync<TenantResourceNotFoundException>(() => f.CreateService().GetVisitorAsync(visitor.Id));
    }

    [Fact]
    public async Task StoreScopedUserCannotAssignCustomerToOutOfScopeStore()
    {
        await using var f = await Fixture.CreateAsync();
        var actor = await f.AddUserAsync("storeadmin", "storeadmin@phase6.test");
        var allowed = await f.AddStoreAsync("SURAT-01", "Surat");
        var forbidden = await f.AddStoreAsync("AMD-01", "Ahmedabad");
        var customer = await f.AddCustomerAsync("CUST-001", allowed.Id, actor.Id);
        f.SetCurrentUser(actor.Id, ["StoreAdmin"], [PermissionCatalog.Operations.CustomersEdit], [allowed.Id]);

        var error = await Assert.ThrowsAsync<TenantBusinessRuleException>(() => f.CreateService().SetCustomerStoresAsync(
            customer.Id, new SetCustomerStoresCommand([forbidden.Id], forbidden.Id), Fixture.Audit(actor.Id)));

        Assert.Contains("outside", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(await f.Db.CustomerStoreAssignments.AnyAsync(x => x.CustomerId == customer.Id && x.StoreId == allowed.Id));
    }

    [Fact]
    public async Task VisitorConversionToNewCustomerRequiresCustomersCreatePermission()
    {
        await using var f = await Fixture.CreateAsync();
        var actor = await f.AddUserAsync("crm", "crm@phase6.test");
        var store = await f.AddStoreAsync("SURAT-01", "Surat");
        var visitor = await f.AddVisitorAsync(store.Id, "VIS-001");
        f.SetCurrentUser(actor.Id, ["CRMStaff"], [PermissionCatalog.Operations.VisitorsConvert], [store.Id]);

        var error = await Assert.ThrowsAsync<TenantBusinessRuleException>(() => f.CreateService().ConvertVisitorAsync(
            visitor.Id, new ConvertAnonymousVisitorCommand(null, "Priya", null, null, null, null), Fixture.Audit(actor.Id)));

        Assert.Contains("Customers.Create", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizedVisitorConversionCreatesCustomerAndStoreAssignmentAtomically()
    {
        await using var f = await Fixture.CreateAsync();
        var actor = await f.AddUserAsync("crm", "crm@phase6.test");
        var store = await f.AddStoreAsync("SURAT-01", "Surat");
        var visitor = await f.AddVisitorAsync(store.Id, "VIS-002");
        f.SetCurrentUser(actor.Id, ["CRMStaff"], [PermissionCatalog.Operations.VisitorsConvert, PermissionCatalog.Operations.CustomersCreate], [store.Id]);

        var customer = await f.CreateService().ConvertVisitorAsync(visitor.Id,
            new ConvertAnonymousVisitorCommand(null, "Priya", "Shah", "9876543210", "priya@example.test", null), Fixture.Audit(actor.Id));

        Assert.Equal("Priya", customer.FirstName);
        Assert.Contains(store.Id, customer.StoreIds);
        f.Db.ChangeTracker.Clear();
        var converted = await f.Db.AnonymousVisitors.AsNoTracking().SingleAsync(x => x.Id == visitor.Id);
        Assert.Equal(customer.Id, converted.ConvertedCustomerId);
        Assert.False(converted.IsActive);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly MutableCurrentUserContext currentUser = new();
        private readonly FixedTimeProvider timeProvider = new(UtcNow);
        private Fixture(SqliteConnection connection, CustSearchDbContext db, Tenant tenant) { this.connection = connection; Db = db; Tenant = tenant; }
        public CustSearchDbContext Db { get; }
        public Tenant Tenant { get; }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<CustSearchDbContext>().UseSqlite(connection).Options;
            var db = new CustSearchDbContext(options);
            await db.Database.EnsureCreatedAsync();
            var tenant = Tenant.Create("T-PHASE6", "Phase Six Retail Private Limited", "Phase Six Retail", "Owner",
                "owner@phase6.test", "9000000000", "IN", "India Standard Time", "INR", 10, 20, 10, UtcNow);
            db.Tenants.Add(tenant); await db.SaveChangesAsync();
            return new Fixture(connection, db, tenant);
        }

        public async Task<UserAccount> AddUserAsync(string userName, string email)
        {
            var user = UserAccount.CreateTenant(Tenant.Id, userName, email, userName, "hash", UtcNow);
            Db.UserAccounts.Add(user); await Db.SaveChangesAsync(); return user;
        }

        public async Task<Store> AddStoreAsync(string code, string name)
        {
            var store = Store.Create(Tenant.Id, code, name, "Retail Road", null, null, "Surat", "Surat", "Gujarat", "395007", "IN",
                null, null, null, null, StoreLocationSource.Manual, "India Standard Time", null, null, UtcNow);
            Db.Stores.Add(store); await Db.SaveChangesAsync(); return store;
        }

        public async Task<Customer> AddCustomerAsync(string code, long storeId, long actorId)
        {
            var customer = Customer.Create(Tenant.Id, code, "Customer", null, null, null, null, UtcNow);
            Db.Customers.Add(customer); await Db.SaveChangesAsync();
            Db.CustomerStoreAssignments.Add(CustomerStoreAssignment.Assign(Tenant.Id, customer.Id, storeId, true, UtcNow, actorId));
            await Db.SaveChangesAsync(); return customer;
        }

        public async Task<AnonymousVisitor> AddVisitorAsync(long storeId, string code)
        {
            var visitor = AnonymousVisitor.Create(Tenant.Id, storeId, code, UtcNow);
            Db.AnonymousVisitors.Add(visitor); await Db.SaveChangesAsync(); return visitor;
        }

        public void SetCurrentUser(long userId, IEnumerable<string> roles, IEnumerable<string> permissions, IEnumerable<long> stores)
        {
            currentUser.UserIdValue=userId; currentUser.TenantIdValue=Tenant.Id;
            currentUser.RolesValue=new HashSet<string>(roles,StringComparer.OrdinalIgnoreCase);
            currentUser.PermissionsValue=new HashSet<string>(permissions,StringComparer.Ordinal);
            currentUser.StoreIdsValue=new HashSet<long>(stores);
        }

        public ShopperCustomerService CreateService() => new(Db, new NoopRepository(), currentUser, timeProvider);
        public static TenantAuditContext Audit(long actor) => new(actor,"127.0.0.1","Phase6IntegrationTest","phase6-test-correlation");
        public async ValueTask DisposeAsync(){await Db.DisposeAsync();await connection.DisposeAsync();}
    }

    private sealed class NoopRepository : IShopperCustomerRepository
    {
        public Task<IReadOnlyList<CustomerSearchRow>> SearchCustomersAsync(long tenantId, IReadOnlySet<long> allowedStoreIds, bool tenantWide, CustomerSearchQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CustomerSearchRow>>([]);
        public Task<IReadOnlyList<AnonymousVisitorSearchRow>> SearchVisitorsAsync(long tenantId, IReadOnlySet<long> allowedStoreIds, bool tenantWide, AnonymousVisitorSearchQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AnonymousVisitorSearchRow>>([]);
    }

    private sealed class MutableCurrentUserContext : ICurrentUserContext
    {
        public long UserIdValue { get; set; }
        public long? TenantIdValue { get; set; }
        public IReadOnlySet<string> RolesValue { get; set; } = new HashSet<string>();
        public IReadOnlySet<string> PermissionsValue { get; set; } = new HashSet<string>();
        public IReadOnlySet<long> StoreIdsValue { get; set; } = new HashSet<long>();
        public bool IsAuthenticated => true; public long UserId=>UserIdValue; public long? TenantId=>TenantIdValue; public bool IsPlatformAdmin=>false; public string SecurityStamp=>"phase6-test"; public IReadOnlySet<string> Roles=>RolesValue; public IReadOnlySet<string> Permissions=>PermissionsValue; public IReadOnlySet<long> StoreIds=>StoreIdsValue;
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        private readonly DateTimeOffset value = new(utcNow); public override DateTimeOffset GetUtcNow()=>value;
    }
}
