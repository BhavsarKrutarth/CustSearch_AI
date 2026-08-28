using CustSearch.Application.Authentication;
using CustSearch.Application.CamerasTracking;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.CamerasTracking;
using CustSearch.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CustSearch.IntegrationTests;

public sealed class CameraPreviewAuthorizationTests:IAsyncLifetime
{
    private static readonly DateTime Now=new(2026,8,26,7,0,0,DateTimeKind.Utc);
    private readonly string databasePath=Path.Combine(Path.GetTempPath(),$"custsearch-preview-{Guid.NewGuid():N}.db");
    private readonly FakeCurrentUser current=new();private Tenant tenant=null!;private Tenant otherTenant=null!;private Store office=null!;private Store remote=null!;private Camera camera=null!;private UserAccount admin=null!;private UserAccount cameraOperator=null!;private UserAccount remoteUser=null!;private UserAccount otherTenantUser=null!;

    public async Task InitializeAsync()
    {
        await using var db=CreateDb();await db.Database.EnsureCreatedAsync();tenant=Tenant.Create("PREVIEW","Preview Retail","Preview","Owner","owner@preview.test","9000000201","IN","India Standard Time","INR",10,30,10,Now);otherTenant=Tenant.Create("OTHER","Other Retail","Other","Owner","owner@other.test","9000000202","IN","India Standard Time","INR",10,30,10,Now);db.Tenants.AddRange(tenant,otherTenant);await db.SaveChangesAsync();office=Store.Create(tenant.Id,"OFFICE","Office","1 Main",null,null,"Ahmedabad",null,"Gujarat","380001","IN",null,null,null,null,StoreLocationSource.Manual,"India Standard Time",null,null,Now);remote=Store.Create(tenant.Id,"REMOTE","Remote","2 Main",null,null,"Ahmedabad",null,"Gujarat","380002","IN",null,null,null,null,StoreLocationSource.Manual,"India Standard Time",null,null,Now);db.Stores.AddRange(office,remote);await db.SaveChangesAsync();admin=UserAccount.CreateTenant(tenant.Id,"preview.admin","admin@preview.test","Preview Admin","hash",Now);cameraOperator=UserAccount.CreateTenant(tenant.Id,"preview.operator","operator@preview.test","Preview Operator","hash",Now);remoteUser=UserAccount.CreateTenant(tenant.Id,"remote.operator","remote@preview.test","Remote Operator","hash",Now);otherTenantUser=UserAccount.CreateTenant(otherTenant.Id,"other.operator","operator@other.test","Other Operator","hash",Now);db.UserAccounts.AddRange(admin,cameraOperator,remoteUser,otherTenantUser);await db.SaveChangesAsync();db.UserStoreAssignments.Add(UserStoreAssignment.Assign(tenant.Id,cameraOperator.Id,office.Id,true,Now,admin.Id));db.UserStoreAssignments.Add(UserStoreAssignment.Assign(tenant.Id,remoteUser.Id,remote.Id,true,Now,admin.Id));camera=Camera.Create(tenant.Id,office.Id,"OFFICE-ENTRY","Office entry","env:CUSTSEARCH_CAMERA_OFFICE_ENTRY01_RTSP",null,CameraDirection.Entry,true,Now);db.Cameras.Add(camera);await db.SaveChangesAsync();current.TenantIdValue=tenant.Id;current.UserIdValue=admin.Id;current.RolesValue=new HashSet<string>(["TenantAdmin"]);
    }
    public Task DisposeAsync(){if(File.Exists(databasePath))File.Delete(databasePath);return Task.CompletedTask;}

    [Fact]
    public async Task ExplicitlyAssignedOfficeUserCanStartAndReadPreview()
    {
        await using var db=CreateDb();var service=Service(db);await service.SaveGrantAsync(camera.Id,cameraOperator.Id,new(true,true,false,null,true),Audit(admin.Id),default);current.UserIdValue=cameraOperator.Id;current.RolesValue=new HashSet<string>(["CameraOperator"]);current.StoreIdsValue=new HashSet<long>([office.Id]);var session=await service.StartSessionAsync(camera.Id,Audit(cameraOperator.Id));var frame=await service.GetFrameAsync(camera.Id,session.SessionId);Assert.Equal("image/jpeg",frame.ContentType);Assert.NotEmpty(frame.Content);Assert.Equal(camera.Id,session.CameraId);
    }

    [Fact]
    public async Task SameTenantUserOutsideCameraStoreCannotReceiveGrant()
    {
        await using var db=CreateDb();var failure=await Assert.ThrowsAsync<CameraTrackingException>(()=>Service(db).SaveGrantAsync(camera.Id,remoteUser.Id,new(true,false,false,null,true),Audit(admin.Id)));Assert.Equal(CameraTrackingFailureKind.Forbidden,failure.Kind);
    }

