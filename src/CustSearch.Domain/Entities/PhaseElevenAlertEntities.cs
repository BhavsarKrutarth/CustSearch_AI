using CustSearch.Domain.Enums;

namespace CustSearch.Domain.Entities;

/// <summary>Tenant-owned authoritative alert. StoreId null means visible tenant-wide.</summary>
public sealed class Alert
{
    private Alert() { }
    private Alert(long tenantId,long? storeId,string alertType,AlertSeverity severity,string title,string message,string entityType,string? entityId,string correlationId,string deduplicationKey,DateTime createdUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);if(storeId.HasValue)ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId.Value);if(!Enum.IsDefined(severity))throw new ArgumentOutOfRangeException(nameof(severity));
        TenantId=tenantId;StoreId=storeId;AlertType=Required(alertType,100);Severity=severity;Title=Required(title,200);Message=Required(message,2000);EntityType=Required(entityType,100);EntityId=Optional(entityId,100);CorrelationId=Required(correlationId,64);DeduplicationKey=Required(deduplicationKey,200);CreatedUtc=Utc(createdUtc,nameof(createdUtc));Status=AlertStatus.New;
    }
    public long Id{get;private set;} public string AlertType{get;private set;}=string.Empty; public long TenantId{get;private set;} public long?StoreId{get;private set;} public AlertSeverity Severity{get;private set;} public string Title{get;private set;}=string.Empty; public string Message{get;private set;}=string.Empty; public string EntityType{get;private set;}=string.Empty; public string?EntityId{get;private set;} public DateTime CreatedUtc{get;private set;} public DateTime?AcknowledgedUtc{get;private set;} public long?AcknowledgedByUserId{get;private set;} public DateTime?ResolvedUtc{get;private set;} public AlertStatus Status{get;private set;} public string CorrelationId{get;private set;}=string.Empty; public string DeduplicationKey{get;private set;}=string.Empty;
    public static Alert Create(long tenantId,long?storeId,string alertType,AlertSeverity severity,string title,string message,string entityType,string?entityId,string correlationId,string deduplicationKey,DateTime createdUtc)=>new(tenantId,storeId,alertType,severity,title,message,entityType,entityId,correlationId,deduplicationKey,createdUtc);
    public void Update(AlertSeverity severity,string title,string message,string entityType,string?entityId){if(Status is AlertStatus.Resolved or AlertStatus.Expired)throw new InvalidOperationException("A closed alert cannot be updated.");if(!Enum.IsDefined(severity))throw new ArgumentOutOfRangeException(nameof(severity));Severity=severity;Title=Required(title,200);Message=Required(message,2000);EntityType=Required(entityType,100);EntityId=Optional(entityId,100);}
    public void MarkDelivered(){if(Status==AlertStatus.New)Status=AlertStatus.Delivered;}
    public void Acknowledge(long userId,DateTime utcNow){ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);if(Status is AlertStatus.Resolved or AlertStatus.Expired)throw new InvalidOperationException("A closed alert cannot be acknowledged.");AcknowledgedByUserId=userId;AcknowledgedUtc=Utc(utcNow,nameof(utcNow));Status=AlertStatus.Acknowledged;}
    public void Resolve(DateTime utcNow){if(Status==AlertStatus.Expired)throw new InvalidOperationException("An expired alert cannot be resolved.");ResolvedUtc=Utc(utcNow,nameof(utcNow));Status=AlertStatus.Resolved;}
    public void Expire(DateTime utcNow){if(Status==AlertStatus.Resolved)return;ResolvedUtc=Utc(utcNow,nameof(utcNow));Status=AlertStatus.Expired;}
    private static string Required(string value,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value);var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(nameof(value));}
    private static string?Optional(string?value,int max)=>string.IsNullOrWhiteSpace(value)?null:Required(value,max);
    private static DateTime Utc(DateTime value,string name)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",name);
}

/// <summary>Durable ordered event cursor used for reconnect recovery and client de-duplication.</summary>
public sealed class RealtimeEvent
{
    private RealtimeEvent() { }
    private RealtimeEvent(long tenantId,long?storeId,long alertId,string eventName,int contractVersion,string correlationId,string deduplicationKey,DateTime occurredUtc)
    {ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);if(storeId.HasValue)ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId.Value);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alertId);if(contractVersion<1)throw new ArgumentOutOfRangeException(nameof(contractVersion));TenantId=tenantId;StoreId=storeId;AlertId=alertId;EventName=Required(eventName,100);ContractVersion=contractVersion;CorrelationId=Required(correlationId,64);DeduplicationKey=Required(deduplicationKey,200);OccurredUtc=Utc(occurredUtc);PayloadJson="{}";}
    public long Id{get;private set;} public long TenantId{get;private set;} public long?StoreId{get;private set;} public long AlertId{get;private set;} public string EventName{get;private set;}=string.Empty; public int ContractVersion{get;private set;} public string PayloadJson{get;private set;}="{}"; public DateTime OccurredUtc{get;private set;} public string CorrelationId{get;private set;}=string.Empty; public string DeduplicationKey{get;private set;}=string.Empty;
    public static RealtimeEvent Create(long tenantId,long?storeId,long alertId,string eventName,int contractVersion,string correlationId,string deduplicationKey,DateTime occurredUtc)=>new(tenantId,storeId,alertId,eventName,contractVersion,correlationId,deduplicationKey,occurredUtc);
    public void SetPayload(string payloadJson){PayloadJson=Required(payloadJson,16000);}
    private static string Required(string value,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value);var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(nameof(value));}
    private static DateTime Utc(DateTime value)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",nameof(value));
}

