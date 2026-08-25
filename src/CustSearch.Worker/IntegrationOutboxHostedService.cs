using CustSearch.Application.Integrations;
using Microsoft.Extensions.Options;

namespace CustSearch.Worker;

/// <summary>Runs outbound integration delivery only after business transactions have committed their outbox rows.</summary>
public sealed class IntegrationOutboxHostedService(IServiceScopeFactory scopeFactory,IOptions<IntegrationDispatcherOptions>options,ILogger<IntegrationOutboxHostedService>logger):BackgroundService
{
    private static readonly Action<ILogger,int,int,int,int,Exception?>Processed=LoggerMessage.Define<int,int,int,int>(LogLevel.Information,new EventId(1201,nameof(Processed)),"Integration outbox processed {Claimed}; delivered {Delivered}; failed {Failed}; dead letters {DeadLetters}");private static readonly Action<ILogger,Exception?>PollingFailed=LoggerMessage.Define(LogLevel.Error,new EventId(1202,nameof(PollingFailed)),"Integration outbox polling failed");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken){var settings=options.Value;if(!settings.Enabled)return;var delay=TimeSpan.FromSeconds(Math.Clamp(settings.PollIntervalSeconds,1,60));while(!stoppingToken.IsCancellationRequested){try{await using var scope=scopeFactory.CreateAsyncScope();var result=await scope.ServiceProvider.GetRequiredService<IIntegrationOutboxProcessor>().ProcessDueAsync(settings.BatchSize,stoppingToken).ConfigureAwait(false);if(result.Claimed>0)Processed(logger,result.Claimed,result.Delivered,result.Failed,result.DeadLettered,null);}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}catch(Exception exception){PollingFailed(logger,exception);}await Task.Delay(delay,stoppingToken).ConfigureAwait(false);}}
}

public sealed class IntegrationDispatcherOptions{public const string SectionName="IntegrationDispatcher";public bool Enabled{get;set;}=true;public int PollIntervalSeconds{get;set;}=2;public int BatchSize{get;set;}=50;}
