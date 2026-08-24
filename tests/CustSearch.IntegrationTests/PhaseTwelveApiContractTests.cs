using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using CustSearch.API.AlertsRealtime;
using CustSearch.API.Controllers;
using CustSearch.API.Security;
using CustSearch.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CustSearch.IntegrationTests;

public sealed class PhaseTwelveApiContractTests
{
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web);
    [Fact]public void SettingsDtosRejectTenantInjectionAndNeverReturnSecretReferenceValues(){foreach(var type in new[]{typeof(SaveIntegrationConfigurationRequest),typeof(RotateIntegrationReferencesRequest)}){Assert.DoesNotContain(type.GetProperties(),x=>string.Equals(x.Name,"TenantId",StringComparison.OrdinalIgnoreCase));Assert.Equal(JsonUnmappedMemberHandling.Disallow,type.GetCustomAttribute<JsonUnmappedMemberHandlingAttribute>()?.UnmappedMemberHandling);}const string injected="""{"provider":"POS","integrationType":3,"enabled":true,"endpointBaseUrl":"https://pos.example.test","timeoutSeconds":10,"retryMaxAttempts":5,"retryBaseDelaySeconds":5,"tenantId":999}""";Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SaveIntegrationConfigurationRequest>(injected,JsonOptions));Assert.NotNull(typeof(IntegrationSettingsController).GetCustomAttribute<RejectClientTenantIdAttribute>());}
    [Theory][InlineData(nameof(IntegrationSettingsController.List),PermissionCatalog.Operations.IntegrationsView)][InlineData(nameof(IntegrationSettingsController.Get),PermissionCatalog.Operations.IntegrationsView)][InlineData(nameof(IntegrationSettingsController.Create),PermissionCatalog.Operations.IntegrationsManage)][InlineData(nameof(IntegrationSettingsController.Update),PermissionCatalog.Operations.IntegrationsManage)][InlineData(nameof(IntegrationSettingsController.Rotate),PermissionCatalog.Operations.IntegrationsManage)][InlineData(nameof(IntegrationSettingsController.History),PermissionCatalog.Operations.WebhooksView)][InlineData(nameof(IntegrationSettingsController.Retry),PermissionCatalog.Operations.WebhooksManage)][InlineData(nameof(IntegrationSettingsController.TestDelivery),PermissionCatalog.Operations.WebhooksManage)]public void SettingsEndpointsRequireExactPermission(string method,string permission){var attribute=typeof(IntegrationSettingsController).GetMethod(method)!.GetCustomAttribute<HasPermissionAttribute>();Assert.Equal(AuthorizationPolicyNames.ForPermission(permission),attribute?.Policy);}
    [Fact]public void InboundEndpointHasHmacBoundarySizeLimitAndPartitionedRateLimit(){var controller=typeof(InboundIntegrationsController);Assert.NotNull(controller.GetCustomAttribute<AllowAnonymousAttribute>());Assert.Equal("integration-inbound",controller.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);var method=controller.GetMethod(nameof(InboundIntegrationsController.Receive))!;Assert.Equal((long)InboundIntegrationsController.MaximumBodyBytes,method.GetCustomAttribute<RequestSizeLimitAttribute>()?.Bytes);Assert.Equal("{integrationId:long}/events",method.GetCustomAttribute<HttpPostAttribute>()?.Template);}
    [Fact]public void DeliveryAuditEntityContainsNoPayloadOrSecretFields(){var names=typeof(CustSearch.Domain.Entities.IntegrationDeliveryLog).GetProperties().Select(x=>x.Name).ToArray();Assert.DoesNotContain(names,x=>x.Contains("Payload",StringComparison.OrdinalIgnoreCase)||x.Contains("Secret",StringComparison.OrdinalIgnoreCase)||x.Contains("Credential",StringComparison.OrdinalIgnoreCase)||x.Contains("Body",StringComparison.OrdinalIgnoreCase));}
}