/// <summary>One idempotent channel delivery persisted with its originating alert transaction.</summary>
public sealed class NotificationOutboxMessage
{
    private NotificationOutboxMessage() { }
    private NotificationOutboxMessage(long tenantId,long?storeId,long alertId,long realtimeEventId,string channel,string eventType,int contractVersion,string payloadJson,string correlationId,string idempotencyKey,DateTime createdUtc)
    {ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);if(storeId.HasValue)ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId.Value);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alertId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(realtimeEventId);if(contractVersion<1)throw new ArgumentOutOfRangeException(nameof(contractVersion));TenantId=tenantId;StoreId=storeId;AlertId=alertId;RealtimeEventId=realtimeEventId;Channel=Required(channel,30);EventType=Required(eventType,100);ContractVersion=contractVersion;PayloadJson=Required(payloadJson,16000);CorrelationId=Required(correlationId,64);IdempotencyKey=Required(idempotencyKey,200);CreatedUtc=Utc(createdUtc);NextAttemptUtc=CreatedUtc;Status=NotificationOutboxStatus.Pending;}
    public long Id{get;private set;} public long TenantId{get;private set;} public long?StoreId{get;private set;} public long AlertId{get;private set;} public long RealtimeEventId{get;private set;} public string Channel{get;private set;}=string.Empty; public string EventType{get;private set;}=string.Empty; public int ContractVersion{get;private set;} public string PayloadJson{get;private set;}="{}"; public NotificationOutboxStatus Status{get;private set;} public int AttemptCount{get;private set;} public DateTime NextAttemptUtc{get;private set;} public string?LastError{get;private set;} public string CorrelationId{get;private set;}=string.Empty; public string IdempotencyKey{get;private set;}=string.Empty; public DateTime CreatedUtc{get;private set;} public DateTime?ProcessedUtc{get;private set;} public byte[]?RowVersion{get;private set;}
    public static NotificationOutboxMessage Queue(long tenantId,long?storeId,long alertId,long realtimeEventId,string channel,string eventType,int contractVersion,string payloadJson,string correlationId,string idempotencyKey,DateTime createdUtc)=>new(tenantId,storeId,alertId,realtimeEventId,channel,eventType,contractVersion,payloadJson,correlationId,idempotencyKey,createdUtc);
    public void StartAttempt(DateTime utcNow,TimeSpan processingLease){if(Status is NotificationOutboxStatus.Delivered or NotificationOutboxStatus.DeadLetter)throw new InvalidOperationException("A terminal outbox message cannot be processed.");utcNow=Utc(utcNow);if(processingLease<=TimeSpan.Zero)throw new ArgumentOutOfRangeException(nameof(processingLease));if(utcNow<NextAttemptUtc)throw new InvalidOperationException("The outbox message is not due.");AttemptCount++;Status=NotificationOutboxStatus.Processing;NextAttemptUtc=utcNow+processingLease;LastError=null;}
    public void MarkDelivered(DateTime utcNow){if(Status!=NotificationOutboxStatus.Processing)throw new InvalidOperationException("Only a processing message can be delivered.");Status=NotificationOutboxStatus.Delivered;ProcessedUtc=Utc(utcNow);LastError=null;}
    public void MarkFailed(string error,DateTime utcNow,int maxAttempts,TimeSpan retryDelay){if(Status!=NotificationOutboxStatus.Processing)throw new InvalidOperationException("Only a processing message can fail.");if(maxAttempts<1)throw new ArgumentOutOfRangeException(nameof(maxAttempts));utcNow=Utc(utcNow);LastError=Required(error,2000);if(AttemptCount>=maxAttempts){Status=NotificationOutboxStatus.DeadLetter;ProcessedUtc=utcNow;return;}Status=AttemptCount==1?NotificationOutboxStatus.Failed:NotificationOutboxStatus.Retrying;NextAttemptUtc=utcNow+retryDelay;}
    private static string Required(string value,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value);var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(nameof(value));}
    private static DateTime Utc(DateTime value)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",nameof(value));
}
