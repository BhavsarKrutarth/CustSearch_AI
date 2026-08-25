using CustSearch.Domain.Enums;

namespace CustSearch.Application.Operations;

public sealed class OperationalException(string message,OperationalFailureKind kind):Exception(message){public OperationalFailureKind Kind{get;}=kind;}
public enum OperationalFailureKind { Validation,Forbidden,NotFound,Conflict,Unavailable }
public sealed record OperationalSettingView(long Id,OperationalScope Scope,long?TenantId,long?StoreId,string Key,string ValueJson,DateTime UpdatedUtc);
public sealed record OperationalSecretReferenceView(long Id,OperationalScope Scope,long?TenantId,long?StoreId,string Key,string MaskedReference,DateTime UpdatedUtc);
public sealed record SaveOperationalSettingCommand(OperationalScope Scope,long?TenantId,long?StoreId,string Key,string ValueJson);
public sealed record SaveSecretReferenceCommand(OperationalScope Scope,long?TenantId,long?StoreId,string Key,string Reference);
public sealed record WorkerControlView(string WorkerType,bool IsPaused,string?Reason,long?UpdatedByUserId,DateTime UpdatedUtc);
public sealed record QueueHealthView(int NotificationBacklog,int NotificationDeadLetters,int IntegrationBacklog,int IntegrationDeadLetters,int ExportBacklog,int ExportFailures,DateTime?OldestPendingUtc);
public sealed record DependencyHealthView(string Name,string Status,string Detail);
public sealed record OperationalHealthView(IReadOnlyList<DependencyHealthView>Dependencies,IReadOnlyList<WorkerControlView>Workers,QueueHealthView Queues);
public sealed record RetentionPolicyView(long Id,RetentionDomain Domain,long?TenantId,long?StoreId,int RetentionDays,bool Enabled,DateTime UpdatedUtc);
public sealed record SaveRetentionPolicyCommand(RetentionDomain Domain,long?TenantId,long?StoreId,int RetentionDays,bool Enabled);
public sealed record WorkerLeaseHandle(string WorkerType,Guid LeaseId,string OwnerId,DateTime ExpiresUtc);
public sealed record RetentionBatchResult(int Policies,int Deleted,int Failed);

public interface IOperationalPlatformService
{
    Task<OperationalHealthView>HealthAsync(CancellationToken ct=default);
    Task<IReadOnlyList<OperationalSettingView>>SettingsAsync(CancellationToken ct=default);
    Task<OperationalSettingView>SaveSettingAsync(SaveOperationalSettingCommand command,string correlationId,CancellationToken ct=default);
    Task<IReadOnlyList<OperationalSecretReferenceView>>SecretReferencesAsync(CancellationToken ct=default);
    Task<OperationalSecretReferenceView>SaveSecretReferenceAsync(SaveSecretReferenceCommand command,string correlationId,CancellationToken ct=default);
    Task<IReadOnlyList<WorkerControlView>>WorkerControlsAsync(CancellationToken ct=default);
    Task<WorkerControlView>SetWorkerPausedAsync(string workerType,bool paused,string?reason,string correlationId,CancellationToken ct=default);
    Task RetryDeadLetterAsync(string queue,long id,string correlationId,CancellationToken ct=default);
    Task<IReadOnlyList<RetentionPolicyView>>RetentionPoliciesAsync(CancellationToken ct=default);
    Task<RetentionPolicyView>SaveRetentionPolicyAsync(SaveRetentionPolicyCommand command,string correlationId,CancellationToken ct=default);
}

public interface IWorkerRuntimeGate
{
    Task<WorkerLeaseHandle?>TryAcquireAsync(string workerType,string ownerId,TimeSpan duration,CancellationToken ct=default);
    Task ReleaseAsync(WorkerLeaseHandle lease,CancellationToken ct=default);
    Task HeartbeatAsync(string instanceId,string workerType,bool ready,string?errorMessage,CancellationToken ct=default);
}

public interface IRetentionProcessor{Task<RetentionBatchResult>RunDueAsync(int batchSize,CancellationToken ct=default);}
