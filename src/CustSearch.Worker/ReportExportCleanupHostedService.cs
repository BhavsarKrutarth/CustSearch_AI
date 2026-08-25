using CustSearch.Application.ReportsExports;
using CustSearch.Infrastructure.ReportsExports;
using Microsoft.Extensions.Options;

namespace CustSearch.Worker;

/// <summary>Runs bounded, idempotent report artifact retention cleanup.</summary>
public sealed class ReportExportCleanupHostedService(IServiceScopeFactory scopes,IOptions<ReportExportOptions>options,ILogger<ReportExportCleanupHostedService>logger):BackgroundService
{
    private static readonly Action<ILogger,int,Exception?> Completed=LoggerMessage.Define<int>(LogLevel.Information,new EventId(1521,nameof(Completed)),"Deleted {DeletedCount} expired report artifacts");
    private static readonly Action<ILogger,Exception?> Failed=LoggerMessage.Define(LogLevel.Error,new EventId(1522,nameof(Failed)),"Report artifact retention cycle failed");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay=TimeSpan.FromSeconds(Math.Clamp(options.Value.CleanupIntervalSeconds,10,3600));
        while(!stoppingToken.IsCancellationRequested)
        {
            try{using var scope=scopes.CreateScope();var maintenance=scope.ServiceProvider.GetRequiredService<IReportExportMaintenance>();var count=await maintenance.DeleteExpiredArtifactsAsync(stoppingToken).ConfigureAwait(false);if(count>0)Completed(logger,count,null);}
            catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception exception){Failed(logger,exception);}
            await Task.Delay(delay,stoppingToken).ConfigureAwait(false);
        }
    }
}
