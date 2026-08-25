using CustSearch.Application.ReportsExports;
using CustSearch.Infrastructure.ReportsExports;
using Microsoft.Extensions.Options;

namespace CustSearch.Worker;

/// <summary>Continuously drains durable report jobs; multiple instances are safe because SQL owns leases.</summary>
public sealed class ReportExportHostedService(IServiceScopeFactory scopes,IOptions<ReportExportOptions>options,ILogger<ReportExportHostedService>logger):BackgroundService
{
    private static readonly Action<ILogger,Exception?> CycleFailed=LoggerMessage.Define(LogLevel.Error,new EventId(1510,nameof(CycleFailed)),"Report export worker cycle failed");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay=TimeSpan.FromSeconds(Math.Clamp(options.Value.PollIntervalSeconds,1,60));
        while(!stoppingToken.IsCancellationRequested)
        {
            try{using var scope=scopes.CreateScope();var processor=scope.ServiceProvider.GetRequiredService<IReportExportProcessor>();if(await processor.ProcessNextAsync(stoppingToken).ConfigureAwait(false))continue;}
            catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception exception){CycleFailed(logger,exception);}
            await Task.Delay(delay,stoppingToken).ConfigureAwait(false);
        }
    }
}

