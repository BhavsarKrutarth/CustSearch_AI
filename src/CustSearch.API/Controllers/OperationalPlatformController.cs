using System.Text.Json.Serialization;
using CustSearch.API.Operations;
using CustSearch.API.Security;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.Operations;
using CustSearch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.API.Controllers;

[ApiController][Route("api/platform/operations")][Authorize(Policy=AuthorizationPolicyNames.PlatformScope)][ServiceFilter(typeof(OperationalExceptionFilter))]
public sealed class OperationalPlatformController(IOperationalPlatformService service):ControllerBase
{
    [HttpGet("health")][HasPermission(PermissionCatalog.Platform.OperationsView)]public Task<OperationalHealthView>Health(CancellationToken ct)=>service.HealthAsync(ct);
    [HttpGet("settings")][HasPermission(PermissionCatalog.Platform.OperationsView)]public Task<IReadOnlyList<OperationalSettingView>>Settings(CancellationToken ct)=>service.SettingsAsync(ct);
    [HttpPut("settings")][HasPermission(PermissionCatalog.Platform.OperationsManage)]public Task<OperationalSettingView>SaveSetting(SaveOperationalSettingRequest request,CancellationToken ct)=>service.SaveSettingAsync(request.Command(),HttpContext.TraceIdentifier,ct);
    [HttpGet("secret-references")][HasPermission(PermissionCatalog.Platform.OperationsView)]public Task<IReadOnlyList<OperationalSecretReferenceView>>SecretReferences(CancellationToken ct)=>service.SecretReferencesAsync(ct);
    [HttpPut("secret-references")][HasPermission(PermissionCatalog.Platform.OperationsManage)]public Task<OperationalSecretReferenceView>SaveSecretReference(SaveSecretReferenceRequest request,CancellationToken ct)=>service.SaveSecretReferenceAsync(request.Command(),HttpContext.TraceIdentifier,ct);
    [HttpGet("workers")][HasPermission(PermissionCatalog.Platform.OperationsView)]public Task<IReadOnlyList<WorkerControlView>>Workers(CancellationToken ct)=>service.WorkerControlsAsync(ct);
    [HttpPost("workers/{workerType}/pause")][HasPermission(PermissionCatalog.Platform.OperationsManage)]public Task<WorkerControlView>Pause(string workerType,WorkerPauseRequest request,CancellationToken ct)=>service.SetWorkerPausedAsync(workerType,true,request.Reason,HttpContext.TraceIdentifier,ct);
    [HttpPost("workers/{workerType}/resume")][HasPermission(PermissionCatalog.Platform.OperationsManage)]public Task<WorkerControlView>Resume(string workerType,CancellationToken ct)=>service.SetWorkerPausedAsync(workerType,false,null,HttpContext.TraceIdentifier,ct);
    [HttpPost("dead-letters/{queue}/{id:long}/retry")][HasPermission(PermissionCatalog.Platform.OperationsManage)]public async Task<IActionResult>Retry(string queue,long id,CancellationToken ct){await service.RetryDeadLetterAsync(queue,id,HttpContext.TraceIdentifier,ct);return NoContent();}
    [HttpGet("retention")][HasPermission(PermissionCatalog.Platform.OperationsView)]public Task<IReadOnlyList<RetentionPolicyView>>Retention(CancellationToken ct)=>service.RetentionPoliciesAsync(ct);
    [HttpPut("retention")][HasPermission(PermissionCatalog.Platform.OperationsManage)]public Task<RetentionPolicyView>SaveRetention(SaveRetentionPolicyRequest request,CancellationToken ct)=>service.SaveRetentionPolicyAsync(request.Command(),HttpContext.TraceIdentifier,ct);
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]public sealed record SaveOperationalSettingRequest(OperationalScope Scope,long?TenantId,long?StoreId,string Key,string ValueJson){public SaveOperationalSettingCommand Command()=>new(Scope,TenantId,StoreId,Key,ValueJson);}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]public sealed record SaveSecretReferenceRequest(OperationalScope Scope,long?TenantId,long?StoreId,string Key,string Reference){public SaveSecretReferenceCommand Command()=>new(Scope,TenantId,StoreId,Key,Reference);}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]public sealed record WorkerPauseRequest(string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]public sealed record SaveRetentionPolicyRequest(RetentionDomain Domain,long?TenantId,long?StoreId,int RetentionDays,bool Enabled){public SaveRetentionPolicyCommand Command()=>new(Domain,TenantId,StoreId,RetentionDays,Enabled);}

