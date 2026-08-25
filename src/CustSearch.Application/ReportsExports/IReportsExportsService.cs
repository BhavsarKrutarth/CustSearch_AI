namespace CustSearch.Application.ReportsExports;

/// <summary>Phase 15 report and export boundary. Tenant and requester identity are always server-derived.</summary>
public interface IReportsExportsService
{
    IReadOnlyList<ReportCatalogItem> GetTenantCatalog();
    IReadOnlyList<ReportCatalogItem> GetPlatformCatalog();
    Task<ReportDataView> PreviewTenantAsync(string type,ReportFilter filter,ReportRequestContext request,CancellationToken ct=default);
    Task<ReportDataView> PreviewPlatformAsync(string type,ReportFilter filter,ReportRequestContext request,CancellationToken ct=default);
    Task<ReportExportJobView> QueueTenantAsync(QueueReportExportCommand command,ReportRequestContext request,CancellationToken ct=default);
    Task<ReportExportJobView> QueuePlatformAsync(QueueReportExportCommand command,ReportRequestContext request,CancellationToken ct=default);
    Task<IReadOnlyList<ReportExportJobView>> ListTenantJobsAsync(ReportExportStatus? status,int take,CancellationToken ct=default);
    Task<IReadOnlyList<ReportExportJobView>> ListPlatformJobsAsync(ReportExportStatus? status,int take,CancellationToken ct=default);
    Task<ReportExportDownload> OpenTenantDownloadAsync(long id,ReportRequestContext request,CancellationToken ct=default);
    Task<ReportExportDownload> OpenPlatformDownloadAsync(long id,ReportRequestContext request,CancellationToken ct=default);
}

public interface IReportsExportsRepository
{
    Task<ReportDataView> QueryTenantAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,string reportType,ReportFilter filter,int take,CancellationToken ct=default);
    Task<ReportDataView> QueryPlatformAsync(string reportType,ReportFilter filter,int take,CancellationToken ct=default);
    Task<ReportExportJobView> CreateJobAsync(long?tenantId,long requesterId,string reportType,string filterJson,ReportExportFormat format,ReportRequestContext request,CancellationToken ct=default);
    Task<IReadOnlyList<ReportExportJobView>> ListJobsAsync(long?tenantId,long requesterId,bool platform,ReportExportStatus?status,int take,CancellationToken ct=default);
    Task<ReportExportJobView?> GetJobAsync(long jobId,long?tenantId,long requesterId,bool platform,CancellationToken ct=default);
    Task<ClaimedReportExportJob?> ClaimAsync(int leaseSeconds,CancellationToken ct=default);
    Task SetProgressAsync(long jobId,Guid leaseToken,byte progress,CancellationToken ct=default);
    Task CompleteAsync(long jobId,Guid leaseToken,ReportArtifactMetadata a,int retentionHours,CancellationToken ct=default);
    Task FailAsync(long jobId,Guid leaseToken,string safeError,CancellationToken ct=default);
    Task<IReadOnlyList<ExpiredReportArtifact>> ExpireArtifactsAsync(int take,CancellationToken ct=default);
    Task AcknowledgeArtifactDeletedAsync(long jobId,string storageReference,CancellationToken ct=default);
    Task<ReportRequesterScope> GetRequesterScopeAsync(long?tenantId,long requesterId,string reportType,CancellationToken ct=default);
    Task RecordAuditAsync(long?tenantId,long?storeId,long actorUserId,string action,string entityType,string?entityId,string afterJson,ReportRequestContext request,CancellationToken ct=default);
}

/// <summary>Worker entry point. It has no HTTP identity and can only process atomically claimed jobs.</summary>
public interface IReportExportProcessor
{
    Task<bool> ProcessNextAsync(CancellationToken ct=default);
}

public interface IReportExportEventDispatcher
{
    Task<int> ProcessDueAsync(int take=50,CancellationToken ct=default);
}

public interface IReportExportMaintenance
{
    Task<int> DeleteExpiredArtifactsAsync(CancellationToken ct=default);
}

public interface IReportExportRealtimePublisher
{
    Task PublishAsync(ReportExportRealtimeEvent message,CancellationToken ct=default);
}

public interface IReportArtifactStore
{
    Task<ReportArtifactMetadata> WriteAsync(long jobId,ReportExportFormat format,ReportDataView data,CancellationToken ct=default);
    Task<Stream> OpenReadAsync(string storageReference,CancellationToken ct=default);
    Task DeleteAsync(string storageReference,CancellationToken ct=default);
}

public enum ReportExportFormat:byte { Csv=1,Excel=2,Pdf=3 }
public enum ReportExportStatus:byte { Queued=1,Processing=2,Completed=3,Failed=4,Expired=5 }

public sealed record ReportCatalogItem(string ReportType,string Name,string Description,string RequiredPermission,bool SupportsStoreFilter,bool SupportsDateFilter);
public sealed record ReportFilter(long?StoreId=null,long?TenantId=null,DateTime?FromUtc=null,DateTime?ToUtc=null);
public sealed record QueueReportExportCommand(string ReportType,ReportExportFormat Format,ReportFilter Filter);
public sealed record ReportRequestContext(string?IpAddress,string?UserAgent,string CorrelationId);
public sealed record ReportDataView(IReadOnlyList<string>Columns,IReadOnlyList<IReadOnlyDictionary<string,object?>>Rows);
public sealed record ReportExportJobView(long Id,long?TenantId,long RequestedByUserId,string ReportType,ReportExportFormat Format,ReportExportStatus Status,byte ProgressPercent,string?StorageReference,string?DownloadFileName,string?ContentType,long?ContentLength,string?Sha256,string?ErrorMessage,DateTime RequestedUtc,DateTime?StartedUtc,DateTime?CompletedUtc,DateTime?ExpiresUtc,int AttemptCount);
public sealed record ClaimedReportExportJob(long Id,long?TenantId,long RequestedByUserId,string ReportType,string FilterJson,ReportExportFormat Format,int AttemptCount,Guid LeaseToken,DateTime RequestedUtc);
public sealed record ReportArtifactMetadata(string StorageReference,string DownloadFileName,string ContentType,long ContentLength,string Sha256);
public sealed record ExpiredReportArtifact(long Id,string StorageReference);
public sealed record ReportExportDownload(Stream Content,string FileName,string ContentType,long ContentLength);
public sealed record ReportRequesterScope(bool TenantWide,IReadOnlyCollection<long>StoreIds);
public sealed record ReportExportRealtimeEvent(long EventId,long JobId,long?TenantId,long RequestedByUserId,string EventType,ReportExportStatus Status,byte ProgressPercent,DateTime OccurredUtc);

public sealed class ReportExportBusinessRuleException(string message):Exception(message);
public sealed class ReportExportNotFoundException(string message):Exception(message);