    [Fact]
    public async Task OtherTenantUserAndCameraAreNotVisible()
    {
        await using var db=CreateDb();var service=Service(db);var assignment=await Assert.ThrowsAsync<CameraTrackingException>(()=>service.SaveGrantAsync(camera.Id,otherTenantUser.Id,new(true,false,false,null,true),Audit(admin.Id)));Assert.Equal(CameraTrackingFailureKind.NotFound,assignment.Kind);current.TenantIdValue=otherTenant.Id;current.UserIdValue=otherTenantUser.Id;current.RolesValue=new HashSet<string>(["CameraOperator"]);var preview=await Assert.ThrowsAsync<CameraTrackingException>(()=>service.StartSessionAsync(camera.Id,Audit(otherTenantUser.Id)));Assert.Equal(CameraTrackingFailureKind.NotFound,preview.Kind);
    }

    [Fact]
    public async Task MonitoringRejectsMoreThanFiveConcurrentCameraSessions()
    {
        await using var db=CreateDb();var additional=Enumerable.Range(2,5).Select(number=>Camera.Create(tenant.Id,office.Id,$"OFFICE-{number}",$"Office {number}",$"env:CUSTSEARCH_CAMERA_OFFICE_{number}_RTSP",null,CameraDirection.Internal,true,Now)).ToArray();db.Cameras.AddRange(additional);await db.SaveChangesAsync();var service=Service(db);foreach(var item in new[]{camera}.Concat(additional))await service.SaveGrantAsync(item.Id,cameraOperator.Id,new(true,false,false,null,true),Audit(admin.Id));current.UserIdValue=cameraOperator.Id;current.RolesValue=new HashSet<string>(["CameraOperator"]);current.StoreIdsValue=new HashSet<long>([office.Id]);foreach(var item in new[]{camera}.Concat(additional.Take(4)))await service.StartSessionAsync(item.Id,Audit(cameraOperator.Id));Assert.Equal(5,await db.CameraPreviewSessions.CountAsync(x=>x.UserId==cameraOperator.Id&&x.Status==CameraPreviewSessionStatus.Active));var limit=await Assert.ThrowsAsync<CameraTrackingException>(()=>service.StartSessionAsync(additional[4].Id,Audit(cameraOperator.Id)));Assert.Equal(CameraTrackingFailureKind.Conflict,limit.Kind);Assert.Contains("maximum 5 concurrent cameras",limit.Message);
    }

    private CameraPreviewService Service(CustSearchDbContext db)=>new(db,current,new FakeFrameSource(),new FixedClock(),Options.Create(new CctvPreviewOptions{Enabled=true,ApiKey="test-key",AiServiceBaseUrl="http://127.0.0.1:8000",SessionLifetimeMinutes=10,FrameRefreshMilliseconds=750,RequestTimeoutSeconds=5}));
    private static TenantAuditContext Audit(long userId)=>new(userId,"127.0.0.1","tests","preview-test");
    private CustSearchDbContext CreateDb()=>new(new DbContextOptionsBuilder<CustSearchDbContext>().UseSqlite(new SqliteConnectionStringBuilder{DataSource=databasePath,Cache=SqliteCacheMode.Shared,Mode=SqliteOpenMode.ReadWriteCreate,Pooling=false}.ToString()).Options);
    private sealed class FakeFrameSource:ICameraFrameSource{public Task<CameraPreviewFrame>GetLatestFrameAsync(string configurationReference,CancellationToken ct=default){Assert.StartsWith("env:CUSTSEARCH_CAMERA_",configurationReference);return Task.FromResult(new CameraPreviewFrame([1,2,3],"image/jpeg",Now,640,360));}}
    private sealed class FixedClock:TimeProvider{public override DateTimeOffset GetUtcNow()=>new(Now);}
    private sealed class FakeCurrentUser:ICurrentUserContext{public long UserIdValue{get;set;}public long TenantIdValue{get;set;}public IReadOnlySet<string>RolesValue{get;set;}=new HashSet<string>();public IReadOnlySet<long>StoreIdsValue{get;set;}=new HashSet<long>();public bool IsAuthenticated=>true;public long UserId=>UserIdValue;public long?TenantId=>TenantIdValue;public bool IsPlatformAdmin=>false;public string SecurityStamp=>"preview";public IReadOnlySet<string>Roles=>RolesValue;public IReadOnlySet<string>Permissions=>new HashSet<string>(["Cameras.Preview"]);public IReadOnlySet<long>StoreIds=>StoreIdsValue;}
}
