using System.Diagnostics;
using CustSearch.Application.Integrations;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.Infrastructure.Integrations;

/// <summary>Claims committed outbound rows, applies bounded retry policy and writes payload-free attempt logs.</summary>
public sealed class IntegrationOutboxProcessor(CustSearchDbContext db,IIntegrationTransport transport,TimeProvider clock):IIntegrationOutboxProcessor
{
    public async Task<IntegrationOutboxProcessResult>ProcessDueAsync(int batchSize=50,CancellationToken cancellationToken=default)
    {
        batchSize=Math.Clamp(batchSize,1,200);var now=clock.GetUtcNow().UtcDateTime;var due=await db.IntegrationOutbox.AsNoTracking().Where(x=>(x.Status==IntegrationOutboxStatus.Pending||x.Status==IntegrationOutboxStatus.Failed||x.Status==IntegrationOutboxStatus.Retrying||x.Status==IntegrationOutboxStatus.Processing)&&x.NextAttemptUtc<=now).OrderBy(x=>x.NextAttemptUtc).ThenBy(x=>x.Id).Select(x=>x.Id).Take(batchSize).ToListAsync(cancellationToken).ConfigureAwait(false);var claimed=0;var delivered=0;var failed=0;var dead=0;
        foreach(var id in due)
        {
            var item=await db.IntegrationOutbox.SingleOrDefaultAsync(x=>x.Id==id,cancellationToken).ConfigureAwait(false);if(item is null)continue;try{item.StartAttempt(now,TimeSpan.FromMinutes(2));await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);claimed++;}catch(DbUpdateConcurrencyException){db.Entry(item).State=EntityState.Detached;continue;}
            var stopwatch=Stopwatch.StartNew();IntegrationTransportResult result;var configuration=await db.IntegrationConfigurations.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==item.IntegrationConfigurationId&&x.TenantId==item.TenantId,cancellationToken).ConfigureAwait(false);if(configuration is null||!configuration.Enabled||!configuration.SupportsOutbound)result=new(false,409,0,"configuration","Integration configuration is unavailable for outbound delivery.");else try{result=await transport.SendAsync(new(item.Id,item.Provider,item.Destination,item.EventType,item.ContractVersion,item.PayloadJson,item.CorrelationId,item.IdempotencyKey,configuration.CredentialReference,configuration.WebhookSigningSecretReference,configuration.TimeoutSeconds),cancellationToken).ConfigureAwait(false);}catch(Exception exception)when(exception is not OperationCanceledException){result=new(false,null,0,"transport",$"{exception.GetType().Name}: outbound delivery failed.");}stopwatch.Stop();var completed=clock.GetUtcNow().UtcDateTime;
            if(result.Success&&result.StatusCode is>=200 and<=299){item.MarkDelivered(result.StatusCode.Value,completed);db.IntegrationDeliveryLogs.Add(IntegrationDeliveryLog.RecordOutbound(item.TenantId,item.IntegrationConfigurationId,item.Id,item.CorrelationId,item.Provider,IntegrationDeliveryStatus.Delivered,result.DurationMilliseconds>0?result.DurationMilliseconds:stopwatch.ElapsedMilliseconds,result.StatusCode,null,completed));delivered++;}
            else{var permanent=result.StatusCode is>=400 and<=499 and not 408 and not 429;item.MarkFailed(result.StatusCode,Safe(result.SafeError),permanent,completed);var status=item.Status==IntegrationOutboxStatus.DeadLetter?IntegrationDeliveryStatus.DeadLetter:IntegrationDeliveryStatus.Retrying;db.IntegrationDeliveryLogs.Add(IntegrationDeliveryLog.RecordOutbound(item.TenantId,item.IntegrationConfigurationId,item.Id,item.CorrelationId,item.Provider,status,result.DurationMilliseconds>0?result.DurationMilliseconds:stopwatch.ElapsedMilliseconds,result.StatusCode,SafeCategory(result.ErrorCategory),completed));failed++;if(item.Status==IntegrationOutboxStatus.DeadLetter)dead++;}
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        return new(claimed,delivered,failed,dead);
    }
    private static string Safe(string?value)=>string.IsNullOrWhiteSpace(value)?"Outbound delivery failed.":value.Length<=2000?value:"Outbound delivery failed.";
    private static string?SafeCategory(string?value)=>string.IsNullOrWhiteSpace(value)?null:value.Length<=100?value:"transport";
}
