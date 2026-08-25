using CustSearch.Application.Operations;
using CustSearch.Infrastructure.Operations;
using Microsoft.Extensions.Options;

namespace CustSearch.Worker;

public sealed partial class OperationalRetentionHostedService(IServiceScopeFactory scopes,IOptions<OperationalPlatformOptions>options,ILogger<OperationalRetentionHostedService>logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken){var settings=options.Value;var delay=TimeSpan.FromMinutes(settings.RetentionPollMinutes);while(!stoppingToken.IsCancellationRequested){try{await using var scope=scopes.CreateAsyncScope();var gate=scope.ServiceProvider.GetRequiredService<IWorkerRuntimeGate>();var lease=await gate.TryAcquireAsync("retention",Environment.MachineName,TimeSpan.FromSeconds(settings.LeaseSeconds),stoppingToken).ConfigureAwait(false);if(lease is not null){try{var result=await scope.ServiceProvider.GetRequiredService<IRetentionProcessor>().RunDueAsync(settings.RetentionBatchSize,stoppingToken).ConfigureAwait(false);LogResult(logger,result.Policies,result.Deleted,result.Failed);}finally{await gate.ReleaseAsync(lease,stoppingToken).ConfigureAwait(false);}}}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}catch(Exception exception){LogFailure(logger,exception);}await Task.Delay(delay,stoppingToken).ConfigureAwait(false);}}
    [LoggerMessage(EventId=1601,Level=LogLevel.Information,Message="Retention processed {Policies} policies, deleted {Deleted} rows, failed {Failed}")]private static partial void LogResult(ILogger logger,int policies,int deleted,int failed);
    [LoggerMessage(EventId=1602,Level=LogLevel.Error,Message="Retention worker cycle failed")]private static partial void LogFailure(ILogger logger,Exception exception);
}

public sealed partial class OperationalHeartbeatHostedService(IServiceScopeFactory scopes,IOptions<OperationalPlatformOptions>options,ILogger<OperationalHeartbeatHostedService>logger):BackgroundService
{
    private readonly string instanceId=$"{Environment.MachineName}:{Environment.ProcessId}";
    protected override async Task ExecuteAsync(CancellationToken stoppingToken){var delay=TimeSpan.FromSeconds(options.Value.HeartbeatSeconds);while(!stoppingToken.IsCancellationRequested){try{await using var scope=scopes.CreateAsyncScope();await scope.ServiceProvider.GetRequiredService<IWorkerRuntimeGate>().HeartbeatAsync(instanceId,"custsearch-worker",true,null,stoppingToken).ConfigureAwait(false);}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}catch(Exception exception){LogFailure(logger,exception);}await Task.Delay(delay,stoppingToken).ConfigureAwait(false);}}
    [LoggerMessage(EventId=1603,Level=LogLevel.Error,Message="Worker heartbeat failed")]private static partial void LogFailure(ILogger logger,Exception exception);
}
