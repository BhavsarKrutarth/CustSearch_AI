using System.Data;
using CustSearch.Application.Abstractions.Data;
using CustSearch.Application.ReportsExports;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CustSearch.Infrastructure.ReportsExports;

/// <summary>Relays durable SQL report events; user-targeted SignalR publication is retried on failure.</summary>
public sealed class ReportExportEventDispatcher(IDbConnectionFactory connections,IReportExportRealtimePublisher publisher,ILogger<ReportExportEventDispatcher>logger):IReportExportEventDispatcher
{
    private static readonly Action<ILogger,long,Exception?> DeliveryFailed=LoggerMessage.Define<long>(LogLevel.Warning,new EventId(1520,nameof(DeliveryFailed)),"Report export event {ReportExportEventId} delivery failed");
    public async Task<int>ProcessDueAsync(int take=50,CancellationToken ct=default)
    {
        take=Math.Clamp(take,1,200);await using var connection=await connections.OpenConnectionAsync(ct).ConfigureAwait(false);var claimed=(await connection.QueryAsync<ClaimedEvent>(new CommandDefinition("dbo.ReportExportEvent_Claim",new{LeaseSeconds=60,Take=take},commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false)).AsList();
        foreach(var item in claimed){try{await publisher.PublishAsync(new(item.Id,item.ReportExportJobId,item.TenantId,item.RequestedByUserId,item.EventType,item.JobStatus,item.ProgressPercent,item.CreatedUtc),ct).ConfigureAwait(false);await connection.ExecuteAsync(new CommandDefinition("dbo.ReportExportEvent_Complete",new{EventId=item.Id,item.LeaseToken},commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false);}catch(OperationCanceledException)when(ct.IsCancellationRequested){throw;}catch(Exception exception){DeliveryFailed(logger,item.Id,exception);await connection.ExecuteAsync(new CommandDefinition("dbo.ReportExportEvent_Fail",new{EventId=item.Id,item.LeaseToken,ErrorMessage=$"{exception.GetType().Name}: realtime delivery failed."},commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false);}}
        return claimed.Count;
    }
    private sealed record ClaimedEvent(long Id,long ReportExportJobId,long?TenantId,long RequestedByUserId,string EventType,ReportExportStatus JobStatus,byte ProgressPercent,DateTime CreatedUtc,Guid LeaseToken);
}

