using System.ComponentModel.DataAnnotations;
using CustSearch.API.AlertsRealtime;
using CustSearch.API.CamerasTracking;
using CustSearch.API.Security;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.CamerasTracking;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CustSearch.API.Controllers;

[ApiController][Route("api/tenant/storage")][Authorize(Policy=AuthorizationPolicyNames.TenantScope)][RejectClientTenantId][ServiceFilter(typeof(CameraTrackingExceptionFilter))]
public sealed class TenantCameraStorageController(ICameraEvidenceStorageService service):ControllerBase
{
    [HttpGet][HasPermission(PermissionCatalog.Operations.StorageViewUsage)]public Task<TenantStorageSummaryView>Summary(CancellationToken ct)=>service.GetTenantStorageAsync(ct);
    [HttpGet("policy")][HasPermission(PermissionCatalog.Operations.StorageViewUsage)]public async Task<TenantStoragePolicyView>Policy(CancellationToken ct)=>(await service.GetTenantStorageAsync(ct)).Policy;
    [HttpGet("usage")][HasPermission(PermissionCatalog.Operations.StorageViewUsage)]public async Task<TenantStorageUsageView>Usage(CancellationToken ct)=>(await service.GetTenantStorageAsync(ct)).Usage;
    [HttpGet("evidence")][HasPermission(PermissionCatalog.Operations.CamerasViewEvents)]public Task<IReadOnlyList<CameraEvidenceView>>Evidence([FromQuery]long?storeId,[FromQuery]long?cameraId,[FromQuery]int take=100,CancellationToken ct=default)=>service.ListEvidenceAsync(storeId,cameraId,take,ct);
}

[ApiController][Route("api/platform/tenants/{tenantId:long}/storage")][Authorize(Policy=AuthorizationPolicyNames.PlatformScope)][ServiceFilter(typeof(CameraTrackingExceptionFilter))]
public sealed class PlatformTenantStorageController(ICameraEvidenceStorageService service,ICurrentUserContext user):ControllerBase
{
    [HttpGet][HasPermission(PermissionCatalog.Platform.TenantsViewUsage)]public Task<TenantStorageSummaryView>Get(long tenantId,CancellationToken ct)=>service.GetPlatformStorageAsync(tenantId,ct);
    [HttpPut("policy")][HasPermission(PermissionCatalog.Platform.TenantStorageManage)]public Task<TenantStorageSummaryView>Save(long tenantId,SaveTenantStoragePolicyRequest request,CancellationToken ct)=>service.SavePlatformPolicyAsync(tenantId,request.Command(),Audit(),ct);
    private TenantAuditContext Audit()=>new(user.UserId,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),HttpContext.TraceIdentifier);
}

public sealed record SaveTenantStoragePolicyRequest(bool StorageEnabled,[param:Range(1048576L,10995116277760L)]long StorageQuotaBytes,[param:Range(1,3650)]int DefaultRetentionDays,[param:Range(1,3650)]int MotionSnapshotRetentionDays,[param:Range(1,3650)]int MotionClipRetentionDays,[param:Range(1,3650)]int FalsePositiveRetentionDays,[param:Range(1,3650)]int UnreviewedEvidenceRetentionDays,[param:Range(1,3650)]int ConfirmedIncidentRetentionDays,[param:Range(1,99)]int WarningPercent,[param:Range(2,100)]int CriticalPercent,bool AllowSnapshots,bool AllowMotionClips,bool AutoCleanupEnabled,QuotaPressurePolicy QuotaPressurePolicy){public SaveTenantStoragePolicyCommand Command()=>new(StorageEnabled,StorageQuotaBytes,DefaultRetentionDays,MotionSnapshotRetentionDays,MotionClipRetentionDays,FalsePositiveRetentionDays,UnreviewedEvidenceRetentionDays,ConfirmedIncidentRetentionDays,WarningPercent,CriticalPercent,AllowSnapshots,AllowMotionClips,AutoCleanupEnabled,QuotaPressurePolicy);}

[ApiController][Route("api/internal/cctv/evidence")][AllowAnonymous][EnableRateLimiting("cctv-inbound")][ServiceFilter(typeof(CameraTrackingExceptionFilter))]
public sealed class CctvEvidenceController(ICameraEvidenceStorageService service):ControllerBase
{
    public const int HardMaximumBodyBytes=104857600;
    [HttpPost][Consumes("image/jpeg","image/png","video/mp4","application/octet-stream")][RequestSizeLimit(HardMaximumBodyBytes)]public async Task<ActionResult<CameraEvidenceIngestionResult>>Upload(CancellationToken ct){await using var body=new MemoryStream();await Request.Body.CopyToAsync(body,ct);if(body.Length>HardMaximumBodyBytes)return StatusCode(413,new{message="Evidence body is too large."});var envelope=new CctvEvidenceEnvelope(Header("X-CustSearch-Service-Id"),Header("X-CustSearch-Timestamp"),Header("X-CustSearch-Signature"),Header("X-CustSearch-Event-Id"),Header("Idempotency-Key"),Header("X-CustSearch-Evidence-Metadata"),body.ToArray(),HttpContext.TraceIdentifier);return Ok(await service.IngestAsync(envelope,ct));}
    private string Header(string name)=>Request.Headers[name].FirstOrDefault()??string.Empty;
}
