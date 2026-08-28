using CustSearch.Application.Security;

namespace CustSearch.Worker;

public sealed partial class SecurityMaintenanceHostedService(
    IServiceScopeFactory scopes,
    ILogger<SecurityMaintenanceHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var result = await scope.ServiceProvider
                    .GetRequiredService<ISecurityMaintenanceProcessor>()
                    .RunOnceAsync(stoppingToken);
                LogResult(
                    logger,
                    result.NotificationsDelivered,
                    result.EscalationsQueued,
                    result.EvidenceExpired,
                    result.PaymentsCorrelated,
                    result.StaleCandidatesResolved,
                    result.OpenCandidates);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogFailure(logger, exception);
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    [LoggerMessage(
        EventId = 1801,
        Level = LogLevel.Information,
        Message = "Security maintenance: notifications={Notifications}, escalations={Escalations}, evidence={Evidence}, correlations={Correlations}, stale={Stale}, open={Open}")]
    private static partial void LogResult(
        ILogger logger,
        int notifications,
        int escalations,
        int evidence,
        int correlations,
        int stale,
        long open);

    [LoggerMessage(EventId = 1802, Level = LogLevel.Error, Message = "Phase 18 security maintenance failed; the next idempotent run will retry")]
    private static partial void LogFailure(ILogger logger, Exception exception);
}
