using System.Text.Json;
using CustSearch.Domain.Enums;

namespace CustSearch.Domain.Entities;

public sealed class OperationalSetting
{
    private OperationalSetting(){}
    private OperationalSetting(OperationalScope scope,long?tenantId,long?storeId,string key,string valueJson,DateTime utcNow){ValidateScope(scope,tenantId,storeId);Scope=scope;TenantId=tenantId;StoreId=storeId;Key=SafeKey(key);ValueJson=Json(valueJson);CreatedUtc=Utc(utcNow);UpdatedUtc=CreatedUtc;}
    public long Id{get;private set;}public OperationalScope Scope{get;private set;}public long?TenantId{get;private set;}public long?StoreId{get;private set;}public string Key{get;private set;}=string.Empty;public string ValueJson{get;private set;}="{}";public DateTime CreatedUtc{get;private set;}public DateTime UpdatedUtc{get;private set;}public byte[]?RowVersion{get;private set;}
    public static OperationalSetting Create(OperationalScope scope,long?tenantId,long?storeId,string key,string valueJson,DateTime utcNow)=>new(scope,tenantId,storeId,key,valueJson,utcNow);
    public void Update(string valueJson,DateTime utcNow){ValueJson=Json(valueJson);UpdatedUtc=Utc(utcNow);}
    public static void ValidateScope(OperationalScope scope,long?tenantId,long?storeId){if(!Enum.IsDefined(scope))throw new ArgumentOutOfRangeException(nameof(scope));if(scope==OperationalScope.Platform&&(tenantId.HasValue||storeId.HasValue)||scope==OperationalScope.Tenant&&(!tenantId.HasValue||storeId.HasValue)||scope==OperationalScope.Store&&(!tenantId.HasValue||!storeId.HasValue))throw new ArgumentException("Operational setting scope is invalid.");if(tenantId is<=0||storeId is<=0)throw new ArgumentOutOfRangeException(nameof(tenantId));}
    private static string SafeKey(string value){var key=Required(value,120);var lower=key.ToLowerInvariant();if(new[]{"password","secret","token","credential","connectionstring","signingkey"}.Any(lower.Contains))throw new ArgumentException("Secrets must use the separate secret-reference store.",nameof(value));return key;}
    private static string Json(string value){var json=Required(value,4000);using var _=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=16});return json;}
    private static string Required(string value,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value);var result=value.Trim();return result.Length<=max?result:throw new ArgumentOutOfRangeException(nameof(value));}
    private static DateTime Utc(DateTime value)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",nameof(value));
}

public sealed class OperationalSecretReference
{
    private OperationalSecretReference(){}
    private OperationalSecretReference(OperationalScope scope,long?tenantId,long?storeId,string key,string reference,DateTime utcNow){OperationalSetting.ValidateScope(scope,tenantId,storeId);Scope=scope;TenantId=tenantId;StoreId=storeId;Key=Required(key,120);Reference=Required(reference,250);CreatedUtc=Utc(utcNow);UpdatedUtc=CreatedUtc;}
    public long Id{get;private set;}public OperationalScope Scope{get;private set;}public long?TenantId{get;private set;}public long?StoreId{get;private set;}public string Key{get;private set;}=string.Empty;public string Reference{get;private set;}=string.Empty;public DateTime CreatedUtc{get;private set;}public DateTime UpdatedUtc{get;private set;}
    public static OperationalSecretReference Create(OperationalScope scope,long?tenantId,long?storeId,string key,string reference,DateTime utcNow)=>new(scope,tenantId,storeId,key,reference,utcNow);
    public void Rotate(string reference,DateTime utcNow){Reference=Required(reference,250);UpdatedUtc=Utc(utcNow);}
    private static string Required(string value,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value);var result=value.Trim();return result.Length<=max?result:throw new ArgumentOutOfRangeException(nameof(value));}
    private static DateTime Utc(DateTime value)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",nameof(value));
}

public sealed class WorkerControl
{
    private WorkerControl(){}
    private WorkerControl(string workerType,DateTime utcNow){WorkerType=Required(workerType,80);UpdatedUtc=Utc(utcNow);}
    public string WorkerType{get;private set;}=string.Empty;public bool IsPaused{get;private set;}public string?Reason{get;private set;}public long?UpdatedByUserId{get;private set;}public DateTime UpdatedUtc{get;private set;}public byte[]?RowVersion{get;private set;}
    public static WorkerControl Create(string workerType,DateTime utcNow)=>new(workerType,utcNow);
    public void SetPaused(bool paused,string?reason,long userId,DateTime utcNow){ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);if(paused&&string.IsNullOrWhiteSpace(reason))throw new ArgumentException("Pause reason is required.",nameof(reason));IsPaused=paused;Reason=paused?Required(reason!,500):null;UpdatedByUserId=userId;UpdatedUtc=Utc(utcNow);}
    private static string Required(string value,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value);var result=value.Trim();return result.Length<=max?result:throw new ArgumentOutOfRangeException(nameof(value));}
    private static DateTime Utc(DateTime value)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",nameof(value));
}

