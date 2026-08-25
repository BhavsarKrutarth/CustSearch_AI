using CustSearch.Application.AlertsRealtime;
using CustSearch.Application.Operations;
using CustSearch.Infrastructure.Operations;
using Microsoft.Extensions.Options;

namespace CustSearch.API.AlertsRealtime;

/// <summary>Dispatches committed notification outbox rows outside the originating business transaction.</summary>
public sealed class NotificationOutboxHostedService(IServiceScopeFactory scopeFactory,IOptions<AlertsRealtimeOptions> options,IOptions<OperationalPlatformOptions>operationalOptions,ILogger<NotificationOutboxHostedService> logger):BackgroundService
{
    private static readonly Action<ILogger,int,int,int,int,Exception?> OutboxProcessed=LoggerMessage.Define<int,int,int,int>(LogLevel.Information,new EventId(1101,nameof(OutboxProcessed)),"Notification outbox processed {Claimed}; delivered {Delivered}; failed {Failed}; dead letters {DeadLetters}");
    private static readonly Action<ILogger,Exception?> OutboxPollingFailed=LoggerMessage.Define(LogLevel.Error,new EventId(1102,nameof(OutboxPollingFailed)),"Notification outbox polling failed");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings=options.Value;if(!settings.DispatcherEnabled)return;var delay=TimeSpan.FromSeconds(Math.Clamp(settings.PollIntervalSeconds,1,60));while(!stoppingToken.IsCancellationRequested){try{await using var scope=scopeFactory.CreateAsyncScope();var gate=scope.ServiceProvider.GetRequiredService<IWorkerRuntimeGate>();var lease=await gate.TryAcquireAsync("notifications",Environment.MachineName,TimeSpan.FromSeconds(operationalOptions.Value.LeaseSeconds),stoppingToken).ConfigureAwait(false);if(lease is not null){try{var result=await scope.ServiceProvider.GetRequiredService<INotificationOutboxProcessor>().ProcessDueAsync(settings.BatchSize,stoppingToken).ConfigureAwait(false);if(result.Claimed>0)OutboxProcessed(logger,result.Claimed,result.Delivered,result.Failed,result.DeadLettered,null);}finally{await gate.ReleaseAsync(lease,stoppingToken).ConfigureAwait(false);}}}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}catch(Exception exception){OutboxPollingFailed(logger,exception);}await Task.Delay(delay,stoppingToken).ConfigureAwait(false);}
    }
}

/// <summary>Non-secret operational controls for the Phase 11 dispatcher.</summary>
public sealed class AlertsRealtimeOptions
{
    public const string SectionName="AlertsRealtime";public bool DispatcherEnabled{get;set;}=true;public int PollIntervalSeconds{get;set;}=2;public int BatchSize{get;set;}=50;
}
