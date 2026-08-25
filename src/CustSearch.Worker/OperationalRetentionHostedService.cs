using CustSearch.Application.Operations;
using CustSearch.Infrastructure.Operations;
using Microsoft.Extensions.Options;

namespace CustSearch.Worker;

/// <summary>Runs bounded consent/template and anonymous-visitor privacy retention.</summary>
public sealed class OperationalRetentionHostedService(IServiceScopeFactory scopes,IOptions<OperationalRetentionOptions>options,ILogger<OperationalRetentionHostedService>logger):BackgroundService
{
    private static readonly Action<ILogger,int,int,int,Exception?> Completed=LoggerMessage.Define<int,int,int>(LogLevel.Information,new EventId(1601,nameof(Completed)),"Retention cycle disabled {TemplatesDisabled} templates, marked {TemplatesDeleted} template records deleted and removed {VisitorsDeleted} anonymous visitors");
    private static readonly Action<ILogger,Exception?> Failed=LoggerMessage.Define(LogLevel.Error,new EventId(1602,nameof(Failed)),"Operational retention cycle failed");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay=TimeSpan.FromSeconds(Math.Clamp(options.Value.IntervalSeconds,30,86400));
        while(!stoppingToken.IsCancellationRequested)
        {
            try{using var scope=scopes.CreateScope();var maintenance=scope.ServiceProvider.GetRequiredService<IOperationalRetentionMaintenance>();var result=await maintenance.RunAsync(stoppingToken).ConfigureAwait(false);if(result.TemplatesDisabled+result.TemplatesMarkedDeleted+result.AnonymousVisitorsDeleted>0)Completed(logger,result.TemplatesDisabled,result.TemplatesMarkedDeleted,result.AnonymousVisitorsDeleted,null);}
            catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception exception){Failed(logger,exception);}
            await Task.Delay(delay,stoppingToken).ConfigureAwait(false);
        }
    }
}
