using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.HouseholdsVisits;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.HouseholdsVisits;
using CustSearch.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.IntegrationTests;

/// <summary>Phase 7 relational security regressions for household/member, visit and co-visit store visibility.</summary>
public sealed class PhaseSevenSecurityServiceTests
{
    private static readonly DateTime UtcNow=new(2026,8,23,14,30,0,DateTimeKind.Utc);

    [Fact]
    public async Task StoreScopedUserCannotReadHouseholdWhoseMembersAreOnlyInAnotherStore()
    {
        await using var f=await Fixture.CreateAsync();var actor=await f.AddUserAsync("manager");var allowed=await f.AddStoreAsync("SURAT");var forbidden=await f.AddStoreAsync("AMD");var customer=await f.AddCustomerAsync("C-FORB",forbidden.Id,actor.Id);var household=await f.AddHouseholdAsync("HH-FORB",customer.Id,actor.Id);
        f.SetCurrentUser(actor.Id,["StoreManager"],[PermissionCatalog.Operations.HouseholdsView],[allowed.Id]);
        await Assert.ThrowsAsync<TenantResourceNotFoundException>(()=>f.CreateService().GetHouseholdAsync(household.Id));
    }

    [Fact]
    public async Task StoreScopedUserCannotAddOutOfScopeCustomerToVisibleHousehold()
    {
        await using var f=await Fixture.CreateAsync();var actor=await f.AddUserAsync("manager");var allowed=await f.AddStoreAsync("SURAT");var forbidden=await f.AddStoreAsync("AMD");var visibleCustomer=await f.AddCustomerAsync("C-OK",allowed.Id,actor.Id);var forbiddenCustomer=await f.AddCustomerAsync("C-NO",forbidden.Id,actor.Id);var household=await f.AddHouseholdAsync("HH-OK",visibleCustomer.Id,actor.Id);
        f.SetCurrentUser(actor.Id,["StoreManager"],[PermissionCatalog.Operations.HouseholdsManageMembers],[allowed.Id]);
        await Assert.ThrowsAsync<TenantResourceNotFoundException>(()=>f.CreateService().SaveHouseholdMemberAsync(household.Id,new(forbiddenCustomer.Id,"Sibling",HouseholdRelationshipSource.StaffVerified),Fixture.Audit(actor.Id)));
    }

    [Fact]
    public async Task StoreScopedUserCannotReadVisitFromAnotherStore()
    {
        await using var f=await Fixture.CreateAsync();var actor=await f.AddUserAsync("manager");var allowed=await f.AddStoreAsync("SURAT");var forbidden=await f.AddStoreAsync("AMD");var customer=await f.AddCustomerAsync("C-1",forbidden.Id,actor.Id);var visit=CustomerVisit.Create(f.Tenant.Id,forbidden.Id,customer.Id,"V-1",UtcNow,CustomerVisitSource.Manual);f.Db.CustomerVisits.Add(visit);await f.Db.SaveChangesAsync();
        f.SetCurrentUser(actor.Id,["StoreManager"],[PermissionCatalog.Operations.VisitsView],[allowed.Id]);
        await Assert.ThrowsAsync<TenantResourceNotFoundException>(()=>f.CreateService().GetVisitAsync(visit.Id));
    }

    [Fact]
    public async Task StoreScopedUserCannotReadVisitPartyFromAnotherStore()
    {
        await using var f=await Fixture.CreateAsync();var actor=await f.AddUserAsync("manager");var allowed=await f.AddStoreAsync("SURAT");var forbidden=await f.AddStoreAsync("AMD");var party=VisitParty.Create(f.Tenant.Id,forbidden.Id,"P-1",UtcNow,VisitPartySource.CctvCoVisit);f.Db.VisitParties.Add(party);await f.Db.SaveChangesAsync();
        f.SetCurrentUser(actor.Id,["StoreManager"],[PermissionCatalog.Operations.VisitPartiesView],[allowed.Id]);
        await Assert.ThrowsAsync<TenantResourceNotFoundException>(()=>f.CreateService().GetVisitPartyAsync(party.Id));
    }

    [Fact]
    public async Task EmptyHouseholdCreationIsRestrictedForStoreScopedUser()
    {
        await using var f=await Fixture.CreateAsync();var actor=await f.AddUserAsync("manager");var store=await f.AddStoreAsync("SURAT");f.SetCurrentUser(actor.Id,["StoreManager"],[PermissionCatalog.Operations.HouseholdsCreate],[store.Id]);
        var ex=await Assert.ThrowsAsync<TenantBusinessRuleException>(()=>f.CreateService().CreateHouseholdAsync(new(null,"Patel Family",null),Fixture.Audit(actor.Id)));
        Assert.Contains("tenant-wide",ex.Message,StringComparison.OrdinalIgnoreCase);
    }

