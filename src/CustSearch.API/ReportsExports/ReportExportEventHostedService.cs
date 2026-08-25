using CustSearch.Application.ReportsExports;

namespace CustSearch.API.ReportsExports;

/// <summary>API-side relay lets the separate Worker publish durable progress through the live SignalR hub.</summary>
public sealed class ReportExportEventHostedService(IServiceScopeFactory scopes,ILogger<ReportExportEventHostedService>logger):BackgroundService
{
    private static readonly Action<ILogger,Exception?> CycleFailed=LoggerMessage.Define(LogLevel.Error,new EventId(1521,nameof(CycleFailed)),"Report export real-time relay cycle failed");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken){while(!stoppingToken.IsCancellationRequested){try{using var scope=scopes.CreateScope();var dispatcher=scope.ServiceProvider.GetRequiredService<IReportExportEventDispatcher>();if(await dispatcher.ProcessDueAsync(50,stoppingToken).ConfigureAwait(false)>0)continue;}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}catch(Exception exception){CycleFailed(logger,exception);}await Task.Delay(TimeSpan.FromSeconds(2),stoppingToken).ConfigureAwait(false);}}
}

