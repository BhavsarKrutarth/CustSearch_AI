using CustSearch.Application.ReportsExports;
using Microsoft.Extensions.Options;

namespace CustSearch.Worker;

public sealed class ExportWorkerOptions{public const string SectionName="ExportWorker";public int PollIntervalSeconds{get;set;}=5;public int BatchSize{get;set;}=10;}

/// <summary>Processes bounded export batches and retention cleanup with graceful cancellation.</summary>
public sealed class ExportJobsHostedService(IServiceScopeFactory scopes,IOptions<ExportWorkerOptions>options,ILogger<ExportJobsHostedService>logger):BackgroundService
{
    private readonly ExportWorkerOptions settings=options.Value;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken){while(!stoppingToken.IsCancellationRequested){try{await using var scope=scopes.CreateAsyncScope();var processor=scope.ServiceProvider.GetRequiredService<IExportJobProcessor>();var result=await processor.ProcessDueAsync(settings.BatchSize,stoppingToken).ConfigureAwait(false);var expired=await processor.ExpireDueAsync(settings.BatchSize*5,stoppingToken).ConfigureAwait(false);if(result.Claimed>0||expired>0)logger.LogInformation("Export worker processed {Claimed} jobs: {Completed} completed, {Failed} failed, {Expired} expired",result.Claimed,result.Completed,result.Failed,expired);}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}catch(Exception exception){logger.LogError(exception,"Export worker cycle failed");}await Task.Delay(TimeSpan.FromSeconds(settings.PollIntervalSeconds),stoppingToken).ConfigureAwait(false);}}
}