    private sealed class Fixture:IAsyncDisposable
    {
        private readonly SqliteConnection connection;private readonly MutableCurrentUserContext currentUser=new();private readonly FixedTimeProvider timeProvider=new(UtcNow);
        private Fixture(SqliteConnection c,CustSearchDbContext db,Tenant tenant){connection=c;Db=db;Tenant=tenant;}
        public CustSearchDbContext Db{get;}public Tenant Tenant{get;}
        public static async Task<Fixture>CreateAsync(){var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();var db=new CustSearchDbContext(new DbContextOptionsBuilder<CustSearchDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();var tenant=Tenant.Create("T-P7","Phase Seven Retail Private Limited","Phase Seven Retail","Owner","owner@phase7.test","9000000000","IN","India Standard Time","INR",10,20,10,UtcNow);db.Tenants.Add(tenant);await db.SaveChangesAsync();return new(c,db,tenant);}
        public async Task<UserAccount>AddUserAsync(string name){var u=UserAccount.CreateTenant(Tenant.Id,name,$"{name}@phase7.test",name,"hash",UtcNow);Db.UserAccounts.Add(u);await Db.SaveChangesAsync();return u;}
        public async Task<Store>AddStoreAsync(string code){var s=Store.Create(Tenant.Id,code,code,"Road",null,null,"Surat","Surat","Gujarat","395007","IN",null,null,null,null,StoreLocationSource.Manual,"India Standard Time",null,null,UtcNow);Db.Stores.Add(s);await Db.SaveChangesAsync();return s;}
        public async Task<Customer>AddCustomerAsync(string code,long storeId,long actor){var c=Customer.Create(Tenant.Id,code,"Customer",null,null,null,null,UtcNow);Db.Customers.Add(c);await Db.SaveChangesAsync();Db.CustomerStoreAssignments.Add(CustomerStoreAssignment.Assign(Tenant.Id,c.Id,storeId,true,UtcNow,actor));await Db.SaveChangesAsync();return c;}
        public async Task<Household>AddHouseholdAsync(string code,long customerId,long actor){var h=Household.Create(Tenant.Id,code,code,null,UtcNow);Db.Households.Add(h);await Db.SaveChangesAsync();Db.HouseholdMembers.Add(HouseholdMember.Link(Tenant.Id,h.Id,customerId,"Member",HouseholdRelationshipSource.AdminVerified,actor,UtcNow));await Db.SaveChangesAsync();return h;}
        public void SetCurrentUser(long userId,IEnumerable<string> roles,IEnumerable<string> permissions,IEnumerable<long> stores){currentUser.UserIdValue=userId;currentUser.TenantIdValue=Tenant.Id;currentUser.RolesValue=new HashSet<string>(roles,StringComparer.OrdinalIgnoreCase);currentUser.PermissionsValue=new HashSet<string>(permissions,StringComparer.Ordinal);currentUser.StoreIdsValue=new HashSet<long>(stores);}
        public HouseholdsVisitsService CreateService()=>new(Db,new NoopRepository(),currentUser,timeProvider);public static TenantAuditContext Audit(long actor)=>new(actor,"127.0.0.1","Phase7Test","phase7-correlation");public async ValueTask DisposeAsync(){await Db.DisposeAsync();await connection.DisposeAsync();}
    }
    private sealed class NoopRepository:IHouseholdsVisitsRepository
    {
        public Task<IReadOnlyList<HouseholdSearchRow>> SearchHouseholdsAsync(long tenantId,IReadOnlySet<long> allowedStoreIds,bool tenantWide,HouseholdSearchQuery query,CancellationToken cancellationToken=default)=>Task.FromResult<IReadOnlyList<HouseholdSearchRow>>([]);
        public Task<IReadOnlyList<CustomerVisitSearchRow>> SearchVisitsAsync(long tenantId,IReadOnlySet<long> allowedStoreIds,bool tenantWide,CustomerVisitSearchQuery query,CancellationToken cancellationToken=default)=>Task.FromResult<IReadOnlyList<CustomerVisitSearchRow>>([]);
        public Task<IReadOnlyList<VisitPartySearchRow>> SearchVisitPartiesAsync(long tenantId,IReadOnlySet<long> allowedStoreIds,bool tenantWide,VisitPartySearchQuery query,CancellationToken cancellationToken=default)=>Task.FromResult<IReadOnlyList<VisitPartySearchRow>>([]);
    }
    private sealed class MutableCurrentUserContext:ICurrentUserContext{public long UserIdValue{get;set;}public long? TenantIdValue{get;set;}public IReadOnlySet<string>RolesValue{get;set;}=new HashSet<string>();public IReadOnlySet<string>PermissionsValue{get;set;}=new HashSet<string>();public IReadOnlySet<long>StoreIdsValue{get;set;}=new HashSet<long>();public bool IsAuthenticated=>true;public long UserId=>UserIdValue;public long? TenantId=>TenantIdValue;public bool IsPlatformAdmin=>false;public string SecurityStamp=>"p7";public IReadOnlySet<string>Roles=>RolesValue;public IReadOnlySet<string>Permissions=>PermissionsValue;public IReadOnlySet<long>StoreIds=>StoreIdsValue;}
    private sealed class FixedTimeProvider(DateTime utc):TimeProvider{private readonly DateTimeOffset value=new(utc);public override DateTimeOffset GetUtcNow()=>value;}
}