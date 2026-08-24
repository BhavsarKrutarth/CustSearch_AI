using System.Globalization;
using CustSearch.Application.AlertsRealtime;
using CustSearch.Application.Authentication;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.AlertsRealtime;
using CustSearch.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.IntegrationTests;

/// <summary>Relational Phase 11 tests for atomic outbox, scope, retries, recovery and concurrent de-duplication.</summary>
public sealed class PhaseElevenAlertsRealtimeServiceTests:IAsyncLifetime
{
    private static readonly DateTime Now=new(2026,8,24,15,0,0,DateTimeKind.Utc);private readonly string databasePath=Path.Combine(Path.GetTempPath(),$"custsearch-p11-{Guid.NewGuid():N}.db");private readonly AlertDeduplicationCoordinator coordinator=new();private Tenant tenant=null!;private UserAccount actor=null!;private Store storeA=null!;private Store storeB=null!;
    public async Task InitializeAsync(){await using var db=CreateDb();await db.Database.EnsureCreatedAsync();tenant=Tenant.Create("P11","Phase Eleven Private Limited","Phase Eleven","Owner","owner@p11.test","9000000011","IN","India Standard Time","INR",10,30,10,Now);db.Tenants.Add(tenant);await db.SaveChangesAsync();actor=UserAccount.CreateTenant(tenant.Id,"alerts-manager","alerts@p11.test","Alerts Manager","hash",Now);db.UserAccounts.Add(actor);await db.SaveChangesAsync();storeA=Store.Create(tenant.Id,"SURAT","Surat","Road",null,null,"Surat","Surat","Gujarat","395007","IN",null,null,null,null,StoreLocationSource.Manual,"India Standard Time",null,null,Now);storeB=Store.Create(tenant.Id,"AMD","Ahmedabad","Road",null,null,"Ahmedabad","Ahmedabad","Gujarat","380001","IN",null,null,null,null,StoreLocationSource.Manual,"India Standard Time",null,null,Now);db.Stores.AddRange(storeA,storeB);await db.SaveChangesAsync();}
    public Task DisposeAsync(){if(File.Exists(databasePath))File.Delete(databasePath);return Task.CompletedTask;}

    [Fact]
    public async Task AlertEventAndOutboxCommitAtomicallyAndDuplicateKeyReturnsOneAlert()
    {
        await using var db=CreateDb();var user=User([storeA.Id]);var service=Service(db,user);var command=new CreateAlertCommand("returning.customer",storeA.Id,AlertSeverity.Warning,"Returning customer","Customer returned.","Customer","9","visit:9:44");var first=await service.CreateAsync(command,Audit());var duplicate=await service.CreateAsync(command,Audit());Assert.Equal(first.Id,duplicate.Id);Assert.Equal(1,await db.Alerts.CountAsync());Assert.Equal(1,await db.RealtimeEvents.CountAsync());Assert.Equal(1,await db.NotificationOutbox.CountAsync());var outbox=await db.NotificationOutbox.SingleAsync();Assert.Equal(NotificationOutboxStatus.Pending,outbox.Status);Assert.Equal(first.Id,outbox.AlertId);
    }

    [Fact]
    public async Task ConcurrentDuplicateRequestsProduceOneAuthoritativeAlertAndOutbox()
    {
        var command=new CreateAlertCommand("security.threshold",storeA.Id,AlertSeverity.Critical,"Threshold","Threshold reached.","Visit","91","security:visit:91");var tasks=Enumerable.Range(0,8).Select(async _=>{await using var db=CreateDb();return await Service(db,User([storeA.Id])).CreateAsync(command,Audit());});var results=await Task.WhenAll(tasks);Assert.Single(results.Select(x=>x.Id).Distinct());await using var verify=CreateDb();Assert.Equal(1,await verify.Alerts.CountAsync(x=>x.DeduplicationKey==command.DeduplicationKey));Assert.Equal(1,await verify.NotificationOutbox.CountAsync(x=>x.AlertId==results[0].Id));
    }

    [Fact]
    public async Task StoreScopedUserCannotReadRecoverOrAcknowledgeAnotherStoreAlert()
    {
        await using var db=CreateDb();var user=User([storeA.Id]);var service=Service(db,user);var alert=await service.CreateAsync(new("store.alert",storeA.Id,AlertSeverity.Info,"Store A","Scoped.","Store",storeA.Id.ToString(CultureInfo.InvariantCulture),"store-a:1"),Audit());user.StoreIdsValue=new HashSet<long>([storeB.Id]);Assert.Empty((await service.ListAsync(null,null)).Items);Assert.Empty((await service.RecoverAsync(0)).Events);await Assert.ThrowsAsync<AlertResourceNotFoundException>(()=>service.AcknowledgeAsync(alert.Id,Audit()));
    }

