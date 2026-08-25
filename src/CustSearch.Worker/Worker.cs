using System.Text.Json;
using CustSearch.Application.Operations;

namespace CustSearch.Worker;

/// <summary>
/// Hosts reliable background processing for future outbox, report and webhook jobs.
/// </summary>
public sealed class Worker(ILogger<Worker> logger,IConfiguration configuration,IServiceScopeFactory scopes) : BackgroundService
{
    private static readonly Action<ILogger, TimeSpan, Exception?> Started =
        LoggerMessage.Define<TimeSpan>(LogLevel.Information, new EventId(1000, nameof(Started)),
            "Worker started with heartbeat interval {HeartbeatInterval}");

    private static readonly Action<ILogger, DateTimeOffset, Exception?> Heartbeat =
        LoggerMessage.Define<DateTimeOffset>(LogLevel.Debug, new EventId(1001, nameof(Heartbeat)),
            "Worker heartbeat at {HeartbeatUtc}");

    private static readonly Action<ILogger, Exception?> Stopped =
        LoggerMessage.Define(LogLevel.Information, new EventId(1002, nameof(Stopped)), "Worker stopped");
    private static readonly Action<ILogger,Exception?> HeartbeatFailed=LoggerMessage.Define(LogLevel.Error,new EventId(1003,nameof(HeartbeatFailed)),"Worker heartbeat persistence failed");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configuredSeconds = configuration.GetValue<int?>("Worker:HeartbeatIntervalSeconds") ?? 300;
        var interval = TimeSpan.FromSeconds(Math.Clamp(configuredSeconds, 10, 3600));
        var startedUtc=DateTime.UtcNow;var instanceId=$"{Environment.MachineName}:CustSearch.Worker";
        Started(logger, interval, null);

        await PersistAsync(1,startedUtc,startedUtc,null,stoppingToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                {
                    break;
                }

                Heartbeat(logger, DateTimeOffset.UtcNow, null);
                await PersistAsync(1,startedUtc,DateTime.UtcNow,null,stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        await PersistAsync(2,startedUtc,DateTime.UtcNow,null,CancellationToken.None).ConfigureAwait(false);Stopped(logger, null);

        async Task PersistAsync(byte status,DateTime started,DateTime?success,string?error,CancellationToken ct)
        {
            try{using var scope=scopes.CreateScope();var repository=scope.ServiceProvider.GetRequiredService<IOperationalPlatformRepository>();await repository.WriteHeartbeatAsync(new(instanceId,"CustSearch.Worker",status,started,success,error,JsonSerializer.Serialize(new{Machine=Environment.MachineName,Environment=configuration["DOTNET_ENVIRONMENT"]??"Production"})),ct).ConfigureAwait(false);}
            catch(OperationCanceledException)when(ct.IsCancellationRequested){throw;}
            catch(Exception exception){HeartbeatFailed(logger,exception);}
        }
    }
}
