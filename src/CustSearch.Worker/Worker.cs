namespace CustSearch.Worker;

/// <summary>
/// Hosts reliable background processing for future outbox, report and webhook jobs.
/// </summary>
public sealed class Worker(ILogger<Worker> logger, IConfiguration configuration) : BackgroundService
{
    private static readonly Action<ILogger, TimeSpan, Exception?> Started =
        LoggerMessage.Define<TimeSpan>(LogLevel.Information, new EventId(1000, nameof(Started)),
            "Worker started with heartbeat interval {HeartbeatInterval}");

    private static readonly Action<ILogger, DateTimeOffset, Exception?> Heartbeat =
        LoggerMessage.Define<DateTimeOffset>(LogLevel.Debug, new EventId(1001, nameof(Heartbeat)),
            "Worker heartbeat at {HeartbeatUtc}");

    private static readonly Action<ILogger, Exception?> Stopped =
        LoggerMessage.Define(LogLevel.Information, new EventId(1002, nameof(Stopped)), "Worker stopped");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configuredSeconds = configuration.GetValue<int?>("Worker:HeartbeatIntervalSeconds") ?? 300;
        var interval = TimeSpan.FromSeconds(Math.Clamp(configuredSeconds, 10, 3600));
        Started(logger, interval, null);

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
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        Stopped(logger, null);
    }
}
