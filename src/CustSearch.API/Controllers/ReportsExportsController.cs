using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CustSearch.API.AlertsRealtime;
using CustSearch.API.ReportsExports;
using CustSearch.API.Security;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.ReportsExports;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.API.Controllers;

[ApiController][Route("api/tenant/reports")][Authorize(Policy=AuthorizationPolicyNames.TenantScope)][RejectClientTenantId][ServiceFilter(typeof(ReportExportExceptionFilter))]
public sealed class ReportsExportsController(IReportsExportsService service,ICurrentUserContext currentUser):ControllerBase
{
    [HttpGet("catalog")][HasPermission(PermissionCatalog.Operations.ReportsView)]public IReadOnlyList<ReportCatalogItem>Catalog()=>service.Catalog();
    [HttpPost("run/{reportType}")][HasPermission(PermissionCatalog.Operations.ReportsView)]public Task<ReportResultView>Run(ReportType reportType,ReportFilterRequest request,CancellationToken ct)=>service.RunAsync(reportType,request.Domain(),ct);
    [HttpPost("exports")][HasPermission(PermissionCatalog.Operations.ReportsExport)]public Task<ExportJobView>Queue(QueueExportRequest request,CancellationToken ct)=>service.QueueAsync(new(request.ReportType,request.Format,request.Filter.Domain()),Audit(),ct);
    [HttpGet("exports")][HasPermission(PermissionCatalog.Operations.ReportsView)]public Task<IReadOnlyList<ExportJobView>>Jobs(CancellationToken ct)=>service.ListJobsAsync(ct);
    [HttpGet("exports/{jobId:long}")][HasPermission(PermissionCatalog.Operations.ReportsView)]public Task<ExportJobView>Job(long jobId,CancellationToken ct)=>service.GetJobAsync(jobId,ct);
    [HttpPost("exports/{jobId:long}/retry")][HasPermission(PermissionCatalog.Operations.ReportsExport)]public Task<ExportJobView>Retry(long jobId,CancellationToken ct)=>service.RetryAsync(jobId,Audit(),ct);
    [HttpPost("exports/{jobId:long}/download-ticket")][HasPermission(PermissionCatalog.Operations.ReportsExport)]public Task<ExportDownloadTicketView>Ticket(long jobId,CancellationToken ct)=>service.CreateDownloadTicketAsync(jobId,Audit(),ct);
    [HttpGet("exports/{jobId:long}/download")][HasPermission(PermissionCatalog.Operations.ReportsExport)]public async Task<IActionResult>Download(long jobId,[FromQuery,Required]string token,CancellationToken ct){var file=await service.OpenDownloadAsync(jobId,token,Audit(),ct).ConfigureAwait(false);return File(file.Content,file.ContentType,file.FileName);}
    private TenantAuditContext Audit()=>new(currentUser.UserId,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),HttpContext.TraceIdentifier);
}

[ApiController][Route("api/platform/reports")][Authorize(Policy=AuthorizationPolicyNames.PlatformScope)][ServiceFilter(typeof(ReportExportExceptionFilter))]
public sealed class PlatformReportsExportsController(IReportsExportsService service,ICurrentUserContext currentUser):ControllerBase
{
    [HttpGet("catalog")][HasPermission(PermissionCatalog.Platform.ReportsView)]public IReadOnlyList<ReportCatalogItem>Catalog()=>service.Catalog();
    [HttpPost("run/{reportType}")][HasPermission(PermissionCatalog.Platform.ReportsView)]public Task<ReportResultView>Run(ReportType reportType,ReportFilterRequest request,CancellationToken ct)=>service.RunAsync(reportType,request.Domain(),ct);
    [HttpPost("exports")][HasPermission(PermissionCatalog.Platform.ReportsExport)]public Task<ExportJobView>Queue(QueueExportRequest request,CancellationToken ct)=>service.QueueAsync(new(request.ReportType,request.Format,request.Filter.Domain()),Audit(),ct);
    [HttpGet("exports")][HasPermission(PermissionCatalog.Platform.ReportsView)]public Task<IReadOnlyList<ExportJobView>>Jobs(CancellationToken ct)=>service.ListJobsAsync(ct);
    [HttpPost("exports/{jobId:long}/retry")][HasPermission(PermissionCatalog.Platform.ReportsExport)]public Task<ExportJobView>Retry(long jobId,CancellationToken ct)=>service.RetryAsync(jobId,Audit(),ct);
    [HttpPost("exports/{jobId:long}/download-ticket")][HasPermission(PermissionCatalog.Platform.ReportsExport)]public Task<ExportDownloadTicketView>Ticket(long jobId,CancellationToken ct)=>service.CreateDownloadTicketAsync(jobId,Audit(),ct);
    [HttpGet("exports/{jobId:long}/download")][HasPermission(PermissionCatalog.Platform.ReportsExport)]public async Task<IActionResult>Download(long jobId,[FromQuery,Required]string token,CancellationToken ct){var file=await service.OpenDownloadAsync(jobId,token,Audit(),ct).ConfigureAwait(false);return File(file.Content,file.ContentType,file.FileName);}
    private TenantAuditContext Audit()=>new(currentUser.UserId,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),HttpContext.TraceIdentifier);
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]public sealed record ReportFilterRequest(DateTime FromUtc,DateTime ToUtc,long[]StoreIds,int Page=1,int PageSize=100){public ReportFilter Domain()=>new(FromUtc,ToUtc,StoreIds??[],Page,PageSize);}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]public sealed record QueueExportRequest(ReportType ReportType,ExportFormat Format,[property:Required]ReportFilterRequest Filter);
