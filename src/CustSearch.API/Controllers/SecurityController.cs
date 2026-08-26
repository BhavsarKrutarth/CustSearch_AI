using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CustSearch.API.AlertsRealtime;
using CustSearch.API.Security;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.Security;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.API.Controllers;
[ApiController][Route("api/tenant/security")][Authorize(Policy=AuthorizationPolicyNames.TenantScope)][RejectClientTenantId][ServiceFilter(typeof(SecurityExceptionFilter))]
public sealed class SecurityController(ISecurityPlatformService service,ICurrentUserContext currentUser):ControllerBase
{
    [HttpGet("incidents")][HasPermission(PermissionCatalog.Security.IncidentsView)]public Task<IReadOnlyList<SecurityIncidentSummary>>List([FromQuery]long?storeId,[FromQuery]SecurityIncidentStatus?status,[FromQuery]int take=100,CancellationToken ct=default)=>service.ListIncidentsAsync(storeId,status,take,ct);
    [HttpGet("incidents/{id:long}")][HasPermission(PermissionCatalog.Security.IncidentsView)]public Task<SecurityIncidentDetail>Detail(long id,CancellationToken ct)=>service.GetIncidentAsync(id,ct);
    [HttpGet("incidents/{id:long}/timeline")][HasPermission(PermissionCatalog.Security.IncidentsView)]public Task<IReadOnlyList<SecurityActionView>>Timeline(long id,CancellationToken ct)=>service.TimelineAsync(id,ct);
    [HttpPost("incidents/{id:long}/acknowledge")][HasPermission(PermissionCatalog.Security.IncidentsAcknowledge)]public Task<SecurityIncidentDetail>Acknowledge(long id,SecurityTransitionRequest request,CancellationToken ct)=>service.TransitionAsync(id,SecurityIncidentStatus.Acknowledged,request.Reason,request.Notes,Audit(),ct);
    [HttpPost("incidents/{id:long}/assign")][HasPermission(PermissionCatalog.Security.IncidentsAssign)]public Task<SecurityIncidentDetail>Assign(long id,SecurityAssignRequest request,CancellationToken ct)=>service.AssignAsync(id,request.UserId,Audit(),ct);
    [HttpPost("incidents/{id:long}/review")][HasPermission(PermissionCatalog.Security.IncidentsReview)]public Task<SecurityIncidentDetail>Review(long id,SecurityTransitionRequest request,CancellationToken ct)=>service.TransitionAsync(id,SecurityIncidentStatus.UnderReview,request.Reason,request.Notes,Audit(),ct);
    [HttpPost("incidents/{id:long}/confirm-loss")][HasPermission(PermissionCatalog.Security.IncidentsConfirmLoss)]public Task<SecurityIncidentDetail>ConfirmLoss(long id,SecurityTransitionRequest request,CancellationToken ct)=>service.TransitionAsync(id,SecurityIncidentStatus.ConfirmedLoss,request.Reason,request.Notes,Audit(),ct);
    [HttpPost("incidents/{id:long}/false-positive")][HasPermission(PermissionCatalog.Security.IncidentsReview)]public Task<SecurityIncidentDetail>FalsePositive(long id,SecurityTransitionRequest request,CancellationToken ct)=>service.TransitionAsync(id,SecurityIncidentStatus.FalsePositive,request.Reason,request.Notes,Audit(),ct);
    [HttpPost("incidents/{id:long}/resolve")][HasPermission(PermissionCatalog.Security.IncidentsResolve)]public Task<SecurityIncidentDetail>Resolve(long id,SecurityTransitionRequest request,CancellationToken ct)=>service.TransitionAsync(id,SecurityIncidentStatus.Resolved,request.Reason,request.Notes,Audit(),ct);
    [HttpGet("incidents/{id:long}/evidence")][HasPermission(PermissionCatalog.Security.EvidenceView)]public Task<IReadOnlyList<SecurityEvidenceView>>Evidence(long id,CancellationToken ct)=>service.EvidenceAsync(id,Audit(),ct);
    [HttpPost("incidents/{id:long}/evidence/{evidenceId:long}/view-ticket")][HasPermission(PermissionCatalog.Security.EvidenceView)]public Task<SecurityEvidenceTicket>ViewTicket(long id,long evidenceId,CancellationToken ct)=>service.EvidenceTicketAsync(id,evidenceId,false,Audit(),ct);
    [HttpPost("incidents/{id:long}/evidence/{evidenceId:long}/export-ticket")][HasPermission(PermissionCatalog.Security.EvidenceExport)]public Task<SecurityEvidenceTicket>ExportTicket(long id,long evidenceId,CancellationToken ct)=>service.EvidenceTicketAsync(id,evidenceId,true,Audit(),ct);
    [HttpGet("incidents/{id:long}/evidence/{evidenceId:long}/view")][HasPermission(PermissionCatalog.Security.EvidenceView)]public async Task<IActionResult>ViewEvidence(long id,long evidenceId,[FromQuery]string token,CancellationToken ct){var file=await service.OpenEvidenceAsync(id,evidenceId,token,false,Audit(),ct).ConfigureAwait(false);return File(file.Content,file.ContentType,file.FileName,enableRangeProcessing:false);}
    [HttpGet("incidents/{id:long}/evidence/{evidenceId:long}/export")][HasPermission(PermissionCatalog.Security.EvidenceExport)]public async Task<IActionResult>ExportEvidence(long id,long evidenceId,[FromQuery]string token,CancellationToken ct){var file=await service.OpenEvidenceAsync(id,evidenceId,token,true,Audit(),ct).ConfigureAwait(false);return File(file.Content,file.ContentType,file.FileName,enableRangeProcessing:false);}
    [HttpGet("settings")][HasPermission(PermissionCatalog.Security.SettingsView)]public Task<SecuritySettingsView>Settings([FromQuery]long?storeId,CancellationToken ct)=>service.GetSettingsAsync(storeId,ct);
    [HttpPut("settings")][HasPermission(PermissionCatalog.Security.SettingsManage)]public Task<SecuritySettingsView>SaveSettings([FromQuery]long?storeId,SaveSecuritySettingsCommand request,CancellationToken ct)=>service.SaveSettingsAsync(storeId,request,Audit(),ct);
    [HttpGet("rules")][HasPermission(PermissionCatalog.Security.RulesView)]public Task<IReadOnlyList<SecurityRuleView>>Rules([FromQuery]long?storeId,CancellationToken ct)=>service.ListRulesAsync(storeId,ct);
    [HttpPost("rules")][HasPermission(PermissionCatalog.Security.RulesManage)]public Task<SecurityRuleView>SaveRule([FromQuery]long?storeId,SaveSecurityRuleCommand request,CancellationToken ct)=>service.SaveRuleAsync(storeId,request,Audit(),ct);
    [HttpGet("reports")][HasPermission(PermissionCatalog.Security.ReportsView)]public Task<SecurityReportView>Report([FromQuery]long?storeId,[FromQuery]DateTime fromUtc,[FromQuery]DateTime toUtc,CancellationToken ct)=>service.ReportAsync(storeId,fromUtc,toUtc,ct);
    private TenantAuditContext Audit()=>new(currentUser.UserId,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),HttpContext.TraceIdentifier);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]public sealed record SecurityTransitionRequest([param:StringLength(100)]string?Reason,[param:StringLength(2000)]string?Notes);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]public sealed record SecurityAssignRequest([param:Range(1,long.MaxValue)]long UserId);
