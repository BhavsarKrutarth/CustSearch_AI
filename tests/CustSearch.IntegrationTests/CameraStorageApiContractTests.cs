using System.Reflection;
using CustSearch.API.AlertsRealtime;
using CustSearch.API.Controllers;
using CustSearch.API.Security;
using CustSearch.Application.Authorization;

namespace CustSearch.IntegrationTests;

public sealed class CameraStorageApiContractTests
{
    [Theory][InlineData(typeof(TenantCameraStorageController),nameof(TenantCameraStorageController.Summary),PermissionCatalog.Operations.StorageViewUsage)][InlineData(typeof(TenantCameraStorageController),nameof(TenantCameraStorageController.Policy),PermissionCatalog.Operations.StorageViewUsage)][InlineData(typeof(TenantCameraStorageController),nameof(TenantCameraStorageController.Usage),PermissionCatalog.Operations.StorageViewUsage)][InlineData(typeof(TenantCameraStorageController),nameof(TenantCameraStorageController.Evidence),PermissionCatalog.Operations.CamerasViewEvents)][InlineData(typeof(PlatformTenantStorageController),nameof(PlatformTenantStorageController.Get),PermissionCatalog.Platform.TenantsViewUsage)][InlineData(typeof(PlatformTenantStorageController),nameof(PlatformTenantStorageController.Save),PermissionCatalog.Platform.TenantStorageManage)]public void StorageEndpointsUseExactPermission(Type controller,string method,string permission){var attribute=controller.GetMethod(method)!.GetCustomAttribute<HasPermissionAttribute>();Assert.Equal(AuthorizationPolicyNames.ForPermission(permission),attribute?.Policy);}
    [Fact]public void TenantStorageRejectsBrowserTenantIdAndInternalUploadIsBounded(){Assert.NotNull(typeof(TenantCameraStorageController).GetCustomAttribute<RejectClientTenantIdAttribute>());var method=typeof(CctvEvidenceController).GetMethod(nameof(CctvEvidenceController.Upload))!;var limit=method.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute>();Assert.NotNull(limit);Assert.Equal(CctvEvidenceController.HardMaximumBodyBytes,((Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata)limit!).MaxRequestBodySize);}
    [Fact]public void EvidenceRetentionOptionsRejectUnsafeWorkerBounds(){var options=new CustSearch.Application.CamerasTracking.EvidenceRetentionOptions();Assert.True(options.IsValid());options.LeaseSeconds=10;Assert.False(options.IsValid());options.LeaseSeconds=600;options.CleanupBatchSize=5001;Assert.False(options.IsValid());}
}
