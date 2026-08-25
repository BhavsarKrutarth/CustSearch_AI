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
public sealed class CamerasTrackingController(ICameraTrackingService service,ICurrentUserContext currentUser,IConfiguration configuration,IHostEnvironment environment):ControllerBase
{
    [HttpGet][HasPermission(PermissionCatalog.Operations.CamerasView)]public Task<IReadOnlyList<CameraView>>List([FromQuery]long?storeId=null,CancellationToken ct=default)=>service.ListCamerasAsync(storeId,ct);
    [HttpPost][HasPermission(PermissionCatalog.Operations.CamerasManage)]public Task<CameraView>Create(SaveCameraRequest request,CancellationToken ct)=>service.SaveCameraAsync(null,request.Command(),Audit(),ct);
    [HttpPut("{cameraId:long}")][HasPermission(PermissionCatalog.Operations.CamerasManage)]public Task<CameraView>Update(long cameraId,SaveCameraRequest request,CancellationToken ct)=>service.SaveCameraAsync(cameraId,request.Command(),Audit(),ct);
    [HttpGet("{cameraId:long}/zones")][HasPermission(PermissionCatalog.Operations.CamerasView)]public Task<IReadOnlyList<CameraZoneView>>Zones(long cameraId,CancellationToken ct)=>service.ListZonesAsync(cameraId,ct);
    [HttpPost("{cameraId:long}/zones")][HasPermission(PermissionCatalog.Operations.CamerasManage)]public Task<CameraZoneView>AddZoneVersion(long cameraId,SaveCameraZoneRequest request,CancellationToken ct)=>service.AddZoneVersionAsync(cameraId,request.Command(),Audit(),ct);
    [HttpGet("tracks")][HasPermission(PermissionCatalog.Operations.CamerasView)]public Task<IReadOnlyList<PersonTrackView>>Tracks([FromQuery]long?storeId=null,[FromQuery]long?afterId=null,[FromQuery]int take=100,CancellationToken ct=default)=>service.ListTracksAsync(storeId,afterId,take,ct);
    [HttpPost("tracks/{trackId:long}/associate")][HasPermission(PermissionCatalog.Operations.CamerasManage)]public Task<PersonTrackView>Associate(long trackId,AssociateTrackRequest request,CancellationToken ct)=>service.AssociateAsync(trackId,new(request.SubjectKind,request.SubjectId),Audit(),ct);
    [HttpGet("capabilities")][HasPermission(PermissionCatalog.Operations.CamerasView)]public ActionResult<object>Capabilities()=>Ok(new{demoMode=configuration.GetValue<bool>("CctvRuntime:DemoMode"),environment=environment.EnvironmentName,identityRecognition=false,databaseAccessFromPython=false});
    private TenantAuditContext Audit()=>new(currentUser.UserId,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),HttpContext.TraceIdentifier);
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SaveCameraRequest([param:Range(1,long.MaxValue)]long StoreId,[param:Required,StringLength(50)]string CameraCode,[param:Required,StringLength(150)]string Name,[param:StringLength(200)]string?RtspConfigurationReference,[param:StringLength(250)]string?Location,CameraDirection Direction,bool IsActive){public SaveCameraCommand Command()=>new(StoreId,CameraCode,Name,RtspConfigurationReference,Location,Direction,IsActive);}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SaveCameraZoneRequest([param:Required,StringLength(50)]string ZoneCode,[param:Required,StringLength(150)]string Name,CameraZoneType ZoneType,[param:Required,StringLength(8000)]string GeometryJson,long?CategoryId){public SaveCameraZoneCommand Command()=>new(ZoneCode,Name,ZoneType,GeometryJson,CategoryId);}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AssociateTrackRequest(TrackingSubjectKind SubjectKind,[param:Range(1,long.MaxValue)]long SubjectId);
