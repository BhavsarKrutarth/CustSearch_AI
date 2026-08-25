using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;

namespace CustSearch.Application.ReportsExports;

public interface IReportsExportsService
{
    IReadOnlyList<ReportCatalogItem>Catalog();
    Task<ReportResultView>RunAsync(ReportType reportType,ReportFilter filter,CancellationToken ct=default);
    Task<ExportJobView>QueueAsync(QueueExportCommand command,TenantAuditContext audit,CancellationToken ct=default);
    Task<IReadOnlyList<ExportJobView>>ListJobsAsync(CancellationToken ct=default);
    Task<ExportJobView>GetJobAsync(long jobId,CancellationToken ct=default);
    Task<ExportJobView>RetryAsync(long jobId,TenantAuditContext audit,CancellationToken ct=default);
    Task<ExportDownloadTicketView>CreateDownloadTicketAsync(long jobId,TenantAuditContext audit,CancellationToken ct=default);
    Task<ExportDownloadFile>OpenDownloadAsync(long jobId,string token,TenantAuditContext audit,CancellationToken ct=default);
}

public interface IReportQueryRepository
{
    Task<ReportResultView>QueryTenantAsync(long tenantId,ReportType reportType,long[]authorizedStoreIds,ReportFilter filter,CancellationToken ct=default);
    Task<ReportResultView>QueryPlatformAsync(ReportType reportType,ReportFilter filter,CancellationToken ct=default);
}

public interface IExportJobProcessor{Task<ExportProcessResult>ProcessDueAsync(int batchSize=10,CancellationToken ct=default);Task<int>ExpireDueAsync(int batchSize=50,CancellationToken ct=default);}
public interface IExportFileStore{Task<StoredExportFile>SaveAsync(long jobId,ExportFormat format,ReportResultView report,CancellationToken ct=default);Task<Stream>OpenReadAsync(string path,CancellationToken ct=default);Task DeleteAsync(string path,CancellationToken ct=default);}
public interface IExportDownloadTokenService{ExportDownloadTicketView Create(long jobId,long requestedByUserId,long?tenantId,DateTime expiresUtc);void Validate(string token,long jobId,long requestedByUserId,long?tenantId,DateTime utcNow);}

public sealed record ReportCatalogItem(ReportType ReportType,string Code,string Name,ReportScope Scope,string RequiredPermission,bool SupportsPaging);
public sealed record ReportFilter(DateTime FromUtc,DateTime ToUtc,long[]StoreIds,int Page=1,int PageSize=100)
{
    public ReportFilter Normalize(){if(FromUtc.Kind!=DateTimeKind.Utc||ToUtc.Kind!=DateTimeKind.Utc||FromUtc>=ToUtc)throw new ReportExportException("A valid UTC date range is required.",ReportExportFailureKind.Validation);if(ToUtc-FromUtc>TimeSpan.FromDays(366))throw new ReportExportException("Report range cannot exceed 366 days.",ReportExportFailureKind.Validation);if(Page<1||PageSize is<1 or>500)throw new ReportExportException("Report paging is invalid.",ReportExportFailureKind.Validation);var stores=(StoreIds??[]).Where(x=>x>0).Distinct().OrderBy(x=>x).ToArray();return this with{StoreIds=stores};}
}
public sealed record QueueExportCommand(ReportType ReportType,ExportFormat Format,ReportFilter Filter);
public sealed record ReportDataRow(string Domain,long?StoreId,string Metric,decimal Value,string?Label,DateTime?OccurredUtc);
public sealed record ReportResultView(ReportType ReportType,DateTime FromUtc,DateTime ToUtc,int Page,int PageSize,long TotalRows,IReadOnlyList<ReportDataRow>Rows);
public sealed record ExportJobView(long Id,long?TenantId,long RequestedByUserId,ReportType ReportType,ExportFormat Format,ExportJobStatus Status,byte Progress,DateTime CreatedUtc,DateTime?StartedUtc,DateTime?CompletedUtc,DateTime ExpiresUtc,string?Error,int AttemptCount,bool CanDownload);
public sealed record ExportDownloadTicketView(string Token,DateTime ExpiresUtc);
public sealed record ExportDownloadFile(Stream Content,string ContentType,string FileName);
public sealed record StoredExportFile(string Path,string FileName,string ContentType);
public sealed record ExportProcessResult(int Claimed,int Completed,int Failed);

public sealed class ReportExportException(string message,ReportExportFailureKind kind):Exception(message){public ReportExportFailureKind Kind{get;}=kind;}
public enum ReportExportFailureKind{Validation,Forbidden,NotFound,Conflict,Unavailable}

public sealed class ReportsExportsOptions
{
    public const string SectionName="ReportsExports";public string StoragePath{get;set;}="App_Data/exports";public int RetentionHours{get;set;}=24;public int DownloadTicketMinutes{get;set;}=5;public string DownloadSigningKey{get;set;}=string.Empty;public int MaximumExportRows{get;set;}=50000;public int LeaseSeconds{get;set;}=120;
    public bool IsValid(bool requireSecret)=>RetentionHours is>=1 and<=720&&DownloadTicketMinutes is>=1 and<=30&&MaximumExportRows is>=100 and<=100000&&LeaseSeconds is>=30 and<=900&&(!requireSecret||DownloadSigningKey.Length>=32)&&!string.IsNullOrWhiteSpace(StoragePath);
}
