using CustSearch.Application.ReportsExports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustSearch.Infrastructure.ReportsExports;

/// <summary>Deletes only opaque, SQL-claimed expired report artifacts and then acknowledges each deletion.</summary>
public sealed class ReportExportMaintenance(IReportsExportsRepository repository,IReportArtifactStore artifacts,IOptions<ReportExportOptions>options,ILogger<ReportExportMaintenance>logger):IReportExportMaintenance
{
    private static readonly Action<ILogger,long,Exception?> DeleteFailed=LoggerMessage.Define<long>(LogLevel.Warning,new EventId(1520,nameof(DeleteFailed)),"Expired report artifact cleanup failed for job {ReportExportJobId}");
    public async Task<int>DeleteExpiredArtifactsAsync(CancellationToken ct=default)
    {
        var expired=await repository.ExpireArtifactsAsync(options.Value.CleanupBatchSize,ct).ConfigureAwait(false);var deleted=0;
        foreach(var item in expired)
        {
            try{await artifacts.DeleteAsync(item.StorageReference,ct).ConfigureAwait(false);await repository.AcknowledgeArtifactDeletedAsync(item.Id,item.StorageReference,ct).ConfigureAwait(false);deleted++;}
            catch(OperationCanceledException)when(ct.IsCancellationRequested){throw;}
            catch(Exception exception){DeleteFailed(logger,item.Id,exception);}
        }
        return deleted;
    }
}
