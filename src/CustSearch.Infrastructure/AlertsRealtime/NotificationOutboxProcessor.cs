using CustSearch.Application.AlertsRealtime;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.Infrastructure.AlertsRealtime;

/// <summary>Claims due outbox rows optimistically and delivers only through explicitly registered channel adapters.</summary>
public sealed class NotificationOutboxProcessor(CustSearchDbContext db,IEnumerable<INotificationChannelAdapter> adapters,TimeProvider clock):INotificationOutboxProcessor
{
    private readonly Dictionary<string,INotificationChannelAdapter> channels=adapters.ToDictionary(x=>x.Channel,StringComparer.OrdinalIgnoreCase);
    public async Task<OutboxProcessResult> ProcessDueAsync(int batchSize=50,CancellationToken cancellationToken=default)
    {
        batchSize=Math.Clamp(batchSize,1,200);var now=clock.GetUtcNow().UtcDateTime;var due=await db.NotificationOutbox.AsNoTracking().Where(x=>(x.Status==NotificationOutboxStatus.Pending||x.Status==NotificationOutboxStatus.Failed||x.Status==NotificationOutboxStatus.Retrying||x.Status==NotificationOutboxStatus.Processing)&&x.NextAttemptUtc<=now).OrderBy(x=>x.NextAttemptUtc).ThenBy(x=>x.Id).Select(x=>x.Id).Take(batchSize).ToListAsync(cancellationToken).ConfigureAwait(false);var claimed=0;var delivered=0;var failed=0;var dead=0;
        foreach(var id in due)
        {
            var message=await db.NotificationOutbox.SingleOrDefaultAsync(x=>x.Id==id,cancellationToken).ConfigureAwait(false);if(message is null)continue;
            try{message.StartAttempt(now,TimeSpan.FromMinutes(2));await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);claimed++;}
            catch(DbUpdateConcurrencyException){db.Entry(message).State=EntityState.Detached;continue;}
            try
            {
                if(!channels.TryGetValue(message.Channel,out var adapter))throw new InvalidOperationException("Notification channel is not configured.");await adapter.DeliverAsync(new(message.Id,message.TenantId,message.StoreId,message.AlertId,message.RealtimeEventId,message.EventType,message.ContractVersion,message.PayloadJson,message.CorrelationId,message.IdempotencyKey),cancellationToken).ConfigureAwait(false);var completed=clock.GetUtcNow().UtcDateTime;message.MarkDelivered(completed);var alert=await db.Alerts.SingleOrDefaultAsync(x=>x.Id==message.AlertId&&x.TenantId==message.TenantId,cancellationToken).ConfigureAwait(false);if(alert is not null&&message.EventType=="alert.created")alert.MarkDelivered();await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);delivered++;
            }
            catch(Exception exception) when(exception is not OperationCanceledException)
            {
                var failedUtc=clock.GetUtcNow().UtcDateTime;var delay=TimeSpan.FromSeconds(Math.Min(300,Math.Pow(2,Math.Max(0,message.AttemptCount-1))*5));message.MarkFailed($"{exception.GetType().Name}: delivery failed.",failedUtc,5,delay);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);failed++;if(message.Status==NotificationOutboxStatus.DeadLetter)dead++;
            }
        }
        return new(claimed,delivered,failed,dead);
    }
}
