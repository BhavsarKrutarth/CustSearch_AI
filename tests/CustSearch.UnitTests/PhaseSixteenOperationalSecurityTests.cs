using CustSearch.Application.Authentication;
using CustSearch.Application.Operations;
using CustSearch.Infrastructure.Operations;
using CustSearch.Application.TenantOperations;

namespace CustSearch.UnitTests;

public sealed class PhaseSixteenOperationalSecurityTests
{
    [Fact]
    public async Task TenantCannotOverridePlatformLockedSafetySetting()
    {
        var service=new OperationalPlatformService(new FakeRepository(),TenantUser());
        var failure=await Assert.ThrowsAsync<OperationalException>(()=>service.SaveTenantSettingAsync(null,new("AutoLinkHouseholdFromFaceSimilarity",SystemSettingValueType.Toggle,"true",null),new(200,null,null,"test")));
        Assert.Equal(OperationalFailureKind.Forbidden,failure.Kind);
    }

    [Fact]
    public async Task RestrictedTenantCannotSelectUnassignedStore()
    {
        var user=TenantUser();user.RolesValue=new HashSet<string>{"STAFF"};
        var service=new OperationalPlatformService(new FakeRepository(),user);
        var failure=await Assert.ThrowsAsync<OperationalException>(()=>service.ListTenantSettingsAsync(999,true));
        Assert.Equal(OperationalFailureKind.NotFound,failure.Kind);
    }

    [Fact]
    public async Task PlatformSaveNeverSuppliesTenantScope()
    {
        var repository=new FakeRepository();var service=new OperationalPlatformService(repository,PlatformUser());
        await service.SavePlatformSettingAsync(new("WebhookRetryCount",SystemSettingValueType.WholeNumber,"7",null),new(100,null,null,"test"));
        Assert.Null(repository.SavedTenantId);Assert.Null(repository.SavedStoreId);Assert.Equal(100,repository.SavedActorId);
    }

    private static FakeUser TenantUser()=>new(){UserIdValue=200,TenantIdValue=10,RolesValue=new HashSet<string>{"TENANTADMIN"},StoreIdsValue=new HashSet<long>{20}};
    private static FakeUser PlatformUser()=>new(){UserIdValue=100,IsPlatformAdminValue=true,RolesValue=new HashSet<string>{"PLATFORMSUPERADMIN"}};

    private sealed class FakeRepository:IOperationalPlatformRepository
    {
        public long?SavedTenantId{get;private set;}public long?SavedStoreId{get;private set;}public long SavedActorId{get;private set;}
        public Task<IReadOnlyList<SystemSettingView>>ListSettingsAsync(long?tenantId,long?storeId,bool effective,CancellationToken ct=default)=>Task.FromResult<IReadOnlyList<SystemSettingView>>([]);
        public Task<SystemSettingView>SaveSettingAsync(long?tenantId,long?storeId,SaveSystemSettingCommand command,TenantAuditContext audit,CancellationToken ct=default){SavedTenantId=tenantId;SavedStoreId=storeId;SavedActorId=audit.ActorUserId;return Task.FromResult(new SystemSettingView(1,tenantId,storeId,command.SettingKey,command.ValueType,command.SettingValue,command.Description,audit.ActorUserId,DateTime.UtcNow,DateTime.UtcNow,tenantId is null?"Platform":storeId is null?"Tenant":"Store"));}
        public Task<AuditLogPage>SearchAuditAsync(long?tenantId,IReadOnlyCollection<long>allowedStoreIds,bool tenantWide,AuditLogQuery query,CancellationToken ct=default)=>Task.FromResult(new AuditLogPage([],0,query.PageNumber,query.PageSize));
        public Task<SystemHealthView>GetHealthAsync(int workerWarningSeconds,CancellationToken ct=default)=>throw new NotImplementedException();
        public Task WriteHeartbeatAsync(WorkerHeartbeat heartbeat,CancellationToken ct=default)=>Task.CompletedTask;
        public Task<RetentionRunResult>RunRetentionAsync(int batchSize,int recognitionMetadataRetentionDays,CancellationToken ct=default)=>Task.FromResult(new RetentionRunResult(0,0,0));
    }
    private sealed class FakeUser:ICurrentUserContext
    {
        public bool IsAuthenticated=>true;public long UserId=>UserIdValue;public long UserIdValue{get;set;}public long?TenantId=>TenantIdValue;public long?TenantIdValue{get;set;}public bool IsPlatformAdmin=>IsPlatformAdminValue;public bool IsPlatformAdminValue{get;set;}public string SecurityStamp=>"test";public IReadOnlySet<string>Roles=>RolesValue;public IReadOnlySet<string>RolesValue{get;set;}=new HashSet<string>();public IReadOnlySet<string>Permissions=>new HashSet<string>();public IReadOnlySet<long>StoreIds=>StoreIdsValue;public IReadOnlySet<long>StoreIdsValue{get;set;}=new HashSet<long>();
    }
}
