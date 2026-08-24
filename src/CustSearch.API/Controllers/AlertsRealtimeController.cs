using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CustSearch.API.AlertsRealtime;
using CustSearch.API.Security;
using CustSearch.Application.AlertsRealtime;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.API.Controllers;

/// <summary>Tenant/store alert REST authority used for notification state and missed-event recovery.</summary>
[ApiController]
[Route("api/tenant/alerts")]
[Authorize(Policy=AuthorizationPolicyNames.TenantScope)]
[RejectClientTenantId]
[ServiceFilter(typeof(AlertExceptionFilter))]
public sealed class AlertsRealtimeController(IAlertsRealtimeService service,ICurrentUserContext currentUser):ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCatalog.Operations.AlertsView)]
    public Task<AlertListView>List([FromQuery]long?storeId=null,[FromQuery]AlertStatus?status=null,[FromQuery]int take=100,CancellationToken ct=default)=>service.ListAsync(storeId,status,take,ct);

    [HttpGet("recovery")]
    [HasPermission(PermissionCatalog.Operations.AlertsView)]
    public Task<AlertRecoveryView>Recover([FromQuery]long afterEventId=0,[FromQuery]int take=200,CancellationToken ct=default)=>service.RecoverAsync(afterEventId,take,ct);

    [HttpGet("metrics")]
    [HasPermission(PermissionCatalog.Operations.AlertsConfigure)]
    public Task<AlertHealthMetricsView>Metrics(CancellationToken ct)=>service.GetMetricsAsync(ct);

    [HttpGet("{alertId:long}")]
    [HasPermission(PermissionCatalog.Operations.AlertsView)]
    public Task<AlertView>Get(long alertId,CancellationToken ct)=>service.GetAsync(alertId,ct);

    [HttpPost]
    [HasPermission(PermissionCatalog.Operations.AlertsConfigure)]
    public Task<AlertView>Create(CreateAlertRequest request,CancellationToken ct)=>service.CreateAsync(new(request.AlertType,request.StoreId,request.Severity,request.Title,request.Message,request.EntityType,request.EntityId,request.DeduplicationKey),Audit(),ct);

    [HttpPut("{alertId:long}")]
    [HasPermission(PermissionCatalog.Operations.AlertsConfigure)]
    public Task<AlertView>Update(long alertId,UpdateAlertRequest request,CancellationToken ct)=>service.UpdateAsync(alertId,new(request.Severity,request.Title,request.Message,request.EntityType,request.EntityId),Audit(),ct);

    [HttpPost("{alertId:long}/acknowledge")]
    [HasPermission(PermissionCatalog.Operations.AlertsAcknowledge)]
    public Task<AlertView>Acknowledge(long alertId,AcknowledgeAlertRequest request,CancellationToken ct)=>service.AcknowledgeAsync(alertId,Audit(),ct);

    [HttpPost("{alertId:long}/resolve")]
    [HasPermission(PermissionCatalog.Operations.AlertsConfigure)]
    public Task<AlertView>Resolve(long alertId,ResolveAlertRequest request,CancellationToken ct)=>service.ResolveAsync(alertId,Audit(),ct);

    private TenantAuditContext Audit()=>new(currentUser.UserId,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),HttpContext.TraceIdentifier);
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateAlertRequest([param:Required,StringLength(100)]string AlertType,long?StoreId,AlertSeverity Severity,[param:Required,StringLength(200)]string Title,[param:Required,StringLength(2000)]string Message,[param:Required,StringLength(100)]string EntityType,[param:StringLength(100)]string?EntityId,[param:Required,StringLength(200)]string DeduplicationKey);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateAlertRequest(AlertSeverity Severity,[param:Required,StringLength(200)]string Title,[param:Required,StringLength(2000)]string Message,[param:Required,StringLength(100)]string EntityType,[param:StringLength(100)]string?EntityId);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AcknowledgeAlertRequest;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ResolveAlertRequest;
