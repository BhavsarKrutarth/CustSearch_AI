using System.Text.Json;
using CustSearch.Application.ReportsExports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustSearch.Infrastructure.ReportsExports;

/// <summary>Claims one durable export, queries through static stored procedures and writes a private artifact.</summary>
public sealed class ReportExportProcessor(IReportsExportsRepository repository,IReportArtifactStore artifacts,IOptions<ReportExportOptions>options,ILogger<ReportExportProcessor>logger):IReportExportProcessor
{
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web);
    private static readonly Action<ILogger,long,Exception?> ExportFailed=LoggerMessage.Define<long>(LogLevel.Error,new EventId(1501,nameof(ExportFailed)),"Report export job {ReportExportJobId} failed");
    private static readonly Action<ILogger,long,Exception?> MarkFailedFailed=LoggerMessage.Define<long>(LogLevel.Error,new EventId(1502,nameof(MarkFailedFailed)),"Unable to mark report export job {ReportExportJobId} failed");
    public async Task<bool>ProcessNextAsync(CancellationToken ct=default)
    {
        var job=await repository.ClaimAsync(options.Value.LeaseSeconds,ct).ConfigureAwait(false);if(job is null)return false;
        try
        {
            var filter=JsonSerializer.Deserialize<ReportFilter>(job.FilterJson,JsonOptions)??throw new InvalidOperationException("Stored report filter is invalid.");var requester=await repository.GetRequesterScopeAsync(job.TenantId,job.RequestedByUserId,job.ReportType,ct).ConfigureAwait(false);await repository.SetProgressAsync(job.Id,job.LeaseToken,10,ct).ConfigureAwait(false);ReportDataView data;if(job.TenantId is>0 and var tenantId)data=await repository.QueryTenantAsync(tenantId,requester.StoreIds,requester.TenantWide,job.ReportType,filter,10000,ct).ConfigureAwait(false);else data=await repository.QueryPlatformAsync(job.ReportType,filter,10000,ct).ConfigureAwait(false);await repository.SetProgressAsync(job.Id,job.LeaseToken,60,ct).ConfigureAwait(false);var artifact=await artifacts.WriteAsync(job.Id,job.Format,data,ct).ConfigureAwait(false);await repository.CompleteAsync(job.Id,job.LeaseToken,artifact,options.Value.RetentionHours,ct).ConfigureAwait(false);return true;
        }
        catch(OperationCanceledException)when(ct.IsCancellationRequested){throw;}
        catch(Exception exception){ExportFailed(logger,job.Id,exception);try{await repository.FailAsync(job.Id,job.LeaseToken,$"{exception.GetType().Name}: export generation failed.",ct).ConfigureAwait(false);}catch(Exception failure){MarkFailedFailed(logger,job.Id,failure);}return true;}
    }
}
