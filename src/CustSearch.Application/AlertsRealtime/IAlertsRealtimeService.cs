using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;

namespace CustSearch.Application.AlertsRealtime;

/// <summary>Phase 11 tenant/store alert boundary. Tenant and actor identity never come from browser DTOs.</summary>
public interface IAlertsRealtimeService
{
    Task<AlertListView> ListAsync(long?storeId,AlertStatus?status,int take=100,CancellationToken cancellationToken=default);
    Task<AlertView> GetAsync(long alertId,CancellationToken cancellationToken=default);
    Task<AlertView> CreateAsync(CreateAlertCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<AlertView> UpdateAsync(long alertId,UpdateAlertCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<AlertView> AcknowledgeAsync(long alertId,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<AlertView> ResolveAsync(long alertId,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<AlertRecoveryView> RecoverAsync(long afterEventId,int take=200,CancellationToken cancellationToken=default);
    Task<AlertHealthMetricsView> GetMetricsAsync(CancellationToken cancellationToken=default);
}

public sealed record CreateAlertCommand(string AlertType,long?StoreId,AlertSeverity Severity,string Title,string Message,string EntityType,string?EntityId,string DeduplicationKey);
public sealed record UpdateAlertCommand(AlertSeverity Severity,string Title,string Message,string EntityType,string?EntityId);
public sealed record AlertView(long Id,string AlertType,long?StoreId,AlertSeverity Severity,string Title,string Message,string EntityType,string?EntityId,DateTime CreatedUtc,DateTime?AcknowledgedUtc,long?AcknowledgedByUserId,DateTime?ResolvedUtc,AlertStatus Status,string CorrelationId,string DeduplicationKey);
public sealed record AlertListView(IReadOnlyList<AlertView> Items,int UnreadCount,long LastEventId);
public sealed record AlertRealtimeEventV1(long EventId,string EventType,int ContractVersion,DateTime OccurredUtc,long TenantId,long?StoreId,string CorrelationId,AlertView Alert);
public sealed record AlertRecoveryView(long RequestedAfterEventId,long NextCursor,IReadOnlyList<AlertRealtimeEventV1> Events);
public sealed record AlertHealthMetricsView(long OutboxBacklog,long DeliverySuccesses,long DeliveryFailures,long Retries,long DeadLetters,DateTime?OldestPendingUtc,long SignalRConnections,long Reconnects);

/// <summary>Safe Phase 11 business-rule failure mapped to a client validation response.</summary>
public sealed class AlertBusinessRuleException(string message):Exception(message);

/// <summary>Hides alerts outside the authenticated tenant/store scope as not found.</summary>
public sealed class AlertResourceNotFoundException(string message):Exception(message);

/// <summary>Reliable dispatcher for due notification outbox records.</summary>
public interface INotificationOutboxProcessor
{
    Task<OutboxProcessResult> ProcessDueAsync(int batchSize=50,CancellationToken cancellationToken=default);
}

public sealed record OutboxProcessResult(int Claimed,int Delivered,int Failed,int DeadLettered);

/// <summary>Pluggable delivery boundary. External provider adapters are registered only when configured.</summary>
public interface INotificationChannelAdapter
{
    string Channel{get;}
    Task DeliverAsync(NotificationDeliveryMessage message,CancellationToken cancellationToken=default);
}

public sealed record NotificationDeliveryMessage(long OutboxId,long TenantId,long?StoreId,long AlertId,long RealtimeEventId,string EventType,int ContractVersion,string PayloadJson,string CorrelationId,string IdempotencyKey);

/// <summary>Tracks live SignalR connection and reconnect counters without controlling authorization.</summary>
public interface IAlertConnectionMetrics
{
    long ActiveConnections(long tenantId);
    long Reconnects(long tenantId);
    long TotalActiveConnections();
    long TotalReconnects();
    void Connected(string connectionId,long tenantId);
    void Disconnected(string connectionId);
    void Reconnected(string connectionId,long tenantId);
}
