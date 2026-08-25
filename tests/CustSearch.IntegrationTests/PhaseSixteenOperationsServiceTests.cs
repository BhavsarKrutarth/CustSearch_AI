using CustSearch.Application.Authentication;
using CustSearch.Application.Operations;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Operations;
using CustSearch.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CustSearch.IntegrationTests;

public sealed class PhaseSixteenOperationsServiceTests:IAsyncLifetime
{
    private static readonly DateTime Now=new(2026,8,25,12,0,0,DateTimeKind.Utc);private readonly string path=Path.Combine(Path.GetTempPath(),$"custsearch-p16-{Guid.NewGuid():N}.db");private readonly CurrentUser user=new();private long actorId;
    public async Task InitializeAsync(){await using var db=CreateDb();await db.Database.EnsureCreatedAsync();var actor=UserAccount.CreatePlatform("ops","ops@example.test","Operations","hash",Now);db.UserAccounts.Add(actor);await db.SaveChangesAsync();actorId=actor.Id;user.UserIdValue=actor.Id;}
    public Task DisposeAsync(){if(File.Exists(path))File.Delete(path);return Task.CompletedTask;}
    [Fact]public async Task SettingsUseStrictScopeAndSecretsAreMasked(){await using var db=CreateDb();var service=Service(db);var setting=await service.SaveSettingAsync(new(OperationalScope.Platform,null,null,"Reports.PageSize","100"),"p16");Assert.Equal("100",setting.ValueJson);var secret=await service.SaveSecretReferenceAsync(new(OperationalScope.Platform,null,null,"RedisPassword","vault://redis/password-abcd"),"p16");Assert.Equal("****abcd",secret.MaskedReference);Assert.DoesNotContain("vault",secret.MaskedReference,StringComparison.OrdinalIgnoreCase);}
    [Fact]public async Task UnknownTenantScopeIsRejected(){await using var db=CreateDb();var exception=await Assert.ThrowsAsync<OperationalException>(()=>Service(db).SaveSettingAsync(new(OperationalScope.Tenant,999,null,"Alerts.Retention","30"),"p16"));Assert.Equal(OperationalFailureKind.NotFound,exception.Kind);}
    [Fact]public async Task TenantUserCannotReachPlatformControls(){await using var db=CreateDb();user.Platform=false;var exception=await Assert.ThrowsAsync<OperationalException>(()=>Service(db).HealthAsync());Assert.Equal(OperationalFailureKind.Forbidden,exception.Kind);}
    [Fact]public async Task AuditRowsCannotBeChangedAndSensitiveMetadataIsRejected(){await using var db=CreateDb();var audit=AuditLog.Record(null,null,actorId,"PlatformUser","Viewed","Health",null,null,"{}",null,null,"p16",Now);db.AuditLogs.Add(audit);await db.SaveChangesAsync();db.Entry(audit).Property(x=>x.Action).CurrentValue="Changed";await Assert.ThrowsAsync<InvalidOperationException>(()=>db.SaveChangesAsync());db.ChangeTracker.Clear();db.AuditLogs.Add(AuditLog.Record(null,null,actorId,"PlatformUser","Changed","Config",null,null,"{\"accessToken\":\"unsafe\"}",null,null,"p16",Now));await Assert.ThrowsAsync<InvalidOperationException>(()=>db.SaveChangesAsync());}
    private OperationalPlatformService Service(CustSearchDbContext db)=>new(db,user,new FixedClock(),Options.Create(new OperationalPlatformOptions()));private CustSearchDbContext CreateDb()=>new(new DbContextOptionsBuilder<CustSearchDbContext>().UseSqlite(new SqliteConnectionStringBuilder{DataSource=path,Mode=SqliteOpenMode.ReadWriteCreate,Cache=SqliteCacheMode.Shared,Pooling=false}.ToString()).Options);
    private sealed class FixedClock:TimeProvider{public override DateTimeOffset GetUtcNow()=>new(Now);}
    private sealed class CurrentUser:ICurrentUserContext{public long UserIdValue{get;set;}public bool Platform{get;set;}=true;public bool IsAuthenticated=>true;public long UserId=>UserIdValue;public long?TenantId=>null;public bool IsPlatformAdmin=>Platform;public string SecurityStamp=>"p16";public IReadOnlySet<string>Roles=>new HashSet<string>{"PlatformOperationsAdmin"};public IReadOnlySet<string>Permissions=>new HashSet<string>{"PlatformOperations.View","PlatformOperations.Manage"};public IReadOnlySet<long>StoreIds=>new HashSet<long>();}
}

