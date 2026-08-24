using System.Globalization;
using System.Text.Json;
using CustSearch.Application.AlertsRealtime;
using CustSearch.Application.Authentication;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.Infrastructure.AlertsRealtime;

/// <summary>Implements server-scoped alert lifecycle, atomic outbox creation and durable reconnect recovery.</summary>
public sealed class AlertsRealtimeService(CustSearchDbContext db,ICurrentUserContext currentUser,TimeProvider clock,AlertDeduplicationCoordinator deduplication,IAlertConnectionMetrics connectionMetrics):IAlertsRealtimeService
{
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web);

    public async Task<AlertListView> ListAsync(long?storeId,AlertStatus?status,int take=100,CancellationToken cancellationToken=default)
    {
        var tenantId=RequireTenant();take=Math.Clamp(take,1,200);if(storeId.HasValue)EnsureStoreAccess(storeId.Value);if(status.HasValue&&!Enum.IsDefined(status.Value))throw new AlertBusinessRuleException("Alert status is invalid.");
        var stores=currentUser.StoreIds.ToArray();var query=VisibleAlerts(tenantId,stores);if(storeId.HasValue)query=query.Where(x=>x.StoreId==storeId);if(status.HasValue)query=query.Where(x=>x.Status==status.Value);
        var rows=await query.OrderByDescending(x=>x.CreatedUtc).ThenByDescending(x=>x.Id).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);var items=rows.Select(Map).ToList();
        var unread=await VisibleAlerts(tenantId,stores).CountAsync(x=>x.Status==AlertStatus.New||x.Status==AlertStatus.Delivered,cancellationToken).ConfigureAwait(false);
        var eventQuery=VisibleEvents(tenantId,stores);var cursor=await eventQuery.MaxAsync(x=>(long?)x.Id,cancellationToken).ConfigureAwait(false)??0;
        return new(items,unread,cursor);
    }

    public async Task<AlertView> GetAsync(long alertId,CancellationToken cancellationToken=default)=>Map(await RequireVisibleAlertAsync(alertId,cancellationToken).ConfigureAwait(false));

    public async Task<AlertView> CreateAsync(CreateAlertCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(command);ArgumentNullException.ThrowIfNull(audit);var tenantId=RequireTenant();if(command.StoreId.HasValue)await EnsureActiveStoreAsync(tenantId,command.StoreId.Value,cancellationToken).ConfigureAwait(false);var key=Required(command.DeduplicationKey,"DeduplicationKey",200);
        return await deduplication.ExecuteAsync(tenantId,key,async()=>
        {
            var existing=await db.Alerts.SingleOrDefaultAsync(x=>x.TenantId==tenantId&&x.DeduplicationKey==key,cancellationToken).ConfigureAwait(false);if(existing is not null){EnsureVisible(existing);return Map(existing);}
            var now=clock.GetUtcNow().UtcDateTime;await using var transaction=await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var alert=Alert.Create(tenantId,command.StoreId,command.AlertType,command.Severity,command.Title,command.Message,command.EntityType,command.EntityId,audit.CorrelationId,key,now);db.Alerts.Add(alert);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await QueueEventAsync(alert,"alert.created",audit,now,cancellationToken).ConfigureAwait(false);db.AuditLogs.Add(AuditLog.Record(tenantId,alert.StoreId,audit.ActorUserId,"User","AlertCreated","Alert",alert.Id.ToString(CultureInfo.InvariantCulture),null,JsonSerializer.Serialize(Map(alert),JsonOptions),audit.IpAddress,audit.UserAgent,audit.CorrelationId,now));await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);return Map(alert);
            }
            catch(DbUpdateException){await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);db.ChangeTracker.Clear();var duplicate=await db.Alerts.SingleOrDefaultAsync(x=>x.TenantId==tenantId&&x.DeduplicationKey==key,cancellationToken).ConfigureAwait(false);if(duplicate is not null){EnsureVisible(duplicate);return Map(duplicate);}throw;}
        },cancellationToken).ConfigureAwait(false);
    }

    public async Task<AlertView> UpdateAsync(long alertId,UpdateAlertCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(command);var alert=await RequireVisibleAlertAsync(alertId,cancellationToken).ConfigureAwait(false);var before=JsonSerializer.Serialize(Map(alert),JsonOptions);var now=clock.GetUtcNow().UtcDateTime;await using var transaction=await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);alert.Update(command.Severity,command.Title,command.Message,command.EntityType,command.EntityId);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);await QueueEventAsync(alert,"alert.updated",audit,now,cancellationToken).ConfigureAwait(false);db.AuditLogs.Add(AuditLog.Record(alert.TenantId,alert.StoreId,audit.ActorUserId,"User","AlertUpdated","Alert",alert.Id.ToString(CultureInfo.InvariantCulture),before,JsonSerializer.Serialize(Map(alert),JsonOptions),audit.IpAddress,audit.UserAgent,audit.CorrelationId,now));await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);return Map(alert);
    }

    public async Task<AlertView> AcknowledgeAsync(long alertId,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        var alert=await RequireVisibleAlertAsync(alertId,cancellationToken).ConfigureAwait(false);if(alert.Status==AlertStatus.Acknowledged&&alert.AcknowledgedByUserId==currentUser.UserId)return Map(alert);var before=JsonSerializer.Serialize(Map(alert),JsonOptions);var now=clock.GetUtcNow().UtcDateTime;await using var transaction=await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);alert.Acknowledge(currentUser.UserId,now);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);await QueueEventAsync(alert,"alert.acknowledged",audit,now,cancellationToken).ConfigureAwait(false);db.AuditLogs.Add(AuditLog.Record(alert.TenantId,alert.StoreId,audit.ActorUserId,"User","AlertAcknowledged","Alert",alert.Id.ToString(CultureInfo.InvariantCulture),before,JsonSerializer.Serialize(Map(alert),JsonOptions),audit.IpAddress,audit.UserAgent,audit.CorrelationId,now));await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);return Map(alert);
    }

    public async Task<AlertView> ResolveAsync(long alertId,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        var alert=await RequireVisibleAlertAsync(alertId,cancellationToken).ConfigureAwait(false);if(alert.Status==AlertStatus.Resolved)return Map(alert);var before=JsonSerializer.Serialize(Map(alert),JsonOptions);var now=clock.GetUtcNow().UtcDateTime;await using var transaction=await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);alert.Resolve(now);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);await QueueEventAsync(alert,"alert.resolved",audit,now,cancellationToken).ConfigureAwait(false);db.AuditLogs.Add(AuditLog.Record(alert.TenantId,alert.StoreId,audit.ActorUserId,"User","AlertResolved","Alert",alert.Id.ToString(CultureInfo.InvariantCulture),before,JsonSerializer.Serialize(Map(alert),JsonOptions),audit.IpAddress,audit.UserAgent,audit.CorrelationId,now));await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);return Map(alert);
    }

    public async Task<AlertRecoveryView> RecoverAsync(long afterEventId,int take=200,CancellationToken cancellationToken=default)
    {
        if(afterEventId<0)throw new AlertBusinessRuleException("Recovery cursor cannot be negative.");var tenantId=RequireTenant();take=Math.Clamp(take,1,500);var rows=await VisibleEvents(tenantId,currentUser.StoreIds.ToArray()).Where(x=>x.Id>afterEventId).OrderBy(x=>x.Id).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);var events=new List<AlertRealtimeEventV1>(rows.Count);foreach(var row in rows){var item=JsonSerializer.Deserialize<AlertRealtimeEventV1>(row.PayloadJson,JsonOptions)??throw new InvalidOperationException("Stored real-time event payload is invalid.");events.Add(item);}return new(afterEventId,events.Count==0?afterEventId:events[^1].EventId,events);
    }

    public async Task<AlertHealthMetricsView> GetMetricsAsync(CancellationToken cancellationToken=default)
    {
        var tenantId=RequireTenant();var q=db.NotificationOutbox.AsNoTracking().Where(x=>x.TenantId==tenantId);var backlog=await q.LongCountAsync(x=>x.Status==NotificationOutboxStatus.Pending||x.Status==NotificationOutboxStatus.Processing||x.Status==NotificationOutboxStatus.Failed||x.Status==NotificationOutboxStatus.Retrying,cancellationToken).ConfigureAwait(false);var successes=await q.LongCountAsync(x=>x.Status==NotificationOutboxStatus.Delivered,cancellationToken).ConfigureAwait(false);var failures=await q.LongCountAsync(x=>x.Status==NotificationOutboxStatus.Failed||x.Status==NotificationOutboxStatus.Retrying||x.Status==NotificationOutboxStatus.DeadLetter,cancellationToken).ConfigureAwait(false);var retries=await q.SumAsync(x=>(long)(x.AttemptCount>1?x.AttemptCount-1:0),cancellationToken).ConfigureAwait(false);var dead=await q.LongCountAsync(x=>x.Status==NotificationOutboxStatus.DeadLetter,cancellationToken).ConfigureAwait(false);var oldest=await q.Where(x=>x.Status==NotificationOutboxStatus.Pending||x.Status==NotificationOutboxStatus.Processing||x.Status==NotificationOutboxStatus.Failed||x.Status==NotificationOutboxStatus.Retrying).MinAsync(x=>(DateTime?)x.CreatedUtc,cancellationToken).ConfigureAwait(false);return new(backlog,successes,failures,retries,dead,oldest,connectionMetrics.ActiveConnections(tenantId),connectionMetrics.Reconnects(tenantId));
    }

    private async Task QueueEventAsync(Alert alert,string eventName,TenantAuditContext audit,DateTime now,CancellationToken ct)
    {
        var eventKey=$"{eventName}:{alert.Id}:{now.Ticks}";var realtime=RealtimeEvent.Create(alert.TenantId,alert.StoreId,alert.Id,eventName,1,audit.CorrelationId,eventKey,now);db.RealtimeEvents.Add(realtime);await db.SaveChangesAsync(ct).ConfigureAwait(false);var envelope=new AlertRealtimeEventV1(realtime.Id,eventName,1,now,alert.TenantId,alert.StoreId,audit.CorrelationId,Map(alert));var payload=JsonSerializer.Serialize(envelope,JsonOptions);realtime.SetPayload(payload);db.NotificationOutbox.Add(NotificationOutboxMessage.Queue(alert.TenantId,alert.StoreId,alert.Id,realtime.Id,"SignalR",eventName,1,payload,audit.CorrelationId,$"signalr:{realtime.Id}",now));await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    private IQueryable<Alert> VisibleAlerts(long tenantId,long[]stores)=>db.Alerts.AsNoTracking().Where(x=>x.TenantId==tenantId&&(x.StoreId==null||stores.Contains(x.StoreId.Value)));
    private IQueryable<RealtimeEvent> VisibleEvents(long tenantId,long[]stores)=>db.RealtimeEvents.AsNoTracking().Where(x=>x.TenantId==tenantId&&(x.StoreId==null||stores.Contains(x.StoreId.Value)));
    private async Task<Alert>RequireVisibleAlertAsync(long id,CancellationToken ct){ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);var tenantId=RequireTenant();var stores=currentUser.StoreIds.ToArray();return await db.Alerts.SingleOrDefaultAsync(x=>x.Id==id&&x.TenantId==tenantId&&(x.StoreId==null||stores.Contains(x.StoreId.Value)),ct).ConfigureAwait(false)??throw new AlertResourceNotFoundException("Alert was not found.");}
    private async Task EnsureActiveStoreAsync(long tenantId,long storeId,CancellationToken ct){EnsureStoreAccess(storeId);if(!await db.Stores.AnyAsync(x=>x.Id==storeId&&x.TenantId==tenantId&&x.IsActive,ct).ConfigureAwait(false))throw new AlertResourceNotFoundException("Store was not found.");}
    private void EnsureVisible(Alert alert){if(alert.TenantId!=RequireTenant()||(alert.StoreId.HasValue&&!currentUser.StoreIds.Contains(alert.StoreId.Value)))throw new AlertResourceNotFoundException("Alert was not found.");}
    private void EnsureStoreAccess(long storeId){ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);if(!currentUser.StoreIds.Contains(storeId))throw new AlertResourceNotFoundException("Store was not found.");}
    private long RequireTenant()=>currentUser.IsAuthenticated&&!currentUser.IsPlatformAdmin&&currentUser.TenantId is>0?currentUser.TenantId.Value:throw new AlertBusinessRuleException("A tenant-scoped authenticated session is required.");
    private static string Required(string value,string name,int max){if(string.IsNullOrWhiteSpace(value))throw new AlertBusinessRuleException($"{name} is required.");var v=value.Trim();return v.Length<=max?v:throw new AlertBusinessRuleException($"{name} cannot exceed {max} characters.");}
    private static AlertView Map(Alert x)=>new(x.Id,x.AlertType,x.StoreId,x.Severity,x.Title,x.Message,x.EntityType,x.EntityId,x.CreatedUtc,x.AcknowledgedUtc,x.AcknowledgedByUserId,x.ResolvedUtc,x.Status,x.CorrelationId,x.DeduplicationKey);
}