public sealed class WorkerLease
{
    private WorkerLease(){}
    private WorkerLease(string workerType,Guid leaseId,string ownerId,DateTime acquiredUtc,DateTime expiresUtc){WorkerType=Required(workerType,80);ArgumentOutOfRangeException.ThrowIfEqual(leaseId,Guid.Empty);LeaseId=leaseId;OwnerId=Required(ownerId,150);AcquiredUtc=Utc(acquiredUtc);RenewedUtc=AcquiredUtc;ExpiresUtc=Utc(expiresUtc);if(ExpiresUtc<=AcquiredUtc)throw new ArgumentOutOfRangeException(nameof(expiresUtc));}
    public string WorkerType{get;private set;}=string.Empty;public Guid LeaseId{get;private set;}public string OwnerId{get;private set;}=string.Empty;public DateTime AcquiredUtc{get;private set;}public DateTime RenewedUtc{get;private set;}public DateTime ExpiresUtc{get;private set;}public byte[]?RowVersion{get;private set;}
    public static WorkerLease Acquire(string workerType,Guid leaseId,string ownerId,DateTime acquiredUtc,DateTime expiresUtc)=>new(workerType,leaseId,ownerId,acquiredUtc,expiresUtc);
    public void Reassign(Guid leaseId,string ownerId,DateTime utcNow,DateTime expiresUtc){if(ExpiresUtc>utcNow)throw new InvalidOperationException("An active worker lease cannot be reassigned.");ArgumentOutOfRangeException.ThrowIfEqual(leaseId,Guid.Empty);LeaseId=leaseId;OwnerId=Required(ownerId,150);AcquiredUtc=Utc(utcNow);RenewedUtc=AcquiredUtc;ExpiresUtc=Utc(expiresUtc);}
    public void Release(Guid leaseId,DateTime utcNow){if(LeaseId!=leaseId)throw new InvalidOperationException("Worker lease ownership mismatch.");ExpiresUtc=Utc(utcNow);RenewedUtc=ExpiresUtc;}
    private static string Required(string value,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value);var result=value.Trim();return result.Length<=max?result:throw new ArgumentOutOfRangeException(nameof(value));}
    private static DateTime Utc(DateTime value)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",nameof(value));
}

public sealed class RetentionPolicy
{
    private RetentionPolicy(){}
    private RetentionPolicy(RetentionDomain domain,long?tenantId,long?storeId,int retentionDays,bool enabled,DateTime utcNow){if(!Enum.IsDefined(domain))throw new ArgumentOutOfRangeException(nameof(domain));if(tenantId is<=0||storeId is<=0||storeId.HasValue&&!tenantId.HasValue)throw new ArgumentException("Retention scope is invalid.");ValidateDays(retentionDays);Domain=domain;TenantId=tenantId;StoreId=storeId;RetentionDays=retentionDays;Enabled=enabled;CreatedUtc=Utc(utcNow);UpdatedUtc=CreatedUtc;}
    public long Id{get;private set;}public RetentionDomain Domain{get;private set;}public long?TenantId{get;private set;}public long?StoreId{get;private set;}public int RetentionDays{get;private set;}public bool Enabled{get;private set;}public DateTime CreatedUtc{get;private set;}public DateTime UpdatedUtc{get;private set;}public byte[]?RowVersion{get;private set;}
    public static RetentionPolicy Create(RetentionDomain domain,long?tenantId,long?storeId,int retentionDays,bool enabled,DateTime utcNow)=>new(domain,tenantId,storeId,retentionDays,enabled,utcNow);
    public void Update(int retentionDays,bool enabled,DateTime utcNow){ValidateDays(retentionDays);RetentionDays=retentionDays;Enabled=enabled;UpdatedUtc=Utc(utcNow);}
    private static void ValidateDays(int value){ArgumentOutOfRangeException.ThrowIfLessThan(value,1);ArgumentOutOfRangeException.ThrowIfGreaterThan(value,36500);}
    private static DateTime Utc(DateTime value)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",nameof(value));
}

public sealed class RetentionRun
{
    private RetentionRun(){}
    private RetentionRun(long policyId,Guid runId,DateTime utcNow){ArgumentOutOfRangeException.ThrowIfNegativeOrZero(policyId);ArgumentOutOfRangeException.ThrowIfEqual(runId,Guid.Empty);PolicyId=policyId;RunId=runId;StartedUtc=Utc(utcNow);}
    public long Id{get;private set;}public long PolicyId{get;private set;}public Guid RunId{get;private set;}public int DeletedCount{get;private set;}public string Status{get;private set;}="Processing";public string?Error{get;private set;}public DateTime StartedUtc{get;private set;}public DateTime?CompletedUtc{get;private set;}
    public static RetentionRun Start(long policyId,Guid runId,DateTime utcNow)=>new(policyId,runId,utcNow);
    public void Complete(int deletedCount,DateTime utcNow){ArgumentOutOfRangeException.ThrowIfNegative(deletedCount);DeletedCount=deletedCount;Status="Completed";CompletedUtc=Utc(utcNow);}
    public void Fail(string error,DateTime utcNow){ArgumentException.ThrowIfNullOrWhiteSpace(error);Error=error.Trim()[..Math.Min(error.Trim().Length,2000)];Status="Failed";CompletedUtc=Utc(utcNow);}
    private static DateTime Utc(DateTime value)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",nameof(value));
}

public sealed class WorkerHeartbeat
{
    private WorkerHeartbeat(){}
    private WorkerHeartbeat(string instanceId,string workerType,DateTime utcNow){InstanceId=instanceId;WorkerType=workerType;LastHeartbeatUtc=utcNow;StartedUtc=utcNow;IsReady=true;}
    public string InstanceId{get;private set;}=string.Empty;public string WorkerType{get;private set;}=string.Empty;public DateTime StartedUtc{get;private set;}public DateTime LastHeartbeatUtc{get;private set;}public bool IsReady{get;private set;}public string?LastError{get;private set;}
    public static WorkerHeartbeat Start(string instanceId,string workerType,DateTime utcNow)=>new(instanceId,workerType,utcNow);
    public void Beat(bool ready,string?error,DateTime utcNow){IsReady=ready;LastError=string.IsNullOrWhiteSpace(error)?null:error.Trim()[..Math.Min(error.Trim().Length,1000)];LastHeartbeatUtc=utcNow;}
}