    [Fact]
    public async Task TenantWideAlertIsVisibleButStoreAlertRemainsStoreIsolated()
    {
        await using var db=CreateDb();var user=User([storeA.Id]);var service=Service(db,user);await service.CreateAsync(new("tenant.notice",null,AlertSeverity.Info,"Tenant notice","All stores.","Tenant",tenant.Id.ToString(CultureInfo.InvariantCulture),"tenant:notice:1"),Audit());await service.CreateAsync(new("store.notice",storeA.Id,AlertSeverity.Warning,"Store notice","Store only.","Store",storeA.Id.ToString(CultureInfo.InvariantCulture),"store:notice:1"),Audit());user.StoreIdsValue=new HashSet<long>([storeB.Id]);var list=await service.ListAsync(null,null);Assert.Single(list.Items);Assert.Null(list.Items[0].StoreId);var recovery=await service.RecoverAsync(0);Assert.Single(recovery.Events);Assert.Null(recovery.Events[0].StoreId);
    }

    [Fact]
    public async Task FailedDeliveryRetriesAndEventuallyMarksAlertDelivered()
    {
        await using var db=CreateDb();var clock=new MutableTimeProvider(Now);var service=Service(db,User([storeA.Id]),clock);var alert=await service.CreateAsync(new("retry.alert",storeA.Id,AlertSeverity.Warning,"Retry","Retry delivery.","Store",storeA.Id.ToString(CultureInfo.InvariantCulture),"retry:1"),Audit());var adapter=new FlakyAdapter(1);var processor=new NotificationOutboxProcessor(db,[adapter],clock);var first=await processor.ProcessDueAsync();Assert.Equal(1,first.Failed);var failed=await db.NotificationOutbox.SingleAsync();Assert.Equal(NotificationOutboxStatus.Failed,failed.Status);clock.Advance(TimeSpan.FromSeconds(5));var second=await processor.ProcessDueAsync();Assert.Equal(1,second.Delivered);Assert.Equal(2,failed.AttemptCount);Assert.Equal(AlertStatus.Delivered,(await db.Alerts.SingleAsync(x=>x.Id==alert.Id)).Status);Assert.Equal(1,adapter.Delivered);
    }

    private CustSearchDbContext CreateDb()=>new(new DbContextOptionsBuilder<CustSearchDbContext>().UseSqlite(new SqliteConnectionStringBuilder{DataSource=databasePath,Cache=SqliteCacheMode.Shared,Mode=SqliteOpenMode.ReadWriteCreate}.ToString()).Options);
    private AlertsRealtimeService Service(CustSearchDbContext db,MutableCurrentUser user,TimeProvider?clock=null)=>new(db,user,clock??new MutableTimeProvider(Now),coordinator,new FakeMetrics());
    private MutableCurrentUser User(IEnumerable<long>stores)=>new(){UserIdValue=actor.Id,TenantIdValue=tenant.Id,StoreIdsValue=stores.ToHashSet()};
    private TenantAuditContext Audit()=>new(actor.Id,"127.0.0.1","Phase11Test","phase11-correlation");
    private sealed class MutableCurrentUser:ICurrentUserContext{public long UserIdValue{get;set;}public long?TenantIdValue{get;set;}public IReadOnlySet<long>StoreIdsValue{get;set;}=new HashSet<long>();public bool IsAuthenticated=>true;public long UserId=>UserIdValue;public long?TenantId=>TenantIdValue;public bool IsPlatformAdmin=>false;public string SecurityStamp=>"p11";public IReadOnlySet<string>Roles=>new HashSet<string>(["StoreManager"]);public IReadOnlySet<string>Permissions=>new HashSet<string>();public IReadOnlySet<long>StoreIds=>StoreIdsValue;}
    private sealed class MutableTimeProvider(DateTime utc):TimeProvider{private DateTimeOffset now=new(utc);public override DateTimeOffset GetUtcNow()=>now;public void Advance(TimeSpan value)=>now=now.Add(value);}
    private sealed class FakeMetrics:IAlertConnectionMetrics{public long ActiveConnections(long tenantId)=>0;public long Reconnects(long tenantId)=>0;public void Connected(string connectionId,long tenantId){}public void Disconnected(string connectionId){}public void Reconnected(string connectionId,long tenantId){}}
    private sealed class FlakyAdapter(int failures):INotificationChannelAdapter{private int remaining=failures;public string Channel=>"SignalR";public int Delivered{get;private set;}public Task DeliverAsync(NotificationDeliveryMessage message,CancellationToken cancellationToken=default){if(remaining-->0)throw new InvalidOperationException("simulated");Delivered++;return Task.CompletedTask;}}
}
