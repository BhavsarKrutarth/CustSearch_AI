using System.ComponentModel.DataAnnotations;
using CustSearch.API.Operations;
using CustSearch.API.Security;
using CustSearch.Application.Authentication;
using CustSearch.Application.AlertsRealtime;
using CustSearch.Application.Authorization;
using CustSearch.Application.Operations;
using CustSearch.Application.TenantOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CustSearch.API.Controllers;

[ApiController]
[Route("api/platform/operations")]
[Authorize(Policy=AuthorizationPolicyNames.PlatformScope)]
[ServiceFilter<OperationalExceptionFilter>]
public sealed class PlatformOperationsController(IOperationalPlatformService service,ICurrentUserContext currentUser,IAlertConnectionMetrics realtimeMetrics,HealthCheckService healthChecks,IOptions<OperationalRedisOptions>redisOptions,IConfiguration configuration):ControllerBase
{
    [HttpGet("settings")][HasPermission(PermissionCatalog.Platform.SettingsView)] public Task<IReadOnlyList<SystemSettingView>>Settings(CancellationToken ct)=>service.ListPlatformSettingsAsync(ct);
    [HttpPut("settings/{settingKey}")][HasPermission(PermissionCatalog.Platform.SettingsManage)] public Task<SystemSettingView>SaveSetting([StringLength(100)]string settingKey,SaveSystemSettingRequest request,CancellationToken ct)=>service.SavePlatformSettingAsync(request.ToCommand(settingKey),Audit(),ct);
    [HttpGet("audit")][HasPermission(PermissionCatalog.Platform.AuditView)] public Task<AuditLogPage>AuditLogs([FromQuery]string?action=null,[FromQuery]string?entityType=null,[FromQuery]DateTime?fromUtc=null,[FromQuery]DateTime?toUtc=null,[FromQuery,Range(1,int.MaxValue)]int pageNumber=1,[FromQuery,Range(1,200)]int pageSize=50,CancellationToken ct=default)=>service.SearchPlatformAuditAsync(new(null,action,entityType,fromUtc,toUtc,pageNumber,pageSize),ct);
    [HttpGet("health")][HasPermission(PermissionCatalog.Platform.SystemHealthView)] public async Task<OperationalHealthResponse>Health(CancellationToken ct)
    {
        var core=await service.GetSystemHealthAsync(ct).ConfigureAwait(false);var redis=redisOptions.Value.Enabled?await healthChecks.CheckHealthAsync(x=>x.Name=="redis",ct).ConfigureAwait(false):null;var redisStatus=redis is null?"Disabled":redis.Status switch{HealthStatus.Healthy=>"Healthy",HealthStatus.Degraded=>"Warning",_=>"Offline"};var pythonConfigured=configuration.GetSection("CctvServices").GetChildren().Any();
        return new(core,"Healthy",redisStatus,redisOptions.Value.SignalRBackplaneEnabled?redisStatus:"Disabled","Healthy",realtimeMetrics.TotalActiveConnections(),realtimeMetrics.TotalReconnects(),pythonConfigured?"Warning":"Offline");
    }
    private TenantAuditContext Audit()=>new(currentUser.UserId,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),HttpContext.TraceIdentifier);
}

[ApiController]
[Route("api/tenant/operations")]
[Authorize(Policy=AuthorizationPolicyNames.TenantScope)]
[ServiceFilter<OperationalExceptionFilter>]
public sealed class TenantOperationsPlatformController(IOperationalPlatformService service,ICurrentUserContext currentUser):ControllerBase
{
    [HttpGet("settings")][HasPermission(PermissionCatalog.Operations.SettingsView)] public Task<IReadOnlyList<SystemSettingView>>Settings([FromQuery]long?storeId=null,[FromQuery]bool effective=true,CancellationToken ct=default)=>service.ListTenantSettingsAsync(storeId,effective,ct);
    [HttpPut("settings/{settingKey}")][HasPermission(PermissionCatalog.Operations.SettingsManage)] public Task<SystemSettingView>SaveSetting([StringLength(100)]string settingKey,SaveSystemSettingRequest request,[FromQuery]long?storeId=null,CancellationToken ct=default)=>service.SaveTenantSettingAsync(storeId,request.ToCommand(settingKey),Audit(),ct);
    [HttpGet("audit")][HasPermission(PermissionCatalog.Operations.AuditLogsView)] public Task<AuditLogPage>AuditLogs([FromQuery]long?storeId=null,[FromQuery]string?action=null,[FromQuery]string?entityType=null,[FromQuery]DateTime?fromUtc=null,[FromQuery]DateTime?toUtc=null,[FromQuery,Range(1,int.MaxValue)]int pageNumber=1,[FromQuery,Range(1,200)]int pageSize=50,CancellationToken ct=default)=>service.SearchTenantAuditAsync(new(storeId,action,entityType,fromUtc,toUtc,pageNumber,pageSize),ct);
    private TenantAuditContext Audit()=>new(currentUser.UserId,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),HttpContext.TraceIdentifier);
}

public sealed record SaveSystemSettingRequest(SystemSettingValueType ValueType,[param:Required,StringLength(1000)]string SettingValue,[param:StringLength(500)]string?Description)
{public SaveSystemSettingCommand ToCommand(string key)=>new(key,ValueType,SettingValue,Description);}
public sealed record OperationalHealthResponse(SystemHealthView Core,string ApiStatus,string RedisStatus,string RedisBackplaneStatus,string SignalRStatus,long ActiveWebSocketConnections,long WebSocketReconnects,string PythonAiStatus);
