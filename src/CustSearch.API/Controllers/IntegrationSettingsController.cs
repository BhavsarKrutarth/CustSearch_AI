using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using CustSearch.API.AlertsRealtime;
using CustSearch.API.Integrations;
using CustSearch.API.Security;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.Integrations;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.API.Controllers;

[ApiController]
[Route("api/tenant/integrations")]
[Authorize(Policy=AuthorizationPolicyNames.TenantScope)]
[RejectClientTenantId]
[ServiceFilter(typeof(IntegrationExceptionFilter))]
public sealed class IntegrationSettingsController(IIntegrationManagementService service,ICurrentUserContext currentUser):ControllerBase
{
    [HttpGet][HasPermission(PermissionCatalog.Operations.IntegrationsView)]public Task<IReadOnlyList<IntegrationConfigurationView>>List(CancellationToken ct)=>service.ListAsync(ct);
    [HttpGet("{integrationId:long}")][HasPermission(PermissionCatalog.Operations.IntegrationsView)]public Task<IntegrationConfigurationView>Get(long integrationId,CancellationToken ct)=>service.GetAsync(integrationId,ct);
    [HttpPost][HasPermission(PermissionCatalog.Operations.IntegrationsManage)]public Task<IntegrationConfigurationView>Create(SaveIntegrationConfigurationRequest request,CancellationToken ct)=>service.SaveAsync(null,request.Command(),Audit(),ct);
    [HttpPut("{integrationId:long}")][HasPermission(PermissionCatalog.Operations.IntegrationsManage)]public Task<IntegrationConfigurationView>Update(long integrationId,SaveIntegrationConfigurationRequest request,CancellationToken ct)=>service.SaveAsync(integrationId,request.Command(),Audit(),ct);
    [HttpPost("{integrationId:long}/rotate-references")][HasPermission(PermissionCatalog.Operations.IntegrationsManage)]public Task<IntegrationConfigurationView>Rotate(long integrationId,RotateIntegrationReferencesRequest request,CancellationToken ct)=>service.RotateReferencesAsync(integrationId,new(request.CredentialReference,request.WebhookSigningSecretReference,request.SigningGraceMinutes),Audit(),ct);
    [HttpGet("deliveries")][HasPermission(PermissionCatalog.Operations.WebhooksView)]public Task<IReadOnlyList<IntegrationDeliveryLogView>>History([FromQuery]long?integrationId=null,[FromQuery]int take=100,CancellationToken ct=default)=>service.DeliveryHistoryAsync(integrationId,take,ct);
    [HttpPost("deliveries/{deliveryId:long}/retry")][HasPermission(PermissionCatalog.Operations.WebhooksManage)]public Task<IntegrationOutboxView>Retry(long deliveryId,CancellationToken ct)=>service.RetryDeliveryAsync(deliveryId,Audit(),ct);
    [HttpPost("{integrationId:long}/test-delivery")][HasPermission(PermissionCatalog.Operations.WebhooksManage)]public Task<IntegrationOutboxView>TestDelivery(long integrationId,CancellationToken ct)=>service.QueueOutboundAsync(new(integrationId,"integration.test",1,JsonSerializer.Serialize(new{kind="connection-test",requestedUtc=DateTime.UtcNow}),$"test:{integrationId}:{Guid.NewGuid():N}",HttpContext.TraceIdentifier),ct);
    private TenantAuditContext Audit()=>new(currentUser.UserId,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),HttpContext.TraceIdentifier);
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SaveIntegrationConfigurationRequest([param:Required,StringLength(100)]string Provider,IntegrationType IntegrationType,bool Enabled,[param:Required,StringLength(500)]string EndpointBaseUrl,[param:StringLength(200)]string?CredentialReference,[param:StringLength(200)]string?WebhookSigningSecretReference,[param:Range(1,120)]int TimeoutSeconds,[param:Range(1,10)]int RetryMaxAttempts,[param:Range(1,300)]int RetryBaseDelaySeconds){public SaveIntegrationConfigurationCommand Command()=>new(Provider,IntegrationType,Enabled,EndpointBaseUrl,CredentialReference,WebhookSigningSecretReference,TimeoutSeconds,RetryMaxAttempts,RetryBaseDelaySeconds);}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RotateIntegrationReferencesRequest([param:StringLength(200)]string?CredentialReference,[param:StringLength(200)]string?WebhookSigningSecretReference,[param:Range(0,1440)]int SigningGraceMinutes);
