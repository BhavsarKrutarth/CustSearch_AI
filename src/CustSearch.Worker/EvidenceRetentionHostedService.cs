using CustSearch.Application.CamerasTracking;
using CustSearch.Application.Operations;
using Microsoft.Extensions.Options;

namespace CustSearch.Worker;

public sealed partial class EvidenceRetentionHostedService(
    IServiceScopeFactory scopes,
    IOptions<EvidenceRetentionOptions> options,
    ILogger<EvidenceRetentionHostedService> logger) : BackgroundService
{
    private readonly string ownerId = $"{Environment.MachineName}:{Environment.ProcessId}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var delay = TimeSpan.FromMinutes(settings.PollMinutes);
        while (!stoppingToken.IsCancellationRequested)
        {
            if (settings.Enabled)
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var gate = scope.ServiceProvider.GetRequiredService<IWorkerRuntimeGate>();
                    var lease = await gate.TryAcquireAsync(
                        "evidence-retention",
                        ownerId,
                        TimeSpan.FromSeconds(settings.LeaseSeconds),
                        stoppingToken);
                    if (lease is not null)
                    {
                        try
                        {
                            var result = await scope.ServiceProvider
                                .GetRequiredService<ICameraEvidenceMaintenanceProcessor>()
                                .RunOnceAsync(
                                    settings.CleanupBatchSize,
                                    settings.ReconciliationTenantBatchSize,
                                    TimeSpan.FromHours(settings.ReconciliationIntervalHours),
                                    stoppingToken);
                            LogResult(
                                logger,
                                result.ExpiredDeleted,
                                result.MissingDeleted,
                                result.TenantsReconciled,
                                result.Failed,
                                result.BytesReleased);
                        }
                        finally
                        {
                            await gate.ReleaseAsync(lease, stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    LogFailure(logger, exception);
                }
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    [LoggerMessage(
        EventId = 1810,
        Level = LogLevel.Information,
        Message = "Evidence retention completed: expired={Expired}, missing={Missing}, tenants={Tenants}, failed={Failed}, bytesReleased={BytesReleased}")]
    private static partial void LogResult(
        ILogger logger,
        int expired,
        int missing,
        int tenants,
        int failed,
        long bytesReleased);

    [LoggerMessage(EventId = 1811, Level = LogLevel.Error, Message = "Evidence retention cycle failed; the next idempotent cycle will retry")]
    private static partial void LogFailure(ILogger logger, Exception exception);
}
