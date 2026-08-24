using CustSearch.Application.AlertsRealtime;
using Microsoft.Extensions.Options;

namespace CustSearch.API.AlertsRealtime;

/// <summary>Dispatches committed notification outbox rows outside the originating business transaction.</summary>
public sealed class NotificationOutboxHostedService(IServiceScopeFactory scopeFactory,IOptions<AlertsRealtimeOptions> options,ILogger<NotificationOutboxHostedService> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings=options.Value;if(!settings.DispatcherEnabled)return;var delay=TimeSpan.FromSeconds(Math.Clamp(settings.PollIntervalSeconds,1,60));while(!stoppingToken.IsCancellationRequested){try{await using var scope=scopeFactory.CreateAsyncScope();var result=await scope.ServiceProvider.GetRequiredService<INotificationOutboxProcessor>().ProcessDueAsync(settings.BatchSize,stoppingToken).ConfigureAwait(false);if(result.Claimed>0)logger.LogInformation("Notification outbox processed {Claimed}; delivered {Delivered}; failed {Failed}; dead letters {DeadLetters}",result.Claimed,result.Delivered,result.Failed,result.DeadLettered);}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}catch(Exception exception){logger.LogError(exception,"Notification outbox polling failed");}await Task.Delay(delay,stoppingToken).ConfigureAwait(false);}
    }
}

/// <summary>Non-secret operational controls for the Phase 11 dispatcher.</summary>
public sealed class AlertsRealtimeOptions
{
    public const string SectionName="AlertsRealtime";public bool DispatcherEnabled{get;set;}=true;public int PollIntervalSeconds{get;set;}=2;public int BatchSize{get;set;}=50;
}
