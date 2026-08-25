using System.ComponentModel.DataAnnotations;
using CustSearch.API.ReportsExports;
using CustSearch.API.Security;
using CustSearch.Application.Authorization;
using CustSearch.Application.ReportsExports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.API.Controllers;

/// <summary>Tenant reports never accept TenantId; tenant/store scope comes from the authenticated session.</summary>
[ApiController]
[Route("api/tenant/reports")]
[Authorize(Policy=AuthorizationPolicyNames.TenantScope)]
[ServiceFilter<ReportExportExceptionFilter>]
public sealed class TenantReportsController(IReportsExportsService service):ControllerBase
{
    [HttpGet("catalog")][HasPermission(PermissionCatalog.Tenant.ReportsView)] public IReadOnlyList<ReportCatalogItem>Catalog()=>service.GetTenantCatalog();
    [HttpGet("preview")][HasPermission(PermissionCatalog.Tenant.ReportsView)] public Task<ReportDataView>Preview([FromQuery,Required,StringLength(100)]string reportType,[FromQuery]long?storeId,[FromQuery]DateTime?fromUtc,[FromQuery]DateTime?toUtc,CancellationToken ct)=>service.PreviewTenantAsync(reportType,new(storeId,null,fromUtc,toUtc),RequestContext(),ct);
    [HttpPost("exports")][HasPermission(PermissionCatalog.Tenant.ReportsExport)] public async Task<ActionResult<ReportExportJobView>>Queue(QueueReportExportRequest request,CancellationToken ct){var job=await service.QueueTenantAsync(request.ToTenantCommand(),RequestContext(),ct).ConfigureAwait(false);return Accepted($"/api/tenant/reports/exports/{job.Id}",Public(job));}
    [HttpGet("exports")][HasPermission(PermissionCatalog.Tenant.ReportsExport)] public async Task<IReadOnlyList<ReportExportJobView>>Jobs([FromQuery]ReportExportStatus?status=null,[FromQuery,Range(1,200)]int take=100,CancellationToken ct=default)=>(await service.ListTenantJobsAsync(status,take,ct).ConfigureAwait(false)).Select(Public).ToArray();
    [HttpGet("exports/{jobId:long}/download")][HasPermission(PermissionCatalog.Tenant.ReportsExport)] public async Task<IActionResult>Download(long jobId,CancellationToken ct){var file=await service.OpenTenantDownloadAsync(jobId,RequestContext(),ct).ConfigureAwait(false);return File(file.Content,file.ContentType,file.FileName,enableRangeProcessing:true);}
    internal static ReportExportJobView Public(ReportExportJobView job)=>job with{StorageReference=null};
    private ReportRequestContext RequestContext()=>new(HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),HttpContext.TraceIdentifier);
}

/// <summary>Platform reports require explicit platform scope and never grant tenant support access implicitly.</summary>
[ApiController]
[Route("api/platform/reports")]
[Authorize(Policy=AuthorizationPolicyNames.PlatformScope)]
[ServiceFilter<ReportExportExceptionFilter>]
public sealed class PlatformReportsController(IReportsExportsService service):ControllerBase
{
    [HttpGet("catalog")][HasPermission(PermissionCatalog.Platform.ReportsView)] public IReadOnlyList<ReportCatalogItem>Catalog()=>service.GetPlatformCatalog();
    [HttpGet("preview")][HasPermission(PermissionCatalog.Platform.ReportsView)] public Task<ReportDataView>Preview([FromQuery,Required,StringLength(100)]string reportType,[FromQuery]long?tenantId,[FromQuery]DateTime?fromUtc,[FromQuery]DateTime?toUtc,CancellationToken ct)=>service.PreviewPlatformAsync(reportType,new(null,tenantId,fromUtc,toUtc),RequestContext(),ct);
    [HttpPost("exports")][HasPermission(PermissionCatalog.Platform.ReportsExport)] public async Task<ActionResult<ReportExportJobView>>Queue(QueueReportExportRequest request,CancellationToken ct){var job=await service.QueuePlatformAsync(request.ToPlatformCommand(),RequestContext(),ct).ConfigureAwait(false);return Accepted($"/api/platform/reports/exports/{job.Id}",TenantReportsController.Public(job));}
    [HttpGet("exports")][HasPermission(PermissionCatalog.Platform.ReportsExport)] public async Task<IReadOnlyList<ReportExportJobView>>Jobs([FromQuery]ReportExportStatus?status=null,[FromQuery,Range(1,200)]int take=100,CancellationToken ct=default)=>(await service.ListPlatformJobsAsync(status,take,ct).ConfigureAwait(false)).Select(TenantReportsController.Public).ToArray();
    [HttpGet("exports/{jobId:long}/download")][HasPermission(PermissionCatalog.Platform.ReportsExport)] public async Task<IActionResult>Download(long jobId,CancellationToken ct){var file=await service.OpenPlatformDownloadAsync(jobId,RequestContext(),ct).ConfigureAwait(false);return File(file.Content,file.ContentType,file.FileName,enableRangeProcessing:true);}
    private ReportRequestContext RequestContext()=>new(HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),HttpContext.TraceIdentifier);
}

public sealed record QueueReportExportRequest(
    [param:Required,StringLength(100)]string ReportType,
    ReportExportFormat Format,
    long?StoreId,
    long?TenantId,
    DateTime?FromUtc,
    DateTime?ToUtc)
{
    public QueueReportExportCommand ToTenantCommand()=>new(ReportType,Format,new(StoreId,null,FromUtc,ToUtc));
    public QueueReportExportCommand ToPlatformCommand()=>new(ReportType,Format,new(null,TenantId,FromUtc,ToUtc));
}
