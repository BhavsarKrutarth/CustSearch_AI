using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
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

namespace CustSearch.API.Controllers;

[ApiController]
[Route("api/tenant/cameras")]
[Authorize(Policy=AuthorizationPolicyNames.TenantScope)]
[RejectClientTenantId]
[ServiceFilter(typeof(CameraTrackingExceptionFilter))]
public sealed class CamerasTrackingController(ICameraTrackingService service,ICameraPreviewService preview,ICameraMotionRuleService motion,ICurrentUserContext currentUser,IConfiguration configuration,IHostEnvironment environment):ControllerBase
{
    [HttpGet][HasPermission(PermissionCatalog.Operations.CamerasView)]public Task<IReadOnlyList<CameraView>>List([FromQuery]long?storeId=null,CancellationToken ct=default)=>service.ListCamerasAsync(storeId,ct);
    [HttpGet("quota")][HasPermission(PermissionCatalog.Operations.CamerasView)]public Task<CameraQuotaView>Quota(CancellationToken ct=default)=>service.GetCameraQuotaAsync(ct);
    [HttpPost][HasPermission(PermissionCatalog.Operations.CamerasManage)]public Task<CameraView>Create(SaveCameraRequest request,CancellationToken ct)=>service.SaveCameraAsync(null,request.Command(),Audit(),ct);
    [HttpPut("{cameraId:long}")][HasPermission(PermissionCatalog.Operations.CamerasManage)]public Task<CameraView>Update(long cameraId,SaveCameraRequest request,CancellationToken ct)=>service.SaveCameraAsync(cameraId,request.Command(),Audit(),ct);
    [HttpGet("{cameraId:long}/zones")][HasPermission(PermissionCatalog.Operations.CamerasView)]public Task<IReadOnlyList<CameraZoneView>>Zones(long cameraId,CancellationToken ct)=>service.ListZonesAsync(cameraId,ct);
    [HttpPost("{cameraId:long}/zones")][HasPermission(PermissionCatalog.Operations.CamerasManageZones)]public Task<CameraZoneView>AddZoneVersion(long cameraId,SaveCameraZoneRequest request,CancellationToken ct)=>service.AddZoneVersionAsync(cameraId,request.Command(),Audit(),ct);
    [HttpGet("tracks")][HasPermission(PermissionCatalog.Operations.CamerasView)]public Task<IReadOnlyList<PersonTrackView>>Tracks([FromQuery]long?storeId=null,[FromQuery]long?afterId=null,[FromQuery]int take=100,CancellationToken ct=default)=>service.ListTracksAsync(storeId,afterId,take,ct);
    [HttpPost("tracks/{trackId:long}/associate")][HasPermission(PermissionCatalog.Operations.CamerasManage)]public Task<PersonTrackView>Associate(long trackId,AssociateTrackRequest request,CancellationToken ct)=>service.AssociateAsync(trackId,new(request.SubjectKind,request.SubjectId),Audit(),ct);
    [HttpGet("capabilities")][HasPermission(PermissionCatalog.Operations.CamerasView)]public ActionResult<object>Capabilities()=>Ok(new{demoMode=configuration.GetValue<bool>("CctvRuntime:DemoMode"),previewEnabled=configuration.GetValue<bool>("CctvPreview:Enabled"),environment=environment.EnvironmentName,identityRecognition=false,databaseAccessFromPython=false});
    [HttpGet("{cameraId:long}/preview-grants")][HasPermission(PermissionCatalog.Operations.CamerasManage)]public Task<IReadOnlyList<CameraPreviewGrantView>>PreviewGrants(long cameraId,CancellationToken ct)=>preview.ListGrantsAsync(cameraId,ct);
    [HttpPut("{cameraId:long}/preview-grants/{userId:long}")][HasPermission(PermissionCatalog.Operations.CamerasManage)]public Task<CameraPreviewGrantView>SavePreviewGrant(long cameraId,long userId,SaveCameraPreviewGrantRequest request,CancellationToken ct)=>preview.SaveGrantAsync(cameraId,userId,request.Command(),Audit(),ct);
    [HttpDelete("{cameraId:long}/preview-grants/{userId:long}")][HasPermission(PermissionCatalog.Operations.CamerasManage)]public async Task<IActionResult>RemovePreviewGrant(long cameraId,long userId,CancellationToken ct){await preview.RemoveGrantAsync(cameraId,userId,Audit(),ct);return NoContent();}
    [HttpPost("{cameraId:long}/preview-sessions")][HasPermission(PermissionCatalog.Operations.CamerasPreview)]public Task<CameraPreviewSessionView>StartPreview(long cameraId,CancellationToken ct)=>preview.StartSessionAsync(cameraId,Audit(),ct);
    [HttpGet("{cameraId:long}/preview-sessions/{sessionId:guid}/frame")][HasPermission(PermissionCatalog.Operations.CamerasPreview)]public async Task<IActionResult>PreviewFrame(long cameraId,Guid sessionId,CancellationToken ct){var frame=await preview.GetFrameAsync(cameraId,sessionId,ct);Response.Headers.CacheControl="no-store, no-cache, max-age=0";Response.Headers.Pragma="no-cache";Response.Headers["X-Frame-Captured-Utc"]=frame.CapturedUtc.ToString("O");Response.Headers["X-Frame-Width"]=frame.Width.ToString(System.Globalization.CultureInfo.InvariantCulture);Response.Headers["X-Frame-Height"]=frame.Height.ToString(System.Globalization.CultureInfo.InvariantCulture);return File(frame.Content,frame.ContentType);}
    [HttpDelete("{cameraId:long}/preview-sessions/{sessionId:guid}")][HasPermission(PermissionCatalog.Operations.CamerasPreview)]public async Task<IActionResult>EndPreview(long cameraId,Guid sessionId,CancellationToken ct){await preview.EndSessionAsync(cameraId,sessionId,Audit(),ct);return NoContent();}
    [HttpGet("motion-rule-catalog")][HasPermission(PermissionCatalog.Operations.CamerasView)]public IReadOnlyList<MotionRuleCatalogItem>MotionRuleCatalog()=>motion.GetCatalog();
    [HttpGet("{cameraId:long}/motion-settings")][HasPermission(PermissionCatalog.Operations.CamerasView)]public Task<CameraMotionSettingsView>MotionSettings(long cameraId,CancellationToken ct)=>motion.GetSettingsAsync(cameraId,ct);
    [HttpPut("{cameraId:long}/motion-settings")][HasPermission(PermissionCatalog.Operations.CamerasManageRules)]public Task<CameraMotionSettingsView>SaveMotionSettings(long cameraId,SaveCameraMotionSettingsRequest request,CancellationToken ct)=>motion.SetSettingsAsync(cameraId,request.Enabled,Audit(),ct);
    [HttpPut("{cameraId:long}/detection-zone-setting")][HasPermission(PermissionCatalog.Operations.CamerasManageZones)]public Task<CameraMotionSettingsView>SaveDetectionZoneSetting(long cameraId,SaveCameraDetectionZoneSettingRequest request,CancellationToken ct)=>motion.SetDetectionZoneAsync(cameraId,request.Enabled,Audit(),ct);
    [HttpGet("{cameraId:long}/motion-rules")][HasPermission(PermissionCatalog.Operations.CamerasView)]public Task<IReadOnlyList<CameraMotionRuleView>>MotionRules(long cameraId,CancellationToken ct)=>motion.ListAsync(cameraId,ct);
    [HttpPost("{cameraId:long}/motion-rules")][HasPermission(PermissionCatalog.Operations.CamerasManageRules)]public Task<CameraMotionRuleView>CreateMotionRule(long cameraId,SaveCameraMotionRuleRequest request,CancellationToken ct)=>motion.SaveAsync(cameraId,null,request.Command(),Audit(),ct);
    [HttpPut("{cameraId:long}/motion-rules/{ruleId:long}")][HasPermission(PermissionCatalog.Operations.CamerasManageRules)]public Task<CameraMotionRuleView>UpdateMotionRule(long cameraId,long ruleId,SaveCameraMotionRuleRequest request,CancellationToken ct)=>motion.SaveAsync(cameraId,ruleId,request.Command(),Audit(),ct);
    private TenantAuditContext Audit()=>new(currentUser.UserId,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),HttpContext.TraceIdentifier);
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SaveCameraRequest([param:Range(1,long.MaxValue)]long StoreId,[param:Required,StringLength(50)]string CameraCode,[param:Required,StringLength(150)]string Name,[param:StringLength(200)]string?RtspConfigurationReference,[param:StringLength(250)]string?Location,CameraDirection Direction,bool IsActive){public SaveCameraCommand Command()=>new(StoreId,CameraCode,Name,RtspConfigurationReference,Location,Direction,IsActive);}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SaveCameraZoneRequest([param:Required,StringLength(50)]string ZoneCode,[param:Required,StringLength(150)]string Name,CameraZoneType ZoneType,[param:Required,StringLength(4000)]string GeometryJson,long?CategoryId){public SaveCameraZoneCommand Command()=>new(ZoneCode,Name,ZoneType,GeometryJson,CategoryId);}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AssociateTrackRequest(TrackingSubjectKind SubjectKind,[param:Range(1,long.MaxValue)]long SubjectId);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SaveCameraPreviewGrantRequest(bool CanViewLive,bool CanViewTracking,bool CanControl,DateTime?ValidUntilUtc,bool IsActive){public SaveCameraPreviewGrantCommand Command()=>new(CanViewLive,CanViewTracking,CanControl,ValidUntilUtc,IsActive);}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]public sealed record SaveCameraMotionSettingsRequest(bool Enabled);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]public sealed record SaveCameraDetectionZoneSettingRequest(bool Enabled);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SaveCameraMotionRuleRequest([param:Required,StringLength(100)]string RuleCode,[param:Required,StringLength(150)]string RuleName,bool IsEnabled,[param:Range(typeof(decimal),"0","1")]decimal MinimumConfidence,[param:Range(1,100)]int Sensitivity,[param:Range(0,86400)]int MinimumDurationSeconds,[param:Range(0,86400)]int CooldownSeconds,TimeOnly?StartTime,TimeOnly?EndTime,[param:Required,StringLength(27)]string DaysOfWeek,bool EvidenceSnapshotEnabled,bool EvidenceClipEnabled,[param:Range(0,300)]int EvidencePreEventSeconds,[param:Range(0,300)]int EvidencePostEventSeconds,AlertSeverity Severity,bool CreateAlert,bool RealtimeNotificationEnabled,long?ZoneId){public SaveCameraMotionRuleCommand Command()=>new(RuleCode,RuleName,IsEnabled,MinimumConfidence,Sensitivity,MinimumDurationSeconds,CooldownSeconds,StartTime,EndTime,DaysOfWeek,EvidenceSnapshotEnabled,EvidenceClipEnabled,EvidencePreEventSeconds,EvidencePostEventSeconds,Severity,CreateAlert,RealtimeNotificationEnabled,ZoneId);}
