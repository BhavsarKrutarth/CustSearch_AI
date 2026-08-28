using System.Reflection;
using System.Text.Json;
using CustSearch.API.AlertsRealtime;
using CustSearch.API.Controllers;
using CustSearch.API.Security;
using CustSearch.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CustSearch.IntegrationTests;

public sealed class PhaseThirteenApiContractTests
{
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web);
    [Fact]public void BrowserTenantIdInjectionIsRejected(){var injected="""{"storeId":1,"cameraCode":"ENTRY","name":"Entry","rtspConfigurationReference":"vault:entry","direction":1,"isActive":true,"tenantId":999}""";Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SaveCameraRequest>(injected,JsonOptions));Assert.NotNull(typeof(CamerasTrackingController).GetCustomAttribute<RejectClientTenantIdAttribute>());}
    [Theory][InlineData(nameof(CamerasTrackingController.List),PermissionCatalog.Operations.CamerasView)][InlineData(nameof(CamerasTrackingController.Quota),PermissionCatalog.Operations.CamerasView)][InlineData(nameof(CamerasTrackingController.Create),PermissionCatalog.Operations.CamerasManage)][InlineData(nameof(CamerasTrackingController.Update),PermissionCatalog.Operations.CamerasManage)][InlineData(nameof(CamerasTrackingController.Zones),PermissionCatalog.Operations.CamerasView)][InlineData(nameof(CamerasTrackingController.AddZoneVersion),PermissionCatalog.Operations.CamerasManageZones)][InlineData(nameof(CamerasTrackingController.Tracks),PermissionCatalog.Operations.CamerasView)][InlineData(nameof(CamerasTrackingController.Associate),PermissionCatalog.Operations.CamerasManage)][InlineData(nameof(CamerasTrackingController.MotionRuleCatalog),PermissionCatalog.Operations.CamerasView)][InlineData(nameof(CamerasTrackingController.MotionSettings),PermissionCatalog.Operations.CamerasView)][InlineData(nameof(CamerasTrackingController.SaveMotionSettings),PermissionCatalog.Operations.CamerasManageRules)][InlineData(nameof(CamerasTrackingController.SaveDetectionZoneSetting),PermissionCatalog.Operations.CamerasManageZones)][InlineData(nameof(CamerasTrackingController.MotionRules),PermissionCatalog.Operations.CamerasView)][InlineData(nameof(CamerasTrackingController.CreateMotionRule),PermissionCatalog.Operations.CamerasManageRules)][InlineData(nameof(CamerasTrackingController.UpdateMotionRule),PermissionCatalog.Operations.CamerasManageRules)]public void TenantEndpointsRequireExactPermission(string method,string permission){var attribute=typeof(CamerasTrackingController).GetMethod(method)!.GetCustomAttribute<HasPermissionAttribute>();Assert.Equal(AuthorizationPolicyNames.ForPermission(permission),attribute?.Policy);}
    [Fact]public void PythonBoundaryHasHmacSizeAndRateLimitContract(){var controller=typeof(CctvEventsController);Assert.NotNull(controller.GetCustomAttribute<AllowAnonymousAttribute>());Assert.Equal("cctv-inbound",controller.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);var method=controller.GetMethod(nameof(CctvEventsController.Receive))!;var limit=method.GetCustomAttribute<RequestSizeLimitAttribute>();Assert.NotNull(limit);Assert.Equal((long)CctvEventsController.MaximumBodyBytes,((Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata)limit).MaxRequestBodySize);}
    [Fact]public void CameraResponseDoesNotExposeFullRtspReference(){var names=typeof(CustSearch.Application.CamerasTracking.CameraView).GetProperties().Select(x=>x.Name).ToArray();Assert.DoesNotContain("RtspConfigurationReference",names);Assert.Contains("RtspConfigurationHint",names);}
}
