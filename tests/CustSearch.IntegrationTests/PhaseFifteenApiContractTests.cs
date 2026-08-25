using System.Reflection;
using System.Text.Json;
using CustSearch.API.AlertsRealtime;
using CustSearch.API.Controllers;
using CustSearch.Application.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.IntegrationTests;

public sealed class PhaseFifteenApiContractTests
{
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web);
    [Fact]public void BrowserTenantIdAndUnknownFilterFieldsAreRejected(){var raw="""{"reportType":1,"format":1,"filter":{"fromUtc":"2026-08-01T00:00:00Z","toUtc":"2026-08-25T00:00:00Z","storeIds":[7],"page":1,"pageSize":100},"tenantId":99}""";Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<QueueExportRequest>(raw,JsonOptions));Assert.NotNull(typeof(ReportsExportsController).GetCustomAttribute<RejectClientTenantIdAttribute>());}
    [Fact]public void TenantAndPlatformControllersUseSeparatePolicies(){var tenant=typeof(ReportsExportsController).GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();var platform=typeof(PlatformReportsExportsController).GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();Assert.Equal(AuthorizationPolicyNames.TenantScope,tenant?.Policy);Assert.Equal(AuthorizationPolicyNames.PlatformScope,platform?.Policy);}
    [Theory][InlineData(nameof(ReportsExportsController.Run),PermissionCatalog.Operations.ReportsView)][InlineData(nameof(ReportsExportsController.Queue),PermissionCatalog.Operations.ReportsExport)][InlineData(nameof(ReportsExportsController.Download),PermissionCatalog.Operations.ReportsExport)]public void TenantEndpointsRequireExactPermissions(string method,string permission){var attribute=typeof(ReportsExportsController).GetMethod(method)!.GetCustomAttribute<HasPermissionAttribute>();Assert.Equal(AuthorizationPolicyNames.ForPermission(permission),attribute?.Policy);}
    [Fact]public void DownloadEndpointNeverAcceptsAFilePath(){var names=typeof(ReportsExportsController).GetMethod(nameof(ReportsExportsController.Download))!.GetParameters().Select(x=>x.Name).ToArray();Assert.DoesNotContain("path",names);Assert.Contains("token",names);}
}
