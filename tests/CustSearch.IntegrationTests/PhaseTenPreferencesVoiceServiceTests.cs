using CustSearch.Application.Authentication;
using CustSearch.Application.PreferencesVoice;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using CustSearch.Infrastructure.PreferencesVoice;
using CustSearch.Infrastructure.TenantOperations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.IntegrationTests;

/// <summary>Phase 10 relational regressions for dynamic triggers, server-resolved aliases, confirmation and tenant/store isolation.</summary>
public sealed class PhaseTenPreferencesVoiceServiceTests
{
    private static readonly DateTime Now=new(2026,8,24,3,0,0,DateTimeKind.Utc);

    [Fact]
    public async Task StoreTriggerAndAliasesAreDynamicAndCannotCrossStore()
    {
        await using var f=await Fixture.CreateAsync();f.UseStoreA();var service=f.CreateService();
        var started=await service.StartVoiceSessionAsync(new(f.StoreA.Id,f.CustomerA.Id,"Apna Add"),f.Audit());Assert.Equal("Apna Add",started.MatchedTrigger);
        var ex=await Assert.ThrowsAsync<TenantBusinessRuleException>(()=>service.StartVoiceSessionAsync(new(f.StoreA.Id,f.CustomerA.Id,"Smart Add"),f.Audit()));Assert.Contains("does not match",ex.Message,StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownCategoryCreatesNoPreferenceSignal()
    {
        await using var f=await Fixture.CreateAsync();f.UseStoreA();var service=f.CreateService();var session=await service.StartVoiceSessionAsync(new(f.StoreA.Id,f.CustomerA.Id,"Magic Add"),f.Audit());
        var ex=await Assert.ThrowsAsync<TenantBusinessRuleException>(()=>service.InterpretVoiceSessionAsync(session.Id,new("Unknown Random Category",95,null,"test"),f.Audit()));Assert.Contains("Category not found",ex.Message,StringComparison.OrdinalIgnoreCase);Assert.Empty(await f.Db.CustomerPreferenceSignals.Where(x=>x.CustomerId==f.CustomerA.Id).ToListAsync());
    }

    [Fact]
    public async Task AmbiguousAliasRequiresServerCandidateSelectionThenExplicitConfirmation()
    {
        await using var f=await Fixture.CreateAsync();f.UseStoreA();await f.AddAliasAsync(f.CategoryA.Id,"Party Saree");await f.AddAliasAsync(f.CategoryB.Id,"Party Saree");var service=f.CreateService();var session=await service.StartVoiceSessionAsync(new(f.StoreA.Id,f.CustomerA.Id,"Magic Add"),f.Audit());
        var ambiguous=await service.InterpretVoiceSessionAsync(session.Id,new("Party Saree",94,null,"ambiguous"),f.Audit());Assert.True(ambiguous.NeedsCategorySelection);Assert.Equal(2,ambiguous.Candidates.Count);Assert.Equal(VoiceCommandSessionStatus.Listening,ambiguous.Session.Status);Assert.Empty(await f.Db.CustomerPreferenceSignals.Where(x=>x.CustomerId==f.CustomerA.Id).ToListAsync());
        var resolved=await service.InterpretVoiceSessionAsync(session.Id,new("Party Saree",94,f.CategoryA.Id,"selected"),f.Audit());Assert.False(resolved.NeedsCategorySelection);Assert.Equal(VoiceCommandSessionStatus.PendingConfirmation,resolved.Session.Status);Assert.Empty(await f.Db.CustomerPreferenceSignals.Where(x=>x.CustomerId==f.CustomerA.Id).ToListAsync());
        var confirmed=await service.ConfirmVoiceSessionAsync(session.Id,f.Audit());Assert.Equal(VoiceCommandSessionStatus.Confirmed,confirmed.Status);var signals=await f.Db.CustomerPreferenceSignals.Where(x=>x.CustomerId==f.CustomerA.Id).ToListAsync();Assert.Single(signals);Assert.Equal(PreferenceSignalSource.VoiceConfirmed,signals[0].Source);Assert.Equal(f.CategoryA.Id,signals[0].ReferenceId);
    }

    [Fact]
    public async Task RejectedConfirmationCreatesNoPreferenceSignal()
    {
        await using var f=await Fixture.CreateAsync();f.UseStoreA();var service=f.CreateService();var session=await service.StartVoiceSessionAsync(new(f.StoreA.Id,f.CustomerA.Id,"Magic Add"),f.Audit());var interpreted=await service.InterpretVoiceSessionAsync(session.Id,new(f.CategoryA.Name,98,null,"reject"),f.Audit());Assert.Equal(VoiceCommandSessionStatus.PendingConfirmation,interpreted.Session.Status);await service.RejectVoiceSessionAsync(session.Id,f.Audit());Assert.Empty(await f.Db.CustomerPreferenceSignals.Where(x=>x.CustomerId==f.CustomerA.Id).ToListAsync());
    }

    [Fact]
    public async Task StoreScopedUserCannotTargetCustomerFromAnotherStore()
    {
        await using var f=await Fixture.CreateAsync();f.UseStoreA();var ex=await Assert.ThrowsAsync<TenantResourceNotFoundException>(()=>f.CreateService().StartVoiceSessionAsync(new(f.StoreA.Id,f.CustomerB.Id,"Magic Add"),f.Audit()));Assert.Equal("Customer",ex.Message);
    }

    [Fact]
    public async Task RecalculationIsDeterministicForSameFactsAndWeightVersion()
    {
        await using var f=await Fixture.CreateAsync();f.UseStoreA();var service=f.CreateService();await service.AddCustomerTagAsync(f.CustomerA.Id,new(f.StoreA.Id,PreferenceType.Category,f.CategoryA.Id,f.CategoryA.Name,80,100,"manual"),f.Audit());var first=await service.RecalculateCustomerAsync(f.CustomerA.Id,f.Audit());var second=await service.RecalculateCustomerAsync(f.CustomerA.Id,f.Audit());Assert.Single(first.Scores);Assert.Single(second.Scores);Assert.Equal(first.Scores[0].Score,second.Scores[0].Score);Assert.Equal(first.Scores[0].PreferenceType,second.Scores[0].PreferenceType);Assert.Equal(first.Scores[0].ReferenceId,second.Scores[0].ReferenceId);
    }

    private sealed class Fixture:IAsyncDisposable
    {
        private readonly SqliteConnection connection;private readonly MutableCurrentUser currentUser=new();private readonly FixedTimeProvider clock=new(Now);
        private Fixture(SqliteConnection c,CustSearchDbContext db,Tenant tenant,UserAccount actor,Store storeA,Store storeB,Customer customerA,Customer customerB,ProductCategory categoryA,ProductCategory categoryB){connection=c;Db=db;Tenant=tenant;Actor=actor;StoreA=storeA;StoreB=storeB;CustomerA=customerA;CustomerB=customerB;CategoryA=categoryA;CategoryB=categoryB;}
        public CustSearchDbContext Db{get;}public Tenant Tenant{get;}public UserAccount Actor{get;}public Store StoreA{get;}public Store StoreB{get;}public Customer CustomerA{get;}public Customer CustomerB{get;}public ProductCategory CategoryA{get;}public ProductCategory CategoryB{get;}
        public static async Task<Fixture>CreateAsync()
        {
            var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();var db=new CustSearchDbContext(new DbContextOptionsBuilder<CustSearchDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();
            var tenant=Tenant.Create("T-P10","Phase Ten Retail Private Limited","Phase Ten Retail","Owner","owner@phase10.test","9000000010","IN","India Standard Time","INR",10,30,10,Now);db.Tenants.Add(tenant);await db.SaveChangesAsync();
            var actor=UserAccount.CreateTenant(tenant.Id,"voice-manager","voice@phase10.test","Voice Manager","hash",Now);db.UserAccounts.Add(actor);await db.SaveChangesAsync();
            var a=Store.Create(tenant.Id,"SURAT","Surat","Road",null,null,"Surat","Surat","Gujarat","395007","IN",null,null,null,null,StoreLocationSource.Manual,"India Standard Time",null,null,Now);var b=Store.Create(tenant.Id,"AMD","Ahmedabad","Road",null,null,"Ahmedabad","Ahmedabad","Gujarat","380001","IN",null,null,null,null,StoreLocationSource.Manual,"India Standard Time",null,null,Now);db.Stores.AddRange(a,b);await db.SaveChangesAsync();
            var ca=Customer.Create(tenant.Id,"C-A","Asha","Customer",null,null,null,Now);var cb=Customer.Create(tenant.Id,"C-B","Other","Customer",null,null,null,Now);db.Customers.AddRange(ca,cb);await db.SaveChangesAsync();db.CustomerStoreAssignments.AddRange(CustomerStoreAssignment.Assign(tenant.Id,ca.Id,a.Id,true,Now,actor.Id),CustomerStoreAssignment.Assign(tenant.Id,cb.Id,b.Id,true,Now,actor.Id));await db.SaveChangesAsync();
            var catA=ProductCategory.Create(tenant.Id,a.Id,"BANARASI","Banarasi Saree",null,Now);var catB=ProductCategory.Create(tenant.Id,a.Id,"PARTY","Party Wear Saree",null,Now);db.ProductCategories.AddRange(catA,catB);await db.SaveChangesAsync();
            db.StoreVoiceCommandSettings.AddRange(StoreVoiceCommandSetting.Create(tenant.Id,a.Id,"Magic Add",VoiceResponseMode.InAppAndVoice,Now),StoreVoiceCommandSetting.Create(tenant.Id,b.Id,"Smart Add",VoiceResponseMode.InAppAndVoice,Now));await db.SaveChangesAsync();db.StoreVoiceCommandAliases.Add(StoreVoiceCommandAlias.Create(tenant.Id,a.Id,"Apna Add",Now));await db.SaveChangesAsync();db.StoreVoiceCommandRuntimeSettings.AddRange(StoreVoiceCommandRuntimeSetting.Create(tenant.Id,a.Id,"en-IN",true,30,70,Now),StoreVoiceCommandRuntimeSetting.Create(tenant.Id,b.Id,"en-IN",true,30,70,Now));await db.SaveChangesAsync();
            return new(c,db,tenant,actor,a,b,ca,cb,catA,catB);
        }
        public void UseStoreA(){currentUser.UserIdValue=Actor.Id;currentUser.TenantIdValue=Tenant.Id;currentUser.RolesValue=new HashSet<string>(["StoreManager"],StringComparer.OrdinalIgnoreCase);currentUser.StoreIdsValue=new HashSet<long>([StoreA.Id]);}
        public TenantAuditContext Audit()=>new(Actor.Id,"127.0.0.1","Phase10Test","phase10-correlation");
        public async Task AddAliasAsync(long categoryId,string text){Db.ProductCategoryAliases.Add(ProductCategoryAlias.Create(Tenant.Id,StoreA.Id,categoryId,text,"en-IN",Actor.Id,Now));await Db.SaveChangesAsync();}
        public PreferencesVoiceService CreateService(){var tenantOps=new TenantOperationsService(Db,new TenantOperationsRepository(Db),currentUser,new PasswordHasher<UserAccount>(),clock);return new PreferencesVoiceService(Db,currentUser,tenantOps,clock);}
        public async ValueTask DisposeAsync(){await Db.DisposeAsync();await connection.DisposeAsync();}
    }
    private sealed class MutableCurrentUser:ICurrentUserContext{public long UserIdValue{get;set;}public long?TenantIdValue{get;set;}public IReadOnlySet<string>RolesValue{get;set;}=new HashSet<string>();public IReadOnlySet<long>StoreIdsValue{get;set;}=new HashSet<long>();public bool IsAuthenticated=>true;public long UserId=>UserIdValue;public long?TenantId=>TenantIdValue;public bool IsPlatformAdmin=>false;public string SecurityStamp=>"p10";public IReadOnlySet<string>Roles=>RolesValue;public IReadOnlySet<string>Permissions=>new HashSet<string>();public IReadOnlySet<long>StoreIds=>StoreIdsValue;}
    private sealed class FixedTimeProvider(DateTime utc):TimeProvider{private readonly DateTimeOffset value=new(utc);public override DateTimeOffset GetUtcNow()=>value;}
}
