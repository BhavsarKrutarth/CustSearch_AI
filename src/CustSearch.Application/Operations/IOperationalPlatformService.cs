using CustSearch.Application.TenantOperations;

namespace CustSearch.Application.Operations;

public interface IOperationalPlatformService
{
    Task<IReadOnlyList<SystemSettingView>> ListPlatformSettingsAsync(CancellationToken ct=default);
    Task<IReadOnlyList<SystemSettingView>> ListTenantSettingsAsync(long?storeId,bool effective,CancellationToken ct=default);
    Task<SystemSettingView> SavePlatformSettingAsync(SaveSystemSettingCommand command,TenantAuditContext audit,CancellationToken ct=default);
    Task<SystemSettingView> SaveTenantSettingAsync(long?storeId,SaveSystemSettingCommand command,TenantAuditContext audit,CancellationToken ct=default);
    Task<AuditLogPage> SearchPlatformAuditAsync(AuditLogQuery query,CancellationToken ct=default);
    Task<AuditLogPage> SearchTenantAuditAsync(AuditLogQuery query,CancellationToken ct=default);
    Task<SystemHealthView> GetSystemHealthAsync(CancellationToken ct=default);
}

public interface IOperationalPlatformRepository
{
    Task<IReadOnlyList<SystemSettingView>> ListSettingsAsync(long?tenantId,long?storeId,bool effective,CancellationToken ct=default);
    Task<SystemSettingView> SaveSettingAsync(long?tenantId,long?storeId,SaveSystemSettingCommand command,TenantAuditContext audit,CancellationToken ct=default);
    Task<AuditLogPage> SearchAuditAsync(long?tenantId,IReadOnlyCollection<long>allowedStoreIds,bool tenantWide,AuditLogQuery query,CancellationToken ct=default);
    Task<SystemHealthView> GetHealthAsync(int workerWarningSeconds,CancellationToken ct=default);
    Task WriteHeartbeatAsync(WorkerHeartbeat heartbeat,CancellationToken ct=default);
    Task<RetentionRunResult> RunRetentionAsync(int batchSize,int recognitionMetadataRetentionDays,CancellationToken ct=default);
}

public interface IOperationalRetentionMaintenance
{
    Task<RetentionRunResult> RunAsync(CancellationToken ct=default);
}

public enum SystemSettingValueType:byte { Toggle=1,WholeNumber=2,Numeric=3,Text=4 }
public sealed record SystemSettingView(long Id,long?TenantId,long?StoreId,string SettingKey,SystemSettingValueType ValueType,string SettingValue,string?Description,long?UpdatedByUserId,DateTime CreatedUtc,DateTime UpdatedUtc,string SourceScope);
public sealed record SaveSystemSettingCommand(string SettingKey,SystemSettingValueType ValueType,string SettingValue,string?Description);
public sealed record AuditLogQuery(long?StoreId=null,string?Action=null,string?EntityType=null,DateTime?FromUtc=null,DateTime?ToUtc=null,int PageNumber=1,int PageSize=50);
public sealed record AuditLogItem(long Id,long?TenantId,long?StoreId,long?UserId,string ActorType,string Action,string EntityType,string?EntityId,string?IpAddress,string CorrelationId,DateTime CreatedUtc,long TotalCount);
public sealed record AuditLogPage(IReadOnlyList<AuditLogItem>Items,long TotalCount,int PageNumber,int PageSize);
public sealed record DatabaseHealth(string DatabaseName,string ServerName,string ProductVersion,DateTime CheckedUtc,string Status);
public sealed record WorkerHealth(string InstanceId,string WorkerName,byte Status,DateTime StartedUtc,DateTime LastHeartbeatUtc,DateTime?LastSuccessfulCycleUtc,DateTime?LastErrorUtc,string?LastError,string HealthStatus);
public sealed record QueueHealth(long ReportQueueDepth,long WebhookQueueDepth,long NotificationQueueDepth,long ReportEventBacklog);
public sealed record CameraHealth(long TotalCameras,long OnlineCameras,long NonOnlineCameras);
public sealed record SystemHealthView(DatabaseHealth Database,IReadOnlyList<WorkerHealth>Workers,QueueHealth Queues,CameraHealth Cameras);
public sealed record WorkerHeartbeat(string InstanceId,string WorkerName,byte Status,DateTime StartedUtc,DateTime?LastSuccessfulCycleUtc,string?LastError,string?MetadataJson);
public sealed record RetentionRunResult(int TemplatesDisabled,int TemplatesMarkedDeleted,int AnonymousVisitorsDeleted);

public enum OperationalFailureKind { Validation,Forbidden,NotFound }
public sealed class OperationalException(string message,OperationalFailureKind kind):Exception(message){public OperationalFailureKind Kind{get;}=kind;}
